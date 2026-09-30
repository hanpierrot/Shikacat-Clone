using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomRemoveController : MonoBehaviour
{
    private bool canRemoveRoomOnClick = true;

    public bool TryRemoveAt(int row, int col)
    {
        if (!canRemoveRoomOnClick) return false;

        Cell cell = GridManager.Instance.GetCell(row, col);
        if (cell == null || cell.AssignedRoom == null) return false;

        GridManager.Instance.RemoveRoom(cell.AssignedRoom);
        return true;
    }
}
