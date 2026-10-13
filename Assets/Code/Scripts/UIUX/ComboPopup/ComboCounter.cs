using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboCounter : MonoBehaviour
{
    [SerializeField] private RoomBuildController buildController;
    [SerializeField] private ComboPopup popup;
    
    [Header("Popup Position")]
    [SerializeField] private bool showAtRoom = true;
    [SerializeField] private float roomOffsetY = 0f;
    [SerializeField] private Transform fixedAnchor;
    
    public int Combo { get; private set; }
    public event Action<int> ComboChanged;
    
    private readonly Dictionary<Room, int> pendingPopups = new Dictionary<Room, int>();
    
    private void OnEnable()
    {
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
        GridManager.Instance.RoomRemoved += HandleRoomRemoved;
        GameManager.Instance.LevelLoaded += HandleLevelLoaded;
        if (buildController != null)
        {
            buildController.PlacementFailed += ResetCombo;
            buildController.RoomAnimationFinished += HandleRoomAnimationFinished;
        }
    }

    private void OnDisable()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
            GridManager.Instance.RoomRemoved -= HandleRoomRemoved;
        }
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= HandleLevelLoaded;
        if (buildController != null)
        {
            buildController.PlacementFailed -= ResetCombo;
            buildController.RoomAnimationFinished -= HandleRoomAnimationFinished;
        }
    }
    
    private void HandleRoomCommitted(Room room)
    {
        SetCombo(Combo + 1);
        if (Combo >= 2) pendingPopups[room] = Combo;
    }
    
    private void HandleRoomRemoved(Room room) => SetCombo(0);
    
    private void HandleRoomAnimationFinished(Room room)
    {
        if (!pendingPopups.TryGetValue(room, out int combo)) return;
        pendingPopups.Remove(room);

        if (popup != null) popup.TryShow(combo, ResolvePopupPosition(room));
    }

    private void HandleLevelLoaded()
    {
        SetCombo(0);
        if (popup != null) popup.Hide();
    }

    private void ResetCombo() => SetCombo(0);

    private void SetCombo(int value)
    {
        if (value == 0) pendingPopups.Clear();

        if (Combo == value) return;
        Combo = value;
        ComboChanged?.Invoke(Combo);
    }

    private Vector3 ResolvePopupPosition(Room room)
    {
        Vector3 popupPosition = popup.transform.position;

        if (!showAtRoom)
        {
            if (fixedAnchor != null) popupPosition = new Vector3(fixedAnchor.position.x, fixedAnchor.position.y, popupPosition.z);
            return popupPosition;
        }

        GridManager.Instance.GetWorldRect(room.MinRow, room.MaxRow, room.MinColumn, room.MaxColumn,
            out Vector3 center, out Vector2 size);
        return new Vector3(center.x, center.y + roomOffsetY, popupPosition.z);
    }
}
