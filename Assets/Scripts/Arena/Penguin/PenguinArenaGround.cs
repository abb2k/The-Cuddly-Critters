using UnityEngine;

public class PenguinArenaGround : MonoBehaviour
{
    [SerializeField] private float minVerticalImpact = 25f;
    [SerializeField] private AudioClip hardHitSound;

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
        if (minVerticalImpact > impactSpeed || !ArenaManager.Get().GetCurrentArena<PenguinArena>().didBattleStart) return;

        foreach (var gyser in ArenaManager.Get().GetCurrentArena<PenguinArena>().gysers)
        {
            gyser.Activate(true);
        }
        ArenaManager.Get().RunCamChake(.3f, 2);

        AudioManager.PlayTemporarySource(hardHitSound);
    }
}
