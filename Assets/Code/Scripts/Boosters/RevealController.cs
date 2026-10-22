using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevealController : MonoBehaviour
{
    public enum RejectReason { Locked, AlreadyBuilt, Busy }

    [SerializeField] private RoomBuildController buildController;
    [SerializeField] private BoosterButton button;

    public event Action<RejectReason> Rejected;

    public bool IsArmed { get; private set; }

    private void Start()
    {
        BoosterManager.Instance.SetHandler(BoosterType.Reveal, ToggleArm);
        GameManager.Instance.LevelLoaded += Disarm;
        buildController.PressInterceptor = HandlePress;
        Disarm();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= Disarm;
        if (buildController != null && buildController.PressInterceptor == HandlePress)
            buildController.PressInterceptor = null;
    }

    private void Update()
    {
        if (IsArmed && GameManager.Instance.IsGameOver) Disarm();
    }

    private bool ToggleArm()
    {
        if (IsArmed) Disarm();
        else Arm();
        return false;
    }

    private void Arm()
    {
        IsArmed = true;
        if (button != null) button.SetActiveMark(true);
    }

    private void Disarm()
    {
        IsArmed = false;
        if (button != null) button.SetActiveMark(false);
    }

    private bool HandlePress(int row, int col)
    {
        if (!IsArmed) return false;

        GridManager grid = GridManager.Instance;
        LevelData level = grid.Level;
        if (level == null || !level.TryGetClue(row, col, out LevelData.ClueEntry clue)) return true;
        
        int found = -1;
        for (int i = 0; i < level.solution.Length; i++)
        {
            LevelData.ClueEntry c = level.clues[level.solution[i].clueIndex];
            if (c.row != row || c.col != col) continue;
            found = i;
            break;
        }
        if (found < 0) return true;

        LevelData.SolutionRoom sol = level.solution[found];
        int maxRow = sol.row + sol.height - 1;
        int maxCol = sol.col + sol.width - 1;
        
        foreach (Room room in grid.Rooms)
        {
            if (room.MinRow == sol.row && room.MaxRow == maxRow
                                       && room.MinColumn == sol.col && room.MaxColumn == maxCol)
            {
                Rejected?.Invoke(RejectReason.AlreadyBuilt);
                return true;
            }
        }

        bool covered = grid.GetCell(row, col).AssignedRoom != null;
        if (RoomRules.IsLocked(clue.type, clue.unlockRoomCount, covered, grid.Rooms.Count))
        {
            Rejected?.Invoke(RejectReason.Locked);
            return true;
        }

        if (grid.OverlapsPlacingRoom(sol.row, maxRow, sol.col, maxCol))
        {
            Rejected?.Invoke(RejectReason.Busy);
            return true;
        }
        
        BoosterManager.Instance.Consume(BoosterType.Reveal);
        Disarm();

        grid.IsRevealing = true;
        try
        {
            buildController.BuildRoom(sol.row, maxRow, sol.col, maxCol, row, col);
        }
        finally
        {
            grid.IsRevealing = false;
        }

        return true;
    }
}
