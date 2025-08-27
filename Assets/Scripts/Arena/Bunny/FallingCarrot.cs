using UnityEngine;

public class FallingCarrot : MonoBehaviour
{
    private DamageInfo myDamage;
    public void Setup(DamageInfo damage, Vector2 position)
    {
        transform.position = position;

        myDamage = damage;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.TryGetComponent(out FallingCarrot _)) return;
        
        if (collision.transform.TryGetComponent(out Player player))
        {
            player.GetComponent<IHittable>().OnHit(myDamage);
        }

        Destroy(gameObject);
    }
}
