using UnityEngine;

public class ResponsiveBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer topPanel;
    [SerializeField] private SpriteRenderer middlePanel;
    [SerializeField] private SpriteRenderer bottomPanel;

    private void Awake()
    {
        Apply();
    }

    private void Apply()
    {
        Vector2 visibleSize = CameraViewport.GetVisibleWorldSize(targetCamera);
        float camCenterY = targetCamera.transform.position.y;
        float topEdge = camCenterY + visibleSize.y * 0.5f;
        float bottomEdge = camCenterY - visibleSize.y * 0.5f;
        
        float topPanelHeight = topPanel.size.y;
        topPanel.size = new Vector2(visibleSize.x, topPanelHeight);
        Vector3 topPos = topPanel.transform.position;
        topPos.y = topEdge - topPanelHeight * 0.5f;
        topPanel.transform.position = topPos;
        
        float bottomPanelHeight = bottomPanel.size.y;
        bottomPanel.size = new Vector2(visibleSize.x, bottomPanelHeight);
        Vector3 bottomPos = bottomPanel.transform.position;
        bottomPos.y = bottomEdge + bottomPanelHeight * 0.5f;
        bottomPanel.transform.position = bottomPos;

        float topPanelBottomY = topPos.y - topPanelHeight * 0.5f;
        float bottomPanelTopY = bottomPos.y + bottomPanelHeight * 0.5f;
        float gapHeight = topPanelBottomY - bottomPanelTopY;
        float gapCenterY = (topPanelBottomY + bottomPanelTopY) * 0.5f;

        Vector3 middlePos = middlePanel.transform.position;
        middlePos.y = gapCenterY;
        middlePanel.transform.position = middlePos;
        middlePanel.size = new Vector2(visibleSize.x, gapHeight);
    }
}
