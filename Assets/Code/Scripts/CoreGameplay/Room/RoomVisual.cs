using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RoomVisual : MonoBehaviour
{
    private static readonly int WallTexId = Shader.PropertyToID("_WallTex");
    
    [SerializeField] private SpriteRenderer panel; 
    [SerializeField] private CatSkinSetter cat;
    
    [Header("Place Animation")]
    [SerializeField] private float growStartScale = 0f;
    [SerializeField] private float growTime = 0.18f;
    [SerializeField] private float liftHeight = 0.3f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverTime = 0.08f;
    [SerializeField] private float dropTime = 0.14f;
    [SerializeField] private float landPunch = 0.08f;
    [SerializeField] private float landPunchTime = 0.2f;
    [SerializeField] private int animatingOrderBoost = 50;
    
    private MaterialPropertyBlock block;
    private Sequence placeSequence;
    private Renderer[] renderers;
    private int[] baseOrders;
    
    public void SetUp(Vector3 center, Vector2 size, Sprite floorSprite, Sprite wallSprite, float scaleFactor)
    {
        KillPlaceAnimation();
        
        transform.position = center;
        transform.localScale = Vector3.one * scaleFactor;

        panel.sprite = floorSprite;
        panel.size = size / scaleFactor;
        
        block ??= new MaterialPropertyBlock();
        panel.GetPropertyBlock(block);
        block.SetTexture(WallTexId, wallSprite != null ? wallSprite.texture : Texture2D.blackTexture);
        panel.SetPropertyBlock(block);
    }

    public void PlayPlaceAnimation(Action onLanded)
    {
        KillPlaceAnimation();
        
        Vector3 restPosition = transform.position;
        Vector3 restScale = transform.localScale;
        Vector3 liftedPosition = restPosition + Vector3.up * (liftHeight * restScale.x);
        
        SetSortingBoost(true);
        
        transform.position = liftedPosition;
        transform.localScale = Vector3.one * growStartScale;
        
        placeSequence = DOTween.Sequence().SetTarget(transform);
        placeSequence.Append(transform.DOScale(restScale * hoverScale, growTime).SetEase(Ease.OutBack));
        placeSequence.AppendInterval(hoverTime);
        placeSequence.Append(transform.DOMove(restPosition, dropTime).SetEase(Ease.InQuad));
        placeSequence.Join(transform.DOScale(restScale, dropTime).SetEase(Ease.InQuad));
        placeSequence.AppendCallback(() =>
        {
            SetSortingBoost(false);
            onLanded?.Invoke();
        });
        placeSequence.Append(transform.DOPunchScale(restScale * landPunch, landPunchTime, 6, 0.6f));
        placeSequence.OnKill(() =>
        {
            SetSortingBoost(false);
            transform.position = restPosition;
            transform.localScale = restScale;
            placeSequence = null;
        });
    }

    public void SetCat(CatColor? color, Action onIntroFinished = null)
    {
        if (cat == null)
        {
            onIntroFinished?.Invoke();
            return;
        }

        if (color == null)
        {
            cat.Clear();
            cat.gameObject.SetActive(false);
            return;
        }
        
        cat.gameObject.SetActive(true);
        cat.SetUp(color.Value, onIntroFinished);
    }

    private void CacheRenderers()
    {
        var all = GetComponentsInChildren<Renderer>(true);
        var list = new List<Renderer>();
        foreach (Renderer r in all)
        {
            if (cat != null && r.transform.IsChildOf(cat.transform)) continue;
            list.Add(r);
        }
        
        renderers = list.ToArray();
        baseOrders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseOrders[i] = renderers[i].sortingOrder;
    }

    private void SetSortingBoost(bool boosted)
    {
        if (renderers == null) CacheRenderers();

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = baseOrders[i] + (boosted ? animatingOrderBoost : 0);
    }
    
    public void Release()
    {
        KillPlaceAnimation();
        SetCat(null);
    }

    private void KillPlaceAnimation()
    {
        if (placeSequence != null && placeSequence.IsActive())
            placeSequence.Kill();
    }

    private void OnDisable()
    {
        KillPlaceAnimation();
    }
}
