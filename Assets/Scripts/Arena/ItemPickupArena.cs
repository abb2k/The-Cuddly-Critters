using System.Threading.Tasks;
using UnityEngine;

public class ItemPickupArena : ArenaHolder
{
    public override async Task RunEntryAnim()
    {
        await base.RunEntryAnim();
    }

    public override async Task RunExitAnim()
    {
        await base.RunExitAnim();
    }
}
