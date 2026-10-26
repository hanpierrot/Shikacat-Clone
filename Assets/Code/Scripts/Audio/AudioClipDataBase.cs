using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Sfx
{
    Click, EarnCoin, EarnReward, BlockTrue, LoadLevel, Win, Failed, Warning,
    EndTimeWarning, UnlockObstacle, PoofStar, Booster2, FreezeStart, FreezeAppear
}

[CreateAssetMenu(menuName = "Shikacat/Audio Clip Database")]
public class AudioClipDataBase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public float dontReplaceSeconds;
        public bool restartOnRetrigger;
        public float cutAfterSeconds; 
        public HapticType haptic;
    }

    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    public Entry[] sfx;
}
