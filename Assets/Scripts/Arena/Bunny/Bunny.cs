using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public enum BunnyStates
{
    Idle,
    burrow,
    Jump,
    CarrotShower,
    Tired
}

public class Bunny : BossEnemy, IHitReciever
{
    [SerializeField] private float normalResistence;
    [SerializeField] private float tiredResistence;

    [SerializeField] private BunnyStates currentState;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D feet;
    [SerializeField] private Animator anim;
    private bool didAttackLoopStart;
    private Rigidbody2D rb;
    private Collider2D hitbox;
    private bool isOnGround;
    [Header("EntryTransition")]
    [SerializeField] private Vector2[] startPoints;
    [SerializeField] private Vector2 targetStartPos;
    [SerializeField] private float startDelay;
    [SerializeField] private float startWalkTime;
    [SerializeField] private float startNameWait;

    [Header("IdleState")]
    [SerializeField] private Vector2 minMaxIdleTime;
    [SerializeField] private float idleTransitionTime;
    [SerializeField] private float idleWalkSpeed;
    [SerializeField] private DamageInfo idleDamage;

    [Header("BurrowState")]
    [SerializeField] private Vector2 burrowBarageAmount;
    [SerializeField] private float burrowPrepTime;
    [SerializeField] private float burrowPrepMoveAMount;
    [SerializeField] private float burrowEntryTime;
    [SerializeField] private float burrowUndergroundYLevel;
    [SerializeField] private Vector2 burrowStayTime;
    [SerializeField] private float burrowExitStayOffset;
    [SerializeField] private Vector2 burrowWarnTime;
    [Space]
    [SerializeField] private Vector2 burrowExitForce;
    [SerializeField] private GameObject burrowWarning;
    [SerializeField] private float burrowExitDelay;
    [SerializeField] private DamageInfo burrowDamage;
    private GameObject burrowHitboxFix;

    [Header("JumpState")]
    [SerializeField] private float jumpPrepTime;
    [SerializeField] private float jumpEntryTime;
    [SerializeField] private float jumpHeightOffset;
    [SerializeField] private float jumpStayTime;
    [SerializeField] private float jumpBoomForce;
    [SerializeField] private float jumpExitDelayTime;
    [SerializeField] private float jumpPlatDisableTime;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private DamageInfo jumpDamage;
    [SerializeField] private DamageInfo shockwaveDamage;
    private bool disPlatsOnNextHit;
    private BunnyArena bunnyArena;

    [Header("CarrotShowerState")]
    [SerializeField] private Vector2 CSPos;
    [SerializeField] private float CSEntryTime;
    [SerializeField] private float CSStayTime;
    [SerializeField] private GameObject carrotPrefab;
    [SerializeField] private float SpawnCarrotPer;
    [SerializeField] private float cloudMovementSpeed;
    [SerializeField] private float carrotMovementSpeed;
    [SerializeField] private Vector2 MinMaxCloudMovement;
    [SerializeField] private DamageInfo carrotDamage;
    [SerializeField] private DamageInfo groundCarrotDamage;
    private Coroutine carrotSpawnRoutine;
    private Tween carrSpwn = null;

    [Header("TiredState")]
    [SerializeField] private Vector2 attackToBeTired;
    [SerializeField] private float tiredTime;
    [SerializeField] private float tiredRecoverTime;

    private int currentTirenessLevel = 0;
    private int lastSelectedTirePoint;
    private bool isOnPlat;

    private Sequence currentOngoingState = null;

