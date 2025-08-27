using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Pool;

//note: all audio here uses special blend so if you put the source on another object and that object is far away, it will come from that direction and sound far away

public enum AudioGroup
{
    Master,
    Music,
    SFX
}

public class AudioManager : Singleton<AudioManager>
{
    private readonly Dictionary<string, AudioSource> _stableSources = new();
    private readonly Dictionary<string, (Coroutine, AudioSource)> _temporarySources = new();
    private readonly ObjectPool<GameObject> _audioPool = new ObjectPool<GameObject>(
        createFunc: () =>
        {
            var obj = new GameObject("PooledAudioSource");
            obj.SetActive(false);

            var audio = obj.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1;
            obj.AddComponent<FollowObject>().ignoreZ = false;

            return obj;
        },
        actionOnGet: obj => obj.SetActive(true),
        actionOnRelease: obj =>
        {
            OnSourceReleased?.Invoke(obj.GetComponent<AudioSource>());
            obj.SetActive(false);
            obj.GetComponent<AudioSource>().Stop();
            obj.GetComponent<FollowObject>().target = null;
        }
    );

    private static readonly int _maxAudioPoolSize = 50;

    private static AudioListener _currentActiveLisener = null;

    protected override string SingletonName => "AudioManager";

    public static event UnityAction<AudioSource> OnSourceReleased;

    private AudioMixer _mainMixer = null;

    /// <summary>
    /// AudioManager.Get()[AudioGroup.Master] = 100;
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public float this[AudioGroup group]
    {
        get
        {
            return _mainMixer.GetFloat(group.ToString() + "Volume", out var volume) ? volume : 0;
        }
        set
        {
            _mainMixer.SetFloat(group.ToString() + "Volume", Mathf.Clamp(value, 0, 100) - 80);
        }
    }

    protected override void Awake()
    {
        _mainMixer = Resources.Load<AudioMixer>("MainMixer");
        if (!_mainMixer) throw new System.Exception("No 'MainMixer' found.");
    }

    /// <summary>
    /// Creates an audio source that stays until deleted
    /// </summary>
    /// <param name="sourceName">The name of the audio source</param>
    /// <param name="clip">The clip to be assigned to the created source</param>
    /// <param name="overrideExistingSource">Whether to override any already existing source with this same name (destroying the other source),
    /// if this is false the function will return null when finding a source with the same name</param>
    /// <param name="audioOrigin">The origin of the audio, when null the source is the found audio listener</param>
    /// <returns>the newly created audio source</returns>
    public static AudioSource CreateStableSource(string sourceName, AudioClip clip = null, AudioGroup group = AudioGroup.Music, bool overrideExistingSource = false, GameObject audioOrigin = null)
    {
        if (Get()._stableSources.TryGetValue(sourceName, out var sameNamedSource) && !overrideExistingSource)
            return null;

        GameObject audioObject = null;

        if (overrideExistingSource && sameNamedSource != null)
        {
            sameNamedSource.Stop();
            audioObject = sameNamedSource.gameObject;
        }

        audioObject ??= Get()._audioPool.TryGet(_maxAudioPoolSize);
        if (audioObject == null) return null;

        audioOrigin ??= (GetActiveLisener().gameObject ?? null);

        var createdSource = audioObject.GetComponent<AudioSource>();
        createdSource.clip = clip;
        createdSource.volume = 1;
        createdSource.outputAudioMixerGroup = Get()._mainMixer.FindMatchingGroups(group.ToString()).FirstOrDefault();

        audioObject.GetComponent<FollowObject>().target = audioOrigin.transform;

        Get()._stableSources.Add(sourceName, createdSource);

        return createdSource;
    }

    /// <summary>
    /// Gets the existing stable source with the given name
    /// </summary>
    /// <param name="sourceName">The name of the requsted source</param>
    /// <returns>The found source, or null if that source does not exist</returns>
    public static AudioSource GetStableSource(string sourceName)
    {
        if (Get()._stableSources.TryGetValue(sourceName, out var foundSource))
            return foundSource;

        return null;
    }

