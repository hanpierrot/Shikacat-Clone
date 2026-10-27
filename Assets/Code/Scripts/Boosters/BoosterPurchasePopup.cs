using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoosterPurchasePopup : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text descText;
    [SerializeField] private Text priceText;
    [SerializeField] private Text amountText;
    [SerializeField] private Image iconImage;
    [SerializeField] private CoinHud coinHud;

    [SerializeField] private Button buyButton;
    [SerializeField] private Button cancelButton;

    private BoosterType current;
    private Color priceColor;
    private bool colorCached;

    private void Awake()
    {
        buyButton.onClick.AddListener(OnBuy);
        cancelButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (coinHud != null) coinHud.gameObject.SetActive(true);
    }

    public void Show(BoosterType type, int price, int pack)
    {
        current = type;
        BoosterManager.Entry entry = BoosterManager.Instance.GetEntry(type);

        if (!colorCached && priceText != null)
        {
            priceColor = priceText.color;
            colorCached = true;
        }

        if (titleText != null) titleText.text = DisplayName(type);
        if (descText != null && entry != null) descText.text = entry.description;
        if (amountText != null) amountText.text = $"x{pack}";
        if (priceText != null)
        {
            priceText.text = price.ToString();
            priceText.color = priceColor;
        }
        if (iconImage != null && entry != null && entry.icon != null) iconImage.sprite = entry.icon;

        gameObject.SetActive(true);

        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = true;
    }

    private void OnBuy()
    {
        if (BoosterManager.Instance.TryBuy(current))
        {
            AudioManager.PlaySfx(Sfx.EarnReward);
            Close();
            return;
        }

        buyButton.transform.DOKill(true);
        buyButton.transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 8, 0.6f);
        if (priceText != null)
        {
            priceText.color = new Color(1f, 0.35f, 0.3f);
            DOVirtual.DelayedCall(0.6f, () =>
            {
                if (priceText != null) priceText.color = priceColor;
            }).SetTarget(this);
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (coinHud != null) coinHud.gameObject.SetActive(false);
        DOTween.Kill(this);
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = false;
    }

    private static string DisplayName(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Hint: return "HINT";
            case BoosterType.Freeze: return "FREEZE";
            default: return "REVEAL";
        }
    }
}
