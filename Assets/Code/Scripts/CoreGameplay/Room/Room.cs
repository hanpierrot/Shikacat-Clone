using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : IRoomBounds
{
    public int MinRow { get; }
    public int MaxRow { get; }
    public int MinColumn { get; }
    public int MaxColumn { get; }
    public Cell ClueCell { get; }
    public RoomVisual Visual { get; set; }
    
    public Room(int minRow, int maxRow, int minColumn, int maxColumn, Cell clueCell)
    {
        MinRow = minRow;
        MaxRow = maxRow;
        MinColumn = minColumn;
        MaxColumn = maxColumn;
        ClueCell = clueCell;
    }

    public bool Overlaps(int minRow, int maxRow, int minColumn, int maxColumn)
    {
        return minRow <= MaxRow && maxRow >= MinRow
            && minColumn <= MaxColumn && maxColumn >= MinColumn;
    }
}
