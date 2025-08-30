using UnityEngine;

public class PenguinArenaGround : MonoBehaviour
{
    [SerializeField] private float minVerticalImpact = 25f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (otherRb == null) return;

        float verticalVelocity = -collision.relativeVelocity.y;

        if (verticalVelocity >= minVerticalImpact)
        {
            OnHitAbove(collision.gameObject, verticalVelocity);
        }
    }

    private void OnHitAbove(GameObject hitter, float impactSpeed)
    {
        if (minVerticalImpact > impactSpeed) return;

        Debug.Log($"SHAKE");
    }
}
