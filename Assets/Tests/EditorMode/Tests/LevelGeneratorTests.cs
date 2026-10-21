using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class LevelGeneratorTests
{
    private const string ConfigPath = "Assets/Data/generator_config.json";

    private static GeneratorConfig LoadConfig()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigPath);
        if (asset == null) Assert.Ignore($"Chưa có {ConfigPath}");

        Assert.IsTrue(GeneratorConfig.TryParse(asset.text, out GeneratorConfig config, out string error), error);
        return config;
    }

    private static readonly int[] SampleLevels = { 1, 5, 12, 21, 25, 30, 36, 51, 55, 60 };

    [Test]
    public void GeneratedLevels_AreValidAndSolvable()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 7 };

        foreach (int number in SampleLevels)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out GenerationInfo info);
            Assert.IsNotNull(level, $"Level {number} sinh thất bại");
            Assert.IsTrue(level.Validate(out string error), $"Level {number}: {error}");   // Gồm cả kiểm tra solution

            LevelSolver.Result solved = LevelSolver.Solve(level, 2, 200000);
            Assert.IsTrue(solved.IsSolvable, $"Level {number} không giải được");
            Assert.IsTrue(solved.LockScheduleFeasible, $"Level {number}: clue khoá không mở được");
        }
    }

    [Test]
    public void SameSeedAndLevel_ProduceIdenticalJson()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 99 };

        LevelData a = LevelGenerator.Generate(config, 40, options, out _);
        LevelData b = LevelGenerator.Generate(config, 40, options, out _);

        Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b));
    }

    [Test]
    public void HardTier_UsesHolesAndRespectsUniqueness()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 3 };
        int levelsWithHoles = 0;

        for (int number = 51; number <= 60; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out GenerationInfo info);
            Assert.IsNotNull(level);
            Assert.IsTrue(info.Unique || info.Warning != null, $"Level {number}: không duy nhất mà cũng không có cảnh báo");
            if (level.holes.Length > 0) levelsWithHoles++;
        }

        Assert.Greater(levelsWithHoles, 0, "10 level Hard mà không có level nào có ô tắt (xác suất gần như bằng 0)");
    }

    [Test]
    public void LockedThresholds_AreAlwaysFeasible()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 11 };
        var withLocked = new List<int>();

        for (int number = 51; number <= 70; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out GenerationInfo info);
            Assert.IsNotNull(level);
            Assert.IsTrue(LevelSolver.IsLockScheduleFeasible(level), $"Level {number}");
            if (info.Locked > 0) withLocked.Add(number);
        }

        Assert.Greater(withLocked.Count, 0, "20 level Hard mà không có level nào có clue khoá");
    }
    
    [Test]
    public void LockedThresholds_RespectTierCap()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 11 };

        for (int number = 51; number <= 70; number++)
        {
            GeneratorConfig.Tier tier = config.GetTier(number);
            LevelData level = LevelGenerator.Generate(config, number, options, out _);
            Assert.IsNotNull(level);

            foreach (LevelData.ClueEntry clue in level.clues)
            {
                if (clue.type != ClueType.Locked) continue;
                Assert.LessOrEqual(clue.unlockRoomCount, tier.lockedMaxUnlock, $"Level {number}");
            }
        }
    }
    
    [Test]
    public void Mechanics_AppearOnlyFromUnlockLevels()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 7 };
        GeneratorConfig.MechanicUnlocks m = config.mechanics;

        for (int number = 1; number < m.holesFromLevel; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out _);
            Assert.IsNotNull(level, $"Level {number}");
            Assert.AreEqual(0, level.holes.Length, $"Level {number}: không được có ô tắt");
        }

        int firstSpecial = Mathf.Min(m.hiddenFromLevel, Mathf.Min(m.directionalFromLevel, m.lockedFromLevel));
        for (int number = 1; number < firstSpecial; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out _);
            foreach (LevelData.ClueEntry clue in level.clues)
                Assert.AreEqual(ClueType.Normal, clue.type, $"Level {number}: chưa mở clue đặc biệt");
        }
    }

    [Test]
    public void IntroductionLevels_ForceTheNewMechanic()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 7 };
        GeneratorConfig.MechanicUnlocks m = config.mechanics;

        Assert.Greater(LevelGenerator.Generate(config, m.holesFromLevel, options, out _).holes.Length, 0,
            $"Level {m.holesFromLevel} phải có ô tắt");
        Assert.IsTrue(HasType(LevelGenerator.Generate(config, m.hiddenFromLevel, options, out _), ClueType.Hidden),
            $"Level {m.hiddenFromLevel} phải có clue ẩn");

        LevelData shaped = LevelGenerator.Generate(config, m.directionalFromLevel, options, out _);
        Assert.IsTrue(HasType(shaped, ClueType.Horizontal) || HasType(shaped, ClueType.Vertical) || HasType(shaped, ClueType.Square),
            $"Level {m.directionalFromLevel} phải có clue hình dạng");

        Assert.IsTrue(HasType(LevelGenerator.Generate(config, m.lockedFromLevel, options, out _), ClueType.Locked),
            $"Level {m.lockedFromLevel} phải có clue khoá");
    }

    [Test]
    public void GridSize_FollowsCurve()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 7 };

        for (int number = 1; number <= 20; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out GenerationInfo info);
            Assert.IsNotNull(level, $"Level {number}");

            config.GetGridRange(number, out int min, out int max);
            Assert.GreaterOrEqual(info.Grid, min, $"Level {number}: grid quá nhỏ");
            Assert.LessOrEqual(info.Grid, max, $"Level {number}: grid quá lớn");
        }
    }
    
    [Test]
    public void ComboClues_AppearOnlyFromUnlockLevelAndStayValid()
    {
        GeneratorConfig config = LoadConfig();
        var options = new GenerationOptions { Seed = 7 };
        int unlock = config.mechanics.comboFromLevel;

        for (int number = 1; number < unlock; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out _);
            foreach (LevelData.ClueEntry clue in level.clues)
                Assert.AreEqual(ClueShape.None, clue.shape, $"Level {number}: chưa mở clue kép");
        }

        LevelData intro = LevelGenerator.Generate(config, unlock, options, out _);
        Assert.IsTrue(HasCombo(intro), $"Level {unlock} phải có clue kép");

        for (int number = unlock; number < unlock + 20; number++)
        {
            LevelData level = LevelGenerator.Generate(config, number, options, out _);
            Assert.IsNotNull(level, $"Level {number}");
            Assert.IsTrue(level.Validate(out string error), $"Level {number}: {error}");
        }
    }

    private static bool HasCombo(LevelData level)
    {
        foreach (LevelData.ClueEntry clue in level.clues)
            if ((clue.type == ClueType.Hidden || clue.type == ClueType.Locked) && clue.shape != ClueShape.None)
                return true;
        return false;
    }

    private static bool HasType(LevelData level, ClueType type)
    {
        foreach (LevelData.ClueEntry clue in level.clues)
            if (clue.type == type) return true;
        return false;
    }
}