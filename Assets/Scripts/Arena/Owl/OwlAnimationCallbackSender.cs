using UnityEngine;

public class OwlAnimationCallbackSender : MonoBehaviour
{
    [SerializeField] private Owl mainOwl;
    public void OnWingFlap()
    {
        mainOwl.OnWingFlap();
    }
}
