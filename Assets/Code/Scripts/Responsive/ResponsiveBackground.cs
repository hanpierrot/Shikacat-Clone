using UnityEngine;

public class ResponsiveBackground : MonoBehaviour
{
    private enum HudAnchor
    {
        None,
        TopOffset,
        CenterAboveUnderlay
    }
    
    [SerializeField] private Camera targetCamera;
    
    [Header("Backgrounds")]
    [SerializeField] private SpriteRenderer mainBackground; 
    [SerializeField] private SpriteRenderer gridUnderlay;
    [SerializeField] private Transform underlayAnchor; 
    [SerializeField] private float underlayOffsetY = 0f;

    [Header("HUD")]
    [SerializeField] private Transform hudRoot;
    [SerializeField] private HudAnchor hudAnchor = HudAnchor.CenterAboveUnderlay;
    [SerializeField] private float hudTopOffset = 0.8f;

    private void Awake()
    {
        Apply();
    }

    private void Apply()
    {
        if (targetCamera == null) return;
        
        Vector2 visibleSize = CameraViewport.GetVisibleWorldSize(targetCamera);
        Vector3 camPos = targetCamera.transform.position;

        float mainWidth = visibleSize.x;
        if (mainBackground != null)
            mainWidth = FitMainBackground(visibleSize, camPos);

        float underlayTopY = camPos.y + visibleSize.y * 0.5f;
        
        if (gridUnderlay != null)
            underlayTopY = FitUnderlay(mainWidth, camPos);

        if (hudRoot != null)
            PlaceHud(visibleSize, camPos, underlayTopY);
    }

    private float FitMainBackground(Vector2 visibleSize, Vector3 camPos)
    {
        Transform t = mainBackground.transform;
        t.position = new Vector3(camPos.x, camPos.y, t.position.z);

        if (mainBackground.drawMode == SpriteDrawMode.Simple)
        {
            Vector2 spriteSize = mainBackground.sprite.bounds.size;
            float scale = Mathf.Max(visibleSize.x / spriteSize.x, visibleSize.y / spriteSize.y);
            t.localScale = new Vector3(scale, scale, 1f);
            return spriteSize.x * scale;
        }

        t.localScale = Vector3.one;
        mainBackground.size = visibleSize; 
        return visibleSize.x;
    }
    
    private float FitUnderlay(float side, Vector3 camPos)
    {
        Transform t = gridUnderlay.transform;
        Vector3 center = underlayAnchor != null ? underlayAnchor.position : camPos;
        float centerY = center.y + underlayOffsetY;
        t.position = new Vector3(center.x, centerY, t.position.z);

        if (gridUnderlay.drawMode == SpriteDrawMode.Simple)
        {
            Vector2 spriteSize = gridUnderlay.sprite.bounds.size;
            t.localScale = new Vector3(side / spriteSize.x, side / spriteSize.y, 1f);
        }
        else
        {
            t.localScale = Vector3.one;
            gridUnderlay.size = new Vector2(side, side);
        }

        return centerY + side * 0.5f;
    }
    
    private void PlaceHud(Vector2 visibleSize, Vector3 camPos, float underlayTopY)
    {
        float topEdge = camPos.y + visibleSize.y * 0.5f;
        float y;

        switch (hudAnchor)
        {
            case HudAnchor.TopOffset:
                y = topEdge - hudTopOffset;
                break;
            case HudAnchor.CenterAboveUnderlay:
                y = (topEdge + underlayTopY) * 0.5f;
                break;
            default:
                return;
        }

        hudRoot.position = new Vector3(camPos.x, y, hudRoot.position.z);
    }

    
}
