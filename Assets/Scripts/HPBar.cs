using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private float tickValue;
    [SerializeField] private Vector2 tickPadding;
    [SerializeField] private float tickMargin;
    [SerializeField] private Image ticksBox;
    [SerializeField] private Transform ticksContainer;
    [SerializeField] private GameObject tickPrefab;
    [SerializeField] private Sprite tickSprite;

    private Dictionary<float, Image> ticks = new();

    public void UpdateBar(float hp, float maxHP, bool refreshTicks = false)
    {
        int maxTicks = (int)(maxHP / tickValue);
        if (ticks.Count != maxTicks || refreshTicks)
            RefreshTicks(maxTicks);

        float currHPTickValue = hp;

        foreach (var (currTickValue, tickImage) in ticks)
        {
            if (currHPTickValue > (currTickValue + tickValue / 3f * 2))
                tickImage.color = Color.green;
            else if (currHPTickValue > (currTickValue + tickValue / 3f))
                tickImage.color = Color.orange;
            else if (currHPTickValue > currTickValue)
                tickImage.color = Color.red;
            else
                tickImage.color = new Color(0, 0, 0, 0);

            tickImage.sprite = tickSprite;
        }

        ticksContainer.GetComponent<GridLayoutGroup>().spacing = Vector2.right * tickMargin;
    }

    private void RefreshTicks(int tickAmount)
    {
        if (!Application.isPlaying) return;
        
        foreach (Transform child in ticksContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < tickAmount; i++)
        {
            var newTick = Instantiate(tickPrefab, ticksContainer);
            newTick.SetActive(true);

            ticksContainer.GetComponent<RectTransform>().sizeDelta -= Vector2.right * tickPadding.x + Vector2.up * tickPadding.y;

            ticksContainer.GetComponent<GridLayoutGroup>().cellSize = Vector2.one * (ticksContainer.GetComponent<RectTransform>().sizeDelta.x / tickAmount - tickMargin - tickPadding.x);
            

            ticks.Add(tickValue * i, newTick.GetComponent<Image>());
        }
    }

}
