using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public enum OwlAttacks
{
    Idle,
    Swoop,
    Woosh,
    Nosedive
}

public class Owl : Enemy
{
    [SerializeField] private float normalResistence;
    [SerializeField] private float wooshResistence;

    [SerializeField] private OwlAttacks currentAttack;

    [SerializeField] private float maxHeight;
    [SerializeField] private Vector2 maxEdges;

    [SerializeField] private SpriteRenderer sr;

    [Header("IdleAttack")]
    [SerializeField] private float idleUpSpeed;
    [SerializeField] private Vector2 idleSideSpeedMinMax;
    [SerializeField] private Vector2 minMaxIdleTime;
    [Header("Nosedive")]
    [SerializeField] private float divePrepTime;
    [SerializeField] private float divePrepMoveAmount;
    [SerializeField] private float diveEntryTime;
    [SerializeField] private float diveExitTime;
    [SerializeField] private Vector2 exitOffset;
    [Header("Swoop")]
    [SerializeField] private Vector2 swoopCenterPos;
    [SerializeField] private float swoopDistance;
    [SerializeField] private float swoopEntryTime;
    [SerializeField] private float swoopTime;
    [SerializeField] private float swoopBranchShakeTime;

    private GameObject swoopCenter;
    private Transform swoopPos;
    private bool isSwooping;

    [Header("Woosh")]
    [SerializeField] private float lightDetectRadius;
    [SerializeField] private int annoyingLightsAmount;
    [SerializeField] private Vector2 wooshStayPos;
    [SerializeField] private float wooshEnterTime;
    [SerializeField] private float wooshRadius;
    [SerializeField] private float wooshCircleAngle;
    [SerializeField] private float wooshForce;

    private int lightsOnInRange = 0;
    private bool isWooshing = false;

    protected override async void Start()
    {
        base.Start();

        BossbarManager.Get().AttachToEnemy(this);

        swoopCenter = new GameObject();
        swoopCenter.name = "OwlSwoopCenter";
        swoopPos = Instantiate(swoopCenter, swoopCenter.transform).transform;
        swoopCenter.name = "SwoopPos";

        await Task.Delay(1000);

        NextAttack();
    }

    void FixedUpdate()
    {
        lightsOnInRange = 0;
        var hits = Physics2D.OverlapCircleAll(transform.position, lightDetectRadius);
        foreach (var hit in hits)
        {
            if (!hit.transform.TryGetComponent(out TreeBranchLight light)) continue;

            if (light.isOn) lightsOnInRange++;
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
        seq.Append(sr.DOColor(Color.red, .5f));
        seq.Append(sr.DOColor(Color.white, .5f));

        seq.Play();
    }

    public void NextAttack()
    {
        WeightedList<OwlAttacks> possibleAttacks = new();

        if (Player.Get().transform.position.y > -4.5f) // on branch
            possibleAttacks.Add(OwlAttacks.Swoop, 70);

        if (lightsOnInRange >= annoyingLightsAmount)
            possibleAttacks.Add(OwlAttacks.Woosh, 100);

        possibleAttacks.Add(OwlAttacks.Idle, 25);
        possibleAttacks.Add(OwlAttacks.Nosedive, 50);

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
        NextAttack();
    }

    async void IdleAttack()
    {
        float idleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
        float sideSpeed = Random.Range(idleSideSpeedMinMax.x, idleSideSpeedMinMax.y);

        for (float t = 0; t < idleTime; t += Time.deltaTime)
        {
            if (transform.position.y < maxHeight)
                transform.position += Vector3.up * idleUpSpeed * Time.deltaTime;


            if (transform.position.x < maxEdges.y && transform.position.x > maxEdges.x)
                transform.position += Vector3.right * sideSpeed * Time.deltaTime;

            await Task.Yield();
        }

        OnAttackComplete();
    }

    void WooshAttack()
    {
        var seq = DOTween.Sequence();

        seq.Append(transform.DOMove(wooshStayPos, wooshEnterTime).SetEase(Ease.InOutSine));
        seq.AppendCallback(() => isWooshing = true);
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
        swoopPos.position = Vector3.right * swoopDistance;

        swoopCenter.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        var seq = DOTween.Sequence();

        seq.Append(transform.DOMove(swoopPos.position, swoopEntryTime).SetEase(Ease.OutSine));
        seq.Join(transform.DORotate(swoopCenter.transform.eulerAngles, swoopEntryTime).SetEase(Ease.OutSine));
        seq.Append(swoopCenter.transform.DORotate(new Vector3(0, 0, 360), swoopTime, RotateMode.LocalAxisAdd));
        seq.JoinCallback(async () =>
        {
            isSwooping = true;

            for (float t = 0; t < swoopTime; t += Time.deltaTime)
            {
                if (transform == null) break;
                transform.position = swoopPos.position;
                transform.rotation = swoopCenter.transform.rotation;
                await Task.Yield();
            }
        });
        seq.AppendCallback(() =>
        {
            isSwooping = false;
            transform.DORotate(Vector3.zero, swoopEntryTime).SetEase(Ease.OutSine);
            OnAttackComplete();
        });

        seq.Play();
    }

    void NosediveAttack()
    {
        var playerPos = Player.Get().transform.position;
        var seq = DOTween.Sequence();

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

        seq.Append(transform.DORotate(new Vector3(0, 0, angleToPlayer), divePrepTime).SetEase(Ease.InOutSine));
        seq.Join(transform.DOMove(transform.position - (Vector3)(dirToPlayer.normalized * divePrepMoveAmount), divePrepTime).SetEase(Ease.InOutSine));
        seq.Append(
            transform.DOMoveX(playerPos.x, diveEntryTime)
                .SetEase(Ease.InBack)
        );
        seq.Join(
            transform.DOMoveY(playerPos.y, diveEntryTime / 1.1f)
                .SetEase(Ease.InOutSine)
        );
        seq.Append(
            transform.DOMoveX(exitpos, diveExitTime)
                .SetEase(Ease.OutBack)
        );
        seq.Join(
            transform.DOMoveY(Mathf.Min(maxHeight, playerPos.y + exitOffset.y), diveExitTime / 1.2f)
                .SetEase(Ease.InOutSine)
        );
        seq.Join(transform.DORotate(Vector3.zero, diveExitTime / 2f).SetEase(Ease.InOutSine));
        seq.AppendCallback(OnAttackComplete);

        seq.Play();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isSwooping && collision.gameObject.TryGetComponent(out TreeBranch branch))
        {
            branch.ShakeFor(swoopBranchShakeTime);
        }
    }
}
