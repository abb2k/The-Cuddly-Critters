using UnityEngine;

public class BattleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private Canvas myCanvas;
    [SerializeField] private DialogueSettings dialogueSettings;
    private Dialogue curr;
    public void OnInteract()
    {
        if (Player.Get().ItemEquipped == null) return;

        switch (GameManager.Get().progressIndex)
        {
            case 0:
                ArenaManager.Get().SpawnBossWithArena("PenguinArena");
                break;
            case 1:
                ArenaManager.Get().SpawnBossWithArena("OwlArena");
                break;
            case 2:
                ArenaManager.Get().SpawnBossWithArena("BunnyArena");
                break;
        }

        if (curr != null) {
            var a = DialogueManager.Get();
            a.StopDialogue(curr);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out Player _)) return;

        DialogueText currDialogue = new DialogueText("You have not chosen an item,\nan item must be chosen to enter the arena.");
        if (Player.Get().ItemEquipped != null)
            currDialogue = new DialogueText($"You have chosen the {Player.Get().ItemEquipped.itemName}.\nEnter if you are ready...\n[E]");

        dialogueSettings.texts.Clear();
        dialogueSettings.texts.Add(currDialogue);
        dialogueSettings.alignment = TMPro.HorizontalAlignmentOptions.Center;
        dialogueSettings.autoSkip = true;
        dialogueSettings.unskippable = true;
        dialogueSettings.freezePlayer = false;
        dialogueSettings.autoSkipSpeed = -1;

        curr = DialogueManager.Get().createDialogue(dialogueSettings, myCanvas.transform);
    }
    
    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out Player _)) return;

        if (curr != null) {
            var a = DialogueManager.Get();
            a.StopDialogue(curr);
        }
    }
}
