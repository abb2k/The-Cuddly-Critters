using DG.Tweening;
using UnityEngine;

public class BunnyBurrowWarning : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    public void Startup(float time)
    {
        particles.Play();

        DOTween.Sequence()
            .AppendInterval(time)
            .AppendCallback(() => particles.Stop())
            .AppendInterval(Mathf.Max(particles.main.startLifetime.constantMin, particles.main.startLifetime.constantMax))
            .AppendCallback(() => Destroy(gameObject));
    }
}
