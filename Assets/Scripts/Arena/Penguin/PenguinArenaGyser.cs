using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PenguinArenaGyser : MonoBehaviour, IAnimCallbackReciever
{
    [SerializeField] private Vector2 minMaxActivationTime;
    [SerializeField] private float eruptionForce;
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private AudioClip prepSound;
    [SerializeField] private AudioClip eruptSound;

    private bool isOngoing = false;
    private bool spawnFishOnCurrentEruption = false;

    private Tween timer;
    private List<Rigidbody2D> bodiesInRange = new();

    void Start()
    {
        ResetTimer();
    }

    void ResetTimer()
    {
        if (timer != null)
            timer.Kill();

        timer = DOTween.Sequence().AppendInterval(Random.Range(minMaxActivationTime.x, minMaxActivationTime.y)).AppendCallback(() => Activate(false));
    }

    public void Activate(bool fish)
    {
        if (isOngoing) return;
        isOngoing = true;

        if (timer != null)
            timer.Kill();

        spawnFishOnCurrentEruption = fish;

        GetComponent<Animator>().Play("GyserErput");

        AudioManager.PlayTemporarySource(prepSound, 1, 3);
    }


    public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        isOngoing = false;

        ResetTimer();
    }

    public void OnErupt()
    {
        if (spawnFishOnCurrentEruption)
        {
            spawnFishOnCurrentEruption = false;
            var fishBody = Instantiate(fishPrefab).GetComponent<Rigidbody2D>();
            fishBody.transform.position = transform.position;
            fishBody.AddForceY(eruptionForce, ForceMode2D.Impulse);
            fishBody.AddForceX(Random.value - .5f, ForceMode2D.Impulse);
            fishBody.AddTorque(eruptionForce / 10, ForceMode2D.Impulse);
        }

        AudioManager.PlayTemporarySource(eruptSound);

        bodiesInRange.ForEach(body =>
        {
            if (body.TryGetComponent(out Player player))
                player.CancleJump();
            body.AddForceY(eruptionForce, ForceMode2D.Impulse);
        });
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Rigidbody2D rb))
            bodiesInRange.Add(rb);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Rigidbody2D rb) && bodiesInRange.Contains(rb))
            bodiesInRange.Remove(rb);
    }
}
