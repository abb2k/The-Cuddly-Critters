using System.Threading.Tasks;
using UnityEngine;

public class PenguinArena : ArenaHolder
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
