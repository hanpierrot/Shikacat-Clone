using UnityEngine;

public class RoomVisual : MonoBehaviour
{
    private static readonly int WallTexId = Shader.PropertyToID("_WallTex");
    
    [SerializeField] private SpriteRenderer panel; 
    [SerializeField] private SpriteRenderer cat;
    
    private MaterialPropertyBlock block;
    
    public void SetUp(Vector3 center, Vector2 size, Sprite floorSprite, Sprite wallSprite, float scaleFactor)
    {
        transform.position = center;
        transform.localScale = Vector3.one * scaleFactor;

        panel.size = size / scaleFactor;
        panel.sprite = floorSprite;
        
        block ??= new MaterialPropertyBlock();
        panel.GetPropertyBlock(block);
        block.SetTexture(WallTexId, wallSprite != null ? wallSprite.texture : Texture2D.blackTexture);
        panel.SetPropertyBlock(block);
    }

    public void SetCat(Sprite catSprite)
    {
        if (cat == null) return;
        cat.sprite = catSprite;
        cat.enabled = catSprite != null;
    }
}
