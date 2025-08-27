using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class OwlArena : ArenaHolder
{
    [SerializeField] private TreeBranch[] branches;
    [SerializeField] private float downDistance;
    public override async Task RunEntryAnim()
    {
        Dictionary<TreeBranch, Vector3> originalPoses = new();
        foreach (var branch in branches)
        {
            originalPoses.Add(branch, branch.transform.position);
            branch.transform.DOMoveY(branch.transform.position.y - downDistance, 0);
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
        
    }
}
