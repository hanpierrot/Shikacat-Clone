using UnityEngine;

public static class CameraViewport
{
    public static Vector2 GetVisibleWorldSize(Camera camera)
    {
        float height = camera.orthographicSize * 2f;
        float width = height * camera.aspect;
        return new Vector2(width, height);
    }
}
