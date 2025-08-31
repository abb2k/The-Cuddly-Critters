using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public enum OwlAttacks
{
    Idle,
    Swoop,
    Woosh,
    Nosedive
}

public class Owl : BossEnemy, IHitReciever
{
    [SerializeField] private float normalResistence;
    [SerializeField] private float wooshResistence;

    [SerializeField] private OwlAttacks currentAttack;

    [SerializeField] private float maxHeight;
    [SerializeField] private Vector2 maxEdges;

    [SerializeField] private SpriteRenderer sr;

    [SerializeField] private Animator anim;

    [SerializeField] private AudioClip[] wingFlapSound;
    [SerializeField] private AudioClip hootSFX;

    [Header("EntryAnimation")]
    [SerializeField] private Vector2 entryPoint;
    [SerializeField] private float entryDownOffset;
    [SerializeField] private float entryWaitTime;
    [SerializeField] private float entryFlyTime;
    [SerializeField] private float entryStayTime;
    private bool didAttackLoopStart;

    [Header("IdleAttack")]
    [SerializeField] private float idleUpSpeed;
    [SerializeField] private Vector2 idleSideSpeedMinMax;
    [SerializeField] private Vector2 minMaxIdleTime;
    [SerializeField] private DamageInfo idleInfo;

    [Header("Nosedive")]
    [SerializeField] private float divePrepTime;
    [SerializeField] private float divePrepMoveAmount;
    [SerializeField] private float diveEntryTime;
    [SerializeField] private float diveExitTime;
    [SerializeField] private Vector2 exitOffset;
    [SerializeField] private DamageInfo nosediveInfo;
    [SerializeField] private AudioClip diveWooshSFX;

    [Header("Swoop")]
    [SerializeField] private Vector2 swoopCenterPos;
    [SerializeField] private float swoopDistance;
    [SerializeField] private float swoopEntryTime;
    [SerializeField] private float swoopTime;
    [SerializeField] private float swoopBranchShakeTime;
    [SerializeField] private DamageInfo swoopInfo;

    private GameObject swoopCenter;
    private Transform swoopPos;

    [Header("Woosh")]
    [SerializeField] private float lightDetectRadius;
    [SerializeField] private int annoyingLightsAmount;
    [SerializeField] private Vector2[] wooshStayPositions;
    [SerializeField] private float wooshEnterTime;
    [SerializeField] private float wooshRadius;
    [SerializeField] private float wooshCircleAngle;
    [SerializeField] private float wooshForce;
    [SerializeField] private float wooshTime;
    [SerializeField] private float wooshExitTime;
    [SerializeField] private DamageInfo wooshInfo;

    private List<TreeBranchLight> lightsInScene = new();
    private bool isWooshing = false;

    private Sequence currentSeq;
    private Coroutine currentIdleAttack = null;
    private Coroutine currentSwoopFollow = null;

    private OwlArena owlArena;

    public ParticleSystem vfx;

    [Header("Death/Escape")]
    [SerializeField] private float escapeHeight;
    [SerializeField] private float escapeTime;
    [SerializeField] private float deathHeight;
    [SerializeField] private float deathTime;
    [SerializeField] private DialogueSettings defeatDialogue;

    

    protected override void Start()
    {
        base.Start();

        ArenaManager.Get().OnArenaChangedStart += OnArenaChanged;
    }

    void StartAttackLoop()
    {
        BossbarManager.Get().AttachToEnemy(this);

        owlArena = ArenaManager.Get().GetCurrentArena<OwlArena>();
        lightsInScene = owlArena.branches.Select(b => b.GetLight()).ToList();
        lightsInScene.RemoveAll(l => l == null);

        swoopCenter = new GameObject();
        swoopCenter.name = "OwlSwoopCenter";
        swoopPos = Instantiate(swoopCenter, swoopCenter.transform).transform;
        swoopCenter.name = "SwoopPos";

        NextAttack();
        didAttackLoopStart = true;
        invincible = false;
    }

