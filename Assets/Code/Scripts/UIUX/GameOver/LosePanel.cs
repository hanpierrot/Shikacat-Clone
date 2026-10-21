using System;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

// Panel "Level Failed" hiện khi người chơi thua và bỏ qua (hoặc không có) hồi sinh
public class LosePanel : MonoBehaviour
{
    [SerializeField] private Text titleText;          // Ribbon/title
    [SerializeField] private Text livesLostText;      // Holder/bg/lose/Image/Text (Legacy)
    [SerializeField] private GameObject heartGroup;   // Holder/bg/lose/Image (trái tim đỏ), chỉ hiện khi có trừ tim
    [SerializeField] private Button retryButton;      // Retry
    [SerializeField] private Button closeButton;      // Ribbon/Close
    [SerializeField] private SkeletonGraphic cat;     // Holder/bg/Image/Graphic

    [SerializeField] private float popTime = 0.25f;

    public event Action RetryRequested;
    public event Action Closed;

    private void Awake()
    {
        retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
        closeButton.onClick.AddListener(() => Closed?.Invoke());
    }

    // livesLost = 0 thì ẩn trái tim (game hiện chưa có hệ thống tim)
    public void Show(int level, int livesLost)
    {
        DOTween.Kill(this);
        gameObject.SetActive(true);

        titleText.text = $"Level {level}";
        livesLostText.text = $"-{livesLost}";
        if (heartGroup != null) heartGroup.SetActive(livesLost > 0);

        if (cat != null) cat.AnimationState.SetAnimation(0, "animation", true);

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, popTime).SetEase(Ease.OutBack).SetTarget(this);
    }

    public void Hide()
    {
        DOTween.Kill(this);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
    }
}
