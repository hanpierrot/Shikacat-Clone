using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoosterTutorialPanel : MonoBehaviour
{
    [SerializeField] private RectTransform glow;
    [SerializeField] private RectTransform hand;       // Pivot đặt ở đầu ngón tay; có Canvas override sorting cao hơn nút
    [SerializeField] private Text bubbleText;
    [SerializeField] private Vector2 handOffset = new Vector2(30f, -10f);
    [SerializeField] private float bobDistance = 24f;
    [SerializeField] private float bobTime = 0.5f;

    private Tween bobTween;

    public void Show(RectTransform target, string text)
    {
        gameObject.SetActive(true);
        bubbleText.text = text;

        var root = (RectTransform)transform;
        Vector2 local = root.InverseTransformPoint(target.position);   // Canvas Overlay: vị trí thế giới của UI chính là toạ độ màn hình
        glow.anchoredPosition = local;
        Vector2 start = local + handOffset;
        hand.anchoredPosition = start;

        bobTween?.Kill();
        bobTween = DOTween.To(() => hand.anchoredPosition, v => hand.anchoredPosition = v,
                start + new Vector2(bobDistance, -bobDistance), bobTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void Hide()
    {
        bobTween?.Kill();
        bobTween = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        bobTween?.Kill();
        bobTween = null;
    }
}
