using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FreezeButtonView : MonoBehaviour
{
    [SerializeField] private GameObject activing;
    [SerializeField] private Image activingFill;
    
    [SerializeField] private TMP_Text activingText;
    [SerializeField] private CanvasGroup dimGroup;
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.6f;

    private bool subscribed;
    
    private void Start()
    {
        GameManager.Instance.FreezeProgress += HandleProgress;
        GameManager.Instance.FreezeEnded += HandleEnded;
        subscribed = true;
        Refresh();
    }

    private void OnDestroy()
    {
        if (!subscribed || GameManager.Instance == null) return;
        GameManager.Instance.FreezeProgress -= HandleProgress;
        GameManager.Instance.FreezeEnded -= HandleEnded;
    }

    private void OnEnable()
    {
        if(subscribed) Refresh();
    }
    
    private void Refresh()
    {
        GameManager gm = GameManager.Instance;
        if (gm.IsFrozen) HandleProgress(gm.FreezeRemaining, gm.FreezeDuration);
        else HandleEnded();
    }

    private void HandleProgress(float remaining, float total)
    {
        if (!activing.activeSelf) activing.SetActive(true);

        activingFill.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
        activingText.text = Mathf.CeilToInt(remaining).ToString();
        if (dimGroup != null) dimGroup.alpha = dimAlpha;
    }

    private void HandleEnded()
    {
        activing.SetActive(false);
        if (dimGroup != null) dimGroup.alpha = 1f;
    }
}
