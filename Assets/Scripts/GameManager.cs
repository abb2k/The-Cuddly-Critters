using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public int progressIndex { get; private set; } = 0;

    public List<SpecialItem> ItemsAvailable { get; private set; } = new();

    public void AddProgress()
    {
        progressIndex++;
    }

    public void SetAvailableItems(params SpecialItem[] items)
    {
        ItemsAvailable = items.ToList();
    }

    public void RemoveItem(SpecialItem item)
    {
        if (ItemsAvailable.Contains(item))
            ItemsAvailable.Remove(item);
    }
}
