using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboCounter : MonoBehaviour
{
    [SerializeField] private RoomBuildController buildController;
    [SerializeField] private ComboPopup popup;
    [SerializeField] private Vector2 worldOffset = Vector2.zero;
    
    public int Combo { get; private set; }
    public event Action<int> ComboChanged;
   
    private Coroutine prewarmRoutine;
    
    private void OnEnable()
    {
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
        GridManager.Instance.RoomRemoved += HandleRoomRemoved;
        GameManager.Instance.LevelLoaded += HandleLevelLoaded;
        if (buildController != null) buildController.PlacementFailed += ResetCombo;
    }

    private void OnDisable()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
            GridManager.Instance.RoomRemoved -= HandleRoomRemoved;
        }
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= HandleLevelLoaded;
        if (buildController != null) buildController.PlacementFailed -= ResetCombo;
    }
    
    private void HandleRoomCommitted(Room room)
    {
        SetCombo(Combo + 1);
        if (Combo < 2 || popup == null || buildController == null) return;
        
        Vector3 position = buildController.LastReleaseWorldPosition + (Vector3)worldOffset;
        popup.TryShow(Combo, position);
    }
    
    private void HandleRoomRemoved(Room room) => SetCombo(0);
    
    private void HandleLevelLoaded()
    {
        SetCombo(0);
        if (popup == null) return;

        popup.Hide();
        if (prewarmRoutine != null) StopCoroutine(prewarmRoutine);
        prewarmRoutine = StartCoroutine(PrewarmRoutine());
    }
    
    private IEnumerator PrewarmRoutine()
    {
        yield return null;
        while (popup.PrewarmStep())
            yield return null;
        prewarmRoutine = null;
    }

    private void ResetCombo() => SetCombo(0);

    private void SetCombo(int value)
    {
        if (Combo == value) return;
        Combo = value;
        ComboChanged?.Invoke(Combo);
    }
}
