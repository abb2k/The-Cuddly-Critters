using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossbarCanvas : MonoBehaviour
{
    [SerializeField] private Slider HPBar;
    [SerializeField] private TextMeshProUGUI barText;

    public void SetValue(float value, float maxValue)
    {
        HPBar.value = value / maxValue;

        barText.text = $"{value:F2}/{maxValue:F2}";
    }
}
