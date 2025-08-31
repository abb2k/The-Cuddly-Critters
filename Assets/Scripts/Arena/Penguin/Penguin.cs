using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public enum PenguinStates
{
    Idle,
    Slide,
    CircleHalf,
    CircleRound,
    Eat,
}

public class Penguin : BossEnemy, IHitReciever
{
    [SerializeField] private float normalResistence;
    [SerializeField] private float eatingResistence;

    [SerializeField] private PenguinStates currentState;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Animator anim;
    [SerializeField] private float rotationSpeed = 10f;

    private bool didAttackLoopStart;

    private PenguinArena penguinArena;
    private Rigidbody2D rb;
    [Header("EntryAnimation")]
    [SerializeField] private Vector2 entryPoint;
    [SerializeField] private float entryWaitTime;
    [SerializeField] private float entryNameWaitTime;

    [Header("IdleState")]
    [SerializeField] private Vector2 idleTime;
    [SerializeField] private DamageInfo idleDamage;

    [Header("SlideState")]
    [SerializeField] private float slidePrepWaitTime;
    [SerializeField] private Vector2 minMaxSlideForce;
    [SerializeField] private Vector2 slideTime;
    [SerializeField] private DamageInfo slideDamage;
    [SerializeField] private AudioClip slideSFX;

    [Header("HalfCircleState")]
    [SerializeField] private float halfCirclePrepWaitTime;
    [SerializeField] private float halfCircleForce;
    [SerializeField] private float halfCircleWaitTime;
    [SerializeField] private DamageInfo halfCircleDamage;

    [Header("FullCircleState")]
    [SerializeField] private float fullCirclePrepForce;
    [SerializeField] private float fullCirclePrepWaitTime;
    [SerializeField] private float fullCircleForce;
    [SerializeField] private float fullCircleWaitTime;
    [SerializeField] private DamageInfo fullCircleDamage;

    [Header("EatState")]
    [SerializeField] private float eatWaitTime;
    [SerializeField] private AudioClip eatSFX;
    private bool collectedFish;

    [Header("Death/Escape")]
    [SerializeField] private float escapeHeight;
    [SerializeField] private float escapeTime;
    [SerializeField] private float deathHeight;
    [SerializeField] private float deathTime;
    [SerializeField] private DialogueSettings defeatDialogue;
    private Sequence currentSeq;

    private bool faceTowardPlayer;

    protected override void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ArenaManager.Get().OnArenaChangedStart += OnArenaChanged;
        base.Start();

        var source = AudioManager.CreateStableSource("PenguinSlide", slideSFX, false, "SFX");
        source.loop = true;
        source.Play();
        source.volume = 0;

