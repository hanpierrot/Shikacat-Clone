using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CollectButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text amountText;                 // Text "x20" là con của Win_Coin, nằm dưới coin
    [SerializeField, FormerlySerializedAs("coinIcon")]
    private RectTransform flyCoinTemplate;                        // Mẫu (tắt sẵn) để nhân bản coin bay
    [SerializeField] private RectTransform flySource;             // Win_Coin: coin bay ra từ đây
    [SerializeField] private GameObject coinRoot;                 // Win_Coin: tắt khi nhận xong
    [SerializeField] private RectTransform walletTarget;
    
    [Header("Animation")]
    [SerializeField] private int flyCount = 6;
    [SerializeField] private float flyTime = 0.6f;
    [SerializeField] private float flyStagger = 0.06f;
    [SerializeField] private float countTime = 0.6f;
    
    public event Action Clicked;     // Bắt đầu animation cộng tiền (coin đã được cộng và lưu từ lúc thắng)
    public event Action Collected;   // Animation xong, nút đã tắt

    private int amount;
    private bool collecting;
    private Sequence sequence;

    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }

    public void Show(int reward)
    {
        Kill();
        amount = reward;
        collecting = false;

        amountText.text = $"x{reward}";
        button.interactable = true;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);

        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetTarget(transform);
    }

    public void Hide()
    {
        Kill();
        gameObject.SetActive(false);
    }

    private void OnClick()
    {
        if (collecting) return;
        collecting = true;
        button.interactable = false;

        Clicked?.Invoke();   // Coin đã được cộng và lưu khi thắng màn, nút chỉ chạy animation

        PlayCollectAnimation();
    }

    private void PlayCollectAnimation()
    {
        Kill();
        sequence = DOTween.Sequence().SetTarget(transform);

        sequence.Append(DOVirtual.Float(amount, 0f, countTime, v => amountText.text = $"x{Mathf.RoundToInt(v)}"));
        sequence.Join(transform.DOPunchScale(Vector3.one * 0.15f, countTime, 6, 0.6f));

        if (walletTarget != null && flyCoinTemplate != null)
        {
            Vector3 origin = (flySource != null ? flySource : flyCoinTemplate).position;
            for (int i = 0; i < flyCount; i++)
            {
                RectTransform flying = Instantiate(flyCoinTemplate, transform.parent);
                flying.position = origin;
                flying.localScale = Vector3.one;
                flying.gameObject.SetActive(false);

                float start = i * flyStagger;
                sequence.InsertCallback(start, () => flying.gameObject.SetActive(true));
                sequence.Insert(start, flying.DOMove(walletTarget.position, flyTime).SetEase(Ease.InQuad));
                sequence.InsertCallback(start + flyTime, () => Destroy(flying.gameObject));
            }
        }

        sequence.OnComplete(() =>
        {
            sequence = null;
            if (coinRoot != null) coinRoot.SetActive(false);
            gameObject.SetActive(false);
            Collected?.Invoke();
        });
    }

    private void Kill()
    {
        if (sequence != null && sequence.IsActive()) sequence.Kill();
        sequence = null;
        transform.DOKill();
    }

    private void OnDisable()
    {
        Kill();
    }
    
}
