using UnityEngine;
using UnityEngine.Events;

public class BossEnemy : Enemy
{
    public virtual void RunEntryAnim(UnityAction<string> showName, UnityAction onEnded){}
}
