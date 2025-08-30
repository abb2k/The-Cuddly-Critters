using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PenguinArena : ArenaHolder
{
    public PenguinArenaGyser[] gysers;
    [SerializeField] private float goUpTime;
    [SerializeField] private float goUpOffset;
    [SerializeField] private Rigidbody2D goUpParent;
    [SerializeField] private BoxCollider2D topCollider;
    [SerializeField] private AudioClip bgMusic;
    public bool didBattleStart = false;
    public override async Task RunEntryAnim()
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

        await Task.Delay((int)(goUpTime * 1000));
        topCollider.enabled = true;
        didBattleStart = true;
        ArenaManager.Get().PlayGlobalArenaMusic(bgMusic, .1f, 1);
    }
    public override async Task RunExitAnim()
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

        await Task.Delay((int)(goUpTime * 1000));
        topCollider.enabled = true;
    }
}
