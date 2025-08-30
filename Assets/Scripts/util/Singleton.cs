using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _objLock = new();

    protected virtual string SingletonName => null;
    protected virtual bool CreateIfNone => true;

    protected virtual void Awake()
    {
        if (_instance)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        if (SingletonName != null)
            _instance.gameObject.name = SingletonName;

        DontDestroyOnLoad(_instance);
    }

    public static T Get()
    {
        lock (_objLock)
        {
            if (_instance == null && Application.isPlaying)
            {
                _instance ??= new GameObject().AddComponent<T>();
                var me = _instance.GetComponent<Singleton<T>>();
                
                DontDestroyOnLoad(_instance);
                if (me.SingletonName != null)
                {
                    _instance.gameObject.name = me.SingletonName;
                }
                if (!me.CreateIfNone)
                {
                    Destroy(me.gameObject);
                    _instance = null;
                }
                else me.OnLoaded();
            }
            

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
    protected virtual void OnLoaded() {}
}
