using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Serializable]
    private struct Slot
    {
        public BoosterType type;
        public Button buyButton;
        public Image buyImage;
        public TMP_Text nameText;
        public TMP_Text countText;
        public TMP_Text priceText;
        public Image icon;
    }

    [SerializeField] private BoosterDatabase database;
    [SerializeField] private Slot[] slots;
    [SerializeField] private CoinUI coinUI;
    [SerializeField] private Color unaffordableTint = new Color(0.55f, 0.55f, 0.55f, 1f);

    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            BoosterDatabase.Entry e = database.Get(slots[i].type);
            if (e == null) continue;

            slots[i].nameText.text = e.displayName;
            slots[i].countText.text = "x" + e.valueOneBuy;
            slots[i].priceText.text = e.priceOneBuy.ToString();
            if (e.icon != null) slots[i].icon.sprite = e.icon;
            slots[i].buyButton.onClick.AddListener(() => Buy(index));
        }
    }

    private void OnEnable()
    {
        CoinWallet.Changed += HandleCoinsChanged;
        Refresh();

        foreach (Slot slot in slots)
        {
            slot.nameText.ForceMeshUpdate();
            slot.countText.ForceMeshUpdate();
            slot.priceText.ForceMeshUpdate();
        }
    }

    private void OnDisable()
    {
        CoinWallet.Changed -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int balance) => Refresh();

    private void Refresh()
    {
        foreach (Slot slot in slots)
        {
            BoosterDatabase.Entry e = database.Get(slot.type);
            if (e == null) continue;
            slot.buyImage.color = CoinWallet.Balance >= e.priceOneBuy ? Color.white : unaffordableTint;
        }
    }

    private void Buy(int index)
    {
        Slot slot = slots[index];
        BoosterDatabase.Entry e = database.Get(slot.type);

        if (!CoinWallet.TrySpend(e.priceOneBuy))
        {
            Transform t = slot.buyButton.transform;
            t.DOKill(true);
            t.DOShakePosition(0.25f, new Vector3(12f, 0f, 0f), 20, 90f, false, true).SetUpdate(true).SetTarget(t);
            return;
        }

        BoosterInventory.Add(slot.type, e.valueOneBuy);
        AudioManager.PlaySfx(Sfx.EarnCoin);
        if (coinUI != null) coinUI.PlayEffect();
    }
}
