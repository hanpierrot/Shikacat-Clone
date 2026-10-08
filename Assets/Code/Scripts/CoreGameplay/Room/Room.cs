public class Room : IRoomBounds
{
    public int MinRow { get; }
    public int MaxRow { get; }
    public int MinColumn { get; }
    public int MaxColumn { get; }
    public RoomVisual Visual { get; set; }
    public CatColor Color { get; set; }
    
    public Room(int minRow, int maxRow, int minColumn, int maxColumn)
    {
        MinRow = minRow;
        MaxRow = maxRow;
        MinColumn = minColumn;
        MaxColumn = maxColumn;
    }

    public bool Overlaps(int minRow, int maxRow, int minColumn, int maxColumn)
    {
        return minRow <= MaxRow && maxRow >= MinRow
            && minColumn <= MaxColumn && maxColumn >= MinColumn;
    }
    
    public bool IsAdjacentTo(Room other)
    {
        bool rowsOverlap = MinRow <= other.MaxRow && other.MinRow <= MaxRow;
        bool colsOverlap = MinColumn <= other.MaxColumn && other.MinColumn <= MaxColumn;

        bool sideBySide = rowsOverlap && (MaxColumn + 1 == other.MinColumn || other.MaxColumn + 1 == MinColumn);
        bool stacked = colsOverlap && (MaxRow + 1 == other.MinRow || other.MaxRow + 1 == MinRow);
        return sideBySide || stacked;
    }
}
