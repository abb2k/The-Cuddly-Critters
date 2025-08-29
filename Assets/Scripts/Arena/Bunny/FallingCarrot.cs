using UnityEngine;

public class FallingCarrot : MonoBehaviour
{
    private DamageInfo myDamage;
    private bool killOnHit;
    public void Setup(DamageInfo damage, Vector2 position, Vector2 direction, bool killOnHit = true)
    {
        transform.position = position;

        myDamage = damage;

        this.killOnHit = killOnHit;
        GetComponent<Rigidbody2D>().linearVelocity = direction;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 90);
    }

    void OnTriggerEnter2D(Collider2D collision)
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
