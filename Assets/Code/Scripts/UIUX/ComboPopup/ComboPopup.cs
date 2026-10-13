using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboPopup : MonoBehaviour
{
    [Serializable]
    public struct Tier
    {
        public int minCombo;
        public Sprite sprite;
    }

    [SerializeField] private ComboPopupItem itemPrefab;
    [SerializeField] private Transform poolRoot; 
    [SerializeField] private int initialPoolSize = 3;
    [SerializeField] private Tier[] tiers;

    public event Action<int, Vector3> Shown;

    private readonly List<ComboPopupItem> pool = new List<ComboPopupItem>();
    private bool poolReady;

    private void EnsurePool()
    {
        if (poolReady) return;
        poolReady = true;

        if (poolRoot == null) poolRoot = transform;

        var existing = poolRoot.GetComponentsInChildren<ComboPopupItem>(true);
        pool.AddRange(existing);
        foreach (ComboPopupItem item in existing)
            item.gameObject.SetActive(false);

        while (pool.Count < initialPoolSize)
            CreateItem();
    }
    
    private ComboPopupItem CreateItem()
    {
        ComboPopupItem item = Instantiate(itemPrefab, poolRoot);
        item.gameObject.SetActive(false);
        pool.Add(item);
        return item;
    }
    
    private ComboPopupItem GetItem()
    {
        EnsurePool();

        foreach (ComboPopupItem item in pool)
            if (!item.gameObject.activeSelf) return item;

        return CreateItem();
    }
    
    public bool TryShow(int combo, Vector3 worldPosition)
    {
        int tierIndex = FindTier(combo);
        if (tierIndex < 0 || tiers[tierIndex].sprite == null || itemPrefab == null) return false;

        GetItem().Play(tiers[tierIndex].sprite, worldPosition);
        Shown?.Invoke(tierIndex, worldPosition);
        return true;
    }
    
    public void Hide()
    {
        foreach (ComboPopupItem item in pool)
            item.Hide();
    }

    private int FindTier(int combo)
    {
        int found = -1;
        for (int i = 0; i < tiers.Length; i++)
            if (combo >= tiers[i].minCombo) found = i;
        return found;
    }
}
