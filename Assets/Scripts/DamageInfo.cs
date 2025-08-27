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
}
