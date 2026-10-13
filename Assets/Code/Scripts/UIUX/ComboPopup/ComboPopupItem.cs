using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ComboPopupItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation")]
    [SerializeField] private float growTime = 0.25f;
    [SerializeField] private float holdTime = 0.6f;
    [SerializeField] private float fadeTime = 0.25f;

    private Sequence sequence;
    private Vector3 baseScale = Vector3.one;
    private bool cached;

    public void Play(Sprite sprite, Vector3 worldPosition)
    {
        if (!cached)
        {
            baseScale = transform.localScale; 
            cached = true;
        }

        Kill();

        spriteRenderer.sprite = sprite;
        SetAlpha(1f);
        transform.position = worldPosition;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);

        sequence = DOTween.Sequence().SetTarget(transform);
        sequence.Append(transform.DOScale(baseScale, growTime).SetEase(Ease.OutBack));
        sequence.AppendInterval(holdTime);
        sequence.Append(transform.DOScale(Vector3.zero, fadeTime).SetEase(Ease.InBack)); 
        sequence.OnComplete(() =>
        {
            sequence = null;
            gameObject.SetActive(false);
        });
    }

    public void Hide()
    {
        Kill();
        gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    private void Kill()
    {
        if (sequence != null && sequence.IsActive())
            sequence.Kill();
        sequence = null;
    }

    private void OnDisable()
    {
        Kill();
    }
}
