using System;
using System.Collections.Generic;
using NUnit.Framework;

public class RoomRulesTests
{
    private class FakeRoom : IRoomBounds
    {
        public int MinRow { get; }
        public int MaxRow { get; }
        public int MinColumn { get; }
        public int MaxColumn { get; }

        public FakeRoom(int minRow, int maxRow, int minCol, int maxCol)
        {
            MinRow = minRow; MaxRow = maxRow; MinColumn = minCol; MaxColumn = maxCol;
        }
    }
    
    private static readonly IRoomBounds[] NoRooms = Array.Empty<IRoomBounds>();
    
    private static LevelData.ClueEntry Clue(int row, int col, int value, ClueType type = ClueType.Normal, int unlock = 0)
        => new LevelData.ClueEntry { row = row, col = col, value = value, type = type, unlockRoomCount = unlock };
    
    private static LevelData.SolutionRoom Sol(int row, int col, int width, int height, int clueIndex)
        => new LevelData.SolutionRoom { row = row, col = col, width = width, height = height, clueIndex = clueIndex };

    private static LevelData MakeLevel(int rows, int cols, LevelData.ClueEntry[] clues,
        LevelData.CellCoord[] holes = null)
    {
        var level = new LevelData
        {
            rows = rows,
            columns = cols,
            timeLimit = 60f,
            clues = clues,
            holes = holes ?? Array.Empty<LevelData.CellCoord>()
        };
        level.Prepare();
        return level;
    }
    
    private static LevelData TwoRowLevel()
    {
        var level = MakeLevel(2, 2, new[] { Clue(0, 0, 2), Clue(1, 0, 2) });
        level.moveLimit = 4;
        return level;
    }

