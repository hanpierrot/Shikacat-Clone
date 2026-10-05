using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridInputHandler))]
public class RoomBuildController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GridInputHandler inputHandler;
    [SerializeField] private RoomRemoveController roomRemoveController;
    
    [SerializeField] private SpriteRenderer dragPreviewOverlay;
    [SerializeField] private Sprite validPreviewSprite;
    [SerializeField] private Sprite invalidPreviewSprite;
    [SerializeField] private RoomVisual roomPanelPrefab;
    [SerializeField] private Transform roomVisualRoot;
    [SerializeField] private CatPalette catPalette;
    [SerializeField] private float referenceCellSize = 1f;
    
    private readonly List<RoomVisual> roomPanelPool = new List<RoomVisual>();

    private int startRow, startCol;
    private int currentMinRow, currentMaxRow, currentMinCol, currentMaxCol;
    private bool isPressValid;
    private bool isResolving;

    private void Awake()
    {
        if (inputHandler == null) inputHandler = GetComponent<GridInputHandler>();
        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);

        if (roomVisualRoot != null)
        {
            var existingPanels = roomVisualRoot.GetComponentsInChildren<RoomVisual>(true);
            roomPanelPool.AddRange(existingPanels);
            foreach (var panel in existingPanels)
                panel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        inputHandler.PressStarted += HandlePressStarted;
        inputHandler.PressMoved += HandlePressMoved;
        inputHandler.PressEnded += HandlePressEnded;
        GridManager.Instance.RoomRemoved += ReleaseRoomPanel;
    }

    private void OnDisable()
    {
        inputHandler.PressStarted -= HandlePressStarted;
        inputHandler.PressMoved -= HandlePressMoved;
        inputHandler.PressEnded -= HandlePressEnded;
        if (GridManager.Instance != null)
            GridManager.Instance.RoomRemoved -= ReleaseRoomPanel;
    }

    private void HandlePressStarted(Vector2 screenPosition)
    {
        if (isResolving) return;
        
        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        isPressValid = GridManager.Instance.WorldToGrid(world, out startRow, out startCol);
        if (!isPressValid) return;

        SetPreviewBounds(startRow, startRow, startCol, startCol);
    }
    
    private void HandlePressMoved(Vector2 screenPosition)
    {
        if (!isPressValid || isResolving) return;

        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        if (!GridManager.Instance.WorldToGrid(world, out int row, out int col)) return;

        SetPreviewBounds(
            Mathf.Min(startRow, row), Mathf.Max(startRow, row),
            Mathf.Min(startCol, col), Mathf.Max(startCol, col));
    }

    private void HandlePressEnded(Vector2 screenPosition, bool wasDrag)
    {
        if (!isPressValid || isResolving) return;
        
        bool isValid = TryValidateSelection(out _);
        if (!isValid)
        {
            UpdatePreviewOverlay(invalidPreviewSprite);
            isResolving = true;
            StartCoroutine(ResolveAfterDelay(wasDrag));
            return;
        }

        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);
        
        ResolvePressEnd(wasDrag);
    }
    private IEnumerator ResolveAfterDelay(bool wasDrag)
    {
        yield return new WaitForSeconds(0.25f);
        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);
        ResolvePressEnd(wasDrag);
        isResolving = false;
    }
    
    private void ResolvePressEnd(bool wasDrag)
    {
        bool handledAsRemove = !wasDrag && roomRemoveController != null
                                        && roomRemoveController.TryRemoveAt(currentMinRow, currentMinCol);

        if (!handledAsRemove)
            TryCommitSelection();

        ResetBounds();
        isPressValid = false;
    }
    
    private void TryCommitSelection()
    {
        if (TryValidateSelection(out Cell clueCell))
        {
            Room room = GridManager.Instance.CommitRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, clueCell);
            SpawnRoomPanel(room, clueCell.ClueColor);
        }
    }

    private void SetPreviewBounds(int minRow, int maxRow, int minCol, int maxCol)
    {
        if (GridManager.Instance.ContainsHole(minRow, maxRow, minCol, maxCol))
        {
            minRow = maxRow = startRow;
            minCol = maxCol = startCol;
        }
        
        currentMinRow = minRow;
        currentMaxRow = maxRow;
        currentMinCol = minCol;
        currentMaxCol = maxCol;
        
        UpdatePreviewOverlay(validPreviewSprite);
    }

    private void UpdatePreviewOverlay(Sprite overlaySprite)
    {
        if (dragPreviewOverlay == null) return;
        
        GridManager.Instance.GetWorldRect(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, out Vector3 center, out Vector2 size);
        
        float scaleFactor = GridManager.Instance.CellSize / referenceCellSize;
        dragPreviewOverlay.transform.position = center;
        dragPreviewOverlay.transform.localScale = Vector3.one * scaleFactor;
        dragPreviewOverlay.size = size / scaleFactor;
        dragPreviewOverlay.sprite = overlaySprite;
        dragPreviewOverlay.gameObject.SetActive(true);
    }

    private void SpawnRoomPanel(Room room, CatColor color)
    {
        if (roomPanelPrefab == null || catPalette == null) return;
        if (!catPalette.TryGet(color, out var entry)) return;
        
        RoomVisual panel = GetPooledPanel();
        GridManager.Instance.GetWorldRect(room.MinRow, room.MaxRow, room.MinColumn, room.MaxColumn, out Vector3 center, out Vector2 size);
        
        float scaleFactor = GridManager.Instance.CellSize / referenceCellSize;
        panel.SetUp(center, size, entry.panelBoxSprite, entry.panelBoxWallSprite, scaleFactor);
        panel.gameObject.SetActive(true);
        room.Visual = panel;
    }

    private RoomVisual GetPooledPanel()
    {
        foreach (var panel in roomPanelPool)
            if (!panel.gameObject.activeSelf) return panel;
        
        RoomVisual newPanel = Instantiate(roomPanelPrefab, roomVisualRoot);
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
        
        if (GridManager.Instance.HasRoomWithBounds(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol))
            return false;

        int clueCount = 0;
        for (int r = currentMinRow; r <= currentMaxRow; r++)
        {
            for (int c = currentMinCol; c <= currentMaxCol; c++)
            {
                Cell cell = GridManager.Instance.GetCell(r, c);
                if(cell == null) return false;
                if (!cell.HasClue) continue;
                
                clueCount++;
                if(clueCount > 1) return false;
                clueCell = cell;
            }
        }
        
        return clueCount == 1 && clueCell.ClueValue == area;
    }
}
