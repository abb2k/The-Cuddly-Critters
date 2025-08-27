using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyForArena", menuName = "Scriptable Objects/EnemyForArena")]
public class EnemyForArena : ScriptableObject
{
    [System.Serializable]
    private struct sceneNBoss
    {
        public string sceneName;
        public GameObject bossPrefab;
    }
    [SerializeField] private List<sceneNBoss> enemyForArenaList;
    private Dictionary<string, GameObject> enemyForArena = new();

    void OnEnable()
    {
        enemyForArena = enemyForArenaList.ToDictionary(info => info.sceneName, info => info.bossPrefab);
    }

    public GameObject this[string index]
    {
        get => enemyForArena[index];
        set => enemyForArena[index] = value;
    }

    public bool Contains(string key) => enemyForArena.ContainsKey(key);
}