    /// <summary>
    /// Deletes an existing stable source with the given name
    /// </summary>
    /// <param name="sourceName">The name of the source to delete</param>
    public static void DeleteStableSource(string sourceName)
    {
        if (!Get()._stableSources.TryGetValue(sourceName, out var foundSource)) return;

        Get()._audioPool.Release(foundSource.gameObject);
        Get()._stableSources.Remove(sourceName);
    }

    /// <summary>
    /// A function for creating audio sources that audomatically get deleted after a period of time
    /// </summary>
    /// <param name="clip">the audio clip to be used, the function will return null if the clip is null</param>
    /// <param name="volume">The clips volume</param>
    /// <param name="repeats">Amount of times the clip repeats until destroyed</param>
    /// <param name="overrideExistingSource">If you play a temporary source when an already playing source has this the overrideKey, The playing source will be overwirtten by the new one</param>
    /// <param name="audioOrigin">The origin of the audio, when null the source is the found audio listener</param>
    /// <returns>The created audio source</returns>
    public static AudioSource PlayTemporarySource(AudioClip clip, float volume = 1, AudioGroup group = AudioGroup.SFX, uint repeats = 1, string overrideExistingSource = null, GameObject audioOrigin = null)
    {
        if (clip == null) return null;

        GameObject audioObject = null;

        if (overrideExistingSource != null && Get()._temporarySources.TryGetValue(overrideExistingSource, out var foundSource))
        {
            Get().StopCoroutine(foundSource.Item1);
            audioObject = foundSource.Item2.gameObject;
        }

        audioObject ??= Get()._audioPool.TryGet(_maxAudioPoolSize);
        if (audioObject == null) return null;

        audioOrigin ??= (GetActiveLisener().gameObject ?? null);

        var createdSource = audioObject.GetComponent<AudioSource>();
        createdSource.clip = clip;
        createdSource.volume = volume;
        createdSource.outputAudioMixerGroup = Get()._mainMixer.FindMatchingGroups(group.ToString()).FirstOrDefault();

        audioObject.GetComponent<FollowObject>().target = audioOrigin.transform;

        var routine = Get().StartCoroutine(
            TemporarySourceDeletion(createdSource, repeats, overrideExistingSource)
        );

        if (overrideExistingSource != null)
            Get()._temporarySources[overrideExistingSource] = (routine, createdSource);

        return createdSource;
    }

    /// <summary>
    /// Deletes the given source after a set amount of repeats
    /// </summary>
    private static IEnumerator TemporarySourceDeletion(AudioSource source, uint repeats, string overrideExistingSource = null)
    {
        if (source == null) yield break;

        if (repeats == 0)
        {
            if (overrideExistingSource != null && Get()._temporarySources.ContainsKey(overrideExistingSource))
                Get()._temporarySources.Remove(overrideExistingSource);

            Get()._audioPool.Release(source.gameObject);

            yield break;
        }

        source.Stop();
        source.Play();

        yield return new WaitUntil(() => !source.isPlaying);

        var routine = Get().StartCoroutine(
            TemporarySourceDeletion(source, repeats - 1, overrideExistingSource)
        );

        if (overrideExistingSource != null && Get()._temporarySources.ContainsKey(overrideExistingSource))
            Get()._temporarySources[overrideExistingSource] = (routine, source);
    }

    /// <summary>
    /// Gets the first or already found listener
    /// </summary>
    /// <returns>The current known listener, if no listener is known they attempt to find one, if non are found returns null</returns>
    private static AudioListener GetActiveLisener()
    {
        if (_currentActiveLisener) return _currentActiveLisener;

        _currentActiveLisener = FindFirstObjectByType<AudioListener>();

        return _currentActiveLisener;
    }

    protected override void OnSingletonDestroyed()
    {
        //clean up all stable sources if this AudioManager is somehow destroyed
        while (_stableSources.Count > 0)
            DeleteStableSource(_stableSources.Keys.First());
    }
}
