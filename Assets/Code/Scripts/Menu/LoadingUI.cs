using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ProgressImageMask progressImage;
    [SerializeField] private float loadDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    public float LoadDuration => loadDuration;

    public void Begin(float fadeIn = 0f)
    {
        canvasGroup.DOKill();
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        SetProgress(0f);

        if (fadeIn <= 0f)
        {
            canvasGroup.alpha = 1f;
            return;
        }

        canvasGroup.alpha = 0f;
        DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 1f, fadeIn).SetUpdate(true).SetTarget(canvasGroup);
    }

    public void SetProgress(float value) => progressImage.Progress = value;

    public void Show(Action work, Action onComplete, float? duration = null)
    {
        Begin();
        work?.Invoke();
        StartCoroutine(Run(duration ?? loadDuration, onComplete));
    }

    private IEnumerator Run(float seconds, Action onComplete)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetProgress(t / seconds);
            yield return null;
        }

        SetProgress(1f);
        Hide(onComplete);
    }

    public void Hide(Action onComplete = null)
    {
        canvasGroup.DOKill();
        canvasGroup.blocksRaycasts = false;
        DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 0f, fadeOutDuration).SetUpdate(true).SetTarget(canvasGroup).OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void OnDestroy() => canvasGroup.DOKill();
}
