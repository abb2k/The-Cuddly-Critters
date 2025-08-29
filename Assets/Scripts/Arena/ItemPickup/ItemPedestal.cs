using DG.Tweening;
using UnityEngine;

public class ItemPedestal : MonoBehaviour, IInteractable
{
    [SerializeField] private SpecialItem item;
    [SerializeField] private SpriteRenderer display;
    [SerializeField] private Canvas myCanvas;
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
        DialogueSettings settings = new()
        {
            hideBG = true,
            texts = new()
            {
                new DialogueText(item.description)
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
