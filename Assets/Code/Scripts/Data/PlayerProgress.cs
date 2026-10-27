using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerProgress
{
    private const string LevelKey = "Shikacat.CurrentLevelIndex";
    private const string TutorialKey = "Shikacat.TutorialDone";

    public static bool TutorialDone => PlayerPrefs.GetInt(TutorialKey, 0) == 1;

    public static bool HasSavedProgress => PlayerPrefs.HasKey(LevelKey);

    public static bool ShouldShowTutorial => !TutorialDone && !HasSavedProgress;

    public static void SetTutorialDone()
    {
        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.Save();
    }

    public static void ClearTutorial()
    {
        PlayerPrefs.DeleteKey(TutorialKey);
        PlayerPrefs.Save();
    }

    public static int LoadLevelIndex() => PlayerPrefs.GetInt(LevelKey, 0);
    
    public static void SaveLevelIndex(int index)
    {
        PlayerPrefs.SetInt(LevelKey, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(LevelKey);
        PlayerPrefs.DeleteKey(TutorialKey);
        BoosterTutorial.ResetSeen();
        MechanicIntro.ResetSeen();
        PlayerPrefs.Save();
    }
}
