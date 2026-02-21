using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioAlchemist : MonoBehaviour
{
    #region Public config

    public static AudioAlchemist Instance;

    [Header("Sound Data (populate in inspector)")]
    public SoundSubject[] soundSubjects;

    [Header("Mixer")]
    public AudioMixer mixer;
    public string masterParam = "Master";
    public string musicParam = "Music";
    public string sfxParam = "SFX";
    public string uiParam = "UI";

    [Header("Pools & Limits")]
    [Tooltip("Maximum simultaneous SFX sources in the pool. Hard limit; replacement by priority.")]
    public int maxSfxSources = 24;

    [Tooltip("UI pool size (small short one-shots like clicks).")]
    public int uiPoolSize = 6;

    [Header("Misc")]
    public bool destroyOnLoad = false;
    public bool logWarnings = false;

    #endregion

    #region Internal structures

    private Dictionary<AudioID, Sound> dict = new Dictionary<AudioID, Sound>();
    private Dictionary<string, Sound[]> groups = new Dictionary<string, Sound[]>();

    private List<PlayingInstance> sfxActive = new List<PlayingInstance>(32);
    private Queue<AudioSource> sfxIdle = new Queue<AudioSource>();

    private Queue<AudioSource> uiPool = new Queue<AudioSource>();

    private AudioSource musicA;
    private AudioSource musicB;
    private AudioSource activeMusic;
    private AudioSource idleMusic;

    private AudioSource ambienceSource;

    private Dictionary<AudioSource, Coroutine> runningFades = new Dictionary<AudioSource, Coroutine>();

    private float timeCounter = 0f;

    #endregion

    #region Unity lifecycle

    private void Awake()
    {
        InitSingleton();
        BuildDictionary();
        PreparePoolsAndChannels();
    }

    private void InitSingleton()
    {
        if (destroyOnLoad) return;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Build / Setup

    private void BuildDictionary()
    {
        dict.Clear();
        groups.Clear();

        if (soundSubjects == null) return;

        foreach (var subject in soundSubjects)
        {
            if (subject == null) continue;

            if (!groups.ContainsKey(subject.groupName))
                groups.Add(subject.groupName, subject.sounds);

            foreach (var s in subject.sounds)
            {
                if (s == null || s.id == AudioID.None) continue;

                if (!dict.ContainsKey(s.id))
                    dict.Add(s.id, s);

                // Initialize dedicated AudioSource for loops
                if (s.loop && s.source == null)
                {
                    var src = gameObject.AddComponent<AudioSource>();
                    ConfigureSourceForSound(src, s);
                    s.source = src;
                }
            }
        }
    }

    private void PreparePoolsAndChannels()
    {
        for (int i = 0; i < maxSfxSources; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = null;
            sfxIdle.Enqueue(src);
        }

        for (int i = 0; i < uiPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = null;
            uiPool.Enqueue(src);
        }

        musicA = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();
        SetupMusicSource(musicA);
        SetupMusicSource(musicB);
        activeMusic = musicA;
        idleMusic = musicB;

        ambienceSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.playOnAwake = false;
        ambienceSource.loop = true;
        ambienceSource.spatialBlend = 0f;
    }

    private void SetupMusicSource(AudioSource s)
    {
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f;
        s.volume = 1f;
        s.priority = 128;
    }

    private void ConfigureSourceForSound(AudioSource src, Sound s)
    {
        src.playOnAwake = false;
        src.clip = s.clip;
        src.loop = s.loop;
        src.volume = s.volume;
        src.pitch = s.GetRandomPitch();
        src.spatialBlend = s.spatial;
        src.outputAudioMixerGroup = s.mixerGroup;
        src.priority = Mathf.Clamp(s.priority, 0, 256);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a sound by AudioID.
    /// If the sound is marked as "loop", it ensures a dedicated AudioSource is created
    /// and handles optional fade-in.
    /// Otherwise, it is played as a one-shot through the SFX pool.
    /// </summary>
    public void Play(AudioID id)
    {
        if (!dict.TryGetValue(id, out var s)) return;
    
        if (s.loop)
        {
            // Ensure a dedicated AudioSource exists for looping sounds
            if (s.source == null)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                ConfigureSourceForSound(s.source, s);
            }
    
            // Refresh dynamic properties
            s.source.clip = s.clip;
            s.source.outputAudioMixerGroup = s.mixerGroup;
            s.source.spatialBlend = s.spatial;
    
            // Start if not currently playing
            if (!s.source.isPlaying)
                s.source.Play();
    
            // Apply fade-in if requested
            if (s.fadeIn)
                StartFade(s.source, s.volume, s.fadeInDuration, true);
            else
                s.source.volume = s.volume; 
        }
        else
        {
            // Normal SFX playback through the pooled non-looping system
            PlayOneShot(id);
        }
    }
    
    /// <summary>
    /// Stops a sound by AudioID.
    /// For looping sounds, fade-out may be applied.
    /// For one-shots, any active matching AudioSource instances are stopped and recycled.
    /// </summary>
    public void Stop(AudioID id)
    {
        if (!dict.TryGetValue(id, out var s)) return;
    
        if (s.loop && s.source != null)
        {
            // Apply fade-out if enabled, otherwise stop immediately
            if (s.fadeOut)
                StartFadeAndStop(s.source, s.fadeOutDuration);
            else
                s.source.Stop();
        }
        else
        {
            // Stop all active one-shot sources that match the clip
            for (int i = sfxActive.Count - 1; i >= 0; i--)
            {
                if (sfxActive[i].clip == s.clip)
                    StopAndRecycle(sfxActive[i]);
            }
        }
    }
    
    /// <summary>
    /// Plays a one-shot SFX using optional overrides for volume, pitch, or priority.
    /// </summary>
    public void PlayOneShot(AudioID id, float? overrideVolume = null, float? overridePitch = null, int? overridePriority = null)
    {
        if (!dict.TryGetValue(id, out var s)) return;
    
        PlaySfxInternal(s, overrideVolume, overridePitch, overridePriority);
    }
    
    /// <summary>
    /// Selects a random sound from a named group and plays it as a one-shot.
    /// Useful for footsteps, impacts, UI variations, etc.
    /// </summary>
    public void PlayOneShotRandom(string group)
    {
        if (!groups.TryGetValue(group, out var arr) || arr == null || arr.Length == 0) return;
    
        var s = arr[UnityEngine.Random.Range(0, arr.Length)];
        PlaySfxInternal(s, null, null, null);
    }
    
    /// <summary>
    /// Crossfades music from the active track to the requested AudioID.
    /// Uses the dual-source system (activeMusic and idleMusic) for seamless transitions.
    /// </summary>
    public void PlayMusic(AudioID id, float crossfadeDuration = 1f)
    {
        if (!dict.TryGetValue(id, out var s) || s.clip == null) return;
    
        // Prepare idle source to fade in as the new active track
        idleMusic.clip = s.clip;
        idleMusic.outputAudioMixerGroup = s.mixerGroup;
        idleMusic.volume = 0f;
        idleMusic.loop = true;
        idleMusic.pitch = s.GetRandomPitch();
        idleMusic.Play();
    
        // Perform crossfade
        StartCoroutine(CrossfadeMusic(activeMusic, idleMusic, crossfadeDuration, s.volume));
    
        // Swap references (idle becomes active)
        var prevActive = activeMusic;
        activeMusic = idleMusic;
        idleMusic = prevActive;
    }
    
    /// <summary>
    /// Stops currently playing music, applying a fade-out if desired.
    /// </summary>
    public void StopMusic(float fadeDuration = 0.5f)
    {
        if (activeMusic == null) return;
        StartFadeAndStop(activeMusic, fadeDuration);
    }
    
    /// <summary>
    /// Plays ambience using the dedicated ambience AudioSource,
    /// applying a fade-in for smooth environmental transitions.
    /// </summary>
    public void PlayAmbience(AudioID id, float fade = 0.5f)
    {
        if (!dict.TryGetValue(id, out var s)) return;
    
        ambienceSource.clip = s.clip;
        ambienceSource.outputAudioMixerGroup = s.mixerGroup;
        ambienceSource.volume = 0f;
        ambienceSource.loop = true;
        ambienceSource.Play();
    
        StartFade(ambienceSource, s.volume, fade, true);
    }
    
    /// <summary>
    /// Stops ambience with optional fade-out.
    /// </summary>
    public void StopAmbience(float fade = 0.5f)
    {
        StartFadeAndStop(ambienceSource, fade);
    }
    
    /// <summary>
    /// Plays a UI sound using the UI audio pool.
    /// UI sounds are isolated from game SFX and always non-spatial.
    /// </summary>
    public void PlayUI(AudioID id)
    {
        if (!dict.TryGetValue(id, out var s)) return;
    
        if (uiPool.Count == 0)
        {
            if (logWarnings) Debug.LogWarning("[AudioAlchemistPro] UI pool exhausted.");
            return;
        }
    
        var src = uiPool.Dequeue();
        ConfigureSourceForSound(src, s);
        src.loop = false;
        src.Play();
    
        StartCoroutine(RecycleUIWhenDone(src));
    }
    
    /// <summary>Sets Master mixer parameter.</summary>
    public void SetMaster(float v) => SetMixer(masterParam, v);
    
    /// <summary>Sets Music mixer parameter.</summary>
    public void SetMusic(float v) => SetMixer(musicParam, v);
    
    /// <summary>Sets SFX mixer parameter.</summary>
    public void SetSFX(float v) => SetMixer(sfxParam, v);
    
    /// <summary>Sets UI mixer parameter.</summary>
    public void SetUI(float v) => SetMixer(uiParam, v);
    
    public float GetMixer01(string param)
    {
        if (!mixer) return 1f;
        if (mixer.GetFloat(param, out float db))
        {
            // db → linear (0–1)
            return Mathf.Pow(10f, db / 20f);
        }
        return 1f;
    }
    
    #endregion

    #region Internal SFX / Pool logic

    private void PlaySfxInternal(Sound s, float? overrideVolume, float? overridePitch, int? overridePriority)
    {
        if (s == null || s.clip == null) return;

        float vol = overrideVolume ?? s.GetRandomVolume();
        float pitch = overridePitch ?? s.GetRandomPitch();
        int priority = overridePriority ?? s.priority;

        AudioSource src = null;
        if (sfxIdle.Count > 0) src = sfxIdle.Dequeue();
        else
        {
            int lowestIndex = -1;
            int lowestPriority = int.MaxValue;
            float oldestTime = float.MaxValue;

            for (int i = 0; i < sfxActive.Count; i++)
            {
                var inst = sfxActive[i];
                if (inst.priority < lowestPriority || (inst.priority == lowestPriority && inst.startTime < oldestTime))
                {
                    lowestPriority = inst.priority;
                    oldestTime = inst.startTime;
                    lowestIndex = i;
                }
            }

            if (lowestIndex >= 0 && priority >= lowestPriority)
            {
                var victim = sfxActive[lowestIndex];
                src = victim.source;
                StopAndRecycle(victim);
                if (sfxIdle.Count > 0) src = sfxIdle.Dequeue();
            }
            else
            {
                if (logWarnings) Debug.Log("[AudioAlchemistPro] SFX dropped (pool full, low priority).");
                return;
            }
        }

        src.clip = s.clip;
        src.volume = vol;
        src.pitch = pitch;
        src.spatialBlend = s.spatial;
        src.loop = false;
        src.outputAudioMixerGroup = s.mixerGroup;
        src.priority = Mathf.Clamp(priority, 0, 256);
        src.Play();

        sfxActive.Add(new PlayingInstance { source = src, clip = s.clip, priority = priority, startTime = timeCounter });

        StartCoroutine(RecycleWhenDone(src));
    }

    private IEnumerator RecycleWhenDone(AudioSource src)
    {
        yield return new WaitWhile(() => src != null && src.isPlaying);

        for (int i = sfxActive.Count - 1; i >= 0; i--)
            if (sfxActive[i].source == src) sfxActive.RemoveAt(i);

        if (src != null) sfxIdle.Enqueue(src);
    }

    private IEnumerator RecycleUIWhenDone(AudioSource src)
    {
        yield return new WaitWhile(() => src != null && src.isPlaying);
        if (src != null)
        {
            src.clip = null;
            uiPool.Enqueue(src);
        }
    }

    private void StopAndRecycle(PlayingInstance inst)
    {
        if (inst == null || inst.source == null) return;

        StopFade(inst.source);
        inst.source.Stop();
        sfxActive.Remove(inst);
        sfxIdle.Enqueue(inst.source);
    }

    #endregion

    #region Fade system

    private void StartFade(AudioSource src, float targetVolume, float duration, bool fadeInPreferred)
    {
        if (src == null) return;
        StopFade(src);

        if (fadeInPreferred && !src.isPlaying) src.Play();

        Coroutine c = StartCoroutine(FadeRoutine(src, targetVolume, duration));
        runningFades[src] = c;
    }

    private void StartFadeAndStop(AudioSource src, float duration)
    {
        if (src == null) return;
        StopFade(src);

        Coroutine c = StartCoroutine(FadeOutAndStopRoutine(src, duration));
        runningFades[src] = c;
    }

    private void StopFade(AudioSource src)
    {
        if (src == null) return;
        if (runningFades.TryGetValue(src, out var c))
        {
            StopCoroutine(c);
            runningFades.Remove(src);
        }
    }

    private IEnumerator FadeRoutine(AudioSource src, float targetVolume, float duration)
    {
        if (src == null) yield break;
        float start = src.volume;
        float t = 0f;
        if (duration <= 0f)
        {
            src.volume = targetVolume;
            runningFades.Remove(src);
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(start, targetVolume, t / duration);
            yield return null;
        }

        src.volume = targetVolume;
        runningFades.Remove(src);
    }

    private IEnumerator FadeOutAndStopRoutine(AudioSource src, float duration)
    {
        if (src == null) yield break;
        float start = src.volume;
        float t = 0f;

        if (duration <= 0f)
        {
            src.Stop();
            runningFades.Remove(src);
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }

        src.volume = start;
        src.Stop();
        runningFades.Remove(src);
    }

    #endregion

    #region Music crossfade

    private IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float duration, float targetVolume)
    {
        if (from == null || to == null) yield break;

        StopFade(from);
        StopFade(to);

        float t = 0f;
        float startVolFrom = from.volume;
        float startVolTo = to.volume;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            from.volume = Mathf.Lerp(startVolFrom, 0f, a);
            to.volume = Mathf.Lerp(startVolTo, targetVolume, a);
            yield return null;
        }

        from.volume = 0f;
        from.Stop();
        to.volume = targetVolume;
    }

    #endregion

    #region Mixer / utils

    private void SetMixer(string param, float value)
    {
        if (!mixer) return;
        float v = Mathf.Clamp(value, 0.0001f, 1f);
        mixer.SetFloat(param, Mathf.Log10(v) * 20f);
    }

    public bool IsPlaying(AudioID id)
    {
        if (!dict.TryGetValue(id, out var s)) return false;

        if (s.loop) return s.source != null && s.source.isPlaying;

        for (int i = 0; i < sfxActive.Count; i++)
            if (sfxActive[i].clip == s.clip) return sfxActive[i].source.isPlaying;

        return false;
    }

    public void PlayCustom(int enumIndex, float volume, float pitch, bool loop)
    {
        AudioID id = (AudioID)enumIndex;

        if (!dict.TryGetValue(id, out var s) || s.clip == null) return;

        AudioSource src = s.source;

        if (loop)
        {
            if (src == null)
            {
                src = gameObject.AddComponent<AudioSource>();
                ConfigureSourceForSound(src, s);
                s.source = src;
            }
            src.clip = s.clip;
            src.volume = volume;
            src.pitch = pitch;
            src.loop = true;
            src.Play();
        }
        else
        {
            PlaySfxInternal(s, volume, pitch, null);
        }
    }

    public Sound Get(AudioID id)
    {
        dict.TryGetValue(id, out var s);
        return s;
    }

    #endregion

    #region Data classes

    [Serializable]
    public class Sound
    {
        public AudioID id = AudioID.None;
        public AudioClip clip;

        [Header("Randomization")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 1f)] public float volumeMin = 1f;
        [Range(0f, 1f)] public float volumeMax = 1f;

        [Range(.1f, 2f)] public float pitch = 1f;
        [Range(.1f, 2f)] public float pitchMin = 1f;
        [Range(.1f, 2f)] public float pitchMax = 1f;

        [Range(0f, 1f)] public float spatial = 0f;
        public AudioMixerGroup mixerGroup;

        public bool loop = false;
        public bool fadeIn = false;
        public bool fadeOut = false;

        // Antes: public float fadeDuration = 0.2f;
        [Tooltip("Duración del fade in (segundos)")]
        public float fadeInDuration = 0.2f;

        [Tooltip("Duración del fade out (segundos)")]
        public float fadeOutDuration = 0.2f;


        [Range(0, 100)] public int priority = 50;

        [HideInInspector] public AudioSource source;

        public float GetRandomVolume()
        {
            if (volumeMin <= 0f && volumeMax <= 0f) return 0f;
            float min = Mathf.Min(volumeMin, volumeMax);
            float max = Mathf.Max(volumeMin, volumeMax);
            if (Mathf.Approximately(min, max)) return volume;
            return UnityEngine.Random.Range(min, max) * volume;
        }

        public float GetRandomPitch()
        {
            float min = Mathf.Min(pitchMin, pitchMax);
            float max = Mathf.Max(pitchMin, pitchMax);
            if (Mathf.Approximately(min, max)) return pitch;
            return UnityEngine.Random.Range(min, max);
        }
    }

    [Serializable]
    public class SoundSubject
    {
        public string groupName;
        public Sound[] sounds;
    }

    private class PlayingInstance
    {
        public AudioSource source;
        public AudioClip clip;
        public int priority;
        public float startTime;
    }

    #endregion
}
