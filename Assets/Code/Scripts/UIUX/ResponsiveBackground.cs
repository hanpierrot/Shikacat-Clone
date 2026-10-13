using UnityEngine;

public class ResponsiveBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer mainBackground;

    private void Awake()
    {
        Apply();
    }
    
    [ContextMenu("Apply")]
    public void Apply()
    {
        if (targetCamera == null || mainBackground == null) return;

        Vector2 visibleSize = CameraViewport.GetVisibleWorldSize(targetCamera);
        Vector3 camPos = targetCamera.transform.position;

        Transform t = mainBackground.transform;
        t.position = new Vector3(camPos.x, camPos.y, t.position.z);

        if (mainBackground.drawMode == SpriteDrawMode.Simple)
        {
            Vector2 spriteSize = mainBackground.sprite.bounds.size;
            float scale = Mathf.Max(visibleSize.x / spriteSize.x, visibleSize.y / spriteSize.y);
            t.localScale = new Vector3(scale, scale, 1f);
            return;
        }

        t.localScale = Vector3.one;
        mainBackground.size = visibleSize;
    }
}
