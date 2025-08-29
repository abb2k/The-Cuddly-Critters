using DG.Tweening;
using UnityEngine;

public class ItemPedestal : MonoBehaviour
{
    [SerializeField] private SpecialItem item;
    [SerializeField] private SpriteRenderer display;
    [SerializeField] private Canvas myCanvas;
    private Dialogue curr;

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
        if (item == null) return;
        if (!collision.gameObject.TryGetComponent(out Player _)) return;

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
            autoSkipSpeed = -1
        };

        curr = DialogueManager.Get().createDialogue(settings, myCanvas.transform);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (item == null) return;
        if (!collision.gameObject.TryGetComponent(out Player _)) return;

        if (curr != null) {
            var a = DialogueManager.Get();
            a.StopDialogue(curr);
        }
    }
}
