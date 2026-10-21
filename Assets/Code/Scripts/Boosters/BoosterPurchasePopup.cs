using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoosterPurchasePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite[] icons;
    
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

    public void Show(BoosterType type, int price, int pack)
    {
        current = type;

        if (!colorCached && priceText != null)
        {
            priceColor = priceText.color;
            colorCached = true;
        }
        
        if (titleText != null) titleText.text = DisplayName(type);
        if (descText != null) descText.text = pack > 1 ? $"Buy {pack} for {price} coins?" : $"Buy 1 for {price} coins?";
        if (priceText != null)
        {
            priceText.text = price.ToString();
            priceText.color = priceColor;
        }
        if (iconImage != null && icons != null && (int)type < icons.Length && icons[(int)type] != null)
            iconImage.sprite = icons[(int)type];

        gameObject.SetActive(true);
        
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = true;
    }

    private void OnBuy()
    {
        if (BoosterManager.Instance.TryBuy(current))
        {
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
        DOTween.Kill(this);
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = false;
    }

    private static string DisplayName(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Hint: return "Hint";
            case BoosterType.Freeze: return "Freeze";
            default: return "Auto Build";
        }
    }
}
