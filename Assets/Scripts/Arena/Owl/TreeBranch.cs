using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TreeBranch : MonoBehaviour
{
    private Collider2D[] branchColliders;
    private Coroutine disableRoutine = null;
    private readonly object objLock = new();
    Tweener shake;
    void Awake()
    {
        branchColliders = GetComponents<Collider2D>();
    }

    public void ShakeFor(float time)
    {
        lock (objLock)
        {
            if (disableRoutine != null) StopCoroutine(disableRoutine);
            if (this != null && gameObject != null && gameObject.activeInHierarchy)
            {
                disableRoutine = StartCoroutine(ShakeTimer(time));

                if (shake == null || !shake.IsPlaying())
                    shake = transform.DOShakeRotation(time, new Vector3(2, 2, 20), 10, 10, true, ShakeRandomnessMode.Harmonic);
            }
        }
    }

    IEnumerator ShakeTimer(float time)
    {
        foreach (var collider in branchColliders) collider.enabled = false;
        yield return new WaitForSeconds(time);
        foreach (var collider in branchColliders) collider.enabled = true;
        disableRoutine = null;
    }
}
