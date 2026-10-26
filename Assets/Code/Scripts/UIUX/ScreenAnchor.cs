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
    public void Refresh() => Apply(true);

    private void Apply(bool force)
    {
        if (rect == null) rect = (RectTransform)transform;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        var parent = rect.parent as RectTransform;
        if (parent == null || canvas == null) return;

        bool playing = Application.isPlaying;
        if (playing && Screen.height <= 0) return;

        var canvasRect = (RectTransform)canvas.rootCanvas.transform;
        Rect c = canvasRect.rect;
        Rect safe = playing ? Screen.safeArea : new Rect(0f, 0f, 1f, 1f);
        if (!force && safe == lastSafeArea && c.size == lastCanvasSize) return;

        lastSafeArea = safe;
        lastCanvasSize = c.size;

        bool useSafe = useSafeArea && playing;
        float unitsPerPixel = playing ? c.height / Screen.height : 0f;
        float y;
        if (edge == Edge.Bottom)
        {
            float inset = useSafe ? safe.yMin * unitsPerPixel : 0f;
            y = c.yMin + inset + offset;
        }
        else
        {
            float inset = useSafe ? (Screen.height - safe.yMax) * unitsPerPixel : 0f;
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
