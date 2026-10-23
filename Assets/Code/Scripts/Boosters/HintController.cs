using System;
using System.Collections.Generic;
using UnityEngine;

public class HintController : MonoBehaviour
{
    [SerializeField] private RoomBuildController buildController;

    public event Action<bool> NoHintAvailable;   // bool: bị chặn vì clue khóa chưa mở

    private readonly List<LevelData.SolutionRoom> free = new List<LevelData.SolutionRoom>();
    private readonly List<LevelData.SolutionRoom> blocked = new List<LevelData.SolutionRoom>();

    private void Start()
    {
        BoosterManager.Instance.SetHandler(BoosterType.Hint, TryHint);
    }

    // Dựng luôn một phòng đáp án đúng: ưu tiên phòng không đè phòng sai, bỏ qua clue khóa chưa mở
    private bool TryHint()
    {
        GridManager grid = GridManager.Instance;
        LevelData level = grid.Level;
        if (level == null || level.solution == null) return false;

        IReadOnlyList<Room> rooms = grid.Rooms;
        free.Clear();
        blocked.Clear();
        int lockedSkipped = 0;

        foreach (LevelData.SolutionRoom sol in level.solution)
        {
            int maxRow = sol.row + sol.height - 1;
            int maxCol = sol.col + sol.width - 1;
            LevelData.ClueEntry clue = level.clues[sol.clueIndex];

            bool alreadyPlaced = false;
            bool overlaps = false;
            bool clueCovered = false;

            foreach (Room room in rooms)
            {
                if (room.MinRow == sol.row && room.MaxRow == maxRow
                    && room.MinColumn == sol.col && room.MaxColumn == maxCol)
                {
                    alreadyPlaced = true;
                    break;
                }

                if (room.Overlaps(sol.row, maxRow, sol.col, maxCol)) overlaps = true;
                if (room.Overlaps(clue.row, clue.row, clue.col, clue.col)) clueCovered = true;
            }

            if (alreadyPlaced) continue;
            if (grid.OverlapsPlacingRoom(sol.row, maxRow, sol.col, maxCol)) continue;   // Đang animation đặt phòng

            if (RoomRules.IsLocked(clue.type, clue.unlockRoomCount, clueCovered, rooms.Count))
            {
                lockedSkipped++;
                continue;
            }

            (overlaps ? blocked : free).Add(sol);
        }

        List<LevelData.SolutionRoom> pool = free.Count > 0 ? free : blocked;
        if (pool.Count == 0)
        {
            NoHintAvailable?.Invoke(lockedSkipped > 0);
            return false;   // Không trừ booster
        }

        buildController.BuildSolutionRoom(pool[UnityEngine.Random.Range(0, pool.Count)]);
        return true;        // BoosterManager sẽ trừ kho
    }
}
