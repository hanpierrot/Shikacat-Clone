using System;
using UnityEngine;
using UnityEngine.UI;

public class BoosterButton : MonoBehaviour
{
    [SerializeField] private BoosterType type;
    [SerializeField] private Button button;
    [SerializeField] private GameObject unlockedRoot;
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject amountBadge;
    [SerializeField] private Text countText;
    [SerializeField] private GameObject addIcon;
    [SerializeField] private GameObject activeMark;
    [SerializeField] private Text unlockLevelText;
    [SerializeField] private Color lockedTint = new Color(0.4f, 0.4f, 0.45f, 1f);   // Màu làm tối khi chưa dùng được

    private bool dimmed;
    private bool dimInitialized;

    // Tutorial bật cờ này để nút vẫn bấm được dù level chưa bắt đầu
    public bool ForceUsable { get; set; }

    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        BoosterInventory.Changed += HandleInventoryChanged;
        BoosterManager.Instance.Unlocked += HandleUnlocked;
        Refresh();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        BoosterInventory.Changed -= HandleInventoryChanged;
        if (BoosterManager.Instance != null) BoosterManager.Instance.Unlocked -= HandleUnlocked;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        bool unlocked = BoosterManager.Instance.IsUnlocked(type);

        // Mới vào màn: chưa dùng được (và tối đi) cho tới khi người chơi chạm vào ô đầu tiên
        bool started = gm != null && gm.LevelStarted;
        bool usable = unlocked && gm != null && !gm.IsGameOver && (started || ForceUsable);
        if (button.interactable != usable) button.interactable = usable;

        bool shouldDim = unlocked && gm != null && !gm.IsGameOver && !started && !ForceUsable;
        if (!dimInitialized || dimmed != shouldDim) ApplyDim(shouldDim);
    }

    private void ApplyDim(bool dim)
    {
        dimmed = dim;
        dimInitialized = true;
        Color tint = dim ? lockedTint : Color.white;

        // CanvasRenderer.SetColor nhân vào màu đỉnh, không đụng tới màu Graphic nên không xung đột với hiệu ứng khác
        foreach (CanvasRenderer cr in GetComponentsInChildren<CanvasRenderer>(true))
            cr.SetColor(tint);
    }

    public event Action Clicked;

    public RectTransform Rect => (RectTransform)transform;

    public void SetActiveMark(bool active)
    {
        if (activeMark != null) activeMark.SetActive(active);
    }

    private void OnClick()
    {
        Clicked?.Invoke();   // Tutorial gỡ overlay trước khi booster chạy
        BoosterManager.Instance.TryUse(type);
    }

    private void HandleInventoryChanged(BoosterType changed, int count)
    {
        if (changed == type) Refresh();
    }

    private void HandleUnlocked(BoosterType unlocked)
    {
        if (unlocked == type) Refresh();
    }

    private void Refresh()
    {
        BoosterManager.Entry entry = BoosterManager.Instance.GetEntry(type);
        bool unlocked = BoosterManager.Instance.IsUnlocked(type);
        int count = BoosterInventory.Count(type);

        if (unlockedRoot != null) unlockedRoot.SetActive(unlocked);
        if (lockedRoot != null) lockedRoot.SetActive(!unlocked);
        if (unlockLevelText != null && entry != null) unlockLevelText.text = $"LV {entry.unlockLevel}";
        if (iconImage != null && entry != null && entry.icon != null) iconImage.sprite = entry.icon;

        bool hasStock = count > 0;
        if (amountBadge != null) amountBadge.SetActive(unlocked && hasStock);
        if (countText != null) countText.text = count.ToString();
        if (addIcon != null) addIcon.SetActive(unlocked && !hasStock);
    }
}
