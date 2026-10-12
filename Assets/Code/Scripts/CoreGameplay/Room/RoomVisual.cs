using UnityEngine;

public class RoomVisual : MonoBehaviour
{
    private static readonly int WallTexId = Shader.PropertyToID("_WallTex");
    
    [SerializeField] private SpriteRenderer panel; 
    [SerializeField] private CatSkinSetter cat;
    
    private MaterialPropertyBlock block;
    
    public void SetUp(Vector3 center, Vector2 size, Sprite floorSprite, Sprite wallSprite, float scaleFactor)
    {
        transform.position = center;
        transform.localScale = Vector3.one * scaleFactor;

        panel.sprite = floorSprite;
        panel.size = size / scaleFactor;
        
        block ??= new MaterialPropertyBlock();
        panel.GetPropertyBlock(block);
        block.SetTexture(WallTexId, wallSprite != null ? wallSprite.texture : Texture2D.blackTexture);
        panel.SetPropertyBlock(block);
    }

    public void SetCat(CatColor? color)
    {
        if (cat == null) return;

        if (color == null)
        {
            cat.Clear();
            cat.gameObject.SetActive(false);
            return;
        }
        
        cat.gameObject.SetActive(true);
        cat.SetUp(color.Value);
    }
}
