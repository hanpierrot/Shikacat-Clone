using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoSingleton<GridManager>
{
    [Header("Grid")]
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform gridRoot;
    
    [Header("Responsive")]
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0f, 1f)] private float boardWidthPercent = 0.9f;
    [SerializeField, Range(0f, 1f)] private float boardHeightPercent = 0.7f;
    
    [Header("Layout")]
    [SerializeField] private Vector2 outerPadding = Vector2.zero;
    [SerializeField, Range(0f, 1f)] private float spacingRatio = 0.1f;
    [SerializeField] private ClueTypeIcons clueTypeIcons;

    public event Action<Room> RoomRemoved;
    public event Action<Room> RoomCommitted;

    private LevelData config;
    private Vector2 boardSize;
    private Cell[,] cells;
    private float cellSize;
    private float step;
    private readonly List<Room> rooms = new List<Room>();
    private readonly List<Cell> cellPool = new List<Cell>();
    
    public float CellSize => cellSize;
    public bool IsReplacingRooms { get; private set; }

    public RoomCheckResult CheckRoom(int minRow, int maxRow, int minCol, int maxCol, out LevelData.ClueEntry clue)
        => RoomRules.Check(config, rooms, minRow, maxRow, minCol, maxCol, out clue);
    
    public void BuildGrid(LevelData newLevel)
    {
        for(int i = rooms.Count - 1; i >=0; i--)
            RemoveRoom(rooms[i]);
        
        config = newLevel;
        
        Vector2 visibleSize = CameraViewport.GetVisibleWorldSize(targetCamera);
        boardSize = new Vector2(visibleSize.x * boardWidthPercent, visibleSize.y * boardHeightPercent);
        
        float availableWidth = boardSize.x - 2f * outerPadding.x;
        float availableHeight = boardSize.y - 2f * outerPadding.y;

        float cellSizeX = availableWidth / (config.columns + (config.columns - 1) * spacingRatio);
        float cellSizeY = availableHeight / (config.rows + (config.rows - 1) * spacingRatio);

        cellSize = Mathf.Min(cellSizeX, cellSizeY);
        step = cellSize * (1f + spacingRatio);
        
        EnsurePoolSize(config.ActiveCellCount);
        
        cells = new Cell[config.rows, config.columns];
        int index = 0;
        for (int r = 0; r < config.rows; r++)
        {
            for (int c = 0; c < config.columns; c++)
            {
                if (config.IsHole(r, c)) continue;
                
                Cell cell = cellPool[index];
                cell.transform.position = GridToWorld(r, c);
                cell.transform.localScale = Vector3.one * cellSize;
                cell.gameObject.SetActive(true);
                
                bool hasClue = config.TryGetClue(r, c, out LevelData.ClueEntry clue);
                Sprite typeIcon = hasClue && clueTypeIcons != null ? clueTypeIcons.Get(clue.type) : null;
                
                cell.Init(r, c, hasClue, clue, typeIcon);
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
        
        if(r < 0 || r >= config.rows || c < 0 || c >= config.columns) return false;
        
        return !config.IsHole(r, c);
    }

    public Cell GetCell(int r, int c)
    {
        if (r < 0 || r >= config.rows || c < 0 || c >= config.columns) return null;
        return cells[r, c];
    }

    public Room CommitRoom(int minRow, int maxRow, int minCol, int maxCol, Cell clueCell)
    {
        IsReplacingRooms = true;
        for(int i = rooms.Count - 1; i >= 0; i--)
            if(rooms[i].Overlaps(minRow, maxRow, minCol, maxCol))
                RemoveRoom(rooms[i]);
        IsReplacingRooms = false;
        
        var room = new Room(minRow, maxRow, minCol, maxCol, clueCell);
        rooms.Add(room);
        
        for (int r = minRow; r <= maxRow; r++)
            for (int c = minCol; c <= maxCol; c++)
                GetCell(r, c)?.SetAssignedRoom(room);
        
        RefreshLocks();
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
        if (!IsReplacingRooms) RefreshLocks();
        RoomRemoved?.Invoke(room);
    }

    public bool IsGridFull()
    {
        for (int r = 0; r < config.rows; r++)
            for (int c = 0; c < config.columns; c++)
            {
                Cell cell = GetCell(r, c);
                if (cell != null && cell.AssignedRoom == null) return false;
            }

        return true;
    }

    public bool ContainsHole(int minRow, int maxRow, int minCol, int maxCol)
    {
        for (int r = minRow; r <= maxRow; r++)
            for (int c = minCol; c <= maxCol; c++)
                if (config.IsHole(r, c)) return true;

        return false;
    }

    private void RefreshLocks()
    {
        if(cells == null) return;
        
        foreach (var cell in cells)
            cell?.RefreshLock(rooms.Count);
    }
    
    public bool AreAllRoomsCorrect() => RoomRules.AllRoomsCorrect(config, rooms);
}