    void FixedUpdate()
    {
        if (didAttackLoopStart)
        {
            if ((Player.Get().transform.position - transform.position).x > 0)
                sr.transform.localEulerAngles = new Vector3(0, 180, 0);
            else
                sr.transform.localEulerAngles = new Vector3(0, 0, 0);
        }


        if (isWooshing)
        {
            WooshUpdate();
            resistence = wooshResistence;
        }
        else
        {
            resistence = normalResistence;
        }
    }

    protected override void OnHurt()
    {
        sr.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(sr.DOColor(Color.red, .05f));
        seq.Append(sr.DOColor(Color.white, .3f));

        seq.Play();

        if (isWooshing)
        {
            GameManager.Get().AddScore(2);
        }
        else
        {
            GameManager.Get().AddScore(1);
        }
    }

    public void NextAttack()
    {
        WeightedList<OwlAttacks> possibleAttacks = new();

        if (!owlArena.isPlayerOnGround) // on branch
            possibleAttacks.Add(OwlAttacks.Swoop, 70);

        int lightsOn = lightsInScene.Sum(l => l.isOn ? 1 : 0);

        if (lightsOn >= annoyingLightsAmount)
            possibleAttacks.Add(OwlAttacks.Woosh, 140);

        possibleAttacks.Add(OwlAttacks.Idle, 25);

        float nodediveWeight = 50;
        if (Player.Get().transform.position.y > transform.position.y)
            nodediveWeight = 25;

        possibleAttacks.Add(OwlAttacks.Nosedive, nodediveWeight);

        currentAttack = possibleAttacks.ChooseRandom();

        switch (currentAttack)
        {
            case OwlAttacks.Idle:
                IdleAttack();
                break;
            case OwlAttacks.Woosh:
                WooshAttack();
                break;
            case OwlAttacks.Swoop:
                SwoopAttack();
                break;
            case OwlAttacks.Nosedive:
                NosediveAttack();
                break;
        }
    }

    void OnAttackComplete()
    {
        if (didAttackLoopStart)
            NextAttack();
    }

    void IdleAttack()
    {
        if (Random.value < .5f)
            anim.Play("OwlFly");
        else
            anim.Play("OwlLookAround");
        float idleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
        float sideSpeed = Random.Range(idleSideSpeedMinMax.x, idleSideSpeedMinMax.y);

        if (currentIdleAttack != null)
            StopCoroutine(currentIdleAttack);
        currentIdleAttack = StartCoroutine(IdleAttackTimer(idleTime, sideSpeed));

        AudioManager.PlayTemporarySource(hootSFX);
    }

    IEnumerator IdleAttackTimer(float idleTime, float sideSpeed)
    {
        for (float t = 0; t < idleTime; t += Time.deltaTime)
        {
            if (transform.position.y < maxHeight)
                transform.position += Vector3.up * idleUpSpeed * Time.deltaTime;


            if (transform.position.x < maxEdges.y && transform.position.x > maxEdges.x)
                transform.position += Vector3.right * sideSpeed * Time.deltaTime;

            yield return null;
        }

        currentIdleAttack = null;

        OnAttackComplete();
    }

    void WooshAttack()
    {
        anim.Play("OwlFly");

        currentSeq = DOTween.Sequence();

        var decidedPos = wooshStayPositions[Random.Range(0, wooshStayPositions.Length)];

        currentSeq.Append(transform.DOMove(decidedPos, wooshEnterTime).SetEase(Ease.InOutSine));
        currentSeq.AppendInterval(wooshTime);
        currentSeq.JoinCallback(() =>
        {
            StartCoroutine(WaitToTurnOffLights());
            vfx.Play();
        });
        currentSeq.AppendInterval(wooshExitTime);
        currentSeq.JoinCallback(() =>
        {
            isWooshing = false;
            OnAttackComplete();
            vfx.Stop();
            
        });
    }

