using DG.Tweening;
using UnityEngine;

public class ItemPedestal : MonoBehaviour, IInteractable
{
    [SerializeField] private SpecialItem item;
    [SerializeField] private SpriteRenderer display;
    [SerializeField] private Canvas myCanvas;
    [SerializeField] private DialogueSettings dialogueSettings;
    private Dialogue curr;

    private bool isCollidingWithPlayer;

    void Start()
    {
        SetItem(item);
    }

    public void SetItem(SpecialItem item)
    {
        this.item = item;

        display.sprite = item == null ? null : item.generalVisual;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out Player _)) return;
        isCollidingWithPlayer = true;
        if (item == null) return;

        RunDialogue();
    }
    void RunDialogue()
    {
        dialogueSettings.texts.Clear();
        dialogueSettings.texts.Add(new DialogueText(item.description));
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
        isCollidingWithPlayer = false;
        if (item == null) return;

        if (curr != null) {
            var a = DialogueManager.Get();
            a.StopDialogue(curr);
        }
    }

    public void OnInteract()
    {
        if (Player.Get().ItemEquipped == null && item != null)
        {
            Player.Get().EquipItem(item);
            SetItem(null);

            if (curr != null)
            {
                var a = DialogueManager.Get();
                a.StopDialogue(curr);
            }
        }
        else if (Player.Get().ItemEquipped != null && item == null)
        {
            SetItem(Player.Get().ItemEquipped);
            Player.Get().EquipItem(null);
            if (isCollidingWithPlayer)
                RunDialogue();
        }
    }
}
