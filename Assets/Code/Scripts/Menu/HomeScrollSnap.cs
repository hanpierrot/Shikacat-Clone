using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class HomeScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private float snapSpeed = 20f;
    [SerializeField] private float miniumSpeedToJumpSnap = 50f;
    [SerializeField] private int defaultPage = 1;
    [SerializeField] private int lockedMaxIndex = -1;

    private ScrollRect scroll;
    private RectTransform content;
    private bool snapping;
    private int page = -1;
    private float targetX;

    public int CurrentPage => page;
    public int PageCount => content != null ? content.childCount : 0;

    public event Action<int> PageChanged;

    private float PageWidth => scroll.viewport != null ? scroll.viewport.rect.width : ((RectTransform)transform).rect.width;

    private void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        content = scroll.content;
    }

    private void OnEnable()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        GoTo(page < 0 ? defaultPage : page, true);
    }

    public void GoTo(int index, bool instant = false)
    {
        int max = PageCount - 1;
        if (lockedMaxIndex >= 0) max = Mathf.Min(max, lockedMaxIndex);
        index = Mathf.Clamp(index, 0, Mathf.Max(0, max));

        bool changed = index != page;
        page = index;
        targetX = -index * PageWidth;

        scroll.StopMovement();
        if (instant)
        {
            snapping = false;
            SetX(targetX);
        }
        else
        {
            snapping = true;
        }

        if (changed) PageChanged?.Invoke(page);
    }

    public void OnBeginDrag(PointerEventData eventData) => snapping = false;

    public void OnEndDrag(PointerEventData eventData)
    {
        float pos = -content.anchoredPosition.x / PageWidth;
        float speed = scroll.velocity.x;

        int index;
        if (Mathf.Abs(speed) >= miniumSpeedToJumpSnap)
            index = speed < 0f ? Mathf.FloorToInt(pos) + 1 : Mathf.FloorToInt(pos);
        else
            index = Mathf.RoundToInt(pos);

        GoTo(index);
    }

    private void Update()
    {
        if (!snapping) return;

        float x = Mathf.Lerp(content.anchoredPosition.x, targetX, 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime));
        if (Mathf.Abs(x - targetX) < 0.5f)
        {
            x = targetX;
            snapping = false;
        }
        SetX(x);
    }

    private void SetX(float x)
    {
        Vector2 p = content.anchoredPosition;
        p.x = x;
        content.anchoredPosition = p;
    }
}
