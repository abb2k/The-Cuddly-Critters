using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IHitReciever
{
    [SerializeField] private PlayerStats stats;
    private PlayerStats defaultStats;
    [SerializeField] private Transform weponTransform;
    [SerializeField] private SpecialItem itemEquipped;
    [SerializeField] LayerMask groundDiscludeMask;
    [SerializeField] private SpriteRenderer itemBodyVisual;
    [SerializeField] private SpriteRenderer itemWeponVisual;
    [SerializeField] private int belowPlayerSorting;
    [SerializeField] private int abovePlayerSorting;
    private Rigidbody2D rb;
    private bool isOnGround = false;
    private bool isAttacking = false;
    private bool isAttackOnCooldown = false;

    private Vector2 movementVec;
    private bool lastMovedLeft = false;
    
    private bool isHoldingJump = false;
    private int currentJumpsLeft;
    private float isCurrentJumpOngoing = 0;

    void Start()
    {
        defaultStats = stats;
        EquipItem(itemEquipped);
        rb = GetComponent<Rigidbody2D>();
    }

    void EquipItem(SpecialItem item)
    {
        itemEquipped = item;

        stats = defaultStats.Clone();
        if (itemEquipped == null) return;

        stats += itemEquipped.modifyedStats;

        itemBodyVisual.gameObject.SetActive(itemEquipped.bodyVisual != null);
        itemBodyVisual.sprite = itemEquipped.bodyVisual;
        itemBodyVisual.sortingOrder = itemEquipped.bodyVisualSorting == ItemVisualSorting.BelowPlayer ? belowPlayerSorting : abovePlayerSorting;

        itemWeponVisual.gameObject.SetActive(itemEquipped.weponVisual != null);
        itemWeponVisual.sprite = itemEquipped.weponVisual;
        itemWeponVisual.sortingOrder = itemEquipped.weponVisualSorting == ItemVisualSorting.BelowPlayer ? belowPlayerSorting : abovePlayerSorting;
    }

    void FixedUpdate()
    {
        Movement();
        Jumping();
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

    void OnJump(InputValue input)
    {
        isHoldingJump = input.isPressed;
        if (!input.isPressed)
            JumpKeyReleased();
        else
            JumpKeyPressed();
    }
    void Jumping()
    {
        if (currentJumpsLeft <= 0 || !isHoldingJump || isCurrentJumpOngoing <= 0) return;
        
        rb.linearVelocityY = stats.jumpForce;
        isCurrentJumpOngoing -= Time.fixedDeltaTime;
    }

    void JumpKeyPressed()
    {
        isCurrentJumpOngoing = stats.jumpTime;
        if (isOnGround)
            currentJumpsLeft++;
    }

    void JumpKeyReleased()
    {
        currentJumpsLeft = math.max(0, currentJumpsLeft - 1);
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
        {
            currentJumpsLeft = stats.airJumpsAllowed;
            if (isHoldingJump)
                currentJumpsLeft++;
        }
    }

    void OnAttack()
    {
        if (isAttacking || isAttackOnCooldown) return;
        isAttacking = true;
        isAttackOnCooldown = true;
        weponTransform.gameObject.SetActive(true);

        var posInCameraSpace = (Vector2)Camera.main.WorldToScreenPoint(transform.position);
        var mousePos = Mouse.current.position.ReadValue();

        var mousePosNormal = (posInCameraSpace - mousePos).normalized;

        weponTransform.localRotation = mousePosNormal.x > 0 ? Quaternion.identity : Quaternion.Euler(0, 0, 180);
        float rotationAngle = mousePosNormal.x > 0 ? stats.attackAngle : -stats.attackAngle;

        weponTransform.localScale = new Vector3(1, 1 * (mousePosNormal.x > 0 ? 1 : -1), 1);

        var seq = DOTween.Sequence();
        seq.Append(
            weponTransform.DORotate(Vector3.forward * rotationAngle, stats.attackTime, RotateMode.LocalAxisAdd)
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
