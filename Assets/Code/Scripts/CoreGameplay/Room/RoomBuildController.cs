using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridInputHandler))]
public class RoomBuildController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GridInputHandler inputHandler;
    
    [SerializeField] private SpriteRenderer dragPreviewOverlay;
    [SerializeField] private Sprite validPreviewSprite;
    [SerializeField] private Sprite invalidPreviewSprite;
    [SerializeField] private SpriteRenderer roomPanelPrefab;
    [SerializeField] private Transform roomVisualRoot;
    [SerializeField] private CatPalette catPalette;
    
    private readonly List<SpriteRenderer> roomPanelPool = new List<SpriteRenderer>();

    private int startRow, startCol;
    private int currentMinRow, currentMaxRow, currentMinCol, currentMaxCol;
    private bool currentIsValid;

    private void Awake()
    {
        if (inputHandler == null) inputHandler = GetComponent<GridInputHandler>();
        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);

        if (roomVisualRoot != null)
        {
            var existingPanels = roomVisualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            roomPanelPool.AddRange(existingPanels);
            foreach (var panel in existingPanels)
                panel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        inputHandler.DragStarted += TryBeginDrag;
        inputHandler.DragMoved += UpdateDrag;
        inputHandler.DragEnded += EndDrag;
        gridManager.RoomRemoved += ReleaseRoomPanel;
    }

    private void OnDisable()
    {
        inputHandler.DragStarted -= TryBeginDrag;
        inputHandler.DragMoved -= UpdateDrag;
        inputHandler.DragEnded -= EndDrag;
        gridManager.RoomRemoved -= ReleaseRoomPanel;
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        if (!gridManager.WorldToGrid(world, out startRow, out startCol)) return;
        
        SetPreviewBounds(startRow, startRow, startCol, startCol);
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        if (!gridManager.WorldToGrid(world, out int row, out int col)) return;

        SetPreviewBounds(
            Mathf.Min(startRow, row), Mathf.Max(startRow, row),
            Mathf.Min(startCol, col), Mathf.Max(startCol, col));
    }
    
    private void EndDrag()
    {
        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);
        
        if (TryValidateSelection(out Cell clueCell))
        {
            Room room = gridManager.CommitRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, clueCell);
            SpawnRoomPanel(room, clueCell.ClueColor);
        }
        
        ResetBounds();
    }

    private void SetPreviewBounds(int minRow, int maxRow, int minCol, int maxCol)
    {
        currentMinRow = minRow;
        currentMaxRow = maxRow;
        currentMinCol = minCol;
        currentMaxCol = maxCol;
        
        currentIsValid = TryValidateSelection(out _);
        UpdatePreviewOverlay();
    }

    private void UpdatePreviewOverlay()
    {
        if (dragPreviewOverlay == null) return;
        
        gridManager.GetWorldRect(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, out Vector3 center, out Vector2 size);
        dragPreviewOverlay.transform.position = center;
        dragPreviewOverlay.size = size;
        dragPreviewOverlay.sprite = currentIsValid ? validPreviewSprite : invalidPreviewSprite;
        dragPreviewOverlay.gameObject.SetActive(true);
    }

    private void SpawnRoomPanel(Room room, CatColor color)
    {
        if (roomPanelPrefab == null || catPalette == null) return;
        if (!catPalette.TryGet(color, out var entry)) return;
        
        SpriteRenderer panel = GetPooledPanel();
        gridManager.GetWorldRect(room.MinRow, room.MaxRow, room.MinColumn, room.MaxColumn, out Vector3 center, out Vector2 size);
        panel.transform.position = center;
        panel.size = size;
        panel.sprite = entry.panelBoxSprite;
        panel.gameObject.SetActive(true);
        room.Visual = panel;
    }

    private SpriteRenderer GetPooledPanel()
    {
        foreach (var panel in roomPanelPool)
            if (!panel.gameObject.activeSelf) return panel;
        
        SpriteRenderer newPanel = Instantiate(roomPanelPrefab, roomVisualRoot);
        roomPanelPool.Add(newPanel);
        return newPanel;
    }

    private void ReleaseRoomPanel(Room room)
    {
        if (room?.Visual == null) return;
        room.Visual.gameObject.SetActive(false);
        room.Visual = null;
    }
    
    private void ResetBounds()
    {
        currentMinRow = 0;
        currentMaxRow = -1;
        currentMinCol = 0;
        currentMaxCol = -1;
    }

    private bool TryValidateSelection(out Cell clueCell)
    {
        clueCell = null;
        int width = currentMaxCol - currentMinCol + 1;
        int height = currentMaxRow - currentMinRow + 1;
        int area = width * height;
        
        if (!gridManager.IsAreaFree(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol))
            return false;

        int clueCount = 0;
        for (int r = currentMinRow; r <= currentMaxRow; r++)
        {
            for (int c = currentMinCol; c <= currentMaxCol; c++)
            {
                Cell cell = gridManager.GetCell(r, c);
                if (cell == null || !cell.HasClue) continue;
                
                clueCount++;
                if(clueCount > 1) return false;
                clueCell = cell;
            }
        }
        
        return clueCount == 1 && clueCell.ClueValue == area;
    }
}
