using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class ModalPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform window;

    protected virtual void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    protected virtual void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = true;

        if (window == null) return;
        window.DOKill();
        window.localScale = Vector3.one * 0.9f;
        window.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).SetTarget(window);
    }

    protected virtual void OnDisable()
    {
        if (window != null) window.DOKill();
        if (GameManager.Instance != null) GameManager.Instance.TimerPaused = false;
    }

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);
}
