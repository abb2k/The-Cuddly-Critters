using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossbarCanvas : MonoBehaviour
{
    public HPBar HPBar;
    [SerializeField] private TextMeshProUGUI bossName;

    public void SetValue(float value, float maxValue)
    {
        HPBar.UpdateBar(value, maxValue);
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
