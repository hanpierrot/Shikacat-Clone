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
    AreaMismatch
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
    
    public static RoomCheckResult Check(
        LevelData level, IEnumerable<IRoomBounds> rooms,
        int minRow, int maxRow, int minCol, int maxCol,
        out LevelData.ClueEntry clue)
    {
        clue = default;
        
        if (minRow < 0 || minCol < 0 || maxRow < minRow || maxCol < minCol
            || maxRow >= level.rows || maxCol >= level.columns)
            return RoomCheckResult.OutOfBounds;

        foreach (IRoomBounds room in rooms)
        {
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
        if(clue.value != Area(minRow, maxRow, minCol, maxCol)) return RoomCheckResult.AreaMismatch;
        
        return RoomCheckResult.Valid;
    }
}
