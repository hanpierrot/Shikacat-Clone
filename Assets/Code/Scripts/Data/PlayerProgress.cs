using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerProgress
{
    private const string LevelKey = "Shikacat.CurrentLevelIndex";
    private const string TutorialKey = "Shikacat.TutorialDone";

    public static bool TutorialDone => PlayerPrefs.GetInt(TutorialKey, 0) == 1;

    public static bool HasSavedProgress => PlayerPrefs.HasKey(LevelKey);

    // Chỉ người chơi mới hoàn toàn (chưa có tiến trình lưu) mới phải xem tutorial mở đầu.
    // Save cũ từ trước khi có tutorial hoặc người chơi đã qua level 1 thì không bị bắt xem lại.
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
        BoosterTutorial.ResetSeen();   // Mọi đường xoá save (menu Editor, cheat) đều phải xoá cả cờ hướng dẫn
        MechanicIntro.ResetSeen();
        PlayerPrefs.Save();
    }
}
