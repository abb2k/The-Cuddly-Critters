using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PenguinArena : ArenaHolder, IHitReciever
{
    public PenguinArenaGyser[] gysers;
    [SerializeField] private float goUpTime;
    [SerializeField] private float goUpOffset;
    [SerializeField] private Rigidbody2D goUpParent;
    [SerializeField] private BoxCollider2D topCollider;
    [SerializeField] private AudioClip bgMusic;
    public bool didBattleStart = false;
    public override IEnumerator RunEntryAnim()
    {
        topCollider.enabled = false;

        Vector2 goUpOGPos = goUpParent.position;
        goUpParent.transform.position -= Vector3.up * goUpOffset;

        DOTween.To(
            () => goUpParent.position.y, x =>
            {

                var pos = goUpParent.position;
                pos.y = x;
                goUpParent.MovePosition(pos);
            },
            goUpOGPos.y,
            goUpTime
        ).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(goUpTime);
        topCollider.enabled = true;
        didBattleStart = true;
        ArenaManager.Get().PlayGlobalArenaMusic(bgMusic, .1f, 1);
    }

    public override IEnumerator RunExitAnim()
    {
        topCollider.enabled = false;

        DOTween.To(
            () => goUpParent.position.y, x =>
            {

                var pos = goUpParent.position;
                pos.y = x;
                goUpParent.MovePosition(pos);
            },
            (goUpParent.position - Vector2.up * goUpOffset).y,
            goUpTime
        ).SetEase(Ease.InQuad);

        yield return new WaitForSeconds(goUpTime);
        topCollider.enabled = true;
    }

    public void HitRecieved(int hitID, IHitReciever.HitType type, bool isTriggerHit, Colliders other)
    {
        if (hitID == 1 && other.collider2D.TryGetComponent(out Penguin _))
            topCollider.enabled = true;
    }
}
