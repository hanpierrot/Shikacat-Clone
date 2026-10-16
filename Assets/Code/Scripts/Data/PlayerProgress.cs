using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerProgress
{
    private const string LevelKey = "Shikacat.CurrentLevelIndex";
    
    public static int LoadLevelIndex() => PlayerPrefs.GetInt(LevelKey, 0);
    
    public static void SaveLevelIndex(int index)
    {
        PlayerPrefs.SetInt(LevelKey, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(LevelKey);
        PlayerPrefs.Save();
    }
}
