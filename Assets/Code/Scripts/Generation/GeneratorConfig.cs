using System;
using UnityEngine;

public enum LimitMode
{
    Timer = 0,
    Moves = 1,
    Both = 2
}

[Serializable]
public class GeneratorConfig
{
    [Serializable]
    public class ShapeWeights
    {
        public float small;
        public float medium;
        public float large;
    }

    [Serializable]
    public class Shape
    {
        public int minSide;
        public int maxSide;
        public int area;
        public ShapeWeights weights;
    }

    [Serializable]
    public class SizeBand
    {
        public string name; 
        public int minGrid; 
        public int maxGrid;
    }
    [Serializable] public class RoomsPerGrid 
    { 
        public int grid; public int min; 
        public float avg; 
        public int max; 
    }

    [Serializable]
    public class CornerBias
    {
        public float line;
        public float two; 
        public float big;
    }

    [Serializable]
    public class Tier
    {
        public string name;
        public int fromLevel;
        public int minGrid;
        public int maxGrid;
        public int rampLevels;
        public float holeChance;
        public int holeMinGroups = 1;
        public int holeMaxGroups = 1;
        public float directionalChance;
        public int hiddenMax;
        public float hiddenFraction;
        public int lockedMax;
        public int lockedMaxUnlock = 3;
        public bool requireUnique;
        public int repairAttempts;
        public int moveSlack;
        public float timeBase;
        public float timePerRoom;
    }

    [Serializable]
    public class LimitBlock
    {
        public int fromLevel;
        public int toLevel;
        public LimitMode mode;
        public int insertRuns;
        public int insertMin;
        public int insertMax;
    }

    [Serializable]
    public class LimitSchedule
    {
        public int bothFromLevel = 30;
        public int bothEvery = 10;
        public int bothMoveExtra = 1;
        public float bothTimeFactor = 1.25f;
        public LimitMode fallbackMode = LimitMode.Timer;
        public LimitBlock[] blocks = new LimitBlock[0];
    }

    [Serializable]
    public class HoleSettings
    {
        public int maxRooms = 3;
        public float maxAreaFraction = 0.25f;
        public int minRoomsLeft = 3;
        public int clusterSize = 2;
    }
    
    [Serializable]
    public class MechanicUnlocks
    {
        public int holesFromLevel = 21;
        public int hiddenFromLevel = 26;
        public int directionalFromLevel = 31;
        public int lockedFromLevel = 36;
        public bool introduceAtUnlock = true;
    }
    
    [Serializable]
    public class GridAnchor
    {
        public int level;
        public int minGrid;
        public int maxGrid;
    }

    public int version;
    public int minRoomArea = 2;
    public float verticalBias = 0.62f;
    public SizeBand[] sizeBands = new SizeBand[0];
    public Shape[] shapes = new Shape[0];
    public RoomsPerGrid[] roomsPerGrid = new RoomsPerGrid[0];
    public CornerBias clueCornerBias = new CornerBias { line = 0.7f, two = 0.7f, big = 0.6f };
    public Tier[] tiers = new Tier[0];
    public LimitSchedule limitSchedule = new LimitSchedule();
    public HoleSettings holes = new HoleSettings();
    public MechanicUnlocks mechanics = new MechanicUnlocks();
    public GridAnchor[] gridCurve = new GridAnchor[0];

    public static bool TryParse(string json, out GeneratorConfig config, out string error)
    {
        try
        {
            config = JsonUtility.FromJson<GeneratorConfig>(json);
        }
        catch (ArgumentException e)
        {
            config = null;
            error = e.Message;
            return false;
        }

        if (config == null)
        {
            error = "Empty file";
            return false;
        }
        
        return config.Validate(out error);
    }
    
