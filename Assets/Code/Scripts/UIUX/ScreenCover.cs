using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ScreenCover : MonoBehaviour
{
    [SerializeField] private RectTransform area;
    [SerializeField] private float minScale = 1f;

    private Vector2 lastSize;

    private void OnEnable() => Refresh();

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;

        RectTransform a = Area;
        if (a != null && a.rect.size != lastSize) Refresh();
    }

    private RectTransform Area => area != null ? area : transform.parent as RectTransform;

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        RectTransform a = Area;
        if (a == null) return;

        Vector2 size = a.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;
        lastSize = size;

        var rt = (RectTransform)transform;
        Vector2 own = rt.rect.size;
        if (own.x <= 0f || own.y <= 0f) return;

        Vector2 need = size + new Vector2(Mathf.Abs(rt.anchoredPosition.x), Mathf.Abs(rt.anchoredPosition.y)) * 2f;
        float k = Mathf.Max(minScale, Mathf.Max(need.x / own.x, need.y / own.y));
        rt.localScale = new Vector3(k, k, 1f);
    }
}
