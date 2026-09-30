using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer floor;
    [SerializeField] private SpriteRenderer wall;

    public void SetUp(Vector3 center, Vector2 size, Sprite floorSprite, Sprite wallSprite)
    {
        transform.position = center;
        floor.size = size;
        floor.sprite = floorSprite;
        wall.size = size;
        wall.sprite = wallSprite;
    }
    
    public void SetActive(bool active) => gameObject.SetActive(active);
    public bool IsActive => gameObject.activeSelf;
}
