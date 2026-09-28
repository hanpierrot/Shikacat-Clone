using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridConfig config;
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform gridRoot;

    private Cell[,] cells;
    private readonly List<Room> rooms = new List<Room>();

    public int Rows => config.rows;
    public int Columns => config.columns;
    public float CellSize => config.cellSize;
    public IReadOnlyList<Room> Rooms => rooms;

    private void Awake()
    {
        BuildGrid(config);
    }

    public void BuildGrid(GridConfig newConfig)
    {
        config = newConfig;
        ClearGrid();
        rooms.Clear();
        
        cells = new Cell[config.rows, config.columns];
        for (int r = 0; r < config.rows; r++)
        {
            for (int c = 0; c < config.columns; c++)
            {
                Vector3 pos = GridToWorld(r, c);
                Cell cell = Instantiate(cellPrefab, pos, Quaternion.identity, gridRoot);
                int clueValue = config.TryGetClue(r, c, out int value) ? value : 0;
                cell.Init(r, c, clueValue);
                cells[r, c] = cell;
            }
        }
    }

    private void ClearGrid()
    {
        if (gridRoot == null) return;
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(gridRoot.GetChild(i).gameObject);
        }
    }

    public Vector3 GridToWorld(int r, int c)
    {
        float halfWidth = (config.columns - 1) * config.cellSize * 0.5f;
        float halfHeight = (config.rows - 1) * config.cellSize * 0.5f;

        float x = c * config.cellSize - halfWidth;
        float y = -r * config.cellSize + halfHeight;
        return gridRoot.position + new Vector3(x, y, 0f);
    }

    public bool WorldToGrid(Vector3 worldPos, out int r, out int c)
    {
        float halfWidth = (config.columns - 1) * config.cellSize * 0.5f;
        float halfHeight = (config.rows - 1) * config.cellSize * 0.5f;

        Vector3 local = worldPos - gridRoot.position;
        c = Mathf.RoundToInt((local.x + halfWidth) / config.cellSize);
        r = Mathf.RoundToInt((halfHeight - local.y) / config.cellSize);
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
        
        return room;
    }
    
    public void RemoveRoom(Room room)
    {
        if (room == null) return;
        for (int r = room.MinRow; r <= room.MaxRow; r++)
            for (int c = room.MinColumn; c <= room.MaxColumn; c++)
                GetCell(r, c)?.SetAssignedRoom(null);
        
        rooms.Remove(room);
    }
}
