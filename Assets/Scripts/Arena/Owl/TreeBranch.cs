using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TreeBranch : MonoBehaviour
{
    private Collider2D branchCollider;
    private Coroutine disableRoutine = null;
    private readonly object objLock = new();
    void Awake()
    {
        branchCollider = GetComponent<Collider2D>();
    }

    public void ShakeFor(float time)
    {
        lock (objLock)
        {
            if (disableRoutine != null) StopCoroutine(disableRoutine);
            if (this != null && gameObject != null && gameObject.activeInHierarchy)
            {
                disableRoutine = StartCoroutine(ShakeTimer(time));

                transform.DOShakeRotation(time, new Vector3(2, 2, 20), 10, 10);
            }
        }
    }

    IEnumerator ShakeTimer(float time)
    {
        branchCollider.enabled = false;
        yield return new WaitForSeconds(time);
        branchCollider.enabled = true;
        disableRoutine = null;
    }
}
