using UnityEngine;

[System.Serializable]
public class DamageInfo
{
    [HideInInspector]
    public GameObject damageSource;
    public float damage;
    [HideInInspector]
    public Vector2 knockbackNormal;
    public float knockbackStrength;

    public static DamageInfo operator +(DamageInfo a, DamageInfo b)
    {
        return new DamageInfo
        {
            damageSource = a.damageSource,
            damage = a.damage + b.damage,
            knockbackNormal = a.knockbackNormal,
            knockbackStrength = a.knockbackStrength + b.knockbackStrength
        };
    }
}
