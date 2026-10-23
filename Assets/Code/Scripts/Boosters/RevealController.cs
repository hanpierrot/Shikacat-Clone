using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevealController : MonoBehaviour
{
    public enum RejectReason { Locked, AlreadyBuilt, NotNormal }

    private enum State { Ready, Built, Locked }

    [SerializeField] private RoomBuildController buildController;
    [SerializeField] private BoosterButton button;
    [SerializeField] private float buildInterval = 0.12f;

    public event Action<RejectReason> Rejected;

    public bool IsArmed { get; private set; }

    private bool building;
    private Coroutine buildRoutine;

    private void Start()
    {
        BoosterManager.Instance.SetHandler(BoosterType.Reveal, ToggleArm);
        GameManager.Instance.LevelLoaded += ResetState;
        buildController.PressInterceptor = HandlePress;
        Disarm();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= ResetState;
        if (buildController != null && buildController.PressInterceptor == HandlePress)
            buildController.PressInterceptor = null;
    }

    private void Update()
    {
        if (IsArmed && GameManager.Instance.IsGameOver) Disarm();
    }

    // Luôn trả false: arm/hủy không trừ kho, kho chỉ trừ khi dựng thành công
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
        SetClueHighlight(true);
    }

    private void Disarm()
    {
        IsArmed = false;
        if (button != null) button.SetActiveMark(false);
        SetClueHighlight(false);
    }

    // Level mới: dừng mọi việc đang chạy
    private void ResetState()
    {
        if (buildRoutine != null) StopCoroutine(buildRoutine);
        buildRoutine = null;
        building = false;
        Disarm();
    }

    private static State Evaluate(GridManager grid, LevelData level, LevelData.SolutionRoom sol)
    {
        int maxRow = sol.row + sol.height - 1;
        int maxCol = sol.col + sol.width - 1;
        LevelData.ClueEntry clue = level.clues[sol.clueIndex];

        foreach (Room room in grid.Rooms)
            if (room.MinRow == sol.row && room.MaxRow == maxRow
                && room.MinColumn == sol.col && room.MaxColumn == maxCol)
                return State.Built;

        bool covered = grid.GetCell(clue.row, clue.col).AssignedRoom != null;
        return RoomRules.IsLocked(clue.type, clue.unlockRoomCount, covered, grid.Rooms.Count) ? State.Locked : State.Ready;
    }

    // Chỉ clue Normal mới được highlight và được Reveal
    private void SetClueHighlight(bool on)
    {
        GridManager grid = GridManager.Instance;
        LevelData level = grid != null ? grid.Level : null;
        if (level == null) return;

        foreach (LevelData.SolutionRoom sol in level.solution)
        {
            LevelData.ClueEntry clue = level.clues[sol.clueIndex];
            Cell cell = grid.GetCell(clue.row, clue.col);
            if (cell == null) continue;

            cell.SetHighlight(on && clue.type == ClueType.Normal && Evaluate(grid, level, sol) == State.Ready);
        }
    }

    private bool HandlePress(int row, int col)
    {
        if (!IsArmed) return false;
        if (building) return true;

        GridManager grid = GridManager.Instance;
        LevelData level = grid.Level;
        if (level == null || !level.TryGetClue(row, col, out LevelData.ClueEntry tapped)) return true;

        if (tapped.type != ClueType.Normal)
        {
            Rejected?.Invoke(RejectReason.NotNormal);
            return true;
        }

        // Mọi phòng đáp án có clue Normal cùng giá trị với clue được chọn
        var targets = new List<LevelData.SolutionRoom>();
        bool anyReady = false;
        foreach (LevelData.SolutionRoom sol in level.solution)
        {
            LevelData.ClueEntry clue = level.clues[sol.clueIndex];
            if (clue.type != ClueType.Normal || clue.value != tapped.value) continue;

            State state = Evaluate(grid, level, sol);
            if (state == State.Built) continue;

            targets.Add(sol);
            if (state == State.Ready) anyReady = true;
        }

        if (targets.Count == 0)
        {
            Rejected?.Invoke(RejectReason.AlreadyBuilt);
            return true;
        }

        if (!anyReady)
        {
            Rejected?.Invoke(RejectReason.Locked);
            return true;
        }

        // Chỉ trừ một lần, trước khi dựng vì phòng cuối có thể thắng ngay
        BoosterManager.Instance.Consume(BoosterType.Reveal);
        Disarm();
        buildRoutine = StartCoroutine(BuildAll(targets));
        return true;
    }

    private IEnumerator BuildAll(List<LevelData.SolutionRoom> pending)
    {
        GridManager grid = GridManager.Instance;
        LevelData level = grid.Level;
        building = true;

        bool progress = true;
        while (pending.Count > 0 && progress && !GameManager.Instance.IsGameOver)
        {
            progress = false;

            for (int i = 0; i < pending.Count; i++)
            {
                LevelData.SolutionRoom sol = pending[i];
                if (Evaluate(grid, level, sol) != State.Ready) continue;   // Clue khóa: chờ đến khi đủ phòng

                int maxRow = sol.row + sol.height - 1;
                int maxCol = sol.col + sol.width - 1;
                float waited = 0f;
                while (grid.OverlapsPlacingRoom(sol.row, maxRow, sol.col, maxCol) && waited < 1f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                buildController.BuildSolutionRoom(sol);
                pending.RemoveAt(i);
                i--;
                progress = true;

                if (GameManager.Instance.IsGameOver) break;
                yield return new WaitForSeconds(buildInterval);
            }
        }

        building = false;
        buildRoutine = null;
    }
}
