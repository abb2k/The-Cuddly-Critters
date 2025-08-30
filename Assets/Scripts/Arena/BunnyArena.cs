using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class BunnyArena : ArenaHolder, IHitReciever
{
    [SerializeField] private float downOffset;
    public BunnyDirtMound leftMound;
    public BunnyDirtMound rightMound;
    public Transform[] platforms;
    public Transform ground;
    private Sequence platDisSeq = null;
    public bool isPlayerInSky;
    public Transform groundSpikesContainer;
    public override async Task RunEntryAnim()
    {
        float time = 2;

        Dictionary<GameObject, Vector3> originalPoses = new();
        DisablePlats(time);
        foreach (var plat in platforms)
        {
            originalPoses.Add(plat.gameObject, plat.transform.position);
            plat.transform.DOMoveY(plat.transform.position.y - downOffset, 0);
        }
        originalPoses.Add(leftMound.gameObject, leftMound.transform.position);
        leftMound.transform.DOMoveY(leftMound.transform.position.y - downOffset, 0);

        originalPoses.Add(rightMound.gameObject, rightMound.transform.position);
        rightMound.transform.DOMoveY(rightMound.transform.position.y - downOffset, 0);

        originalPoses.Add(ground.gameObject, ground.transform.position);
        ground.transform.DOMoveY(ground.transform.position.y - downOffset, 0);

        float shakeStrength = 1;

        bool isTransitionDone = false;

        var seq = DOTween.Sequence()
            .Append(rightMound.transform.DOShakePosition(time / 10, new Vector3(shakeStrength, 0, 0), 10, 0).SetLoops(10))
            .Join(rightMound.transform.DOMoveY(originalPoses[rightMound.gameObject].y, time))
            .Join(ground.transform.DOShakePosition(time / 10, new Vector3(shakeStrength, 0, 0), 10, 0).SetLoops(10))
            .Join(ground.transform.DOMoveY(originalPoses[ground.gameObject].y, time))
            .Join(leftMound.transform.DOShakePosition(time / 10, new Vector3(shakeStrength, 0, 0), 10, 0, false, false).SetLoops(10))
            .Join(leftMound.transform.DOMoveY(originalPoses[leftMound.gameObject].y, time));

        foreach (var plat in platforms)
        {
            seq.Join(plat.transform.DOMoveY(originalPoses[plat.gameObject].y, time).SetEase(Ease.OutExpo));
        }

        seq.AppendCallback(() =>
        {
            isTransitionDone = true;
        });

        while (!isTransitionDone) await Task.Yield();
    }

    public override async Task RunExitAnim()
    {
        float time = 2;
        DisablePlats(time);
        
        float shakeStrength = 1;

        bool isTransitionDone = false;

        var seq = DOTween.Sequence()
            .Append(rightMound.transform.DOShakePosition(time / 10, new Vector3(shakeStrength, 0, 0), 10, 0).SetLoops(10))
            .Join(rightMound.transform.DOMoveY(rightMound.transform.position.y - downOffset, time))
            .Append(ground.transform.DOShakePosition(time / 10, new Vector3(shakeStrength, 0, 0), 10, 0).SetLoops(10))
            .Join(ground.transform.DOMoveY(ground.transform.position.y - downOffset, time))
            .Join(leftMound.transform.DOShakePosition(time / 10, new Vector3(shakeStrength, 0, 0), 10, 0, false, false).SetLoops(10))
            .Join(leftMound.transform.DOMoveY(leftMound.transform.position.y - downOffset, time));

        foreach (var plat in platforms)
        {
            seq.Join(plat.transform.DOMoveY(plat.transform.position.y - downOffset, time).SetEase(Ease.InExpo));
        }

        seq.AppendCallback(() =>
        {
            isTransitionDone = true;
        });

        while (!isTransitionDone) await Task.Yield();
    }

    public void DisablePlats(float time)
    {
        if (platDisSeq != null)
            platDisSeq.Kill();

        foreach (var plat in platforms)
        {
            if (plat == null) continue;
            var colliders = plat.GetComponents<Collider2D>();
            foreach (var col in colliders) col.enabled = false;
            
            platDisSeq = DOTween.Sequence()
                .Append(plat.transform.DOShakeRotation(time, new Vector3(2, 2, 20), 10, 10))
                .AppendCallback(() => {foreach (var col in colliders) col.enabled = true;});
        }    
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        if (hitID == 0 && other.collider2D.TryGetComponent(out Player _))
            isPlayerInSky = type == IHitReciever.HitType.Enter;
    }
}
