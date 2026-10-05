using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    
    private static LevelData.ClueEntry Clue(int row, int col, int value)
        => new LevelData.ClueEntry { row = row, col = col, value = value };

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

    private static readonly IRoomBounds[] NoRooms = Array.Empty<IRoomBounds>();

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
}
