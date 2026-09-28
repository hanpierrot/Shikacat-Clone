using System;
using UnityEngine;

[RequireComponent(typeof(GridManager))]
public class WinConditionChecker : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    public event Action PuzzleSolved;
    public bool IsSolved { get; private set; }

    private void Awake()
    {
        if (gridManager == null) gridManager = GetComponent<GridManager>();
    }

    private void OnEnable()
    {
        gridManager.RoomCommitted += OnRoomChanged;
        gridManager.RoomRemoved += OnRoomChanged;
    }

    private void OnDisable()
    {
        gridManager.RoomCommitted -= OnRoomChanged;
        gridManager.RoomRemoved -= OnRoomChanged;
    }

    private void OnRoomChanged(Room room)
    {
        CheckWinCondition();
    }

    public void CheckWinCondition()
    {
        if (IsSolved) return;
        if (!gridManager.IsFullyCovered()) return;

        IsSolved = true;
        PuzzleSolved?.Invoke();
    }

    public void ResetState()
    {
        IsSolved = false;
    }
}
