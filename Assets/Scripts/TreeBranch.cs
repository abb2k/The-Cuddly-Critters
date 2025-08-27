using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TreeBranch : MonoBehaviour
{
    private Collider2D branchCollider;
    private Coroutine disableRoutine = null;
    void Start()
    {
        branchCollider = GetComponent<Collider2D>();
    }

    public void ShakeFor(float time)
    {
        if (disableRoutine != null) StopCoroutine(disableRoutine);
        disableRoutine = StartCoroutine(ShakeTimer(time));

        transform.DOKill();

        transform.DOShakeRotation(time, new Vector3(2, 2, 20), 10, 10);
    }

    IEnumerator ShakeTimer(float time)
    {
        branchCollider.enabled = false;
        yield return new WaitForSeconds(time);
        branchCollider.enabled = true;
        disableRoutine = null;
    }
}
