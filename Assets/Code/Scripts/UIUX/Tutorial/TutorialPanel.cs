using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] private Text bubbleText;
    [SerializeField] private RectTransform bubble;
    [SerializeField] private Image[] dots;
    [SerializeField] private Color dotActive = new Color(0.4f, 0.4f, 0.65f, 1f);
    [SerializeField] private Color dotIdle = new Color(0.1f, 0.1f, 0.25f, 1f);
    [SerializeField] private Button skipButton;
    [SerializeField] private RectTransform hand;   // Neo giữa, pivot đặt ở đầu ngón tay

    [Header("Hand")]
    [SerializeField] private float pressTime = 0.15f;
    [SerializeField] private float moveTime = 0.8f;
    [SerializeField] private float pauseTime = 0.6f;

    public event Action SkipClicked;

    private Sequence handSequence;

    private void Awake()
    {
        skipButton.onClick.AddListener(() => SkipClicked?.Invoke());
    }

    private void OnDisable()
    {
        StopHand();
    }

    public void SetStep(int index, string text)
    {
        for (int i = 0; i < dots.Length; i++)
            dots[i].color = i == index ? dotActive : dotIdle;

        bubbleText.text = text;
        bubble.DOKill();
        bubble.localScale = Vector3.one * 0.9f;
        bubble.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).SetTarget(bubble);
    }

    // Bàn tay kéo lần lượt qua từng đoạn (from -> to, toạ độ thế giới), lặp 'repeat' lần rồi tắt hẳn
    public void PlayHand(IList<Vector3> from, IList<Vector3> to, Camera worldCamera, int repeat)
    {
        StopHand();
        if (from.Count == 0 || repeat <= 0) return;

        hand.gameObject.SetActive(true);
        handSequence = DOTween.Sequence().SetTarget(hand);

        for (int r = 0; r < repeat; r++)
        {
            for (int i = 0; i < from.Count; i++)
            {
                Vector2 a = ToLocal(from[i], worldCamera);
                Vector2 b = ToLocal(to[i], worldCamera);

                handSequence.AppendCallback(() =>
                {
                    hand.anchoredPosition = a;
                    hand.localScale = Vector3.one;
                });
                handSequence.Append(hand.DOScale(0.85f, pressTime));
                handSequence.Append(DOTween.To(() => hand.anchoredPosition, v => hand.anchoredPosition = v, b, moveTime)
                    .SetEase(Ease.InOutSine));
                handSequence.Append(hand.DOScale(1f, pressTime));
                handSequence.AppendInterval(pauseTime);
            }
        }

        handSequence.OnComplete(() =>
        {
            handSequence = null;
            hand.gameObject.SetActive(false);
        });
    }

    public void StopHand()
    {
        if (handSequence != null && handSequence.IsActive()) handSequence.Kill();
        handSequence = null;
        hand.DOKill();
        hand.gameObject.SetActive(false);
    }

    private Vector2 ToLocal(Vector3 world, Camera worldCamera)
    {
        Vector2 screen = worldCamera.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hand.parent, screen, null, out Vector2 local);
        return local;
    }
}
