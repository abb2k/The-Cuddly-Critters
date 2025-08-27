using System.Collections;
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

    [Header("BurrowState")]
    [SerializeField] private float burrowPrepTime;
    [SerializeField] private float burrowPrepMoveAMount;
    [SerializeField] private float burrowEntryTime;
    [SerializeField] private float burrowUndergroundYLevel;
    [SerializeField] private Vector2 burrowStayTime;
    [SerializeField] private float burrowExitStayOffset;
    [SerializeField] private Vector2 burrowWarnTime;
    [Space]
    [SerializeField] private Vector2 burrowMinExitPos;
    [SerializeField] private Vector2 burrowMaxExitPos;
    [SerializeField] private float burrowLeftSide;
    [SerializeField] private float BurrowRightSide;
    [SerializeField] private Vector2 burrowExitForce;
    [SerializeField] private GameObject burrowWarning;
    [SerializeField] private float burrowExitDelay;

    [Header("JumpState")]
    [SerializeField] private float jumpPrepTime;
    [SerializeField] private float jumpEntryTime;
    [SerializeField] private float jumpHeightOffset;
    [SerializeField] private float jumpStayTime;
    [SerializeField] private float jumpBoomForce;
    [SerializeField] private float jumpExitDelayTime;
    [SerializeField] private float jumpPlatDisableTime;
    private bool disPlatsOnNextHit;
    private BunnyArena bunnyArena;

    [Header("CarrotShowerState")]
    [SerializeField] private Vector2 CSPos;
    [SerializeField] private float CSEntryTime;
    [SerializeField] private float CSStayTime;
    [SerializeField] private Vector2 minCarrotArea;
    [SerializeField] private Vector2 maxCarrotArea;
    [SerializeField] private GameObject carrotPrefab;
    [SerializeField] private Vector2 SpawnCarrotPer;
    [SerializeField] private Vector2 SpawnCarrotAmount;
    [SerializeField] private DamageInfo carrotDamage;
    private Coroutine carrotSpawnRoutine;

    [Header("TiredState")]
    [SerializeField] private Vector2 attackToBeTired;
    [SerializeField] private float tiredTime;
    [SerializeField] private float tiredRecoverTime;

    private int currentTirenessLevel = 0;
    private int lastSelectedTirePoint;

    private Sequence currentOngoingState = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
    }

    protected override void Start()
    {
        ArenaManager.Get().OnArenaChanged += OnArenaChanged;
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

        if (pPos.y < -3.5)//ground area
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
        currentTirenessLevel++;
        NextState();
    }

    void IdleState()
    {
        currentOngoingState = DOTween.Sequence()
            .Append(DOTween.To(() => rb.linearVelocityX, x => rb.linearVelocityX = x, idleWalkSpeed * (Random.value > 0.5f ? -1 : 1), idleTransitionTime))
            .AppendInterval(Random.Range(minMaxIdleTime.x, minMaxIdleTime.y))
            .Append(DOTween.To(() => rb.linearVelocityX, x => rb.linearVelocityX = x, 0f, idleTransitionTime))
            .AppendCallback(OnStateEnded);
    }

    void BurrowState()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        hitbox.isTrigger = true;
        rb.linearVelocity = Vector2.zero;

        var exitPos = burrowMinExitPos;
        exitPos.x = Random.Range(burrowMinExitPos.x, burrowMaxExitPos.x);

        float exitAngle = 0;

        if (exitPos.x < burrowLeftSide)
        {
            exitPos = bunnyArena.leftMound.exitPoint.position;
            exitAngle = bunnyArena.leftMound.exitPoint.eulerAngles.z;
        }
        if (exitPos.x > BurrowRightSide)
        {
            exitPos = bunnyArena.rightMound.exitPoint.position;
            exitAngle = bunnyArena.rightMound.exitPoint.eulerAngles.z;
        }

        float warnTime = Random.Range(burrowWarnTime.x, burrowWarnTime.y);

        float rad = (exitAngle - 90f) * Mathf.Deg2Rad;

        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 force = -dir.normalized * Random.Range(burrowExitForce.x, burrowExitForce.y);

        currentOngoingState = DOTween.Sequence()
            .Append(transform.DOMoveY(transform.position.y + burrowPrepMoveAMount, burrowPrepTime).SetEase(Ease.OutSine))
            .Append(transform.DOMoveY(burrowUndergroundYLevel, burrowEntryTime).SetEase(Ease.OutExpo))
            .AppendInterval(Random.Range(burrowStayTime.x, burrowStayTime.y))
            .Join(transform.DOMove(exitPos + dir.normalized * burrowExitStayOffset, 0))
            .AppendInterval(warnTime)
            .JoinCallback(() =>
            {
                var burrowing = Instantiate(burrowWarning).GetComponent<BunnyBurrowWarning>();
                burrowing.Startup(warnTime);
                burrowing.transform.position = exitPos;
                burrowing.transform.rotation = Quaternion.Euler(0, 0, exitAngle);
            })
            .AppendCallback(() =>
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.WakeUp();
                rb.AddForce(force);
                hitbox.isTrigger = false;
            })
            .AppendInterval(burrowExitDelay)
            .AppendCallback(OnStateEnded);
    }

    async void JumpState()
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
            .AppendCallback(() =>
            {
                var startPos = transform.position;

                DOVirtual.Float(0, 1, jumpEntryTime / 1.15f, t =>
                {
                    playerPos = Player.Get().transform.position;

                    Vector3 targetPos = new Vector3(
                        playerPos.x,
                        playerPos.y + jumpHeightOffset,
                        transform.position.z
                    );
                    var myPos = transform.position;
                    myPos.y = Mathf.Lerp(startPos.y, targetPos.y, t);
                    transform.position = myPos;
                }).SetEase(Ease.OutSine);

                DOVirtual.Float(0, 1, jumpEntryTime, t =>
                {
                    playerPos = Player.Get().transform.position;

                    Vector3 targetPos = new Vector3(
                        playerPos.x,
                        playerPos.y + jumpHeightOffset,
                        transform.position.z
                    );
                    var myPos = transform.position;
                    myPos.x = Mathf.Lerp(startPos.x, targetPos.x, t);
                    transform.position = myPos;
                }).SetEase(Ease.OutSine);
            })
            .AppendInterval(jumpEntryTime)
            .AppendCallback(() =>
            {
                DOVirtual.Float(0, 1, jumpStayTime, t =>
                {
                    playerPos = Player.Get().transform.position;

                    Vector3 targetPos = new Vector3(
                        playerPos.x,
                        playerPos.y + jumpHeightOffset,
                        transform.position.z
                    );
                    transform.position = Vector3.Lerp(transform.position, targetPos, 0.2f);
                }).SetEase(Ease.Linear);
            })
            .AppendInterval(jumpStayTime)
            .AppendCallback(() =>
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.WakeUp();
                rb.AddForce(Vector2.up * -jumpBoomForce, ForceMode2D.Impulse);
                disPlatsOnNextHit = true;
            })
            .AppendInterval(jumpExitDelayTime)
            .AppendCallback(OnStateEnded);
    }

    void CarrotShowerState()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        currentOngoingState = DOTween.Sequence()
            .Append(transform.DOMove(CSPos, CSEntryTime))
            .AppendCallback(() => carrotSpawnRoutine = StartCoroutine(SpawnSkyCarrots()))
            .AppendInterval(CSStayTime)
            .AppendCallback(() =>
            {
                if (carrotSpawnRoutine != null) StopCoroutine(carrotSpawnRoutine);
                rb.bodyType = RigidbodyType2D.Dynamic;
            })
            .AppendCallback(OnStateEnded);
    }

    IEnumerator SpawnSkyCarrots()
    {
        while (true)
        {
            int amount = Random.Range((int)SpawnCarrotAmount.x, (int)SpawnCarrotAmount.y + 1);

            for (int i = 0; i < amount; i++)
            {
                var spawnPos = new Vector2(
                    Random.Range(minCarrotArea.x, maxCarrotArea.x),
                    Random.Range(minCarrotArea.y, maxCarrotArea.y)
                );

                var carrot = Instantiate(carrotPrefab).GetComponent<FallingCarrot>();
                carrot.Setup(carrotDamage, spawnPos);
            }

            yield return new WaitForSeconds(Random.Range(SpawnCarrotPer.x, SpawnCarrotPer.y));
        }
    }

    void TiredState()
    {
        lastSelectedTirePoint = Random.Range((int)attackToBeTired.x, (int)attackToBeTired.y + 1);
        currentTirenessLevel = 0;

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
        if (disPlatsOnNextHit && isOnGround)
        {
            disPlatsOnNextHit = false;
            bunnyArena.DisablePlats(jumpPlatDisableTime);
            ArenaManager.Get().RunCamChake(.2f, 1, 20, 1);
        }
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        LayerMask myMask = LayerMask.GetMask("Enemy", "Player", "NotGround");

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
            isOnGround = type != IHitReciever.HitType.Exit;
        }
    }
    
    protected override async void OnDeath()
    {
        invincible = true;
        didAttackLoopStart = false;

        transform.DOKill();
        if (carrotSpawnRoutine != null)
            StopCoroutine(carrotSpawnRoutine);
        if (currentOngoingState != null)
            currentOngoingState.Kill();

        await ArenaManager.Get().OpenUpArena("");
    }

    void OnArenaChanged(string oldArena, string newArena)
    {
        if (didAttackLoopStart && oldArena.Equals("BunnyArena"))
            OnDeath();
    }
}
