using System.Collections;
using UnityEngine;

// Level đầu tiên có clue của một mechanic mới (Directional, Locked, Hidden): hiện popup giải thích rồi mới cho chơi.
// Dò theo nội dung level nên không phụ thuộc số level trong config.
public class MechanicIntro : MonoBehaviour
{
    public enum Mechanic { Directional = 0, Locked = 1, Hidden = 2 }

    [SerializeField] private MechanicIntroPopup popup;
    [SerializeField] private float extraDelay = 0.25f;   // Chờ thêm sau khi lưới phóng to xong

    private const string SeenKey = "Shikacat.MechanicIntro.";

    private Coroutine routine;
    private bool showing;
    private bool previousInputBlocked;
    private Mechanic currentMechanic;

    // BoosterTutorial chờ cờ này để hai hướng dẫn không hiện chồng lên nhau
    public static bool IsBusy { get; private set; }

    public static void ResetSeen()
    {
        for (int i = 0; i < 3; i++)
            PlayerPrefs.DeleteKey(SeenKey + i);
        PlayerPrefs.Save();
    }

    private static bool Seen(Mechanic m) => PlayerPrefs.GetInt(SeenKey + (int)m, 0) == 1;

    private static void MarkSeen(Mechanic m)
    {
        PlayerPrefs.SetInt(SeenKey + (int)m, 1);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        IsBusy = false;
        GameManager.Instance.LevelLoaded += HandleLevelLoaded;
        popup.Closed += HandleClosed;
        popup.gameObject.SetActive(false);

        // GameManager.Start có thể đã nạp level trước khi script này đăng ký (thứ tự Start không cố định)
        if (GameManager.Instance.HasLevelLoaded) HandleLevelLoaded();
    }

    private void OnDestroy()
    {
        IsBusy = false;
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= HandleLevelLoaded;
        if (popup != null) popup.Closed -= HandleClosed;
    }

    private void HandleLevelLoaded()
    {
        Close(false);

        GameManager gm = GameManager.Instance;
        if (gm.IsEditorStart || gm.IsTutorial) return;

        if (!TryFindUnseen(GridManager.Instance.Level, out Mechanic mechanic)) return;

        IsBusy = true;   // Đặt ngay để BoosterTutorial biết phải chờ
        routine = StartCoroutine(ShowRoutine(mechanic));
    }

    private static bool TryFindUnseen(LevelData level, out Mechanic found)
    {
        found = default;
        if (level == null) return false;

        bool directional = false, locked = false, hidden = false;
        foreach (LevelData.ClueEntry clue in level.clues)
        {
            if (clue.type == ClueType.Locked) locked = true;
            if (clue.type == ClueType.Hidden) hidden = true;
            if (clue.type == ClueType.Horizontal || clue.type == ClueType.Vertical || clue.type == ClueType.Square
                || clue.shape != ClueShape.None)
                directional = true;
        }

        // Nhiều mechanic cùng có mặt thì hiện lần lượt, mỗi level một cái
        if (directional && !Seen(Mechanic.Directional)) { found = Mechanic.Directional; return true; }
        if (locked && !Seen(Mechanic.Locked)) { found = Mechanic.Locked; return true; }
        if (hidden && !Seen(Mechanic.Hidden)) { found = Mechanic.Hidden; return true; }
        return false;
    }

    private IEnumerator ShowRoutine(Mechanic mechanic)
    {
        yield return new WaitForSeconds(GridManager.Instance.CellIntroDuration + extraDelay);   // Chờ lưới phóng to xong
        routine = null;

        GameManager gm = GameManager.Instance;
        if (gm.IsGameOver || gm.IsTutorial)
        {
            IsBusy = false;
            yield break;
        }

        showing = true;
        currentMechanic = mechanic;
        gm.TimerPaused = true;
        previousInputBlocked = GridInputHandler.Blocked;
        GridInputHandler.Blocked = true;

        popup.Show(mechanic);
    }

    private void HandleClosed() => Close(true);

    private void Close(bool markSeen)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (!showing)
        {
            IsBusy = false;
            return;
        }

        showing = false;
        popup.Hide();
        GridInputHandler.Blocked = previousInputBlocked;
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = false;

        if (markSeen) MarkSeen(currentMechanic);
        IsBusy = false;
    }
}
