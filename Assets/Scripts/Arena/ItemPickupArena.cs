using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct DialogueForScore
{
    public float scoreMinimum;
    public DialogueSettings dialogue;
}

public class ItemPickupArena : ArenaHolder
{
    [SerializeField] private SpecialItem[] itemsAvailable;
    [SerializeField] private ItemPedestal[] pedestals;
    [SerializeField] private Vector2 DoorPos;
    [SerializeField] private DialogueSettings startDialogue;
    [SerializeField] private DialogueForScore[] finalDialogues;
    [SerializeField] private AudioClip bgMusic;
    public event UnityAction OnEntryTransitionEnded;
    void Start()
    {
        if (GameManager.Get().progressIndex == 0)
        {
            GameManager.Get().SetAvailableItems(itemsAvailable);
            GameManager.Get().ResetScore();

            GameManager.Get().FadeOut(0);
            GameManager.Get().isInSeqance = true;
            DOTween.Sequence().AppendInterval(1).AppendCallback(() => GameManager.Get().FadeIn(2, StartDialogue));
            OnEntryTransitionEnded += () => GameManager.Get().isInSeqance = true;
        }

        int index = 0;
        foreach (var pedestal in pedestals)
        {
            if (GameManager.Get().ItemsAvailable.Count == index || Player.Get().ItemEquipped == GameManager.Get().ItemsAvailable[index]) return;
            pedestal.SetItem(GameManager.Get().ItemsAvailable[index]);

            index++;
        }
    }

    void StartDialogue()
    {
        ArenaManager.Get().RunCamChake(3, 1);
        DialogueManager.Get().createDialogue(startDialogue).OnDialogueComplete += () => GameManager.Get().isInSeqance = false;
    }

    public override IEnumerator RunEntryAnim()
    {
        bool isInProgress = true;
        GameManager.Get().isInSeqance = true;

        Player.Get().transform.DOMove(DoorPos, 1).SetEase(Ease.OutSine).OnComplete(() => isInProgress = false);

        DOTween.To(() => ArenaManager.Get().templeBG.color.a * 255, x =>
        {
            var color = ArenaManager.Get().templeBG.color;
            color.a = x / 255f;
            ArenaManager.Get().templeBG.color = color;
        }, 255, 1);

        foreach (var pedestal in pedestals)
        {
            var originalPos = pedestal.transform.position;

            pedestal.transform.DOMoveY(-20, 0).SetRelative(true);
            pedestal.transform.DOMoveY(20, 1).SetRelative(true).SetEase(Ease.OutSine);
        }

        while (isInProgress) yield return null;

        GameManager.Get().isInSeqance = false;
        OnEntryTransitionEnded?.Invoke();
        Player.Get().RevivePlayer();

        ArenaManager.Get().PlayGlobalArenaMusic(bgMusic, .2f, 1);
    }

    public override IEnumerator RunExitAnim()
    {
        GameManager.Get().isInSeqance = true;
        DOTween.To(() => ArenaManager.Get().templeBG.color.a * 255, x =>
        {
            var color = ArenaManager.Get().templeBG.color;
            color.a = x / 255f;
            ArenaManager.Get().templeBG.color = color;
        }, 50, 1);
        foreach (var pedestal in pedestals)
        {
            pedestal.transform.DOMoveY(-20, 1).SetRelative(true).SetEase(Ease.InSine);
        }

        yield return new WaitForSeconds(1);
        GameManager.Get().isInSeqance = false;
    }

    public override void OnPayloadRecieved(object[] payload)
    {
        if (GameManager.Get().progressIndex != 0 && payload.Length != 0 && payload[0] is DialogueSettings dialogue)
        {
            dialogue.name = "exitDialogue";
            DialogueManager.Get().createDialogue(dialogue);
        }
        if (GameManager.Get().progressIndex == 3)
        {
            StartCoroutine(WaitForEndingDialogue());
        }
    }

    IEnumerator WaitForEndingDialogue()
    {
        yield return null;
        yield return null;
        yield return new WaitUntil(() => DialogueManager.Get().getDialogue("exitDialogue") == null);

        var orderedDialogues = finalDialogues.ToList().OrderBy(e => e.scoreMinimum).ToList();

        foreach (var dialogue in orderedDialogues)
        {
            if (dialogue.scoreMinimum <= GameManager.Get().Score)
            {
                dialogue.dialogue.name = "endDia";
                DialogueManager.Get().createDialogue(dialogue.dialogue).OnDialogueComplete += GameComplete;
                break;
            }
        }

    }

    void GameComplete()
    {
        GameManager.Get().isInSeqance = true;
        GameManager.Get().FadeOut(1, () =>
        {
            GameManager.Get().ResetScore();
            GameManager.Get().ResetProgress();
            Player.Get().EquipItem(null);
            GameManager.Get().SetAvailableItems(itemsAvailable);

            ArenaManager.Get().OpenUpArena("ItemPickupArena");
        });
    }
}
