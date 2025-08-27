using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WeightedListOption<T>
{
    public WeightedListOption(T element, float weight)
    {
        this.element = element;
        this.weight = weight;
    }
    public T element;
    [Range(0, 100)]
    public float weight;
}

[System.Serializable]
public class WeightedList<T> : IEnumerable<WeightedListOption<T>>
{
    [SerializeField] private List<WeightedListOption<T>> values;

    public WeightedList()
    {
        values = new();
    }

    public void Add(T element, float weight)
    {
        weight = Mathf.Clamp(weight, 0, 100);

        values.Add(new WeightedListOption<T>(element, weight));
    }

    public void RemoveAll(T element)
    {
        values.RemoveAll(e => e.element.Equals(element));
    }

    public T ChooseRandom()
    {
        if (values.Count == 0)
            return default;

        float totalWeight = values.Sum((element) => element.weight);

        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var wehightedElement in values)
        {
            cumulative += wehightedElement.weight;
            if (randomValue <= cumulative)
                return wehightedElement.element;
        }

        return values[^1].element;
    }

    public IEnumerator<WeightedListOption<T>> GetEnumerator()
    {
        return values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