    [Test]
    public void Valid_WhenOneClueAndAreaMatches()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 1, 0, 1, out var clue));
        Assert.AreEqual(4, clue.value);
    }

    [Test]
    public void AreaMismatch_WhenAreaDiffersFromClue()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        Assert.AreEqual(RoomCheckResult.AreaMismatch, RoomRules.Check(level, NoRooms, 0, 0, 0, 2, out _));
    }

    [Test]
    public void NoClue_WhenAreaContainsNoClue()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        Assert.AreEqual(RoomCheckResult.NoClue, RoomRules.Check(level, NoRooms, 2, 3, 2, 3, out _));
    }

    [Test]
    public void MultipleClues_WhenAreaContainsTwoClues()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 2), Clue(0, 1, 2) });
        Assert.AreEqual(RoomCheckResult.MultipleClues, RoomRules.Check(level, NoRooms, 0, 0, 0, 1, out _));
    }

    [Test]
    public void HasHole_WhenAreaCoversHole()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) },
            new[] { new LevelData.CellCoord { row = 1, col = 1 } });
        Assert.AreEqual(RoomCheckResult.HasHole, RoomRules.Check(level, NoRooms, 0, 1, 0, 1, out _));
    }

    [Test]
    public void OutOfBounds_WhenAreaExceedsGrid()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        Assert.AreEqual(RoomCheckResult.OutOfBounds, RoomRules.Check(level, NoRooms, 0, 4, 0, 0, out _));
    }

    [Test]
    public void OutOfBounds_WhenBoundsAreEmpty()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        Assert.AreEqual(RoomCheckResult.OutOfBounds, RoomRules.Check(level, NoRooms, 0, -1, 0, -1, out _));
    }

    [Test]
    public void DuplicateRoom_WhenSameBoundsAlreadyExist()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        var rooms = new List<IRoomBounds> { new FakeRoom(0, 1, 0, 1) };
        Assert.AreEqual(RoomCheckResult.DuplicateRoom, RoomRules.Check(level, rooms, 0, 1, 0, 1, out _));
    }

    [Test]
    public void Valid_WhenOverlappingDifferentRoom()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        var rooms = new List<IRoomBounds> { new FakeRoom(0, 0, 0, 3) };
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, rooms, 0, 1, 0, 1, out _));
    }

    [Test]
    public void Validate_RejectsClueSumNotEqualToActiveCells()
    {
        var level = MakeLevel(2, 2, new[] { Clue(0, 0, 3) });
        level.moveLimit = 4;
        Assert.IsFalse(level.Validate(out _));
    }

    [Test]
    public void Validate_RejectsSquareClueWithNonSquareValue()
    {
        var clue = Clue(0, 0, 6);
        clue.type = ClueType.Square;
        var level = MakeLevel(2, 3, new[] { clue });
        level.moveLimit = 4;
        Assert.IsFalse(level.Validate(out _));
    }

    [Test]
    public void Validate_RejectsLockedClueWithInvalidUnlockCount()
    {
        var locked = Clue(0, 0, 2);
        locked.type = ClueType.Locked;
        locked.unlockRoomCount = 5;
        var level = MakeLevel(2, 2, new[] { locked, Clue(1, 0, 2) });
        level.moveLimit = 4;
        Assert.IsFalse(level.Validate(out _));
    }
    
    [Test]
    public void Horizontal_AcceptsWideAndSquare_RejectsTall()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4, ClueType.Horizontal) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 0, 0, 3, out _));          // 1x4 ngang
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 1, 0, 1, out _));          // 2x2 vuông
        Assert.AreEqual(RoomCheckResult.ShapeMismatch, RoomRules.Check(level, NoRooms, 0, 3, 0, 0, out _));  // 4x1 dọc
    }

    [Test]
    public void Vertical_AcceptsTallAndSquare_RejectsWide()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4, ClueType.Vertical) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 3, 0, 0, out _));
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 1, 0, 1, out _));
        Assert.AreEqual(RoomCheckResult.ShapeMismatch, RoomRules.Check(level, NoRooms, 0, 0, 0, 3, out _));
    }

    [Test]
    public void Vertical_Area6_Accepts2x3Tall_Rejects3x2Wide()
    {
        var level = MakeLevel(3, 3, new[] { Clue(0, 0, 6, ClueType.Vertical) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 2, 0, 1, out _));          // cao 3, rộng 2
        Assert.AreEqual(RoomCheckResult.ShapeMismatch, RoomRules.Check(level, NoRooms, 0, 1, 0, 2, out _));  // cao 2, rộng 3
    }

    [Test]
    public void Square_AcceptsOnlySquare()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4, ClueType.Square) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 1, 0, 1, out _));
        Assert.AreEqual(RoomCheckResult.ShapeMismatch, RoomRules.Check(level, NoRooms, 0, 0, 0, 3, out _));
        Assert.AreEqual(RoomCheckResult.ShapeMismatch, RoomRules.Check(level, NoRooms, 0, 3, 0, 0, out _));
    }

    [Test]
    public void Normal_AcceptsAnyShapeWithMatchingArea()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 0, 0, 3, out _));
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 3, 0, 0, out _));
    }
    
    [Test]
    public void Hidden_AcceptsAnyAreaButStillNeedsOneClue()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4, ClueType.Hidden) });
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, NoRooms, 0, 0, 0, 1, out _));      // 1x2, sai diện tích nhưng vẫn hợp lệ
        Assert.AreEqual(RoomCheckResult.NoClue, RoomRules.Check(level, NoRooms, 3, 3, 3, 3, out _));
    }

    [Test]
    public void AllRoomsCorrect_FalseWhenHiddenRoomHasWrongArea()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 4, ClueType.Hidden) });
        Assert.IsFalse(RoomRules.AllRoomsCorrect(level, new List<IRoomBounds> { new FakeRoom(0, 0, 0, 1) }));
        Assert.IsTrue(RoomRules.AllRoomsCorrect(level, new List<IRoomBounds> { new FakeRoom(0, 1, 0, 1) }));
    }

    [Test]
    public void Locked_RejectedUntilEnoughRooms()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 1), Clue(3, 3, 1, ClueType.Locked, 1) });
        Assert.AreEqual(RoomCheckResult.Locked, RoomRules.Check(level, NoRooms, 3, 3, 3, 3, out _));

        var oneRoom = new List<IRoomBounds> { new FakeRoom(0, 0, 0, 0) };
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, oneRoom, 3, 3, 3, 3, out _));
    }

    [Test]
    public void Locked_StaysUnlockedWhenAlreadyCoveredByRoom()
    {
        var level = MakeLevel(4, 4, new[] { Clue(0, 0, 1), Clue(3, 3, 1, ClueType.Locked, 2) });
        var rooms = new List<IRoomBounds> { new FakeRoom(2, 3, 3, 3) };   // đã phủ ô (3,3), mới có 1 phòng < 2
        Assert.AreEqual(RoomCheckResult.Valid, RoomRules.Check(level, rooms, 3, 3, 3, 3, out _));
    }

    [Test]
    public void IsLocked_Rules()
    {
        Assert.IsTrue(RoomRules.IsLocked(ClueType.Locked, 3, false, 2));
        Assert.IsFalse(RoomRules.IsLocked(ClueType.Locked, 3, false, 3));    // x = 0 thì mở
        Assert.IsFalse(RoomRules.IsLocked(ClueType.Locked, 3, true, 0));     // đã nằm trong phòng thì giữ mở
        Assert.IsFalse(RoomRules.IsLocked(ClueType.Normal, 3, false, 0));
    }
    
    [Test]
    public void LevelCollection_RoundTripsThroughJson()
    {
        LevelData level = MakeLevel(4, 4, new[] { Clue(0, 0, 16, ClueType.Hidden) });
        level.moveLimit = 5;
        var original = new LevelCollection { version = 1, levels = new[] { level } };

        LevelCollection parsed = LevelLoader.Parse(LevelLoader.ToJson(original));

        Assert.AreEqual(1, parsed.levels.Length);
        Assert.AreEqual(ClueType.Hidden, parsed.levels[0].clues[0].type);
        Assert.AreEqual(5, parsed.levels[0].moveLimit);
        Assert.AreEqual(16, parsed.levels[0].ActiveCellCount);
    }
    
    [Test]
    public void TryParse_ReturnsFalseOnBrokenJson()
    {
        Assert.IsFalse(LevelLoader.TryParse("{ not valid json", out LevelCollection collection, out string error));
        Assert.IsNull(collection);
        Assert.IsNotNull(error);
    }
    
    [Test]
    public void Validate_AcceptsCorrectSolution()
    {
        var level = TwoRowLevel();
        level.solution = new[] { Sol(0, 0, 2, 1, 0), Sol(1, 0, 2, 1, 1) };
        Assert.IsTrue(level.Validate(out _));
    }

    [Test]
    public void Validate_RejectsOverlappingSolution()
    {
        var level = TwoRowLevel();
        level.solution = new[] { Sol(0, 0, 2, 1, 0), Sol(0, 0, 2, 1, 1) };
        Assert.IsFalse(level.Validate(out _));
    }

    [Test]
    public void Validate_RejectsSolutionWithWrongArea()
    {
        var level = TwoRowLevel();
        level.solution = new[] { Sol(0, 0, 1, 1, 0), Sol(1, 0, 2, 1, 1) };
        Assert.IsFalse(level.Validate(out _));
    }
}
