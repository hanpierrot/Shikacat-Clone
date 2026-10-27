using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Shikacat/Booster Database")]
public class BoosterDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public BoosterType boosterType;
        public string displayName;
        public int levelUnlock;
        public int valueStart;
        public int valueOneBuy;
        public int priceOneBuy;
        public Sprite icon;
    }

    public Entry[] entries;

    public Entry Get(BoosterType type)
    {
        foreach (Entry e in entries)
            if (e.boosterType == type) return e;
        return null;
    }
}
