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

public class Penguin : BossEnemy
{
    [SerializeField] private float normalResistence;
    [SerializeField] private float eatingResistence;

    [SerializeField] private PenguinStates currentState;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float rotationSpeed = 10f;

    private bool didAttackLoopStart;

    private PenguinArena penguinArena;
    private Rigidbody2D rb;

    [Header("IdleState")]
    [SerializeField] private Vector2 idleTime;

    [Header("SlideState")]
    [SerializeField] private Vector2 minMaxSlideForce;
    [SerializeField] private Vector2 slideTime;

    [Header("HalfCircleState")]
    [SerializeField] private float halfCircleForce;
    [SerializeField] private float halfCircleWaitTime;

    [Header("FullCircleState")]
    [SerializeField] private float fullCirclePrepForce;
    [SerializeField] private float fullCirclePrepWaitTime;
    [SerializeField] private float fullCircleForce;
    [SerializeField] private float fullCircleWaitTime;

    protected override void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ArenaManager.Get().OnArenaChanged += OnArenaChanged;
        base.Start();

        resistence = normalResistence;
    }

    public override void RunEntryAnim(UnityAction<string> showName, UnityAction onEnded)
    {
        var seq = DOTween.Sequence();

        seq.AppendInterval(2);
        seq.AppendCallback(() => showName?.Invoke("Penguin"));
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
        WeightedList<PenguinStates> possibleAttacks = new();

        possibleAttacks.Add(PenguinStates.Idle, 20);
        possibleAttacks.Add(PenguinStates.Slide, 45);
        possibleAttacks.Add(PenguinStates.CircleHalf, 30);
        possibleAttacks.Add(PenguinStates.CircleRound, 50);

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

    void FixedUpdate()
    {

    }

    void OnStateEnded()
    {
        NextState();
    }

    void IdleState()
    {
        DOTween.Sequence().AppendInterval(Random.Range(idleTime.x, idleTime.y)).AppendCallback(() => OnStateEnded());
    }

    void EatingState()
    {

    }

    void SlideState()
    {
        rb.AddForceX(Random.Range(minMaxSlideForce.x, minMaxSlideForce.y) * (Random.value > .5f ? -1 : 1), ForceMode2D.Impulse);
        DOTween.Sequence().AppendInterval(Random.Range(slideTime.x, slideTime.y)).AppendCallback(() => OnStateEnded());
    }

    void CircleHalfState()
    {
        rb.AddForceX(halfCircleForce * (Random.value > .5f ? -1 : 1), ForceMode2D.Impulse);
        DOTween.Sequence().AppendInterval(halfCircleWaitTime).AppendCallback(() => OnStateEnded());
    }

    void CircleRoundState()
    {
        float dir = Random.value > .5f ? -1 : 1;

        DOTween.Sequence()
            .AppendCallback(() => rb.AddForceX(fullCirclePrepForce * (dir * -1), ForceMode2D.Impulse))
            .AppendInterval(fullCirclePrepWaitTime)
            .AppendCallback(() => rb.AddForceX(fullCircleForce * dir, ForceMode2D.Impulse))
            .AppendInterval(fullCircleWaitTime)
            .AppendCallback(() => OnStateEnded());
    }

    protected override async void OnDeath()
    {
        invincible = true;
        didAttackLoopStart = false;

        transform.DOKill();
        // if (carrotSpawnRoutine != null)
        //     StopCoroutine(carrotSpawnRoutine);
        // if (currentOngoingState != null)
        //     currentOngoingState.Kill();

        await ArenaManager.Get().OpenUpArena("");
    }

    void OnArenaChanged(string oldArena, string newArena)
    {
        if (didAttackLoopStart && oldArena.Equals("PenguinArena"))
            OnDeath();

        //temporary
        if (newArena.Equals("PenguinArena"))
            StartAttackLoop();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.GetContact(0);

            Vector2 normal = contact.normal;

            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    
}
