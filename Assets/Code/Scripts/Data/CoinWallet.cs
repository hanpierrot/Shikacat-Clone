using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoinWallet
{
    public const int StartingCoins = 200;
    private const string Key = "Shikacat.Coins";

    private static int balance;
    private static bool loaded;

    public static event Action<int> Changed;

    public static int Balance
    {
        get
        {
            EnsureLoaded();
            return balance;
        }
    }

    public static void Add(int amount)
    {
        if (amount <= 0) return;

        EnsureLoaded();
        balance += amount;
        Save();
    }
    
    public static bool TrySpend(int amount)
    {
        EnsureLoaded();
        if (amount < 0 || balance < amount) return false;

        balance -= amount;
        Save();
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        loaded = false;
        Changed?.Invoke(Balance);
    }
    
    private static void EnsureLoaded()
    {
        if (loaded) return;
        balance = PlayerPrefs.GetInt(Key, StartingCoins);
        loaded = true;
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(Key, balance);
        PlayerPrefs.Save();
        Changed?.Invoke(balance);
    }
}
