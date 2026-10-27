using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FreezeOverlayFX : MonoBehaviour
{
    [Header("Hand + clock (prefab HandStartFreeze)")]
    [SerializeField] private RectTransform handStartRoot;
    [SerializeField] private RectTransform clock;
    [SerializeField] private RectTransform clockButton;
    [SerializeField] private RectTransform hand;
    [SerializeField] private Image ring;
    [SerializeField] private Image snowflake;
    [SerializeField] private Image touchFx;
    [SerializeField] private Vector2 handEnterOffset = new Vector2(260f, -420f);
    [SerializeField] private Vector2 handPressOffset = new Vector2(0f, -24f);   // Ngón trỏ lún xuống bao nhiêu khi bấm nút
    
    [Header("Borders")]
    [SerializeField] private CanvasGroup freezeBorders;
    [SerializeField] private ParticleSystem[] snow;
    [SerializeField] private GameObject freezeBurstRoot;
    [SerializeField] private float bordersFade = 0.4f;


    private Vector2 handRest;
    private Vector2 rootRest;
    private bool restCached;
    private Sequence intro;
    private Tween bordersTween;
    
    private void CacheRest()
    {
        if (restCached) return;
        handRest = hand.anchoredPosition;
        rootRest = handStartRoot.anchoredPosition;
        restCached = true;
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public void PlayIntro(Action onBordersShown)
    {
        CacheRest();
        Stop(true);

        // Cả cụm (các ngón ôm + đồng hồ + nút + ngón trỏ) bay vào cùng nhau; chỉ ngón trỏ ('hand') di chuyển riêng để ấn nút
        handStartRoot.gameObject.SetActive(true);
        handStartRoot.localScale = Vector3.one * 0.6f;
        handStartRoot.localRotation = Quaternion.identity;
        handStartRoot.anchoredPosition = rootRest + handEnterOffset;

        clock.localScale = Vector3.one;
        clock.localRotation = Quaternion.identity;
        clockButton.localScale = Vector3.one;
        hand.localScale = Vector3.one;
        hand.anchoredPosition = handRest;   // Ngón trỏ ở sẵn vị trí như ảnh mẫu, không lơ lửng

        ring.gameObject.SetActive(false);
        snowflake.gameObject.SetActive(false);
        touchFx.gameObject.SetActive(false);

        AudioManager.PlaySfx(Sfx.FreezeStart);

        intro = DOTween.Sequence().SetTarget(this);
        
        // 1. Cả cụm hiện ra và nổi lên
        intro.Append(handStartRoot.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        intro.Join(DOTween.To(() => handStartRoot.anchoredPosition, v => handStartRoot.anchoredPosition = v, rootRest, 0.3f)
            .SetEase(Ease.OutCubic).SetTarget(this));

        // 2. Cả cụm lắc nhẹ
        intro.Append(handStartRoot.DOPunchRotation(new Vector3(0f, 0f, 10f), 0.5f, 10, 0.5f));

        // 3. Ngón trỏ nhấn nhẹ xuống rồi nảy lên: tia sáng bắn, nút lún
        intro.Append(MoveHand(handRest + handPressOffset, 0.1f, Ease.InQuad));
        intro.AppendCallback(ShowTouch);
        intro.Join(clockButton.DOScale(new Vector3(1f, 0.8f, 1f), 0.08f));
        intro.Append(MoveHand(handRest, 0.14f, Ease.OutBack));
        intro.Join(clockButton.DOScale(Vector3.one, 0.14f).SetEase(Ease.OutBack));

        // 4. Bông tuyết hiện trên mặt đồng hồ
        intro.AppendCallback(ShowSnowflake);
        intro.AppendInterval(0.7f);

        // 5. Thu nhỏ tất cả rồi tắt
        intro.Append(handStartRoot.DOScale(0f, 0.35f).SetEase(Ease.InBack));
        intro.AppendCallback(() =>
        {
            handStartRoot.gameObject.SetActive(false);
            ShowBorders();
            onBordersShown?.Invoke();
        });
        intro.OnKill(() => intro = null);
    }
    
    public void PlayWarning() => AudioManager.PlaySfx(Sfx.EndTimeWarning);

    public void Stop(bool immediate)
    {
        DOTween.Kill(this);
        intro = null;
        bordersTween = null;

        handStartRoot.gameObject.SetActive(false);
        if (freezeBurstRoot != null) freezeBurstRoot.SetActive(false);

        if (!freezeBorders.gameObject.activeSelf) return;

        if (immediate)
        {
            HideBordersNow();
            return;
        }

        bordersTween = DOTween.To(() => freezeBorders.alpha, a => freezeBorders.alpha = a, 0f, bordersFade)
            .SetTarget(this)
            .OnComplete(HideBordersNow);
    }

    private void ShowTouch()
    {
        touchFx.gameObject.SetActive(true);
        touchFx.transform.localScale = Vector3.one * 0.4f;
        SetAlpha(touchFx, 1f);

        touchFx.transform.DOScale(1.2f, 0.25f).SetEase(Ease.OutQuad).SetTarget(this);
        FadeImage(touchFx, 0f, 0.3f, 0.1f);
    }

    private void ShowSnowflake()
    {
        snowflake.gameObject.SetActive(true);
        snowflake.transform.localScale = Vector3.zero;
        snowflake.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetTarget(this);

        ring.gameObject.SetActive(true);
        SetAlpha(ring, 0f);
        FadeImage(ring, 0.8f, 0.3f, 0f);
    }

    private void ShowBorders()
    {
        AudioManager.PlaySfx(Sfx.FreezeAppear);

        freezeBorders.gameObject.SetActive(true);
        freezeBorders.alpha = 0f;
        bordersTween = DOTween.To(() => freezeBorders.alpha, a => freezeBorders.alpha = a, 1f, bordersFade).SetTarget(this);

        foreach (ParticleSystem ps in snow)
            if (ps != null) ps.Play(true);

        if (freezeBurstRoot != null)
        {
            freezeBurstRoot.SetActive(true);
            foreach (ParticleSystem ps in freezeBurstRoot.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play(true);
        }
    }

    private void HideBordersNow()
    {
        foreach (ParticleSystem ps in snow)
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        freezeBorders.alpha = 0f;
        freezeBorders.gameObject.SetActive(false);
    }

    private Tweener MoveHand(Vector2 to, float time, Ease ease)
        => DOTween.To(() => hand.anchoredPosition, v => hand.anchoredPosition = v, to, time).SetEase(ease).SetTarget(this);

    private void FadeImage(Image image, float to, float time, float delay)
        => DOTween.To(() => image.color.a, a => SetAlpha(image, a), to, time).SetDelay(delay).SetTarget(this);

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }


}
