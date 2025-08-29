using UnityEngine;

public class BattleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private Canvas myCanvas;
    private Dialogue curr;
    public void OnInteract()
    {
        if (Player.Get().ItemEquipped == null) return;

        switch (GameManager.Get().progressIndex)
        {
            case 0:
                ArenaManager.Get().SpawnBossWithArena("OwlArena");
                break;
            case 1:
                ArenaManager.Get().SpawnBossWithArena("BunnyArena");
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out Player _)) return;

        DialogueText currDialogue = new DialogueText("You have not chosen an item,\nan item must be chosen to enter the arena.");
        if (Player.Get().ItemEquipped != null)
            currDialogue = new DialogueText($"You have chosen the {Player.Get().ItemEquipped.itemName}.\nEnter if you are ready...");

        DialogueSettings settings = new()
        {
            hideBG = true,
            texts = new()
            {
                currDialogue
            },
            unskippable = true,
            alignment = TMPro.HorizontalAlignmentOptions.Center,
            autoSkip = true,
            autoSkipSpeed = -1,
            freezePlayer = false
        };

        curr = DialogueManager.Get().createDialogue(settings, myCanvas.transform);
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
