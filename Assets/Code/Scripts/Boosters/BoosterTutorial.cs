using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Lần đầu một booster được mở khóa: tối màn hình, nút booster sáng lên và bàn tay chỉ vào nút.
// Người chơi bấm đúng nút đó thì overlay tắt và booster được dùng luôn.
public class BoosterTutorial : MonoBehaviour
{
    [Serializable]
    private struct Entry
    {
        public BoosterType type;
        public BoosterButton button;
    }

    [SerializeField] private Entry[] entries;
    [SerializeField] private BoosterTutorialPanel panel;
    [SerializeField] private float extraDelay = 0.25f;   // Chờ thêm sau khi lưới phóng to xong
    [SerializeField] private int overrideOrder = 100;

    private const string SeenKey = "Shikacat.BoosterTutorial.";

    private int activeIndex = -1;
    private Coroutine routine;
    private Canvas addedCanvas;
    private GraphicRaycaster addedRaycaster;
    private bool previousInputBlocked;

    public static void ResetSeen()
    {
        for (int i = 0; i < BoosterInventory.Types; i++)
            PlayerPrefs.DeleteKey(SeenKey + i);
        PlayerPrefs.Save();
    }

    private static bool Seen(BoosterType type) => PlayerPrefs.GetInt(SeenKey + (int)type, 0) == 1;

    private static void MarkSeen(BoosterType type)
    {
        PlayerPrefs.SetInt(SeenKey + (int)type, 1);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        GameManager.Instance.LevelLoaded += HandleLevelLoaded;
        panel.gameObject.SetActive(false);

        // GameManager.Start có thể đã nạp level trước khi script này kịp đăng ký sự kiện (thứ tự Start không cố định),
        // nên bắt kịp level hiện tại, nếu không tutorial của level mở khóa sẽ bị mất sau khi vào lại game
        if (GameManager.Instance.HasLevelLoaded) HandleLevelLoaded();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded()
    {
        Dismiss(false);

        GameManager gm = GameManager.Instance;
        if (gm.IsEditorStart || gm.IsTutorial) return;

        // Booster đã mở khóa mà chưa xem hướng dẫn (kể cả khi người chơi thoát giữa chừng rồi vào lại)
        for (int i = 0; i < entries.Length; i++)
        {
            if (!BoosterManager.Instance.IsUnlocked(entries[i].type) || Seen(entries[i].type)) continue;

            routine = StartCoroutine(ShowRoutine(i));
            return;
        }
    }

    private IEnumerator ShowRoutine(int index)
    {
        // Chờ lưới tạo xong (các ô phóng to hết)
        yield return new WaitForSeconds(GridManager.Instance.CellIntroDuration + extraDelay);
        routine = null;

        while (MechanicIntro.IsBusy) yield return null;   // Popup mechanic (ví dụ level 9) hiện trước, tutorial booster sau

        GameManager gm = GameManager.Instance;
        if (gm.IsGameOver || gm.IsTutorial) yield break;

        Entry entry = entries[index];
        BoosterManager.Entry data = BoosterManager.Instance.GetEntry(entry.type);

        activeIndex = index;
        gm.TimerPaused = true;

        // Khóa mọi thao tác trên lưới (lớp tối đã chắn UI); chỉ còn nút booster được nâng lên là bấm được
        previousInputBlocked = GridInputHandler.Blocked;
        GridInputHandler.Blocked = true;

        // Đưa nút lên trên lớp tối: thêm Canvas override sorting và raycaster riêng cho nút
        addedCanvas = entry.button.gameObject.AddComponent<Canvas>();
        addedCanvas.overrideSorting = true;
        addedCanvas.sortingOrder = overrideOrder;
        addedRaycaster = entry.button.gameObject.AddComponent<GraphicRaycaster>();

        entry.button.ForceUsable = true;   // Tutorial hiện ngay đầu level nên nút phải bấm được dù level chưa bắt đầu
        entry.button.Clicked += HandleButtonClicked;
        panel.Show(entry.button.Rect, data != null ? data.description : string.Empty);
    }

    private void HandleButtonClicked() => Dismiss(true);

    private void Dismiss(bool markSeen)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (activeIndex < 0) return;

        Entry entry = entries[activeIndex];
        entry.button.Clicked -= HandleButtonClicked;
        entry.button.ForceUsable = false;

        if (addedRaycaster != null) Destroy(addedRaycaster);   // Raycaster phải bị hủy trước Canvas
        if (addedCanvas != null) Destroy(addedCanvas);
        addedRaycaster = null;
        addedCanvas = null;

        panel.Hide();
        GridInputHandler.Blocked = previousInputBlocked;
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = false;

        if (markSeen) MarkSeen(entry.type);
        activeIndex = -1;
    }
}