    IEnumerator WaitToTurnOffLights()
    {
        isWooshing = true;
        yield return new WaitForSeconds(1);
        lightsInScene.ForEach(l => l.TurnOff());
    }

    void WooshUpdate()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, wooshRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent(out Player player)) continue;

            var dirToTarget = (hit.transform.position - transform.position).normalized;
            var angle = Vector2.Angle(Vector2.down, dirToTarget);

            if (angle > wooshCircleAngle / 2) continue;

            player.GetComponent<Rigidbody2D>().AddForce((player.transform.position - transform.position).normalized * wooshForce, ForceMode2D.Force);
        }
    }

    void SwoopAttack()
    {
        swoopCenter.transform.position = swoopCenterPos;
        swoopPos.localPosition = Vector3.right * swoopDistance;

        swoopCenter.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        currentSeq = DOTween.Sequence();

        if (currentSwoopFollow != null)
        {
            StopCoroutine(currentSwoopFollow);
            currentSwoopFollow = null;
        }

        anim.Play("OwlOpenWings");

        currentSeq.Append(transform.DOMove(swoopPos.position, swoopEntryTime).SetEase(Ease.OutSine));
        currentSeq.Join(transform.DORotateQuaternion(swoopCenter.transform.rotation, swoopEntryTime).SetEase(Ease.OutSine));
        currentSeq.Append(swoopCenter.transform.DORotate(new Vector3(0, 0, 360), swoopTime, RotateMode.LocalAxisAdd));
        currentSeq.JoinCallback(() =>
        {
            foreach (var branch in owlArena.branches) branch.ShakeFor(swoopBranchShakeTime);

            AudioManager.PlayTemporarySource(diveWooshSFX);

            currentSwoopFollow = StartCoroutine(FollowSwoopPos());
        });
        currentSeq.AppendCallback(() =>
        {
            transform.DORotateQuaternion(Quaternion.identity, .1f).SetEase(Ease.OutSine);
            OnAttackComplete();
        });
    }
    IEnumerator FollowSwoopPos()
    {
        for (float t = 0; t < swoopTime; t += Time.deltaTime)
        {
            if (transform == null) break;
            transform.position = swoopPos.position;
            transform.eulerAngles = swoopCenter.transform.eulerAngles;
            yield return null;
        }

        currentSwoopFollow = null;
    }

    void NosediveAttack()
    {
        var playerPos = Player.Get().transform.position;
        currentSeq = DOTween.Sequence();

        Vector2 dirToPlayer = playerPos - transform.position;

        bool startingFromRightOfPlayer = dirToPlayer.x > 0;

        float exitpos = playerPos.x + exitOffset.x * (startingFromRightOfPlayer ? 1 : -1);
        if (startingFromRightOfPlayer)
        {
            exitpos = Mathf.Min(maxEdges.y, exitpos);
        }
        else
        {
            exitpos = Mathf.Max(maxEdges.x, exitpos);
        }

        float angleToPlayer = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        currentSeq.Append(transform.DORotate(new Vector3(0, 0, angleToPlayer), divePrepTime).SetEase(Ease.InOutSine));
        currentSeq.Join(transform.DOMove(transform.position - (Vector3)(dirToPlayer.normalized * divePrepMoveAmount), divePrepTime).SetEase(Ease.InOutSine));
        currentSeq.Append(
            transform.DOMoveX(playerPos.x, diveEntryTime)
                .SetEase(Ease.InBack)
        );
        currentSeq.JoinCallback(() =>
        {
            transform.eulerAngles = (Player.Get().transform.position - transform.position).x > 0 ? new Vector3(0, 0, 90) : new Vector3(0, 0, -90);
            anim.Play("OwlDive");
            AudioManager.PlayTemporarySource(diveWooshSFX);
        });
        currentSeq.Join(
            transform.DOMoveY(playerPos.y, diveEntryTime / 1.1f)
                .SetEase(Ease.InOutSine)
        );
        currentSeq.Append(
            transform.DOMoveX(exitpos, diveExitTime)
                .SetEase(Ease.OutBack)
        );
        currentSeq.Join(
            transform.DOMoveY(Mathf.Min(maxHeight, playerPos.y + exitOffset.y), diveExitTime / 1.2f)
                .SetEase(Ease.InOutSine)
        );
        currentSeq.Join(transform.DORotate(Vector3.zero, diveExitTime / 2f).SetEase(Ease.InOutSine));
        currentSeq.JoinCallback(() =>
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            anim.Play("OwlOpenWings");
        });
        currentSeq.AppendCallback(OnAttackComplete);
    }

    public override void RunEntryAnim(UnityAction<string> showName, UnityAction onEnded)
    {
        var seq = DOTween.Sequence();

        invincible = true;

        seq.Append(transform.DOMove(entryPoint - Vector2.down * entryDownOffset, 0));
        seq.AppendInterval(entryWaitTime);
        seq.Append(transform.DOMove(entryPoint, entryFlyTime).SetEase(Ease.OutBack));
        seq.AppendInterval(entryStayTime);
        seq.JoinCallback(() =>
        {
            anim.Play("OwlLookAround");
            showName?.Invoke("Owl");
            AudioManager.PlayTemporarySource(hootSFX);
        });
        seq.AppendCallback(() =>
        {
            onEnded?.Invoke();
            StartAttackLoop();
        });

        seq.Play();
    }

    void StopAll(bool loadPickupArena)
    {
        invincible = true;
        didAttackLoopStart = false;

        transform.DOKill();
        if (currentSeq != null)
            currentSeq.Kill();
        if (currentIdleAttack != null)
            StopCoroutine(currentIdleAttack);
        if (currentSwoopFollow != null)
            StopCoroutine(currentSwoopFollow);

        if (loadPickupArena)
            ArenaManager.Get().OpenUpArena("ItemPickupArena", null, defeatDialogue, true);
    }

    protected override void OnDeath()
    {
        StopAll(true);

        anim.Play("OwlDeath");

        GameManager.Get().AddProgress();

        transform.eulerAngles = new Vector3(0, 0, 0);

        DOTween.Sequence()
            .Append(transform.DOMoveY(deathHeight, deathTime).SetEase(Ease.InBack))
            .Join(transform.DORotateQuaternion(Quaternion.identity, .5f))
            .AppendCallback(() => Destroy(gameObject));
    }

    void RunAway()
    {
        StopAll(false);

        anim.Play("OwlFly");

        GameManager.Get().AddProgress();

        transform.DOKill();

        transform.eulerAngles = new Vector3(0, 0, 0);

        DOTween.Sequence()
            .Append(transform.DOMoveY(escapeHeight, escapeTime).SetEase(Ease.InSine))
            .Join(transform.DORotateQuaternion(Quaternion.identity, .5f))
            .AppendCallback(() => Destroy(gameObject));
    }

    void OnArenaChanged(string oldArena, string newArena)
    {
        if (didAttackLoopStart && oldArena.Equals("OwlArena"))
            RunAway();
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        if (hitID == 0 && isTriggerHit && other.collider2D.TryGetComponent(out Player player) && didAttackLoopStart)
        {
            DamageInfo damage;

            switch (currentAttack)
            {
                case OwlAttacks.Swoop:
                    damage = swoopInfo;
                    break;
                case OwlAttacks.Nosedive:
                    damage = nosediveInfo;
                    break;
                case OwlAttacks.Woosh:
                    damage = wooshInfo;
                    break;
                default:
                    damage = idleInfo;
                    break;
            }

            player.GetComponent<IHittable>().OnHit(damage);
        }
    }

    public void OnWingFlap()
    {
        AudioManager.PlayTemporarySource(wingFlapSound[Random.Range(0, wingFlapSound.Length)], 1, 1, "OwlWingFlap");
    }
}
