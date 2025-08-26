using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IHitReciever
{
    [SerializeField] private PlayerStats stats;
    private PlayerStats defaultStats;
    [SerializeField] private Transform weponTransform;
    [SerializeField] private SpecialItem itemEquipped;
    [SerializeField] LayerMask groundDiscludeMask;
    private Rigidbody2D rb;
    private bool isOnGround = false;
    private bool doubleJumpAvailable = false;
    private bool isAttacking = false;
    private bool isAttackOnCooldown = false;

    private Vector2 movementVec;

    bool lastMovedLeft = false;

    void Start()
    {
        defaultStats = stats;
        CreateModifiedStats();
        rb = GetComponent<Rigidbody2D>();
    }

    void CreateModifiedStats()
    {
        stats = defaultStats.Clone();
        if (itemEquipped)
            stats += itemEquipped.modifyedStats;
    }

    void FixedUpdate()
    {
        Movement();
    }

    void OnMove(InputValue input)
    {
        movementVec = input.Get<Vector2>();
    }
    void Movement()
    {
        rb.linearVelocityX = movementVec.x * stats.movementSpeed;

        if (rb.linearVelocityX > 0)
            lastMovedLeft = false;
        else if (rb.linearVelocityX < 0)
            lastMovedLeft = true;
    }

    void OnJump()
    {
        if (isOnGround)
        {
            rb.linearVelocityY = stats.jumpHight;
        }
        else if (doubleJumpAvailable && stats.canDoubleJump) {
            rb.linearVelocityY = stats.jumpHight;
            doubleJumpAvailable = false;
        }
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        if (other.collider2D != null)
        {
            if (hitID == 0)
                OnFeetHit(other.collider2D, type);
            else if (hitID == 1)
                OnWeponHit(other.collider2D);
        }
        
    }

    void OnFeetHit(Collider2D other, IHitReciever.HitType type)
    {
        if ((groundDiscludeMask & (1 << other.gameObject.layer)) != 0) return;

        isOnGround = type != IHitReciever.HitType.Exit;
        if (isOnGround)
            doubleJumpAvailable = true;
    }

    void OnAttack()
    {
        if (isAttacking || isAttackOnCooldown) return;
        isAttacking = true;
        isAttackOnCooldown = true;
        weponTransform.gameObject.SetActive(true);

        weponTransform.localRotation = Quaternion.identity;

        var posInCameraSpace = (Vector2)Camera.main.WorldToScreenPoint(transform.position);
        var mousePos = Mouse.current.position.ReadValue();

        var mousePosNormal = (posInCameraSpace - mousePos).normalized;

        weponTransform.localScale = new Vector3(1, 1 * (mousePosNormal.x > 0 ? 1 : -1), 1);

        var seq = DOTween.Sequence();
        seq.Append(
            weponTransform.DORotate(Vector3.forward * stats.attackAngle, stats.attackTime, RotateMode.LocalAxisAdd)
            //.SetEase(Ease.OutExpo)
        );
        seq.AppendCallback(() =>
        {
            weponTransform.gameObject.SetActive(false);
            isAttacking = false;
        });
        seq.AppendInterval(stats.attackCooldown);
        seq.AppendCallback(() =>
        {
            isAttackOnCooldown = false;
        });

        seq.Play();
    }

    void OnWeponHit(Collider2D other)
    {
        if (!isAttacking || !isAttacking && isAttackOnCooldown) return;
        

    }
}
