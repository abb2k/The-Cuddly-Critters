using UnityEngine;

public enum ItemVisualSorting
{
    BelowPlayer,
    AbovePlayer
}

[CreateAssetMenu(fileName = "SpecialItem", menuName = "Scriptable Objects/SpecialItem")]
public class SpecialItem : ScriptableObject
{
    [Multiline]
    public string description;
    public Sprite generalVisual;
    public Sprite bodyVisual;
    public ItemVisualSorting bodyVisualSorting;
    public Sprite weponVisual;
    public ItemVisualSorting weponVisualSorting;
    public PlayerStats modifyedStats;
}
