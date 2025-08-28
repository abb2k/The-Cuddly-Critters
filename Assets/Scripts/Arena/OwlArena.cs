using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OwlArena : ArenaHolder
{
    [SerializeField] private TreeBranch[] branches;
    [SerializeField] private Transform[] riseWithBranches;
    [SerializeField] private float downDistance;
    private Light2D globalLight;
    public override async Task RunEntryAnim()
    {
        Dictionary<Transform, Vector3> originalPoses = new();
        foreach (var branch in branches)
        {
            originalPoses.Add(branch.transform, branch.transform.position);
            branch.transform.DOMoveY(branch.transform.position.y - downDistance, 0);
        }

        foreach (var other in riseWithBranches)
        {
            originalPoses.Add(other, other.position);
            other.DOMoveY(other.position.y - downDistance, 0);
        }

        Player.Get().TurnLight(true);

        var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.InstanceID)
            .Where(l => l.lightType == Light2D.LightType.Global)
            .ToList();

        if (lights.Count != 0)
        {
            globalLight = lights[0];
            DOTween.To(() => globalLight.intensity, x => globalLight.intensity = x, 0.15f, 1);
        }

        float t = 1.5f;
        float additionalTime = 0;
        foreach (var branch in branches)
        {
            branch.ShakeFor(t);
            if (!Application.isPlaying) return;
            branch.transform.DOMoveY(originalPoses[branch.transform].y, t + additionalTime).SetEase(Ease.OutQuad);
            additionalTime += .1f;
        }

        additionalTime = 0;

        foreach (var other in riseWithBranches)
        {

            if (!Application.isPlaying) return;
            other.DOMoveY(originalPoses[other].y, t + additionalTime).SetEase(Ease.OutQuad);
            additionalTime += .1f;
        }
    }

    public override async Task RunExitAnim()
    {
        Player.Get().TurnLight(false);

        if (globalLight != null)
            DOTween.To(() => globalLight.intensity, x => globalLight.intensity = x, 1, 1);

        int doEnd = 0;

        float t = 1.5f;
        float additionalTime = 0;
        foreach (var branch in branches)
        {
            branch.ShakeFor(t);
            doEnd++;
            if (!Application.isPlaying) return;
            var seq = DOTween.Sequence();
            seq.Append(branch.transform.DOMoveY(branch.transform.position.y - downDistance, t + additionalTime).SetEase(Ease.InQuad));
            seq.AppendCallback(() => doEnd--);
            additionalTime += .1f;
        }
        additionalTime = 0;

        foreach (var other in riseWithBranches)
        {
            doEnd++;
            if (!Application.isPlaying) return;
            var seq = DOTween.Sequence();
            seq.Append(other.DOMoveY(other.position.y - downDistance, t + additionalTime).SetEase(Ease.InQuad));
            seq.AppendCallback(() => doEnd--);
            additionalTime += .1f;
        }

        while (true)
        {
            if (doEnd <= 0) break;

            if (!Application.isPlaying) return;

            await Task.Yield();
        }
    }
}
