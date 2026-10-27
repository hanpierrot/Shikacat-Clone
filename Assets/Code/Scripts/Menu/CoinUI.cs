using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private RectTransform icon;
    [SerializeField] private Button button;
    [SerializeField] private HomeScrollSnap snap;
    [SerializeField] private int shopPage;
    [SerializeField] private RectTransform effect;
    [SerializeField] private float countTime = 0.4f;

    private int displayed;
    private Tween countTween;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() =>
        {
            AudioManager.PlaySfx(Sfx.Click);
            snap.GoTo(shopPage);
        });
    }

    private void OnEnable()
    {
        displayed = CoinWallet.Balance;
        Render(displayed);
        CoinWallet.Changed += HandleChanged;
    }

    private void OnDisable()
    {
        CoinWallet.Changed -= HandleChanged;
        countTween?.Kill();
    }

    public void PlayEffect()
    {
        if (effect == null) return;

        effect.position = icon.position;
        effect.gameObject.SetActive(false);
        effect.gameObject.SetActive(true);
        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear();
            ps.Play();
        }
    }

    private void HandleChanged(int balance)
    {
        countTween?.Kill();
        if (balance <= displayed)
        {
            displayed = balance;
            Render(balance);
            return;
        }

        countTween = DOTween.To(() => displayed, v =>
        {
            displayed = v;
            Render(v);
        }, balance, countTime).SetUpdate(true).SetTarget(this);
    }

    private void Render(int value) => amountText.text = value.ToString();
}
