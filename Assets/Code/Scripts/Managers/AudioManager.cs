using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    [SerializeField] private AudioClipDataBase database;
    [SerializeField] private int poolSize = 8;

    private AudioSource musicSource;
    private AudioSource[] pool;
    private int nextPool;
    private float duck = 1f;
    
    private readonly Dictionary<Sfx, AudioClipDataBase.Entry> entries = new Dictionary<Sfx, AudioClipDataBase.Entry>();
    private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
    private readonly Dictionary<Sfx, AudioSource> retrigger = new Dictionary<Sfx, AudioSource>();
    
    public static void PlaySfx(Sfx id)
    {
        if (Instance != null) Instance.Play(id);
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        
        DontDestroyOnLoad(gameObject);
        
        musicSource = CreateSource();
        musicSource.loop = true;

        pool = new AudioSource[Mathf.Max(1, poolSize)];
        for (int i = 0; i < pool.Length; i++) pool[i] = CreateSource();

        foreach (AudioClipDataBase.Entry e in database.sfx)
            entries[e.id] = e;

        SoundSettings.Changed += ApplyMusicVolume;
    }
    
    protected override void OnDestroy()
    {
        SoundSettings.Changed -= ApplyMusicVolume;
        DOTween.Kill(this);
        base.OnDestroy();
    }

    private void OnApplicationPause(bool paused)
    {
        if (musicSource == null) return;
        if (paused) musicSource.Pause();
        else musicSource.UnPause();
    }

    private AudioSource CreateSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }
    
    public void Play(Sfx id)
    {
        if (!entries.TryGetValue(id, out AudioClipDataBase.Entry e)) return;

        float now = Time.unscaledTime;
        if (e.dontReplaceSeconds > 0f && lastPlayed.TryGetValue(id, out float last) && now - last < e.dontReplaceSeconds)
            return;
        lastPlayed[id] = now;

        Haptics.Play(e.haptic);

        if (!SoundSettings.SfxOn || e.clip == null) return;

        if (e.restartOnRetrigger)
        {
            PlayRestart(e);
            return;
        }

        AudioSource source = pool[nextPool];
        nextPool = (nextPool + 1) % pool.Length;
        source.PlayOneShot(e.clip, e.volume);
    }

    
    private void PlayRestart(AudioClipDataBase.Entry e)
    {
        if (!retrigger.TryGetValue(e.id, out AudioSource source))
        {
            source = CreateSource();
            retrigger[e.id] = source;
        }

        source.Stop();
        source.clip = e.clip;
        source.volume = e.volume;
        source.time = 0f;

        double start = AudioSettings.dspTime;
        source.PlayScheduled(start);
        if (e.cutAfterSeconds > 0f)
            source.SetScheduledEndTime(start + e.cutAfterSeconds);
    }
    
    public void PlayMusic(float fadeTime = 1f)
    {
        if (database.music == null) return;
        if (musicSource.clip == database.music && musicSource.isPlaying) return;

        musicSource.clip = database.music;
        musicSource.volume = 0f;
        musicSource.Play();

        DOTween.Kill("musicFade");
        DOTween.To(() => musicSource.volume, v => musicSource.volume = v, MusicTarget, fadeTime).SetId("musicFade").SetUpdate(true);
    }

    public void DuckMusic(float level, float fadeTime, float holdTime)
    {
        DOTween.Kill("musicDuck");
        Sequence s = DOTween.Sequence().SetId("musicDuck").SetUpdate(true);
        s.Append(DOTween.To(() => duck, v => { duck = v; ApplyMusicVolume(); }, level, fadeTime));
        s.AppendInterval(holdTime);
        s.Append(DOTween.To(() => duck, v => { duck = v; ApplyMusicVolume(); }, 1f, 0.6f));
    }

    private float MusicTarget => SoundSettings.MusicOn ? database.musicVolume * duck : 0f;

    private void ApplyMusicVolume()
    {
        if (musicSource == null || DOTween.IsTweening("musicFade")) return;
        musicSource.volume = MusicTarget;
    }
}
