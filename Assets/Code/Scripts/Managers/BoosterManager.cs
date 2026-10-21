using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoosterManager : MonoSingleton<BoosterManager>
{
    [Serializable]
    public class Entry
    {
        public BoosterType type;
        public int unlockLevel;
        public int price;
        public int packSize = 1;
    }

    [SerializeField] private Entry[] entries =
    {
        new Entry { type = BoosterType.Hint, unlockLevel = 5, price = 100, packSize = 1 },
        new Entry { type = BoosterType.Freeze, unlockLevel = 8, price = 150, packSize = 1 },
        new Entry { type = BoosterType.AutoBuild, unlockLevel = 12, price = 200, packSize = 1 }
    };
    
    [SerializeField] private int unlockGrant = 2;
    [SerializeField] private BoosterPurchasePopup purchasePopup;
    
    public event Action<BoosterType> Unlocked;
    public event Action<BoosterType> Used;
    
    private readonly Func<bool>[] handlers = new Func<bool>[BoosterInventory.Types];

    private void OnEnable()
    {
        GameManager.Instance.LevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= HandleLevelLoaded;
    }
    
    public void SetHandler(BoosterType type, Func<bool> handler) => handlers[(int)type] = handler;

    public bool IsUnlocked(BoosterType type) => BoosterInventory.IsGranted(type);

    public Entry GetEntry(BoosterType type)
    {
        foreach (Entry entry in entries)
            if(entry.type == type) return entry;
        return null;
    }

    private void HandleLevelLoaded()
    {
        GameManager gm = GameManager.Instance;
        
        if(gm.IsEditorStart) return;
        
        foreach (Entry entry in entries)
        {
            if (BoosterInventory.IsGranted(entry.type) || gm.CurrentLevelNumber < entry.unlockLevel) continue;

            BoosterInventory.Add(entry.type, unlockGrant);
            BoosterInventory.MarkGranted(entry.type);
            Unlocked?.Invoke(entry.type);
        }
    }

    public bool TryUse(BoosterType type)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || !IsUnlocked(type)) return false;
        
        if (BoosterInventory.Count(type) <= 0)
        {
            RequestPurchase(type);
            return false;
        }

        Func<bool> handler = handlers[(int)type];
        if (handler == null)
        {
            Debug.LogWarning($"BoosterManager: chưa có hiệu ứng cho {type}.");
            return false;
        }

        if (!handler()) return false;

        BoosterInventory.TryConsume(type);
        Used?.Invoke(type);
        return true;
    }
    
    public void RequestPurchase(BoosterType type)
    {
        Entry entry = GetEntry(type);
        if (purchasePopup == null || entry == null) return;

        purchasePopup.Show(type, entry.price, entry.packSize);
    }
    
    public bool TryBuy(BoosterType type)
    {
        Entry entry = GetEntry(type);
        if (entry == null || !CoinWallet.TrySpend(entry.price)) return false;

        BoosterInventory.Add(type, entry.packSize);
        return true;
    }
}
