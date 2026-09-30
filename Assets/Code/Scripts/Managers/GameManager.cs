using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    private void OnEnable()
    {
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
    }

    private void OnDisable()
    {
        GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
    }

    private void HandleRoomCommitted(Room room)
    {
        if (GridManager.Instance.IsGridFull())
            HandleWin();
    }

    private void HandleWin()
    {
        Debug.Log("Victory!");
    }
}
