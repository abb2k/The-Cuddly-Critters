using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private int tickAmount;
    [SerializeField] private Vector2 tickPadding;
    [SerializeField] private float tickMargin;
    [SerializeField] private float tickSize;
    [SerializeField] private Image ticksBox;
    [SerializeField] private Transform ticksContainer;
    [SerializeField] private GameObject tickPrefab;
    [SerializeField] private Sprite tickSprite;
    private Vector2 originalSize;

    private Dictionary<float, Image> ticks = new();

    void Awake()
    {
        originalSize = ticksBox.GetComponent<RectTransform>().sizeDelta;
    }

    public void UpdateBar(float hp, float maxHP, bool refreshTicks = false)
    {
        float tickValue = maxHP / tickAmount;
        if (ticks.Count != tickAmount || refreshTicks)
            RefreshTicks(tickValue);

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
            tickImage.transform.localScale = Vector2.one * tickSize;
        }

        float hpPrecent = hp / maxHP;

        if (hpPrecent > 1f / 3f * 2)
            ticksBox.color = Color.green;
        else if (hpPrecent > 1f / 3f)
            ticksBox.color = Color.orange;
        else
            ticksBox.color = Color.red;

        ticksContainer.GetComponent<HorizontalLayoutGroup>().spacing = tickMargin;
        ticksContainer.GetComponent<RectTransform>().sizeDelta = originalSize - tickPadding;
    }

    private void RefreshTicks(float tickValue)
    {
        if (!Application.isPlaying) return;

        ticks.Clear();
        
        foreach (Transform child in ticksContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < tickAmount; i++)
        {
            var newTick = Instantiate(tickPrefab, ticksContainer);
            newTick.SetActive(true);        

            ticks.Add(tickValue * i, newTick.GetComponent<HPTick>().image);
        }
    }

}
