using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomCheckResult
{
    Valid,
    OutOfBounds,
    HasHole,
    DuplicateRoom,
    NoClue,
    MultipleClues,
    Locked,
    AreaMismatch,
    ShapeMismatch
}

public interface IRoomBounds
{
    int MinRow { get; }
    int MaxRow { get; }
    int MinColumn { get; }
    int MaxColumn { get; }
}

public static class RoomRules
{
    public static int Area(int minRow, int maxRow, int minCol, int maxCol) => (maxRow - minRow + 1) * (maxCol - minCol + 1);
    
    public static bool IsLocked(ClueType type, int unlockRoomCount, bool isCovered, int roomCount)
        => type == ClueType.Locked && !isCovered && roomCount < unlockRoomCount;

    public static RoomCheckResult Check(
        LevelData level, IEnumerable<IRoomBounds> rooms,
        int minRow, int maxRow, int minCol, int maxCol,
        out LevelData.ClueEntry clue)
    {
        clue = default;
        
        if (minRow < 0 || minCol < 0 || maxRow < minRow || maxCol < minCol
            || maxRow >= level.rows || maxCol >= level.columns)
            return RoomCheckResult.OutOfBounds;

        int roomCount = 0;
        foreach (IRoomBounds room in rooms)
        {
            roomCount++;
            if (room.MinRow == minRow && room.MaxRow == maxRow
                                      && room.MinColumn == minCol && room.MaxColumn == maxCol)
                return RoomCheckResult.DuplicateRoom;
        }
        
        for (int r = minRow; r <= maxRow; r++)
            for (int c = minCol; c <= maxCol; c++)
                if (level.IsHole(r, c)) return RoomCheckResult.HasHole;

        int clueCount = 0;
        for(int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                if (!level.TryGetClue(r, c, out LevelData.ClueEntry found)) continue;

                clueCount++;
                if (clueCount > 1) return RoomCheckResult.MultipleClues;
                clue = found;
            }
        }
        
        if(clueCount == 0) return RoomCheckResult.NoClue;
        if(IsLocked(clue.type, clue.unlockRoomCount, IsCovered(rooms, clue.row, clue.col), roomCount))
            return RoomCheckResult.Locked;
        
        if (clue.type != ClueType.Hidden && clue.value != Area(minRow, maxRow, minCol, maxCol))
            return RoomCheckResult.AreaMismatch;
        
        int width = maxCol - minCol + 1;
        int height = maxRow - minRow + 1;
        if (!MatchesShape(clue.type, width, height)) return RoomCheckResult.ShapeMismatch;
        
        return RoomCheckResult.Valid;
    }
    
    private static bool MatchesShape(ClueType type, int width, int height)
    {
        switch (type)
        {
            case ClueType.Horizontal: return width >= height;
            case ClueType.Vertical: return width <= height;
            case ClueType.Square: return width == height;
            default: return true;
        }
    }

    private static bool IsCovered(IEnumerable<IRoomBounds> rooms, int row, int col)
    {
        foreach (IRoomBounds room in rooms)
        {
            if (row >= room.MinRow && row <= room.MaxRow && col >= room.MinColumn && col <= room.MaxColumn)
                return true;
        }
        return false;
    }

    public static bool AllRoomsCorrect(LevelData level, IEnumerable<IRoomBounds> rooms)
    {
        foreach (IRoomBounds room in rooms)
        {
            if (!TryFindClue(level, room, out LevelData.ClueEntry clue)) 
                return false;
            if (clue.value != Area(room.MinRow, room.MaxRow, room.MinColumn, room.MaxColumn)) 
                return false;
        }
        return true;
    }

    private static bool TryFindClue(LevelData level, IRoomBounds room, out LevelData.ClueEntry clue)
    {
        for (int r = room.MinRow; r <= room.MaxRow; r++)
            for (int c = room.MinColumn; c <= room.MaxColumn; c++)
                if (level.TryGetClue(r, c, out clue)) return true;
        
        clue = default;
        return false;
    }
}
