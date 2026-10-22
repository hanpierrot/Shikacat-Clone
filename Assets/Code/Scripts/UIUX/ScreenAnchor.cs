using UnityEngine;

// Giữ UI cách mép dưới/trên màn hình một khoảng cố định (đơn vị canvas), bất kể tỉ lệ màn hình,
// và cộng thêm vùng safe area (tai thỏ, thanh cử chỉ).
[RequireComponent(typeof(RectTransform))]
public class ScreenAnchor : MonoBehaviour
{
    public enum Edge { Bottom, Top }

    [SerializeField] private Edge edge = Edge.Bottom;
    [SerializeField] private float offset;
    [SerializeField] private bool useSafeArea = true;

    private RectTransform rect;
    private Canvas canvas;
    private Rect lastSafeArea;
    private Vector2 lastCanvasSize;

    private void OnEnable() => Apply(true);

    private void LateUpdate() => Apply(false);

    [ContextMenu("Apply")]
    private void ApplyNow() => Apply(true);

    private void Apply(bool force)
    {
        if (rect == null) rect = (RectTransform)transform;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        var parent = rect.parent as RectTransform;
        if (parent == null || canvas == null || Screen.height <= 0) return;

        var canvasRect = (RectTransform)canvas.rootCanvas.transform;
        Rect c = canvasRect.rect;
        Rect safe = Screen.safeArea;
        if (!force && safe == lastSafeArea && c.size == lastCanvasSize) return;

        lastSafeArea = safe;
        lastCanvasSize = c.size;

        float unitsPerPixel = c.height / Screen.height;
        float y;
        if (edge == Edge.Bottom)
        {
            float inset = useSafeArea ? safe.yMin * unitsPerPixel : 0f;
            y = c.yMin + inset + offset;
        }
        else
        {
            float inset = useSafeArea ? (Screen.height - safe.yMax) * unitsPerPixel : 0f;
            y = c.yMax - inset - offset;
        }

        Vector3 world = canvasRect.TransformPoint(new Vector3(c.center.x, y, 0f));
        float localY = parent.InverseTransformPoint(world).y;
        float anchorY = parent.rect.yMin + rect.anchorMin.y * parent.rect.height;

        Vector2 position = rect.anchoredPosition;
        position.y = localY - anchorY;
        rect.anchoredPosition = position;
    }
}
