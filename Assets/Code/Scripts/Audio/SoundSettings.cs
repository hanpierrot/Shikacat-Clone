using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundSettings
{
    private const string MusicKey = "Shikacat.Audio.Music";
    private const string SfxKey = "Shikacat.Audio.Sfx";
    private const string VibrateKey = "Shikacat.Vibrate";

    public static event Action Changed;

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static bool VibrateOn
    {
        get => PlayerPrefs.GetInt(VibrateKey, 1) == 1;
        set { PlayerPrefs.SetInt(VibrateKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static bool SfxOn
    {
        get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
        set { PlayerPrefs.SetInt(SfxKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }
}
