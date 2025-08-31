using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : Singleton<GameManager>
{
    public int progressIndex { get; private set; } = 0;
    private GlobalCanvas canvas;

    public List<SpecialItem> ItemsAvailable { get; private set; } = new();
    public bool isInSeqance;
    public float Score { get; private set; }

    protected override void OnLoaded()
    {
        canvas = Instantiate(Resources.Load<GameObject>("globalCanvas"), transform).GetComponent<GlobalCanvas>();
    }

    public void AddProgress()
    {
        progressIndex++;
    }

    public void ResetProgress()
    {
        progressIndex = 0;
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

    public void FadeIn(float time, UnityAction callback = null)
    {
        canvas.FG.DOColor(new Color(0, 0, 0, 0), time).OnComplete(() => callback?.Invoke());
    }

    public void FadeOut(float time, UnityAction callback = null)
    {
        canvas.FG.DOColor(Color.black, time).OnComplete(() => callback?.Invoke());
    }

    public void AddScore(float score)
    {
        Score += score;
    }

    public void ResetScore()
    {
        Score = 0;
    }
}
