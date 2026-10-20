using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class RoomVisual : MonoBehaviour
{
    private static readonly int WallTexId = Shader.PropertyToID("_WallTex");
    
    [SerializeField] private SpriteRenderer panel; 
    [SerializeField] private CatSkinSetter cat;
    
    [Header("Decor")]
    [SerializeField] private Transform decorRoot;
    [SerializeField] private SortingGroup decorGroup; 
    [SerializeField] private int maxDecorPerRoom = 6;
    [SerializeField] private int minDecor = 0;
    [SerializeField] private Vector2 sizeRange = new Vector2(0.4f, 0.6f);
    [SerializeField] private float centerClear = 0.75f;   
    
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
    
    private readonly List<SpriteRenderer> decor = new List<SpriteRenderer>();
    private DecorPool decorPool;
    private int decorBaseOrder;
    private bool decorOrderCached;
    
    private readonly List<Vector2> candidateBuffer = new List<Vector2>();
    private readonly List<int> indexBuffer = new List<int>();
    
    private MaterialPropertyBlock block;
    private Sequence placeSequence;
    private Renderer[] renderers;
    private int[] baseOrders;
    
    public bool IsPrewarmed { get; private set; }

    public void Prewarm(CatColor color)
    {
        if (cat != null)
        {
            cat.gameObject.SetActive(true);
            cat.Prewarm(color);
            cat.gameObject.SetActive(false);
        }

        IsPrewarmed = true;
        gameObject.SetActive(false);
    }
    
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

    public void SetDecor(DecorPool pool, DecorSpritePool sprites, Color color,
        IReadOnlyList<Vector2> cellCenters, float cellSize)
    {
        ClearDecor();
        if (pool == null || sprites == null || sprites.Count == 0 || decorRoot == null) return;

        decorPool = pool;
        
        int area = cellCenters.Count;
        int max = Mathf.Min(area / 2, maxDecorPerRoom, sprites.Count); 
        if (max <= 0) return;

        int count = Random.Range(Mathf.Min(minDecor, max), max + 1);
        if (count == 0) return;

        candidateBuffer.Clear();
        foreach (Vector2 p in cellCenters)
        {
            if (area > 2 && p.magnitude < centerClear * cellSize) continue;
            candidateBuffer.Add(p);
        }

        count = Mathf.Min(count, candidateBuffer.Count);
        if (count == 0) return;
        ShuffleCells(candidateBuffer);
        
        indexBuffer.Clear();
        for (int i = 0; i < sprites.Count; i++) indexBuffer.Add(i);
        ShuffleIndexes(indexBuffer);

        if (color.a <= 0f) color = Color.white;
        
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer item = pool.Get(decorRoot);
            item.sprite = sprites.Get(indexBuffer[i]);

            Bounds bounds = item.sprite.bounds;

            item.transform.localScale = Vector3.one; 
            item.transform.localRotation = Quaternion.identity;
            item.transform.localPosition = candidateBuffer[i] - (Vector2)bounds.center;
            
            item.color = color;
            decor.Add(item);
        }
    }
    
    private void ClearDecor()
    {
        if (decorPool != null)
            foreach (SpriteRenderer item in decor)
                decorPool.Release(item);
        decor.Clear();
    }
    
    private static void ShuffleCells(List<Vector2> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void ShuffleIndexes(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
        
        if (decorGroup != null)
        {
            if (!decorOrderCached)
            {
                decorBaseOrder = decorGroup.sortingOrder;
                decorOrderCached = true;
            }
            decorGroup.sortingOrder = decorBaseOrder + (boosted ? animatingOrderBoost : 0);
        }
    }
    
    public void Release()
    {
        KillPlaceAnimation();
        ClearDecor();
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
