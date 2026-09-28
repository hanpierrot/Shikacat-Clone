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

    private int startRow, startCol;
    private int currentMinRow, currentMaxRow, currentMinCol, currentMaxCol;

    private void Awake()
    {
        if (inputHandler == null) inputHandler = GetComponent<GridInputHandler>();
    }

    private void OnEnable()
    {
        inputHandler.DragStarted += TryBeginDrag;
        inputHandler.DragMoved += UpdateDrag;
        inputHandler.DragEnded += EndDrag;
    }

    private void OnDisable()
    {
        inputHandler.DragStarted -= TryBeginDrag;
        inputHandler.DragMoved -= UpdateDrag;
        inputHandler.DragEnded -= EndDrag;
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
        ClearPreviewHighlight();

        if (TryValidateSelection(out Cell clueCell))
        {
            gridManager.CommitRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, clueCell);
        }
    }

    private void SetPreviewBounds(int minRow, int maxRow, int minCol, int maxCol)
    {
        ClearPreviewHighlight();
        currentMinRow = minRow;
        currentMaxRow = maxRow;
        currentMinCol = minCol;
        currentMaxCol = maxCol;
        
        bool isValid = TryValidateSelection(out _);
        for (int r = minRow; r <= maxRow; r++)
            for (int c = minCol; c <= maxCol; c++)
                gridManager.GetCell(r, c)?.SetPreviewHighlight(true, isValid);
    }
    
    private void ClearPreviewHighlight()
    {
        for (int r = currentMinRow; r <= currentMaxRow; r++)
            for (int c = currentMinCol; c <= currentMaxCol; c++)
                gridManager.GetCell(r, c)?.SetPreviewHighlight(false, false);
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
