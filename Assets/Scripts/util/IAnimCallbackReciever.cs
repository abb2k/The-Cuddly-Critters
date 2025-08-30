using UnityEngine;

public interface IAnimCallbackReciever
{
    virtual void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    virtual void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    virtual void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    virtual void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    virtual void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
}
