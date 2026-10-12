using DG.Tweening;
using Spine.Unity;
using UnityEngine;

public class CatSkinSetter : MonoBehaviour
{
    private static readonly string[] JumpAnimations = { "jump1", "jump2", "jump3" };
    private static readonly string[] IdleAnimations = { "front_idle1", "front_idle2", "front_idle3", "front_idle4" };
    
    [SerializeField] private SkeletonAnimation spine;
    
    [Header("Intro Animation")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private float growStartScale = 0f;
    [SerializeField] private float growTime = 0.15f;
    [SerializeField] private float jumpLift = 0.15f; 
    [SerializeField, Range(0.2f, 0.8f)] private float riseRatio = 0.45f;
    
    private Sequence introSequence;
    private Vector3 restLocalPosition;
    private Vector3 restLocalScale;
    private bool restCached;

    private void Reset()
    {
        spine = GetComponent<SkeletonAnimation>();
    }

    private void CacheRest()
    {
        if (restCached) return;
        restLocalPosition = transform.localPosition;
        restLocalScale = transform.localScale;
        restCached = true;
    }
    
    public void SetUp(CatColor color)
    {
        CacheRest();
        KillIntro();

        if (!spine.valid) spine.Initialize(false);

        spine.Skeleton.SetSkin(color.ToSkinName());
        spine.Skeleton.SetSlotsToSetupPose();
        spine.AnimationState.ClearTracks();

        string idle = IdleAnimations[Random.Range(0, IdleAnimations.Length)];
        spine.AnimationState.SetAnimation(0, idle, true);
        
        spine.Update(0);
        spine.LateUpdate();

        if (playIntro) PlayIntro(idle);
        else ResetTransform();
    }

    private void PlayIntro(string idle)
    {
        string jump = JumpAnimations[Random.Range(0, JumpAnimations.Length)];
        Spine.Animation jumpData = spine.Skeleton.Data.FindAnimation(jump);
        float jumpDuration = jumpData != null ? Mathf.Max(0.2f, jumpData.Duration) : 0.5f;
        float riseTime = jumpDuration * riseRatio;
        float fallTime = jumpDuration - riseTime;

        transform.localPosition = restLocalPosition;
        transform.localScale = restLocalScale * growStartScale;

        introSequence = DOTween.Sequence().SetTarget(transform);
        introSequence.Append(transform.DOScale(restLocalScale, growTime).SetEase(Ease.OutBack));

        introSequence.AppendCallback(() =>
        {
            spine.AnimationState.SetAnimation(0, jump, false);
            spine.AnimationState.AddAnimation(0, idle, true, 0f);
        });
        introSequence.Append(transform.DOLocalMoveY(restLocalPosition.y + jumpLift, riseTime).SetEase(Ease.OutQuad));
        introSequence.Append(transform.DOLocalMoveY(restLocalPosition.y, fallTime).SetEase(Ease.InQuad));
        introSequence.OnKill(() =>
        {
            ResetTransform();
            introSequence = null;
        });
    }

    public void Clear()
    {
        KillIntro();
        spine.AnimationState.ClearTracks();
    }
    
    private void ResetTransform()
    {
        if (!restCached) return;
        transform.localPosition = restLocalPosition;
        transform.localScale = restLocalScale;
    }

    private void KillIntro()
    {
        if (introSequence != null && introSequence.IsActive())
            introSequence.Kill();
    }

    private void OnDisable()
    {
        KillIntro();
    }
}
