using DG.Tweening;
using Spine.Unity;
using UnityEngine;

public class WinAnimController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonGraphic wellDone;
    [SerializeField] private SkeletonGraphic[] cats;
    [SerializeField] private SkeletonGraphic coin;
    
    [Header("Cats")]
    [SerializeField] private RectTransform[] catRoots;
    [SerializeField] private float startOffsetY = -1300f;
    [SerializeField] private float jumpTime = 0.55f;
    [SerializeField] private float stagger = 0.12f;
    [SerializeField] private float hoverHeight = 40f;
    [SerializeField] private float hoverTime = 0.5f;
    [SerializeField] private float fallDistance = 1300f;
    [SerializeField] private float fallTime = 0.4f;
    
    [Header("Reward")]
    [SerializeField] private GameObject dropFx;
    [SerializeField] private GameObject coinRoot;
    [SerializeField] private CollectButton collectButton;
    
    private Sequence sequence;
    private Vector2[] restPositions;

    private void CacheRest()
    {
        if (restPositions != null) return;

        restPositions = new Vector2[catRoots.Length];
        for (int i = 0; i < catRoots.Length; i++)
            restPositions[i] = catRoots[i].anchoredPosition; 
    }

    public void Play(int reward)
    {
        CacheRest();
        Stop();

        gameObject.SetActive(true);

        if (dropFx != null) dropFx.SetActive(false);
        if (coinRoot != null) coinRoot.SetActive(false);
        if (collectButton != null) collectButton.Hide();
        
        wellDone.AnimationState.SetAnimation(0, "levelpass_victory_open", false);
        wellDone.AnimationState.AddAnimation(0, "levelpass_victory_idle", true, 0f);
        
        for (int i = 0; i < catRoots.Length; i++)
        {
            catRoots[i].gameObject.SetActive(true);
            SetY(catRoots[i], restPositions[i].y + startOffsetY);
            cats[i].AnimationState.SetAnimation(0, "animation", true);
        }

        sequence = DOTween.Sequence().SetTarget(transform);
        
        for (int i = 0; i < catRoots.Length; i++)
            sequence.Insert(i * stagger, MoveY(catRoots[i], restPositions[i].y, jumpTime).SetEase(Ease.OutBack));
        
        sequence.AppendCallback(() =>
        {
            if (dropFx != null) dropFx.SetActive(true);
        });
        
        for (int i = 0; i < catRoots.Length; i++)
        {
            Tween hover = MoveY(catRoots[i], restPositions[i].y + hoverHeight, hoverTime).SetEase(Ease.OutSine);
            if (i == 0) sequence.Append(hover); else sequence.Join(hover);
        }
        
        for (int i = 0; i < catRoots.Length; i++)
        {
            Tween fall = MoveY(catRoots[i], restPositions[i].y - fallDistance, fallTime).SetEase(Ease.InBack);
            if (i == 0) sequence.Append(fall); else sequence.Join(fall);
        }

        sequence.AppendCallback(() =>
        {
            foreach (RectTransform root in catRoots) root.gameObject.SetActive(false);
            ShowReward(reward);
        });
    }
    
    private void ShowReward(int reward)
    {
        if (coinRoot != null)
        {
            coinRoot.SetActive(true);
            coinRoot.transform.localScale = Vector3.zero;
            coinRoot.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetTarget(coinRoot.transform);
        }

        coin.AnimationState.SetAnimation(0, "rotation", true);

        if (collectButton != null) collectButton.Show(reward);
    }
    
    public void Stop()
    {
        if (sequence != null && sequence.IsActive()) sequence.Kill();
        sequence = null;

        if (coinRoot != null) coinRoot.transform.DOKill();
    }

    private void OnDisable()
    {
        Stop();
        if (dropFx != null) dropFx.SetActive(false);
        if (collectButton != null) collectButton.Hide();
    }

    private static void SetY(RectTransform rect, float y)
    {
        Vector2 p = rect.anchoredPosition;
        p.y = y;
        rect.anchoredPosition = p;
    }

    private static Tweener MoveY(RectTransform rect, float targetY, float duration)
        => DOTween.To(() => rect.anchoredPosition.y, y => SetY(rect, y), targetY, duration);
}
