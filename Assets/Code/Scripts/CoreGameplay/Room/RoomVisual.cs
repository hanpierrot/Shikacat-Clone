using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer floor;
    [SerializeField] private SpriteRenderer wall;

    public void SetUp(Vector3 center, Vector2 size, Sprite floorSprite, Sprite wallSprite, float scaleFactor)
    {
        transform.position = center;
        transform.localScale = Vector3.one * scaleFactor;
        Vector2 localSize = size / scaleFactor;
        floor.size = localSize;
        floor.sprite = floorSprite;
        wall.size = localSize;
        wall.sprite = wallSprite;
    }
}
