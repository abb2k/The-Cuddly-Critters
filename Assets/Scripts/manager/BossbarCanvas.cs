using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossbarCanvas : MonoBehaviour
{
    public GameObject bossbarContainer;
    [SerializeField] private Slider HPBar;
    [SerializeField] private TextMeshProUGUI barText;
    [SerializeField] private TextMeshProUGUI bossName;

    public void SetValue(float value, float maxValue)
    {
        HPBar.value = value / maxValue;

        barText.text = $"{value:F2}/{maxValue:F2}";
    }

    public void ShowName(string name, float fadeInTime = .5f, float fadeStayTime = 1.5f, float fadeOutTime = 1)
    {
        bossName.DOKill();
        bossName.color = new Color(255, 255, 255, 0);

        bossName.text = name;

        var seq = DOTween.Sequence();
        seq.Append(bossName.DOColor(Color.white, fadeInTime));
        seq.AppendInterval(fadeStayTime);
        seq.Append(bossName.DOColor(bossName.color, fadeOutTime));
    }
}
