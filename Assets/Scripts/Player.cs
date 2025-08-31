using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Player : Singleton<Player>, IHitReciever, IHittable
{
    [SerializeField] private PlayerStats stats;
    private PlayerStats defaultStats;
    [SerializeField] private Transform weponTransform;
    [SerializeField] private SpecialItem itemEquipped;
    public SpecialItem ItemEquipped => itemEquipped;
    [SerializeField] private LayerMask groundDiscludeMask;
    [SerializeField] private SpriteRenderer itemBodyVisual;
    [SerializeField] private SpriteRenderer itemWeponVisual;
    [SerializeField] private int belowPlayerSorting;
    [SerializeField] private int abovePlayerSorting;
    [SerializeField] private Light2D myLight;
    [SerializeField] private float lightIntensity;
    [SerializeField] private float lightTransitionTime;
    [SerializeField] private Collider2D feet;
    [SerializeField] private Transform visualsCont;
    [SerializeField] private SpriteRenderer[] flashOnHurt;
    [SerializeField] private HPBar hpBar;
    [Space]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float stepPlayDelay;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] attackSounds;
    public Animator anim;

    [Space]
    [SerializeField] private float health;
    private List<IInteractable> interactions = new();

    private Rigidbody2D rb;
    private bool isOnGround = false;
    private bool isAttacking = false;
    private bool isAttackOnCooldown = false;

    private Vector2 movementVec;
    private bool lastMovedLeft = false;

    private bool isHoldingJump = false;
    private int currentJumpsLeft;
    private float isCurrentJumpOngoing = 0;
    private Coroutine cyotieRourine = null;

    protected override bool CreateIfNone => false;

    private readonly object cyotieObjLock = new();

    private bool isDead;

    [SerializeField] private DialogueSettings deathDialogue;

    void Start()
    {
        defaultStats = stats;
        EquipItem(itemEquipped);
        rb = GetComponent<Rigidbody2D>();
        RevivePlayer();

        StartCoroutine(Footsteps());

        ArenaManager.Get();
    }

    public void TurnLight(bool on)
    {
        if (on)
            DOTween.To(() => myLight.intensity, x => myLight.intensity = x, lightIntensity, lightTransitionTime);
        else
            DOTween.To(() => myLight.intensity, x => myLight.intensity = x, 0, lightTransitionTime);
    }

    public void EquipItem(SpecialItem item)
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
        if (DialogueManager.Get().getFreezePlayer() || GameManager.Get().isInSeqance || isDead)
        {
            rb.linearVelocityX = 0;
            anim.SetBool("walking", false);
            return;
        }
        rb.linearVelocityX = movementVec.x * stats.movementSpeed;

        if (rb.linearVelocityX > 0)
            lastMovedLeft = false;
        else if (rb.linearVelocityX < 0)
            lastMovedLeft = true;

        anim.SetBool("walking", rb.linearVelocityX != 0);

        visualsCont.localEulerAngles = new Vector3(0, lastMovedLeft ? 180 : 0, 0);
    }

    IEnumerator Footsteps()
    {
        while (true)
        {
            if (rb.linearVelocityX != 0 && isOnGround)
                AudioManager.PlayTemporarySource(footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)], 1, 1, "footstep");
            yield return new WaitForSeconds(stepPlayDelay);
        }
    }

    void OnJump(InputValue input)
    {
        if (DialogueManager.Get().getFreezePlayer() || GameManager.Get().isInSeqance || isDead)
        {
            if (isHoldingJump)
                JumpKeyReleased();
            isHoldingJump = false;
            return;
        }
        isHoldingJump = input.isPressed;
        if (!input.isPressed)
            JumpKeyReleased();
        else
            JumpKeyPressed();
    }
    void Jumping()
    {
        if (currentJumpsLeft <= 0 || !isHoldingJump || isCurrentJumpOngoing <= 0) return;
        if (DialogueManager.Get().getFreezePlayer() || GameManager.Get().isInSeqance || isDead)
        {
            isHoldingJump = false;
            return;
        }

        rb.linearVelocityY = stats.jumpForce;
        isCurrentJumpOngoing -= Time.fixedDeltaTime;
    }
    public void CancleJump()
    {
        isCurrentJumpOngoing = 0;
    }

    void JumpKeyPressed()
    {
        isCurrentJumpOngoing = stats.jumpTime;
        if (isOnGround)
            currentJumpsLeft++;

        if (currentJumpsLeft > 0)
        {
            anim.Play("Jumping");
            AudioManager.PlayTemporarySource(jumpSounds[UnityEngine.Random.Range(0, jumpSounds.Length)], 1, 1, "PlayerJump");
        }
    }

    void JumpKeyReleased()
    {
        currentJumpsLeft -= 1;
        if (currentJumpsLeft < 0) currentJumpsLeft = 0;
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        if (other.collider2D != null)
        {
            if (hitID == 0)
                OnFeetHit(other.collider2D, type);
            else if (hitID == 1)
                OnWeponHit(other.collider2D);
            else if (hitID == 2 && other.collider2D.TryGetComponent(out IInteractable inter))
                OnInteractionHit(inter, type == IHitReciever.HitType.Exit);
        }

    }

    void OnInteractionHit(IInteractable interactable, bool didExit)
    {
        if (didExit)
        {
            if (interactions.Contains(interactable))
                interactions.Remove(interactable);
        }
        else
        {
            interactions.Add(interactable);
        }
    }

    void OnInteract()
    {
        if (interactions.Count == 0 || DialogueManager.Get().getFreezePlayer() || GameManager.Get().isInSeqance || isDead) return;

        interactions[0].OnInteract();
        var temp = interactions[0];
        interactions.RemoveAt(0);
        interactions.Add(temp);
    }

    void OnFeetHit(Collider2D other, IHitReciever.HitType type)
    {
        if ((groundDiscludeMask & (1 << other.gameObject.layer)) != 0) return;

        if (other is BoxCollider2D)
        {
            Bounds otherBounds = other.bounds;
            Bounds myBounds = feet.bounds;

            float tolerance = 0.1f;
            bool isAbove = myBounds.min.y >= otherBounds.max.y - tolerance;

            bool xOverlap = myBounds.max.x > otherBounds.min.x && myBounds.min.x < otherBounds.max.x;

            if (!(isAbove && xOverlap)) return;
        }


        if (type != IHitReciever.HitType.Exit)
        {
            if (cyotieRourine != null)
            {
                StopCoroutine(cyotieRourine);
                cyotieRourine = null;
            }
            isOnGround = true;
        }
        else
        {
            lock (cyotieObjLock)
            {
                if (cyotieRourine == null && gameObject.activeInHierarchy)
                    cyotieRourine = StartCoroutine(cyotieTimer());
            }
        }
        if (isOnGround)
        {
            currentJumpsLeft = stats.airJumpsAllowed;
            if (isHoldingJump)
                currentJumpsLeft++;
        }
    }

    IEnumerator cyotieTimer()
    {
        yield return new WaitForSeconds(stats.cyotieTime);
        isOnGround = false;
        cyotieRourine = null;
    }

    void OnAttack()
    {
        if (isAttacking || isAttackOnCooldown || DialogueManager.Get().getFreezePlayer() || GameManager.Get().isInSeqance || isDead) return;
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

        AudioManager.PlayTemporarySource(attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)], 1, 1, "PlayerWeponAttack");
    }

    void OnWeponHit(Collider2D other)
    {
        if (!isAttacking || other.gameObject.layer == LayerMask.NameToLayer("Player") || !isAttacking && isAttackOnCooldown) return;

        if (!other.TryGetComponent(out IHittable hittable)) return;

        var currDamage = stats.damage;
        currDamage.knockbackNormal = (other.transform.position - transform.position).normalized;
        currDamage.damageSource = gameObject;

        hittable.OnHit(currDamage);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Platform")) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.down) <= 0.5f) continue;

            isCurrentJumpOngoing = 0;
        }
    }

    public void OnHit(DamageInfo info)
    {
        if (health == 0 || info == null) return;

        health -= info.damage;
        if (health < 0) health = 0;

        OnDamaged();
        if (health == 0)
        {
            OnDeath();
        }

        hpBar.UpdateBar(health, stats.maxHealth);
    }

    void OnDamaged()
    {
        foreach (var sr in flashOnHurt)
        {
            DOTween.Sequence()
                .Append(sr.DOColor(Color.red, 0))
                .Append(sr.DOColor(Color.white, .5f));
        }

        AudioManager.PlayTemporarySource(hurtSound, 1, 1, "PlayerHurt");
    }

    public void RevivePlayer()
    {
        if (isDead)
        {
            anim.Play("Revive");
            DOTween.Sequence().AppendInterval(2).AppendCallback(() => isDead = false);
        }
        else anim.Play("Idle");

        health = stats.maxHealth;
        hpBar.UpdateBar(health, stats.maxHealth);
    }

    void OnDeath()
    {
        anim.Play("Death");
        if (ItemEquipped != null)
        {
            GameManager.Get().RemoveItem(ItemEquipped);
            EquipItem(null);
        }

        isDead = true;
        
        ArenaManager.Get().OpenUpArena("ItemPickupArena", null, deathDialogue);
    }
}