    public bool Validate(out string error)
    {
        if (shapes.Length == 0) { error = "Thiếu danh sách shapes"; return false; }
        if (sizeBands.Length == 0) { error = "Thiếu sizeBands"; return false; }
        if (roomsPerGrid.Length == 0) { error = "Thiếu roomsPerGrid"; return false; }
        if (tiers.Length == 0) { error = "Thiếu tiers"; return false; }

        foreach (Shape shape in shapes)
        {
            if (shape.weights == null) { error = $"Shape {shape.minSide}x{shape.maxSide} thiếu weights"; return false; }
            if (shape.minSide < 1 || shape.maxSide < shape.minSide || shape.area != shape.minSide * shape.maxSide)
            {
                error = $"Shape {shape.minSide}x{shape.maxSide} có kích thước hoặc area không hợp lệ";
                return false;
            }
        }

        for (int i = 0; i < tiers.Length; i++)
        {
            Tier tier = tiers[i];
            if (tier.minGrid < 3 || tier.maxGrid < tier.minGrid) { error = $"Tier {tier.name}: minGrid/maxGrid không hợp lệ"; return false; }
            if (tier.rampLevels < 1) { error = $"Tier {tier.name}: rampLevels phải >= 1"; return false; }
            if (tier.repairAttempts < 0 || tier.moveSlack < 1) { error = $"Tier {tier.name}: repairAttempts >= 0 và moveSlack >= 1"; return false; }
            if (i > 0 && tier.fromLevel <= tiers[i - 1].fromLevel) { error = "tiers phải sắp tăng dần theo fromLevel"; return false; }
            if (tier.lockedMax > 0 && tier.lockedMaxUnlock < 1) { error = $"Tier {tier.name}: lockedMaxUnlock phải >= 1"; return false; }
        }

        foreach (LimitBlock block in limitSchedule.blocks)
        {
            if (block.toLevel < block.fromLevel) { error = $"Khối {block.fromLevel}-{block.toLevel} không hợp lệ"; return false; }
            if (block.insertRuns > 0 && block.insertMax < block.insertMin) { error = $"Khối {block.fromLevel}-{block.toLevel}: insertMax < insertMin"; return false; }
        }
        
        if (mechanics.holesFromLevel < 1 || mechanics.hiddenFromLevel < 1
                                         || mechanics.directionalFromLevel < 1 
                                         || mechanics.lockedFromLevel < 1)
        {
            error = "mechanics: các mốc FromLevel phải >= 1";
            return false;
        }
        
        for (int i = 0; i < gridCurve.Length; i++)
        {
            GridAnchor a = gridCurve[i];
            if (a.minGrid < 3 || a.maxGrid < a.minGrid) { error = $"gridCurve[{i}]: minGrid/maxGrid không hợp lệ"; return false; }
            if (i > 0 && a.level <= gridCurve[i - 1].level) { error = "gridCurve phải sắp tăng dần theo level"; return false; }
            if (i > 0 && (a.minGrid < gridCurve[i - 1].minGrid || a.maxGrid < gridCurve[i - 1].maxGrid))
            { error = "gridCurve: minGrid/maxGrid không được giảm"; return false; }
        }
        if (holes.clusterSize < 1) { error = "holes.clusterSize phải >= 1"; return false; }

        error = null;
        return true;
    }

    public Tier GetTier(int levelNumber)
    {
        Tier result = tiers[0];
        foreach (Tier tier in tiers)
            if (tier.fromLevel <= levelNumber)
                result = tier;
        
        return result;
    }

    public bool GetGridRange(int level, out int min, out int max)
    {
        min = max = 0;
        if (gridCurve.Length == 0) return false;

        if (level <= gridCurve[0].level)
        {
            min = gridCurve[0].minGrid;
            max = gridCurve[0].maxGrid;
            return true;
        }

        for (int i = 1; i < gridCurve.Length; i++)
        {
            if(level > gridCurve[i].level) continue;
            
            GridAnchor a = gridCurve[i - 1];
            GridAnchor b = gridCurve[i];
            double t = (level - a.level) / (double)(b.level - a.level);
            min = (int)Math.Floor(a.minGrid + (b.minGrid - a.minGrid) * t + 0.5);
            max = (int)Math.Floor(a.maxGrid + (b.maxGrid - a.maxGrid) * t + 0.5);
            return true;
        }

        GridAnchor last = gridCurve[gridCurve.Length - 1];
        min = last.minGrid;
        max = last.maxGrid;
        return true;
    }

    public int GetBand(int grid)
    {
        for (int i = 0; i < sizeBands.Length; i++)
            if (grid <= sizeBands[i].maxGrid) return i;

        return sizeBands.Length - 1;
    }

    public float GetWeight(Shape shape, int band)
    {
        float weight = band == 0 ? shape.weights.small : band == 1 ? shape.weights.medium : shape.weights.large;
        return Math.Max(weight, 0.001f);
    }

    public void GetRoomsRange(int grid, out int min, out int max)
    {
        RoomsPerGrid best = roomsPerGrid[0];
        foreach(RoomsPerGrid entry in roomsPerGrid)
            if(Math.Abs(entry.grid - grid) < Math.Abs(best.grid - grid))
                best = entry;

        min = best.min;
        max = best.max;
    }
    
    public float GetCornerBias(int shortSide)
        => shortSide == 1 ? clueCornerBias.line : shortSide == 2 ? clueCornerBias.two : clueCornerBias.big;

    public LimitMode ResolveMode(int levelNumber, int seed)
    {
        LimitSchedule schedule = limitSchedule;
        if(schedule.bothEvery > 0 && levelNumber >= schedule.bothFromLevel && levelNumber % schedule.bothEvery == 0)
            return LimitMode.Both;
        
        foreach (LimitBlock block in schedule.blocks)
        {
            if (levelNumber < block.fromLevel || levelNumber > block.toLevel) continue;

            if (block.mode != LimitMode.Both && block.insertRuns > 0)
            {
                var rng = new System.Random(unchecked(seed * 31 + block.fromLevel));
                int length = block.toLevel - block.fromLevel + 1;

                for (int run = 0; run < block.insertRuns; run++)
                {
                    int runLength = rng.Next(block.insertMin, block.insertMax + 1);
                    int start = block.fromLevel + rng.Next(0, Math.Max(1, length - runLength + 1));
                    if (levelNumber >= start && levelNumber < start + runLength)
                        return block.mode == LimitMode.Timer ? LimitMode.Moves : LimitMode.Timer;
                }
            }

            return block.mode;
        }

        return schedule.fallbackMode;
    }
}