    [Header("Death/Escape")]
    [SerializeField] private float escapeXpos;
    [SerializeField] private float escapeTime;
    [SerializeField] private float deathHeight;
    [SerializeField] private float deathTime;
    [SerializeField] private DialogueSettings defeatDialogue;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
    }

    public bool IsOnPlats() => bunnyArena.isPlayerInSky;

    protected override void Start()
    {
        ArenaManager.Get().OnArenaChangedStart += OnArenaChanged;
        base.Start();

        lastSelectedTirePoint = Random.Range((int)attackToBeTired.x, (int)attackToBeTired.y + 1);
        resistence = normalResistence;
    }

    public override void RunEntryAnim(UnityAction<string> showName, UnityAction onEnded)
    {
        var seq = DOTween.Sequence();

        invincible = true;
        hitbox.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        seq.Append(transform.DOMove(startPoints[Random.Range(0, startPoints.Length)], 0));
        seq.AppendInterval(startDelay);
        seq.Append(transform.DOMove(targetStartPos, startWalkTime));
        seq.AppendCallback(() => showName?.Invoke("Bunny"));
        seq.AppendInterval(startNameWait);
        seq.AppendCallback(() =>
        {
            hitbox.enabled = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            onEnded?.Invoke();
            StartAttackLoop();
        });

        seq.Play();
    }

    void StartAttackLoop()
    {
        BossbarManager.Get().AttachToEnemy(this);

        bunnyArena = ArenaManager.Get().GetCurrentArena<BunnyArena>();

        foreach (Transform groundCarrot in bunnyArena.groundSpikesContainer)
        {
            groundCarrot.GetComponent<FallingCarrot>().Setup(groundCarrotDamage, null, null, false);
        }

        NextState();
        didAttackLoopStart = true;
        invincible = false;
    }

    protected override void OnHurt()
    {
        sr.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(sr.DOColor(Color.red, .05f));
        seq.Append(sr.DOColor(Color.white, .3f));

        seq.Play();

        if (currentState == BunnyStates.Tired)
        {
            GameManager.Get().AddScore(2);
        }
        else
        {
            GameManager.Get().AddScore(1);
        }
    }

    public void NextState()
    {
        WeightedList<BunnyStates> possibleAttacks = new();

        var pPos = Player.Get().transform.position;

        if (currentTirenessLevel >= lastSelectedTirePoint)
        {
            possibleAttacks.Add(BunnyStates.Tired, 100);
        }

        possibleAttacks.Add(BunnyStates.Idle, 25);
        possibleAttacks.Add(BunnyStates.Jump, 35);

        if (!IsOnPlats())//ground area
            possibleAttacks.Add(BunnyStates.burrow, 35);
        else //sky area
            possibleAttacks.Add(BunnyStates.CarrotShower, 35);

        currentState = possibleAttacks.ChooseRandom();

        switch (currentState)
        {
            case BunnyStates.Idle:
                IdleState();
                break;
            case BunnyStates.burrow:
                BurrowState();
                break;
            case BunnyStates.Jump:
                JumpState();
                break;
            case BunnyStates.CarrotShower:
                CarrotShowerState();
                break;
            case BunnyStates.Tired:
                TiredState();
                break;
        }
    }

    void OnStateEnded()
    {
        if (!didAttackLoopStart) return;

        currentTirenessLevel++;
        NextState();
    }

    void IdleState()
    {
        anim.Play("Idle");
        currentOngoingState = DOTween.Sequence()
            .Append(DOTween.To(() => rb.linearVelocityX, x => rb.linearVelocityX = x, idleWalkSpeed * (Random.value > 0.5f ? -1 : 1), idleTransitionTime))
            .AppendInterval(Random.Range(minMaxIdleTime.x, minMaxIdleTime.y))
            .Append(DOTween.To(() => rb.linearVelocityX, x => rb.linearVelocityX = x, 0f, idleTransitionTime))
            .AppendCallback(OnStateEnded);
    }

    void BurrowState(int iteration = -100)
    {
        if (iteration == 0)
        {
            OnStateEnded();
            return;
        }
        float warnTime = Random.Range(burrowWarnTime.x, burrowWarnTime.y);

        Vector2 dir = Vector2.up;

        GameObject hitGround = null;

        if (iteration == -100)
            iteration = Random.Range((int)burrowBarageAmount.x, (int)burrowBarageAmount.y + 1);

        if (IsOnPlats())
        {
            JumpState(() => BurrowState(iteration - 1));
            return;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        hitbox.isTrigger = true;
        rb.linearVelocity = Vector2.zero;

        currentOngoingState = DOTween.Sequence()
            .AppendCallback(() =>
            {

            })
            .AppendCallback(() =>
            {
                transform.DOMoveY(burrowPrepMoveAMount, burrowPrepTime).SetRelative(true).SetEase(Ease.OutSine);
                anim.Play("DigDown");
            })
            .AppendInterval(burrowPrepTime)
            .Append(transform.DOMoveY(burrowUndergroundYLevel, burrowEntryTime).SetEase(Ease.OutExpo))
            .AppendInterval(Random.Range(burrowStayTime.x, burrowStayTime.y))
            .AppendCallback(() =>
            {
                var pos = Player.Get().transform.position;

                ContactFilter2D filter = default(ContactFilter2D);
                filter.useTriggers = false;
                filter.SetLayerMask(~LayerMask.GetMask("Platform", "Enemy", "Player"));

                List<RaycastHit2D> hits = new();

                Vector2 hitPoint = transform.position;
                float hitAngle = 0;

                Physics2D.Raycast(pos, Vector2.down, filter, hits);
                if (hits.Count != 0)
                {
                    hitPoint = hits[0].point;
                    hitAngle = Vector2.Angle(Vector2.down, hits[0].normal);
                    hitGround = hits[0].transform.gameObject;
                }

                hitAngle -= 90;

                dir = new Vector2(Mathf.Cos(hitAngle * Mathf.Deg2Rad), Mathf.Sin(hitAngle * Mathf.Deg2Rad));

                transform.DOMove(hitPoint + -dir.normalized * burrowExitStayOffset, 0);

                var burrowing = Instantiate(burrowWarning).GetComponent<BunnyBurrowWarning>();
                burrowing.Startup(warnTime);
                burrowing.transform.position = hitPoint;
                burrowing.transform.rotation = Quaternion.Euler(0, 0, hitAngle - 90);
            })
            .AppendInterval(warnTime)
            .AppendCallback(() =>
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.WakeUp();
                rb.AddForce(dir.normalized * Random.Range(burrowExitForce.x, burrowExitForce.y));
                burrowHitboxFix = hitGround;
                anim.Play("PopUp");
                if (!burrowHitboxFix) hitbox.isTrigger = false;
            })
            .AppendInterval(burrowExitDelay)
            .AppendCallback(() =>
            {
                BurrowState(iteration - 1);
            });
    }

    async void JumpState(UnityAction callback = null)
    {
        while (!isOnGround) await Task.Yield();

        var playerPos = Player.Get().transform.position;

        currentOngoingState = DOTween.Sequence()
            .AppendCallback(() =>
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            })
            .AppendInterval(jumpPrepTime)
            .AppendCallback(() => anim.Play("Jump"))
            .Append(transform.DOMoveY(jumpHeightOffset, jumpEntryTime).SetRelative(true).SetEase(Ease.OutExpo))
            .AppendInterval(jumpStayTime)
            .AppendCallback(() =>
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.WakeUp();
                rb.AddForce(Vector2.up * -jumpBoomForce, ForceMode2D.Impulse);
                disPlatsOnNextHit = true;
            })
            .AppendInterval(jumpExitDelayTime)
            .AppendCallback(() =>
            {
                callback?.Invoke();
                if (callback == null)
                    OnStateEnded();
            });
    }

    void CarrotShowerState()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        anim.Play("RainCarrots");

        invincible = true;

        float spikesStartPos = bunnyArena.groundSpikesContainer.position.y;

        currentOngoingState = DOTween.Sequence()
            .Append(transform.DOMoveY(CSPos.y, CSEntryTime))
            .AppendCallback(() =>
            {
                carrotSpawnRoutine = StartCoroutine(SpawnSkyCarrots());
                carrSpwn = DOTween.Sequence()
                .AppendCallback(() =>
                {
                    var carrot = Instantiate(carrotPrefab).GetComponent<FallingCarrot>();
                    var dir = (Player.Get().transform.position - transform.position).normalized;
                    carrot.Setup(carrotDamage, transform.position, dir * carrotMovementSpeed);
                })
                .AppendInterval(SpawnCarrotPer).SetLoops(-1);
            })
            .Join(bunnyArena.groundSpikesContainer.DOMoveY(0, CSEntryTime).SetEase(Ease.OutSine))
            .AppendInterval(CSStayTime)
            .AppendCallback(() =>
            {
                if (carrotSpawnRoutine != null) StopCoroutine(carrotSpawnRoutine);
                carrotSpawnRoutine = null;
                rb.bodyType = RigidbodyType2D.Dynamic;
                carrSpwn.Kill();
                anim.Play("Idle");
                invincible = false;
            })
            .Join(bunnyArena.groundSpikesContainer.DOMoveY(spikesStartPos, CSEntryTime).SetEase(Ease.InSine))
            .AppendCallback(OnStateEnded);
    }

    IEnumerator SpawnSkyCarrots()
    {
        bool movingRight = Random.value > 0.5f;

        while (true)
        {
            var pos = transform.position;
            pos.x += cloudMovementSpeed * Time.deltaTime * (movingRight ? 1 : -1);
            transform.position = pos;

            if (pos.x > MinMaxCloudMovement.y && movingRight)
                movingRight = false;

            if (pos.x < MinMaxCloudMovement.x && !movingRight)
                movingRight = true;

            yield return null;
        }
    }

    void TiredState()
    {
        lastSelectedTirePoint = Random.Range((int)attackToBeTired.x, (int)attackToBeTired.y + 1);
        currentTirenessLevel = 0;

        anim.Play("EnterTired");

        resistence = tiredResistence;

        currentOngoingState = DOTween.Sequence()
            .AppendInterval(tiredTime)
            .AppendInterval(tiredRecoverTime)
            .AppendCallback(() =>
            {
                resistence = normalResistence;
                OnStateEnded();
            });
    }

    void Update()
    {
        if (didAttackLoopStart && currentState != BunnyStates.Tired)
        {
            if ((Player.Get().transform.position - transform.position).x > 0)
                sr.transform.localEulerAngles = new Vector3(0, 180, 0);
            else
                sr.transform.localEulerAngles = new Vector3(0, 0, 0);
        }

        if (disPlatsOnNextHit && isOnGround)
        {
            disPlatsOnNextHit = false;
            bunnyArena.DisablePlats(jumpPlatDisableTime);
            if (!isOnPlat)
                RunShockwave();
            ArenaManager.Get().RunCamChake(.2f, 1, 20, 1);
        }
    }

    void RunShockwave()
    {
        var sheck = Instantiate(shockwavePrefab).GetComponent<CarrotShockwave>();
        sheck.transform.position = transform.position + Vector3.up;
        sheck.damage = shockwaveDamage;
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        LayerMask myMask = LayerMask.GetMask("Enemy", "Player", "NotGround", "NoHitEachother");

        if (hitID == 0 && isTriggerHit && (myMask & (1 << other.collider2D.gameObject.layer)) == 0)
        {
            if (other.collider2D is BoxCollider2D)
            {
                Bounds otherBounds = other.collider2D.bounds;
                Bounds myBounds = feet.bounds;

                float tolerance = 0.1f;
                bool isAbove = myBounds.min.y >= otherBounds.max.y - tolerance;

                bool xOverlap = myBounds.max.x > otherBounds.min.x && myBounds.min.x < otherBounds.max.x;

                if (!(isAbove && xOverlap)) return;
            }
            isOnPlat = other.collider2D.gameObject.layer == LayerMask.NameToLayer("Platform");

            isOnGround = type != IHitReciever.HitType.Exit;
        }
        else if (hitID == 1 && other.collider2D.TryGetComponent(out Player player))
            HitPlayer(player.GetComponent<IHittable>());
    }

    async void StopAll(bool loadPickupArena)
    {
        invincible = true;
        didAttackLoopStart = false;

        transform.DOKill();
        if (carrotSpawnRoutine != null)
            StopCoroutine(carrotSpawnRoutine);
        if (currentOngoingState != null)
            currentOngoingState.Kill();
        if (carrSpwn != null)
            carrSpwn.Kill();

        if (loadPickupArena)
            await ArenaManager.Get().OpenUpArena("ItemPickupArena", null, defeatDialogue);
    }

    protected override void OnDeath()
    {
        StopAll(true);

        anim.Play("Death");

        GameManager.Get().AddProgress();

        DOTween.Sequence()
            .Append(transform.DOMoveY(deathHeight, deathTime).SetEase(Ease.InBack))
            .Join(transform.DORotateQuaternion(Quaternion.identity, .5f))
            .AppendCallback(() => Destroy(gameObject));
    }

    void RunAway()
    {
        StopAll(false);

        anim.Play("Idle");

        GameManager.Get().AddProgress();

        transform.DOKill();

        DOTween.Sequence()
            .Append(transform.DOMoveX(escapeXpos, escapeTime).SetEase(Ease.InSine))
            .AppendCallback(() => Destroy(gameObject));
    }

    void OnArenaChanged(string oldArena, string newArena)
    {
        if (didAttackLoopStart && oldArena.Equals("BunnyArena"))
            RunAway();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (burrowHitboxFix == collision.gameObject)
        {
            burrowHitboxFix = null;
            hitbox.isTrigger = false;
        }
    }

    void HitPlayer(IHittable hittable)
    {
        if (!didAttackLoopStart) return;

        switch (currentState)
        {
            case BunnyStates.CarrotShower:
            case BunnyStates.Idle:
                hittable.OnHit(idleDamage);
                break;
            case BunnyStates.burrow:
                hittable.OnHit(burrowDamage);
                break;
            case BunnyStates.Jump:
                hittable.OnHit(jumpDamage);
                break;
            case BunnyStates.Tired:
                break;
        }
    }
}
