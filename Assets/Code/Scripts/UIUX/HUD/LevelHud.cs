using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelHud : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private Image progressBar;
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite starOnSprite; 
    [SerializeField] private Sprite starOffSprite;
    [SerializeField] private float starLostPunch = 0.3f;

    [Header("Texts")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text limitText;
    [SerializeField] private Image limitIcon;
    [SerializeField] private Sprite timeIconSprite;
    [SerializeField] private Sprite movesIconSprite;
    [SerializeField] private string levelFormat = "Level {0}";
    
    [Header("Freeze")]
    [SerializeField] private GameObject freezeImage;

    private float[] thresholds;
    private bool[] lost;
    
    public void Bind(int levelNumber, bool showsMoves)
    {
        if (levelText != null) levelText.text = string.Format(levelFormat, levelNumber);
        
        if (limitIcon != null)
        {
            Sprite icon = showsMoves ? movesIconSprite : timeIconSprite;
            if (icon != null) limitIcon.sprite = icon;
        }

        CacheThresholds();

        for (int i = 0; i < stars.Length; i++)
        {
            lost[i] = false;
            if (stars[i] == null) continue;
            stars[i].transform.DOKill();
            stars[i].transform.localScale = Vector3.one;
            if (starOnSprite != null) stars[i].sprite = starOnSprite;
        }

        SetProgress(1f);
        SetFrozen(false);
    }

    public void SetProgress(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (progressBar != null && Mathf.Abs(progressBar.fillAmount - fraction) >= 0.004f)
            progressBar.fillAmount = fraction;

        for (int i = 0; i < stars.Length; i++)
        {
            bool isLost = fraction < thresholds[i];
            if (!isLost || lost[i]) continue;

            lost[i] = isLost;
            if (stars[i] == null) continue;

            Sprite sprite = isLost ? starOffSprite : starOnSprite;
            if (sprite != null) stars[i].sprite = sprite;

            if (isLost)
            {
                stars[i].transform.DOKill();
                stars[i].transform.localScale = Vector3.one;
                stars[i].transform.DOPunchScale(Vector3.one * starLostPunch, 0.25f, 6, 0.6f);
            }
        }
    }
    
    public void SetFrozen(bool frozen)
    {
        if (freezeImage != null && freezeImage.activeSelf != frozen) 
            freezeImage.SetActive(frozen);
    }
    
    public int LitStars
    {
        get
        {
            if (lost == null) return stars.Length;

            int count = 0;
            for (int i = 0; i < lost.Length; i++)
                if (!lost[i]) count++;
            return count;
        }
    }

    public void SetLimitText(string text)
    {
        if (limitText != null) limitText.text = text;
    }
    
    private void CacheThresholds()
    {
        if (thresholds == null || thresholds.Length != stars.Length)
        {
            thresholds = new float[stars.Length];
            lost = new bool[stars.Length];
        }

        Canvas.ForceUpdateCanvases();

        var corners = new Vector3[4];
        progressBar.rectTransform.GetWorldCorners(corners);
        float left = corners[0].x;
        float right = corners[2].x;
        bool fillsFromRight = progressBar.fillOrigin == (int)Image.OriginHorizontal.Right;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) { thresholds[i] = 0f; continue; }

            float t = Mathf.InverseLerp(left, right, stars[i].rectTransform.position.x);
            thresholds[i] = fillsFromRight ? 1f - t : t;
        }
    }

    private void OnDestroy()
    {
        foreach (Image star in stars)
            if (star != null) star.transform.DOKill();
    }
}
