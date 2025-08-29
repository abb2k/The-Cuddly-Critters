using System.Threading.Tasks;
using UnityEngine;

public class ItemPickupArena : ArenaHolder
{
    [SerializeField] private SpecialItem[] itemsAvailable;
    [SerializeField] private ItemPedestal[] pedestals;
    void Start()
    {
        if (GameManager.Get().progressIndex == 0)
        {
            GameManager.Get().SetAvailableItems(itemsAvailable);
        }

        int index = 0;
        foreach (var pedestal in pedestals)
        {
            if (GameManager.Get().ItemsAvailable.Count == index || Player.Get().ItemEquipped == GameManager.Get().ItemsAvailable[index]) return;
            pedestal.SetItem(GameManager.Get().ItemsAvailable[index]);

            index++;
        }
    }

    public override async Task RunEntryAnim()
    {
        await base.RunEntryAnim();
    }

    public override async Task RunExitAnim()
    {
        await base.RunExitAnim();
    }
}
