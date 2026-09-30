using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoSingleton<GridManager>
{
    [SerializeField] private GridConfig config;
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private Vector2 boardSize = new Vector2(8f, 8f);
    [SerializeField] private Vector2 outerPadding = Vector2.zero;
    [SerializeField, Range(0f, 1f)] private float spacingRatio = 0.1f;
    [SerializeField] private CatPalette catPalette;

    public event Action<Room> RoomRemoved;
    public event Action<Room> RoomCommitted;
    
    private Cell[,] cells;
    private float cellSize;
    private float step;
    private readonly List<Room> rooms = new List<Room>();
    private readonly List<Cell> cellPool = new List<Cell>();

    public int Rows => config.rows;
    public int Columns => config.columns;
    public float CellSize => cellSize;
    public IReadOnlyList<Room> Rooms => rooms;

    protected override void Awake()
    {
        base.Awake();
        BuildGrid(config);
    }

    public void BuildGrid(GridConfig newConfig)
    {
        config = newConfig;
        for (int i = rooms.Count - 1; i >= 0; i--)
            RemoveRoom(rooms[i]);
        
        float availableWidth = boardSize.x - 2f * outerPadding.x;
        float availableHeight = boardSize.y - 2f * outerPadding.y;

        float cellSizeX = availableWidth / (config.columns + (config.columns - 1) * spacingRatio);
        float cellSizeY = availableHeight / (config.rows + (config.rows - 1) * spacingRatio);

        cellSize = Mathf.Min(cellSizeX, cellSizeY);
        step = cellSize * (1f + spacingRatio);
        
        int needed = config.rows * config.columns;
        EnsurePoolSize(needed);
        
        cells = new Cell[config.rows, config.columns];
        int index = 0;
        for (int r = 0; r < config.rows; r++)
        {
            for (int c = 0; c < config.columns; c++)
            {
                Cell cell = cellPool[index];
                cell.transform.position = GridToWorld(r, c);
                cell.transform.localScale = Vector3.one * cellSize;
                cell.gameObject.SetActive(true);
                
                bool hasClue = config.TryGetClue(r, c, out int value, out CatColor color);
                Sprite catSprite = null;
                if (hasClue && catPalette != null && catPalette.TryGet(color, out var entry))
                    catSprite = entry.catIconSprite;
                
                cell.Init(r, c, hasClue ? value : 0, color, catSprite);
                cells[r, c] = cell;
                index++;
            }
        }

        for (int i = index; i < cellPool.Count; i++)
            cellPool[i].gameObject.SetActive(false);
    }

    public void GetWorldRect(int minRow, int maxRow, int minCol, int maxCol, out Vector3 center, out Vector2 size)
    {
        float halfWidth = (config.columns - 1) * step * 0.5f;
        float halfHeight = (config.rows - 1) * step * 0.5f;

        float centerCol = (minCol + maxCol) * 0.5f;
        float centerRow = (minRow + maxRow) * 0.5f;

        float x = centerCol * step - halfWidth;
        float y = -centerRow * step + halfHeight;

        center = gridRoot.position + new Vector3(x, y, 0f);
        size = new Vector2(
            (maxCol - minCol) * step + cellSize,
            (maxRow - minRow) * step + cellSize);
    }
    
    private void EnsurePoolSize(int needed)
    {
        while (cellPool.Count < needed)
        {
            Cell cell = Instantiate(cellPrefab, gridRoot);
            cell.gameObject.SetActive(false);
            cellPool.Add(cell);
        }
    }

    public Vector3 GridToWorld(int r, int c)
    {
        float halfWidth = (config.columns - 1) * step * 0.5f;
        float halfHeight = (config.rows - 1) * step * 0.5f;

        float x = c * step - halfWidth;
        float y = -r * step + halfHeight;
        return gridRoot.position + new Vector3(x, y, 0f);
    }

    public bool WorldToGrid(Vector3 worldPos, out int r, out int c)
    {
        float halfWidth = (config.columns - 1) * step * 0.5f;
        float halfHeight = (config.rows - 1) * step * 0.5f;

        Vector3 local = worldPos - gridRoot.position;
        c = Mathf.RoundToInt((local.x + halfWidth) / step);
        r = Mathf.RoundToInt((halfHeight - local.y) / step);
        return r >= 0 && r < config.rows && c >= 0 && c < config.columns;
    }

    public Cell GetCell(int r, int c)
    {
        if (r < 0 || r >= config.rows || c < 0 || c >= config.columns) return null;
        return cells[r, c];
    }

    public bool IsAreaFree(int minRow, int maxRow, int minCol, int maxCol, Room ignoreRoom = null)
    {
        foreach (Room room in rooms)
        {
            if (room == ignoreRoom) continue;
            if (room.Overlaps(minRow, maxRow, minCol, maxCol)) return false;
        }
        return true;
    }

    public Room CommitRoom(int minRow, int maxRow, int minCol, int maxCol, Cell clueCell)
    {
        var room = new Room(minRow, maxRow, minCol, maxCol, clueCell);
        rooms.Add(room);
        
        for (int r = minRow; r <= maxRow; r++)
            for (int c = minCol; c <= maxCol; c++)
                GetCell(r, c)?.SetAssignedRoom(room);
        
        RoomCommitted?.Invoke(room);
        return room;
    }
    
    public void RemoveRoom(Room room)
    {
        if (room == null) return;
        for (int r = room.MinRow; r <= room.MaxRow; r++)
            for (int c = room.MinColumn; c <= room.MaxColumn; c++)
                GetCell(r, c)?.SetAssignedRoom(null);
        
        rooms.Remove(room);
        RoomRemoved?.Invoke(room);
    }
    
    public bool IsGridFull()
    {
        for (int r = 0; r < config.rows; r++)
            for (int c = 0; c < config.columns; c++)
                if (GetCell(r, c)?.AssignedRoom == null) return false;

        return true;
    }
}
