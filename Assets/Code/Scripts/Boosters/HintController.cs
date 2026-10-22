using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HintController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private Color color = new Color(1f, 0.92f, 0.3f, 0.55f);
    [SerializeField] private float duration = 3f;
    [SerializeField] private float pulseAlpha = 0.25f;
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private BoosterButton button;

    public event System.Action<bool> NoHintAvailable;   // bool: bị chặn vì clue khóa chưa mở

    private Tween pulse;
    private Tween hideTimer;
    private Sprite fallbackSprite;
    private readonly List<LevelData.SolutionRoom> free = new List<LevelData.SolutionRoom>();
    private readonly List<LevelData.SolutionRoom> blocked = new List<LevelData.SolutionRoom>();

    public bool IsShowing { get; private set; }

    private void Start()
    {
        BoosterManager.Instance.SetHandler(BoosterType.Hint, TryHint);
        GameManager.Instance.LevelLoaded += Hide;
        GridManager.Instance.RoomCommitted += HandleRoomChanged;
        GridManager.Instance.RoomRemoved += HandleRoomChanged;
        Hide();
    }

    private void OnDestroy()
    {
        pulse?.Kill();
        hideTimer?.Kill();
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= Hide;
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RoomCommitted -= HandleRoomChanged;
            GridManager.Instance.RoomRemoved -= HandleRoomChanged;
        }
    }

    private void Update()
    {
        if (IsShowing && GameManager.Instance.IsGameOver) Hide();
    }

    private void HandleRoomChanged(Room _) => Hide();

    private bool TryHint()
    {
        if (IsShowing) return false;

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

            bool alreadyPlaced = false;
            bool overlaps = false;
            bool clueCovered = false;
            LevelData.ClueEntry clue = level.clues[sol.clueIndex];

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

            if (RoomRules.IsLocked(clue.type, clue.unlockRoomCount, clueCovered, rooms.Count))
            {
                lockedSkipped++;
                continue;
            }

            (overlaps ? blocked : free).Add(sol);
        }

        List<LevelData.SolutionRoom> pool = free.Count > 0 ? free : blocked;

        // Không có phòng nào gợi ý được: chỉ return, không hiện khung và không trừ booster
        if (pool.Count == 0)
        {
            NoHintAvailable?.Invoke(lockedSkipped > 0);
            return false;
        }

        Show(pool[Random.Range(0, pool.Count)]);
        return true;
    }

    private void Show(LevelData.SolutionRoom sol)
    {
        GridManager.Instance.GetWorldRect(sol.row, sol.row + sol.height - 1, sol.col, sol.col + sol.width - 1,
            out Vector3 center, out Vector2 size);

        if (highlight.sprite == null) highlight.sprite = GetFallbackSprite();

        Transform t = highlight.transform;
        t.position = center;
        t.localScale = new Vector3(size.x, size.y, 1f);
        highlight.sortingOrder = sortingOrder;
        highlight.color = color;
        highlight.gameObject.SetActive(true);

        IsShowing = true;
        if (button != null) button.SetActiveMark(true);

        pulse?.Kill();
        pulse = DOTween.To(() => highlight.color.a, a => { Color c = highlight.color; c.a = a; highlight.color = c; },
                pulseAlpha, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetTarget(this);

        hideTimer?.Kill();
        hideTimer = DOVirtual.DelayedCall(duration, Hide);
    }

    private void Hide()
    {
        pulse?.Kill();
        hideTimer?.Kill();
        IsShowing = false;
        if (highlight != null) highlight.gameObject.SetActive(false);
        if (button != null) button.SetActiveMark(false);
    }

    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            fallbackSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        return fallbackSprite;
    }
}
