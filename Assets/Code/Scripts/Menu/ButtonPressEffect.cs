using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float duration = 0.12f;

    private Vector3 baseScale = Vector3.one;
    private bool captured;

    private void OnDisable()
    {
        transform.DOKill();
        if (captured) transform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!captured)
        {
            baseScale = transform.localScale;
            captured = true;
        }

        transform.DOKill();
        transform.DOScale(baseScale * pressedScale, duration).SetUpdate(true).SetTarget(transform);
    }

    public void OnPointerUp(PointerEventData eventData) => Release();

    public void OnPointerExit(PointerEventData eventData) => Release();

    private void Release()
    {
        if (!captured) return;
        transform.DOKill();
        transform.DOScale(baseScale, duration).SetUpdate(true).SetTarget(transform);
    }
}
