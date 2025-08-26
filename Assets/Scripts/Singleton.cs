using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _objLock = new();

    protected virtual string SingletonName => "Singleton";

    void Awake()
    {
        if (_instance)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        _instance.gameObject.name = SingletonName;
    }

    public static T Get()
    {
        lock (_objLock)
        {
            _instance ??= new GameObject().AddComponent<T>();

            return _instance;
        }
    }

    void OnDestroy()
    {
        if (this as T != _instance) return;

        OnSingletonDestroyed();
        _instance = null;
    }

    protected virtual void OnSingletonDestroyed() {}
}
