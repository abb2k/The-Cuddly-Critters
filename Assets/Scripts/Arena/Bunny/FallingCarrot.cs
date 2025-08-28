using UnityEngine;

public class FallingCarrot : MonoBehaviour
{
    private DamageInfo myDamage;
    private bool killOnHit;
    public void Setup(DamageInfo damage, Vector2 position, bool killOnHit = true, bool isFalling = true)
    {
        transform.position = position;

        myDamage = damage;

        this.killOnHit = killOnHit;
        GetComponent<Rigidbody2D>().bodyType = isFalling ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.TryGetComponent(out FallingCarrot _)) return;
        
        if (collision.transform.TryGetComponent(out Player player))
        {
            player.GetComponent<IHittable>().OnHit(myDamage);
        }

        if (killOnHit)
            Destroy(gameObject);
    }
}
