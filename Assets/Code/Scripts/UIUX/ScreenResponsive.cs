using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ScreenResponsive : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ResponsiveBackground background;
    [SerializeField] private RoomBuildController roomBuilder;

    private RectTransform canvasRect;
    private CanvasScaler scaler;
    private Vector2Int lastScreen;
    private Vector2 lastCanvasSize;
    private float lastAspect;
    private bool dirty = true;

    private void OnEnable()
    {
        canvasRect = (RectTransform)transform;
        dirty = true;
#if UNITY_EDITOR
        EditorApplication.update += EditorTick;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
#endif
    }

    private void OnRectTransformDimensionsChange()
    {
        if (Application.isPlaying) dirty = true;
    }

    private void Update()
    {
        if (Application.isPlaying) Tick();
    }

#if UNITY_EDITOR
    private void EditorTick()
    {
        if (this == null) return;
        if (!Application.isPlaying) Tick();
    }
#endif

    private void Tick()
    {
        if (canvasRect == null) canvasRect = (RectTransform)transform;

        if (!Application.isPlaying)
        {
            TickEditMode();
            return;
        }

        var screen = new Vector2Int(Screen.width, Screen.height);
        float aspect = targetCamera != null ? targetCamera.aspect : 0f;
        Vector2 canvasSize = canvasRect.rect.size;

        if (!dirty && screen == lastScreen && Mathf.Approximately(aspect, lastAspect) && canvasSize == lastCanvasSize) return;

        dirty = false;
        lastScreen = screen;
        lastAspect = aspect;
        lastCanvasSize = canvasSize;
        Apply();
    }

    // Ở Edit Mode, Screen.width/height và kích thước canvas có thể nhảy qua lại giữa Game view và Scene view khi chuột di chuyển,
    // nên chỉ dùng kích thước pixel của camera (chỉ render vào Game/Simulator view) làm nguồn, và chỉ áp khi canvas đã khớp kích thước đó
    private void TickEditMode()
    {
        if (targetCamera == null) return;

        var pixels = new Vector2Int(targetCamera.pixelWidth, targetCamera.pixelHeight);
        if (pixels.x <= 0 || pixels.y <= 0) return;

        if (!dirty && pixels == lastScreen) return;

        if (scaler == null) scaler = GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            float logWidth = Mathf.Log(pixels.x / scaler.referenceResolution.x, 2f);
            float logHeight = Mathf.Log(pixels.y / scaler.referenceResolution.y, 2f);
            float factor = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
            Vector2 expected = new Vector2(pixels.x / factor, pixels.y / factor);
            if ((canvasRect.rect.size - expected).sqrMagnitude > 1f) return;
        }

        dirty = false;
        lastScreen = pixels;
        Apply();
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        foreach (ScreenAnchor anchor in GetComponentsInChildren<ScreenAnchor>(true))
            anchor.Refresh();

        if (background != null) background.Apply();
        if (!Application.isPlaying) return;

        if (GridManager.Instance != null) GridManager.Instance.Relayout();
        if (roomBuilder != null) roomBuilder.RelayoutRooms();
    }
}
