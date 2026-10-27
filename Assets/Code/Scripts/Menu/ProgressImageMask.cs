using UnityEngine;

public class ProgressImageMask : MonoBehaviour
{
    public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
    public enum FillMode { Resize, Slide }

    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform head;
    [SerializeField] private Direction direction = Direction.LeftToRight;
    [SerializeField] private FillMode fillMode = FillMode.Slide;
    [SerializeField] private float minSize;
    [SerializeField] private bool hideFillAtZero = true;
    [SerializeField] private float headOffset;
    [SerializeField, Range(0f, 1f)] private float progress;

    private bool Horizontal => direction == Direction.LeftToRight || direction == Direction.RightToLeft;
    private bool Reversed => direction == Direction.RightToLeft || direction == Direction.TopToBottom;

    public float Progress
    {
        get => progress;
        set
        {
            progress = Mathf.Clamp01(value);
            Apply();
        }
    }

    private void OnEnable() => Apply();

    private void OnValidate() => Apply();

    private void Apply()
    {
        if (fill == null) return;
        var mask = fill.parent as RectTransform;
        if (mask == null) return;

        float full = Horizontal ? mask.rect.width : mask.rect.height;

        if (hideFillAtZero) fill.gameObject.SetActive(progress > 0f);

        if (fillMode == FillMode.Slide)
        {
            float offset = -full * (1f - progress) * (Reversed ? -1f : 1f);
            Vector2 pos = fill.anchoredPosition;
            if (Horizontal) pos.x = offset;
            else pos.y = offset;
            fill.anchoredPosition = pos;
        }
        else
        {
            float size = Mathf.Lerp(minSize, full, progress);
            fill.SetSizeWithCurrentAnchors(Horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, size);
        }

        if (head != null) PlaceHead(mask, full);
    }

    private void PlaceHead(RectTransform mask, float full)
    {
        float edge = full * progress * (Reversed ? -1f : 1f);
        Rect r = mask.rect;
        Vector3 local = Horizontal
            ? new Vector3((Reversed ? r.xMax : r.xMin) + edge + headOffset, 0f, 0f)
            : new Vector3(0f, (Reversed ? r.yMax : r.yMin) + edge + headOffset, 0f);
        Vector3 world = mask.TransformPoint(local);

        Vector3 pos = head.position;
        if (Horizontal) pos.x = world.x;
        else pos.y = world.y;
        head.position = pos;
    }
}
