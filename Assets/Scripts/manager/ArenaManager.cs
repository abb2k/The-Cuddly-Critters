using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ArenaManager : Singleton<ArenaManager>
{
    private ArenaHolder currentArena;
    private EnemyForArena enemyForArena;
    void Start()
    {
        enemyForArena = Resources.Load<EnemyForArena>("EnemyForArena");

        SpawnBossWithArena("OwlArena");
    }

    public async Task OpenUpArena(string arena, UnityAction sceneLoaded = null)
    {
        if (currentArena != null)
        {
            await currentArena.RunExitAnim();

            await SceneManager.UnloadSceneAsync(currentArena.gameObject.scene);
        }

        if (string.IsNullOrEmpty(arena) || !Application.CanStreamedLevelBeLoaded(arena)) return;

        await SceneManager.LoadSceneAsync(arena, LoadSceneMode.Additive);

        var arenaHolder = GetObjectOfTypeInScene<ArenaHolder>(arena);
        if (arenaHolder == null) return;

        currentArena = arenaHolder;

        sceneLoaded?.Invoke();

        await currentArena.RunEntryAnim();
    }

    public async void SpawnBossWithArena(string arenaName)
    {
        await OpenUpArena(arenaName, () =>
        {
            if (!enemyForArena.Contains(arenaName)) return;

            var boss = Instantiate(enemyForArena[arenaName]).GetComponent<BossEnemy>();
            boss.RunEntryAnim(OnBossName, onBossStart);
        });
    }

    void OnBossName(string name)
    {
        BossbarManager.Get().bossbarCanvas.ShowName(name);
    }

    void onBossStart()
    {
        
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
}
