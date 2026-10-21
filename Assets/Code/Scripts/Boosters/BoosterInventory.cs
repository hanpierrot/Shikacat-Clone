using System;
using UnityEngine;

public enum BoosterType
{
    Hint = 0,
    Freeze = 1,
    Reveal = 2
}

public static class BoosterInventory
{
    public const int Types = 3;

    private const string CountKey = "Shikacat.Booster.Count.";
    private const string GrantedKey = "Shikacat.Booster.Granted.";

    private static readonly int[] counts = new int[Types];
    private static readonly bool[] granted = new bool[Types];
    private static bool loaded;

    public static event Action<BoosterType, int> Changed;

    public static int Count(BoosterType type)
    {
        EnsureLoaded();
        return counts[(int)type];
    }

    public static bool IsGranted(BoosterType type)
    {
        EnsureLoaded();
        return granted[(int)type];
    }

    public static void MarkGranted(BoosterType type)
    {
        EnsureLoaded();
        granted[(int)type] = true;
        PlayerPrefs.SetInt(GrantedKey + (int)type, 1);
        PlayerPrefs.Save();
    }
    
    public static void Add(BoosterType type, int amount)
    {
        if (amount <= 0) return;
        
        EnsureLoaded();
        counts[(int)type] += amount;
        Save(type);
    }

    public static bool TryConsume(BoosterType type)
    {
        EnsureLoaded();
        if (counts[(int)type] <= 0) return false;
        
        counts[(int)type]--;
        Save(type);
        return true;
    }

    public static void Clear()
    {
        for (int i = 0; i < Types; i++)
        {
            counts[i] = 0;
            granted[i] = false;
            PlayerPrefs.DeleteKey(CountKey + i);
            PlayerPrefs.DeleteKey(GrantedKey + i);
        }
        PlayerPrefs.Save();
        loaded = true;

        for (int i = 0; i < Types; i++)
            Changed?.Invoke((BoosterType)i, 0);
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        
        for (int i = 0; i < Types; i++)
        {
            counts[i] = PlayerPrefs.GetInt(CountKey + i, 0);
            granted[i] = PlayerPrefs.GetInt(GrantedKey + i, 0) == 1;
        }
        loaded = true;
    }
    
    private static void Save(BoosterType type)
    {
        PlayerPrefs.SetInt(CountKey + (int)type, counts[(int)type]);
        PlayerPrefs.Save();
        Changed?.Invoke(type, counts[(int)type]);
    }
}
