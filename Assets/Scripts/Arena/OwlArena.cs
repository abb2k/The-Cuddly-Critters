using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OwlArena : ArenaHolder
{
    [SerializeField] private TreeBranch[] branches;
    [SerializeField] private float downDistance;
    private Light2D globalLight;
    public override async Task RunEntryAnim()
    {
        Dictionary<TreeBranch, Vector3> originalPoses = new();
        foreach (var branch in branches)
        {
            originalPoses.Add(branch, branch.transform.position);
            branch.transform.DOMoveY(branch.transform.position.y - downDistance, 0);
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

        foreach (var branch in branches)
        {
            float t = 1.5f;
            branch.ShakeFor(t);
            branch.transform.DOMoveY(originalPoses[branch].y, t).SetEase(Ease.OutQuad);
            await Task.Delay(100);
        }
    }

    public override async Task RunExitAnim()
    {
        Player.Get().TurnLight(false);

        if (globalLight != null)
            DOTween.To(() => globalLight.intensity, x => globalLight.intensity = x, 1, 1);

        int doEnd = 0;

        foreach (var branch in branches)
        {
            float t = 1.5f;
            branch.ShakeFor(t);
            doEnd++;
            var seq = DOTween.Sequence();
            seq.Append(branch.transform.DOMoveY(branch.transform.position.y - downDistance, t).SetEase(Ease.InQuad));
            seq.AppendCallback(() => doEnd--);
            await Task.Delay(100);
        }

        while (true)
        {
            if (doEnd <= 0) break;

            await Task.Yield();
        }
    }
}
