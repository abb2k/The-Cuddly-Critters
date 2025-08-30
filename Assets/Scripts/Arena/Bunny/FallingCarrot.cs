using DG.Tweening;
using UnityEngine;

public class FallingCarrot : MonoBehaviour
{
    private DamageInfo myDamage;
    private bool killOnHit;
    [SerializeField] private LayerMask noKillMask;
    [SerializeField] private Sprite bunnyCarrotClosed;
    [SerializeField] private Sprite bunnyCarrotOpened;
    [SerializeField] private SpriteRenderer sr;
    [Space]
    [SerializeField] private bool doMoveWothBunny;
    [SerializeField] private bool startClosed;
    [SerializeField] private float downTime;
    [SerializeField] private float upTime;
    [SerializeField] private float transitionTime;
    [SerializeField] private Vector2 moveExtra;
    [SerializeField] private Vector2 openOffset;
    [SerializeField] private Vector2 openSize;
    [SerializeField] private BoxCollider2D hitbox;
    private Vector2 closedOffset;
    private Vector2 closedSize;
    Sequence upDownLoop = null;

    void Start()
    {
        closedOffset = hitbox.offset;
        closedSize = hitbox.size;
        if (doMoveWothBunny)
        {
            MoveByBunny();
        }
    }
    public void Setup(DamageInfo damage, Vector2? position, Vector2? direction, bool killOnHit = true)
    {
        if (position.HasValue)
            transform.position = position.Value;

        myDamage = damage;

        this.killOnHit = killOnHit;
        if (direction.HasValue)
        {
            GetComponent<Rigidbody2D>().linearVelocity = direction.Value;
            float angle = Mathf.Atan2(direction.Value.y, direction.Value.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90);
        }
    }

    public void MoveByBunny()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        var moveUp = moveExtra;
        var moveDown = -moveExtra;

        UpdateWBunnySpr(startClosed);

        if (!startClosed)
        {
            var temp = downTime;
            downTime = upTime;
            upTime = temp;
            transform.localPosition += (Vector3)moveExtra;

            var temp2 = moveUp;
            moveUp = moveDown;
            moveDown = temp2;
        }

        upDownLoop = DOTween.Sequence()
            .AppendInterval(downTime)
            .AppendCallback(() => UpdateWBunnySpr(!startClosed))
            .Append(transform.DOLocalMove(moveUp, transitionTime).SetRelative(true))
            .AppendInterval(upTime)
            .AppendCallback(() => UpdateWBunnySpr(startClosed))
            .Append(transform.DOLocalMove(moveDown, transitionTime).SetRelative(true))
            .SetLoops(-1);
    }

    void UpdateWBunnySpr(bool isClosed)
    {
        sr.sprite = isClosed ? bunnyCarrotClosed : bunnyCarrotOpened;

        hitbox.size = isClosed ? closedSize : openSize;
        hitbox.offset = isClosed ? closedOffset : openOffset;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if ((noKillMask & (1 << collision.gameObject.layer)) != 0) return;

        if (collision.transform.TryGetComponent(out Player player))
        {
            player.GetComponent<IHittable>().OnHit(myDamage);
        }

        if (killOnHit)
        {
            Destroy(gameObject);
            Debug.Log(gameObject.name);
        }
    }

    void OnDestroy()
    {
        if (upDownLoop != null)
            upDownLoop.Kill();
    }
}
