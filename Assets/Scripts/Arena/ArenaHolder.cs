using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ArenaHolder : MonoBehaviour
{
    public virtual IEnumerator RunEntryAnim() { yield break; }
    public virtual IEnumerator RunExitAnim() { yield break; }
    public virtual void OnPayloadRecieved(object[] payload) { }
}
