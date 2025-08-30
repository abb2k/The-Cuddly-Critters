using DG.Tweening;
using UnityEngine;

public class PenguinArenaFish : MonoBehaviour, IHittable
{
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float destroyHight;
    [SerializeField] private float fadeAfter;
    public bool WasHit { get; private set; }

    public void OnHit(DamageInfo info)
    {
        if (WasHit) return;
        WasHit = true;

        hitbox.isTrigger = false;
        rb.AddForce(info.knockbackNormal * info.knockbackStrength, ForceMode2D.Impulse);

        var seq = DOTween.Sequence();
        seq.Append(sr.DOColor(Color.red, .05f));
        seq.Append(sr.DOColor(Color.white, .3f));

        seq.Play();

        DOTween.Sequence()
            .Append(sr.DOFade(0, fadeAfter))
            .AppendCallback(() => Destroy(gameObject));
    }

    void Update()
    {
        if (transform.position.y < destroyHight)
            Destroy(gameObject);
    }
}
