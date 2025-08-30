using System.Threading.Tasks;
using UnityEngine;

public class ArenaHolder : MonoBehaviour
{
    public virtual async Task RunEntryAnim() { await Task.Yield(); }
    public virtual async Task RunExitAnim() { await Task.Yield(); }
    public virtual void OnPayloadRecieved(object[] payload) { }
}
