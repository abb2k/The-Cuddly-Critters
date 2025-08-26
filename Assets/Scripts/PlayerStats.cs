using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    [Header("Movement")]
    public float movementSpeed;
    public float jumpHight;

    public bool canDoubleJump = false;
    [Header("Attack")]
    public float attackTime;
    public float attackCooldown;
    public float attackAngle;

    public PlayerStats Clone()
    {
        return (PlayerStats)this.MemberwiseClone();
    }

    public static PlayerStats operator +(PlayerStats a, PlayerStats b)
    {
        return new PlayerStats
        {
            movementSpeed = a.movementSpeed + b.movementSpeed,
            jumpHight = a.jumpHight + b.jumpHight,
            canDoubleJump = b.canDoubleJump,
            attackTime = a.attackTime + b.attackTime,
            attackCooldown = a.attackCooldown + b.attackCooldown,
            attackAngle = a.attackAngle + b.attackAngle
        };
    }
}
