using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Colliders
{
    public Collider2D collider2D = null;
    public Collision2D collision2D = null;

    public Colliders(Collider2D collider2D) => this.collider2D = collider2D;
    public Colliders(Collision2D collision2D) => this.collision2D = collision2D;
}

/// <summary>
/// a interface to easily send hits/collisions from other objects to one specific object without creating a lot of small scripts
/// </summary>
public interface IHitReciever
{
    enum HitType
    {
        Enter,
        Stay,
        Exit
    }
    void HitRecieved(int hitID, HitType type, bool isTriggerHit, Colliders other);
}

public class HitSender : MonoBehaviour
{
    [SerializeField] private GameObject hitReciever;

    [Header("Hit data")]
    [SerializeField] private int hitID;
    [Space]
    [SerializeField] private bool getEnter;
    [SerializeField] private bool getStay;
    [SerializeField] private bool getExit;
    [Space]
    [SerializeField] private bool isTrigger;

    private IHitReciever hitRecieverInterface;

    private void Start()
    {
        hitRecieverInterface = hitReciever.GetComponent<IHitReciever>();

        if (hitRecieverInterface == null)
            Debug.LogError("Referenced hitReciever does not inherate 'IHitReciever' " + gameObject.name);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitRecieverInterface == null || !getEnter || !isTrigger) return;

        hitRecieverInterface.HitRecieved(hitID, IHitReciever.HitType.Enter, isTrigger, new Colliders(collision));
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (hitRecieverInterface == null || !getStay || !isTrigger) return;

        hitRecieverInterface.HitRecieved(hitID, IHitReciever.HitType.Stay, isTrigger, new Colliders(collision));
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (hitRecieverInterface == null || !getExit || !isTrigger) return;

        hitRecieverInterface.HitRecieved(hitID, IHitReciever.HitType.Exit, isTrigger, new Colliders(collision));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hitRecieverInterface == null || !getEnter || isTrigger) return;

        hitRecieverInterface.HitRecieved(hitID, IHitReciever.HitType.Enter, isTrigger, new Colliders(collision));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (hitRecieverInterface == null || !getStay || isTrigger) return;

        hitRecieverInterface.HitRecieved(hitID, IHitReciever.HitType.Stay, isTrigger, new Colliders(collision));
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (hitRecieverInterface == null || !getExit || isTrigger) return;

        hitRecieverInterface.HitRecieved(hitID, IHitReciever.HitType.Exit, isTrigger, new Colliders(collision));
    }

    public int GetHitID() => hitID;
}