        resistence = normalResistence;
    }

    public override void RunEntryAnim(UnityAction<string> showName, UnityAction onEnded)
    {
        var seq = DOTween.Sequence();

        transform.position = entryPoint;

        seq.AppendInterval(entryWaitTime);
        seq.AppendCallback(() => showName?.Invoke("Penguin"));
        seq.AppendInterval(entryNameWaitTime);
        seq.AppendCallback(() =>
        {
            onEnded?.Invoke();
            StartAttackLoop();
        });

        seq.Play();
    }

    void StartAttackLoop()
    {
        BossbarManager.Get().AttachToEnemy(this);

        penguinArena = ArenaManager.Get().GetCurrentArena<PenguinArena>();

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
        rb.linearVelocityX = 0;
        WeightedList<PenguinStates> possibleAttacks = new();

        if (collectedFish)
        {
            possibleAttacks.Add(PenguinStates.Eat, 100);
        }
        else
        {
            possibleAttacks.Add(PenguinStates.Idle, 20);
            possibleAttacks.Add(PenguinStates.Slide, 45);
            possibleAttacks.Add(PenguinStates.CircleHalf, 30);
            possibleAttacks.Add(PenguinStates.CircleRound, 50);
        }


        currentState = possibleAttacks.ChooseRandom();

        switch (currentState)
        {
            case PenguinStates.Idle:
                IdleState();
                break;
            case PenguinStates.Eat:
                EatingState();
                break;
            case PenguinStates.Slide:
                SlideState();
                break;
            case PenguinStates.CircleHalf:
                CircleHalfState();
                break;
            case PenguinStates.CircleRound:
                CircleRoundState();
                break;
        }
    }

    void OnStateEnded()
    {
        if (didAttackLoopStart)
            StartCoroutine(WaitTillStandingUp());
    }

    IEnumerator WaitTillStandingUp()
    {
        yield return new WaitUntil(() => !anim.GetCurrentAnimatorStateInfo(0).IsName("ExitSlide") && !anim.GetCurrentAnimatorStateInfo(0).IsName("SlideLoop"));
        if (didAttackLoopStart)
            NextState();
    }

    void IdleState()
    {
        anim.Play("Idle");
        faceTowardPlayer = true;
        currentSeq = DOTween.Sequence().AppendInterval(Random.Range(idleTime.x, idleTime.y)).AppendCallback(() =>
        {
            faceTowardPlayer = false;
            OnStateEnded();
        });
    }

    void EatingState()
    {
        anim.Play("EatwFish");
        resistence = eatingResistence;
        faceTowardPlayer = false;
        AudioManager.PlayTemporarySource(eatSFX);
        currentSeq = DOTween.Sequence().AppendInterval(eatWaitTime).AppendCallback(() =>
        {
            collectedFish = false;
            resistence = normalResistence;
            OnStateEnded();
        });
    }

    void SlideState()
    {
        FaceTowardPlayer();
        anim.SetBool("isSliding", true);
        currentSeq = DOTween.Sequence()
            .AppendInterval(slidePrepWaitTime)
            .AppendCallback(() =>
            {
                AudioManager.GetStableSource("PenguinSlide").volume = 1;
                rb.AddForceX(Random.Range(minMaxSlideForce.x, minMaxSlideForce.y) * GetDirToPlayer(), ForceMode2D.Impulse);
            })
            .AppendInterval(Random.Range(slideTime.x, slideTime.y)).AppendCallback(() =>
            {
                AudioManager.GetStableSource("PenguinSlide").volume = 0;
                anim.SetBool("isSliding", false);
                OnStateEnded();
            });
    }

    void CircleHalfState()
    {
        FaceTowardPlayer();
        anim.SetBool("isSliding", true);
        currentSeq = DOTween.Sequence()
            .AppendInterval(halfCirclePrepWaitTime)
            .AppendCallback(() =>
            {
                AudioManager.GetStableSource("PenguinSlide").volume = 1;
                rb.AddForceX(halfCircleForce * GetDirToPlayer(), ForceMode2D.Impulse);
            })
            .AppendInterval(halfCircleWaitTime).AppendCallback(() =>
            {
                AudioManager.GetStableSource("PenguinSlide").volume = 0;
                anim.SetBool("isSliding", false);
                OnStateEnded();
            });
    }

    void CircleRoundState()
    {
        float dir = GetDirToPlayer();

        anim.SetBool("isSliding", true);

        FaceTowardPlayer();

        currentSeq = DOTween.Sequence()
            .AppendCallback(() =>
            {
                rb.AddForceX(fullCirclePrepForce * (dir * -1), ForceMode2D.Impulse);
                AudioManager.GetStableSource("PenguinSlide").volume = 1;
            })
            .AppendInterval(fullCirclePrepWaitTime)
            .AppendCallback(() => rb.AddForceX(fullCircleForce * dir, ForceMode2D.Impulse))
            .AppendInterval(fullCircleWaitTime)
            .AppendCallback(() =>
            {
                AudioManager.GetStableSource("PenguinSlide").volume = 0;
                anim.SetBool("isSliding", false);
                OnStateEnded();
            });
    }

    void StopAll(bool loadPickupArena)
    {
        invincible = true;
        didAttackLoopStart = false;

        transform.DOKill();
        if (currentSeq != null)
            currentSeq.Kill();

        AudioManager.DeleteStableSource("PenguinSlide");

        if (loadPickupArena)
            ArenaManager.Get().OpenUpArena("ItemPickupArena", null, defeatDialogue, true);
    }

    protected override void OnDeath()
    {
        StopAll(true);

        anim.Play("Death");

        GameManager.Get().AddProgress();

        transform.eulerAngles = new Vector3(0, 0, 0);
        faceTowardPlayer = false;

        DOTween.Sequence()
            .Append(transform.DOMoveY(deathHeight, deathTime).SetEase(Ease.InBack))
            .Join(transform.DORotateQuaternion(Quaternion.identity, .5f))
            .AppendCallback(() => Destroy(gameObject));
    }

    void RunAway()
    {
        StopAll(false);

        anim.Play("idle");

        GameManager.Get().AddProgress();

        transform.DOKill();

        transform.eulerAngles = new Vector3(0, 0, 0);
        faceTowardPlayer = false;

        DOTween.Sequence()
            .Append(transform.DOMoveY(escapeHeight, escapeTime).SetEase(Ease.InSine))
            .Join(transform.DORotateQuaternion(Quaternion.identity, .5f))
            .AppendCallback(() => Destroy(gameObject));
    }

    void OnArenaChanged(string oldArena, string newArena)
    {
        if (didAttackLoopStart && oldArena.Equals("PenguinArena"))
            RunAway();
    }

    float GetDirToPlayer()
    {
        var dir = Player.Get().transform.position - transform.position;

        return dir.x < 0 ? -1 : 1;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.GetContact(0);

            Vector2 normal = contact.normal;

            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

            Vector3 currentEuler = rb.transform.rotation.eulerAngles;

            var targetRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, angle);

            rb.transform.rotation = Quaternion.Lerp(
                rb.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collectedFish && collision.CompareTag("PenguinFish") && collision.transform.parent.TryGetComponent(out PenguinArenaFish fish) && fish.WasHit)
        {
            collectedFish = true;
            Destroy(collision.transform.parent.gameObject);
        }
    }

    void FaceTowardPlayer()
    {
        transform.eulerAngles = GetDirToPlayer() == -1 ? new Vector3(0, 180, 0) : Vector3.zero;
    }

    void Update()
    {
        if (faceTowardPlayer)
            FaceTowardPlayer();
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        if (hitID == 0 && isTriggerHit && other.collider2D.TryGetComponent(out Player player) && didAttackLoopStart)
        {
            DamageInfo damage = null;

            switch (currentState)
            {
                case PenguinStates.Idle:
                    damage = idleDamage;
                    break;
                case PenguinStates.Slide:
                    damage = slideDamage;
                    break;
                case PenguinStates.CircleHalf:
                    damage = halfCircleDamage;
                    break;
                case PenguinStates.CircleRound:
                    damage = fullCircleDamage;
                    break;
                default:
                    break;
            }

            player.GetComponent<IHittable>().OnHit(damage);
        }
    }
}
