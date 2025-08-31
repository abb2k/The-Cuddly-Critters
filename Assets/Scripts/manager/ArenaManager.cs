using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ArenaManager : Singleton<ArenaManager>
{
    private ArenaHolder currentArena;
    private EnemyForArena enemyForArena;
    public event UnityAction<string, string> OnArenaChanged;
    public event UnityAction<string, string> OnArenaChangedStart;
    public bool IsLoadingArena { get; private set; }
    private Camera mainCam;
    private Tweener currShake;
    public SpriteRenderer templeBG;
    private Sequence currVolumeSeq;
    private AudioClip transitionAudio;
    void Start()
    {
        enemyForArena = Resources.Load<EnemyForArena>("EnemyForArena");
        transitionAudio = Resources.Load<AudioClip>("TransitionClip");

        templeBG = GameObject.FindGameObjectWithTag("TempleBG").GetComponent<SpriteRenderer>();

        SpawnBossWithArena("ItemPickupArena");

        mainCam = Camera.main;

        BossbarManager.Get();
    }

    public void OpenUpArena(string arena, UnityAction sceneLoaded = null, params object[] payload)
    {
        StartCoroutine(OpenUpArenaCoroutine(arena, sceneLoaded, payload));
    }

    private IEnumerator OpenUpArenaCoroutine(string arena, UnityAction sceneLoaded, object[] payload)
    {
        ArenaManager.Get().PlayGlobalArenaMusic(transitionAudio, .1f, 1);
        OnArenaChangedStart?.Invoke(currentArena == null ? null : currentArena.gameObject.scene.name, arena);

        if (currentArena != null)
        {
            yield return StartCoroutine(currentArena.RunExitAnim());

            yield return SceneManager.UnloadSceneAsync(currentArena.gameObject.scene);
        }

        if (string.IsNullOrEmpty(arena) || !Application.CanStreamedLevelBeLoaded(arena))
            yield break;

        IsLoadingArena = true;

        yield return SceneManager.LoadSceneAsync(arena, LoadSceneMode.Additive);

        var arenaHolder = GetObjectOfTypeInScene<ArenaHolder>(arena);
        if (arenaHolder == null)
        {
            IsLoadingArena = false;
            yield break;
        }

        currentArena = arenaHolder;
        IsLoadingArena = false;

        arenaHolder.OnPayloadRecieved(payload);

        OnArenaChanged?.Invoke(currentArena == null ? null : currentArena.gameObject.scene.name, arena);

        sceneLoaded?.Invoke();

        yield return StartCoroutine(currentArena.RunEntryAnim());
    }

    public void SpawnBossWithArena(string arenaName)
    {
        OpenUpArena(arenaName, () =>
        {
            if (!enemyForArena.Contains(arenaName)) return;

            var boss = Instantiate(enemyForArena[arenaName]).GetComponent<BossEnemy>();
            boss.RunEntryAnim(OnBossName, OnBossStart);
        });
    }

    void OnBossName(string name)
    {
        BossbarManager.Get().bossbarCanvas.ShowName(name);
        Player.Get().GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    void OnBossStart()
    {
        Player.Get().GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
    }

    public T GetCurrentArena<T>() where T : class
    {
        if (currentArena is T typedArena)
            return typedArena;

        return null;
    }

    public static T GetObjectOfTypeInScene<T>(string sceneName) where T : Object
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            Debug.LogWarning($"Scene {sceneName} is not loaded.");
            return null;
        }

        var rootObjects = scene.GetRootGameObjects();

        foreach (var root in rootObjects)
        {
            T comp = root.GetComponentInChildren<T>(true);
            if (comp != null)
                return comp;
        }

        return null;
    }

    public void RunCamChake(float duration, float strength, int vibrato = 10, float rando = 10)
    {
        if (currShake != null && currShake.IsActive())
        {
            currShake.Kill();
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.DOShakeRotation(duration, new Vector3(0, 0, strength), vibrato, rando);
        }
        if (currShake == null)
            mainCam.DOShakeRotation(duration, new Vector3(0, 0, strength), vibrato, rando);
    }

    public void PlayGlobalArenaMusic(AudioClip clip, float fadeTime, float maxVolume)
    {
        if (currVolumeSeq != null)
            currVolumeSeq.Kill();
        currVolumeSeq = DOTween.Sequence();

        AudioSource souce = AudioManager.GetStableSource("BGMusic");

        if (souce == null)
        {
            souce = AudioManager.CreateStableSource("BGMusic");
            souce.loop = true;
            souce.Play();
            souce.volume = 0;
        }
        else
        {
            currVolumeSeq.Append(DOTween.To(
                () => souce.volume, x =>
                {
                    souce.volume = x;
                },
                0,
                fadeTime
            ));
        }

        currVolumeSeq.AppendCallback(() =>
        {
            souce.clip = clip;
            souce.Play();
        });
        currVolumeSeq.Append(DOTween.To(
            () => souce.volume, x =>
            {
                souce.volume = x;
            },
            maxVolume,
            fadeTime
        ));
            
    }
}
