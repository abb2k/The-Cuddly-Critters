using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    [Header("Movement")]
    public float movementSpeed;
    public float jumpForce;
    public float jumpTime;
    public int airJumpsAllowed;
    public float cyotieTime = 0;
    [Header("Attack")]
    public DamageInfo damage;
    public float attackTime;
    public float attackCooldown;
    public float attackAngle;
    [Header("Health")]
    public float maxHealth;

    public PlayerStats Clone()
    {
        return (PlayerStats)this.MemberwiseClone();
    }

    public static PlayerStats operator +(PlayerStats a, PlayerStats b)
    {
        return new PlayerStats
        {
            movementSpeed = a.movementSpeed + b.movementSpeed,
            jumpForce = a.jumpForce + b.jumpForce,
            jumpTime = a.jumpTime + b.jumpTime,
            airJumpsAllowed = a.airJumpsAllowed + b.airJumpsAllowed,
            attackTime = a.attackTime + b.attackTime,
            attackCooldown = a.attackCooldown + b.attackCooldown,
            attackAngle = a.attackAngle + b.attackAngle,
            damage = a.damage + b.damage,
            cyotieTime = a.cyotieTime + b.cyotieTime,
            maxHealth = a.maxHealth + b.maxHealth
        };
    }
}
