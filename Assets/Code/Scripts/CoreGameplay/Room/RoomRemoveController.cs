using UnityEngine;

public class RoomRemoveController : MonoBehaviour
{
    public bool TryRemoveAt(int row, int col)
    {
        Cell cell = GridManager.Instance.GetCell(row, col);
        if (cell == null || cell.AssignedRoom == null) return false;

        GridManager.Instance.RemoveRoom(cell.AssignedRoom);
        return true;
    }
}
