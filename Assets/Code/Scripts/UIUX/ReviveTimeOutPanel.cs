using System;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public enum LoseReason
{
    TimeOut = 0,
    OutOfMoves = 1
}

// Popup hồi sinh: hiện khi thua. Chỉ hiển thị và phát sự kiện; việc trừ coin, cộng giờ/lượt do GameManager xử lý
public class ReviveTimeOutPanel : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private Button playOnButton;       // Button Gold
    [SerializeField] private Button closeButton;        // Header/Button Close
    [SerializeField] private Text titleText;            // Header/Text (Legacy)
    [SerializeField] private Text descText;             // Content/decs
    [SerializeField] private Text priceText;            // Button Gold/.../Price
    [SerializeField] private SkeletonGraphic cat;       // Content/cat/catanim

    [Header("Holders")]
    [SerializeField] private GameObject timeoutHolder;
    [SerializeField] private GameObject outOfMovesHolder;
    [SerializeField] private Text timeoutBonusText;
    [SerializeField] private Text movesBonusText;

    [Header("Animation")]
    [SerializeField] private float popTime = 0.25f;

    public event Action ReviveRequested;   // Người chơi bấm "Play On"
    public event Action Declined;          // Người chơi bấm nút đóng

    private Color priceColor = Color.white;
    private bool colorCached;

    private void Awake()
    {
        playOnButton.onClick.AddListener(() => ReviveRequested?.Invoke());
        closeButton.onClick.AddListener(() => Declined?.Invoke());
    }

    public void Show(LoseReason reason, int price, int bonus)
    {
        CachePriceColor();
        DOTween.Kill(this);

        gameObject.SetActive(true);
        timeoutHolder.SetActive(reason == LoseReason.TimeOut);
        outOfMovesHolder.SetActive(reason == LoseReason.OutOfMoves);

        switch (reason)
        {
            case LoseReason.TimeOut:
                titleText.text = "Time Out!";
                descText.text = $"Add +{bonus} seconds to keep playing";
                timeoutBonusText.text = $"+{bonus}s";
                break;
            case LoseReason.OutOfMoves:
                titleText.text = "Out of Moves!";
                descText.text = $"Add +{bonus} moves to keep playing";
                movesBonusText.text = $"+{bonus}";
                break;
        }

        priceText.text = price.ToString();
        priceText.color = priceColor;
        playOnButton.interactable = true;
        closeButton.interactable = true;

        if (cat != null) cat.AnimationState.SetAnimation(0, "boom_fail", true);

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, popTime).SetEase(Ease.OutBack).SetTarget(this);
    }

    public void Hide()
    {
        DOTween.Kill(this);
        playOnButton.transform.DOKill();
        gameObject.SetActive(false);
    }

    // Không đủ coin: nút rung nhẹ và giá chuyển đỏ trong chốc lát
    public void ShowNotEnoughCoins()
    {
        CachePriceColor();

        playOnButton.transform.DOKill(true);
        playOnButton.transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 8, 0.6f).SetTarget(this);

        priceText.color = new Color(1f, 0.35f, 0.3f);
        DOVirtual.DelayedCall(0.6f, () =>
        {
            if (priceText != null) priceText.color = priceColor;
        }).SetTarget(this);
    }

    private void CachePriceColor()
    {
        if (colorCached) return;
        priceColor = priceText.color;
        colorCached = true;
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
    }
}
