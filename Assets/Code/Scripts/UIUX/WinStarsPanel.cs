using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WinStarsPanel : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    [Header("Animation")]
    [SerializeField] private float panelPopTime = 0.2f;
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private float stagger = 0.2f;     // Khoảng cách giữa hai sao sáng liên tiếp
    [SerializeField] private float starPopTime = 0.3f;

    private Sequence sequence;
    private int litCount;

    // count = số sao sáng (0 đến 3). Tất cả bắt đầu ở sprite tắt, rồi sáng lần lượt
    public void Play(int count)
    {
        Kill();
        gameObject.SetActive(true);
        litCount = Mathf.Clamp(count, 0, stars.Length);

        foreach (Image star in stars)
        {
            star.sprite = offSprite;
            star.transform.localScale = Vector3.one;
        }

        transform.localScale = Vector3.zero;

        sequence = DOTween.Sequence().SetTarget(transform);
        sequence.Append(transform.DOScale(Vector3.one, panelPopTime).SetEase(Ease.OutBack));

        for (int i = 0; i < litCount; i++)
        {
            int index = i;
            sequence.InsertCallback(panelPopTime + startDelay + i * stagger, () => Light(index));
        }

        sequence.OnComplete(() => sequence = null);
    }

    // Hoàn tất ngay: các sao sáng đúng số lượng, không animation
    public void Complete()
    {
        if (!gameObject.activeSelf) return;

        Kill();
        transform.localScale = Vector3.one;
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = i < litCount ? onSprite : offSprite;
            stars[i].transform.localScale = Vector3.one;
        }
    }

    public void Hide()
    {
        Kill();
        gameObject.SetActive(false);
    }

    private void Light(int index)
    {
        Image star = stars[index];
        star.sprite = onSprite;
        star.transform.localScale = Vector3.one * 0.5f;
        star.transform.DOScale(Vector3.one, starPopTime).SetEase(Ease.OutBack).SetTarget(star.transform);
    }

    private void Kill()
    {
        if (sequence != null && sequence.IsActive()) sequence.Kill();
        sequence = null;

        transform.DOKill();
        foreach (Image star in stars)
            if (star != null) star.transform.DOKill();
    }

    private void OnDisable()
    {
        Kill();
    }
}
