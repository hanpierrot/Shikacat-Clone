using DG.Tweening;
using TMPro;
using UnityEngine;

public class CoinHud : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private RectTransform coinIcon;           // Đích bay của coin khi nhận thưởng
    [SerializeField] private CollectButton collectButton;      // Nghe Clicked để chạy animation cộng

    [Header("Animation")]
    [SerializeField] private float countDelay = 0.4f;          // Chờ coin bay tới nơi rồi mới bắt đầu đếm
    [SerializeField] private float countTime = 0.6f;

    private bool held;                 // Đang giữ số hiển thị cũ vì phần thưởng chưa "nhận"
    private int displayed;
    private int heldValue;            // Số dư trước khi cộng thưởng
    private Tween countTween;

    private void OnEnable()
    {
        displayed = held ? heldValue : CoinWallet.Balance;   // Đang chờ nhận thưởng thì vẫn hiện số cũ
        Render(displayed);

        CoinWallet.Changed += HandleChanged;
        if (collectButton != null) collectButton.Clicked += HandleCollectClicked;
    }

    private void OnDisable()
    {
        CoinWallet.Changed -= HandleChanged;
        if (collectButton != null) collectButton.Clicked -= HandleCollectClicked;
        KillCount();
    }

    // Gọi TRƯỚC khi cộng thưởng để số trên HUD chưa nhảy ngay
    public void HoldDisplay()
    {
        held = true;
        heldValue = CoinWallet.Balance;
        displayed = heldValue;
        Render(displayed);
    }

    // Hiện đúng số dư. animate = true thì đếm lên dần
    public void Release(bool animate)
    {
        held = false;
        KillCount();

        int target = CoinWallet.Balance;
        if (!animate || target == displayed)
        {
            displayed = target;
            Render(displayed);
            return;
        }

        int from = displayed;
        countTween = DOVirtual.Float(from, target, countTime, v =>
        {
            displayed = Mathf.RoundToInt(v);
            Render(displayed);
        }).SetDelay(countDelay).SetTarget(transform).OnComplete(() =>
        {
            displayed = target;
            Render(displayed);
        });

        transform.DOPunchScale(Vector3.one * 0.1f, countTime, 6, 0.6f).SetDelay(countDelay).SetTarget(transform);
    }

    private void HandleChanged(int balance)
    {
        if (held) return;   // Đang chờ nhận thưởng: giữ số cũ

        displayed = balance;
        Render(displayed);
    }

    private void HandleCollectClicked()
    {
        Release(true);
    }

    private void Render(int value)
    {
        if (amountText != null) amountText.text = value.ToString();
    }

    private void KillCount()
    {
        if (countTween != null && countTween.IsActive()) countTween.Kill();
        countTween = null;
        transform.DOKill();
    }
}
