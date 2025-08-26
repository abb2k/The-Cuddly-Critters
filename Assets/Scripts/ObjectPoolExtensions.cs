using UnityEngine.Pool;

public static class ObjectPoolExtensions
{
    public static T TryGet<T>(this ObjectPool<T> pool, int maxSize) where T : class
    {
        if (pool.CountActive < maxSize)
            return pool.Get();

        return null;
    }
}