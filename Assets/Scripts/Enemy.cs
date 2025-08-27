using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour, IHittable
{
    protected float health;
    public float maxHealth;
    protected float resistence;

    public float Health => health;

    public event UnityAction<Enemy> OnHurtEvent;


    protected virtual void Start()
    {
        health = maxHealth;
    }

    public void OnHit(DamageInfo info)
    {
        if (health == 0) return;

        health -= info.damage / resistence;
        if (health < 0) health = 0;

        OnHurt();
        OnHurtEvent?.Invoke(this);

        if (health == 0)
            OnDeath();
    }
    
    protected virtual void OnDeath() {}
    protected virtual void OnHurt() {}
}
