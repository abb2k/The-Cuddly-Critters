using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class TreeBranchLight : MonoBehaviour, IHittable
{
    [SerializeField] private Light2D myLight;
    [SerializeField] private AudioClip turnOnSFX;
    [SerializeField] private AudioClip turnOffSFX;
    private Tween tunTween = null;
    public bool isOn;

    //Light Sprite
    public SpriteRenderer mySprite;

    public event UnityAction<TreeBranchLight, bool> OnLightStateChanged;

    public void OnHit(DamageInfo info)
    {
        if (info.damageSource.layer != LayerMask.NameToLayer("Player")) return;

        TurnOn();
    }

    public void TurnOn()
    {
        tunTween?.Kill();
        tunTween = DOTween.To(() => myLight.intensity, x => myLight.intensity = x, 1f, 2f);
        isOn = true;

        // Enable sprite
        if (mySprite != null) mySprite.enabled = true;

        OnLightStateChanged?.Invoke(this, true);

        AudioManager.PlayTemporarySource(turnOnSFX);
    }

    public void TurnOff()
    {
        tunTween?.Kill();
        tunTween = DOTween.To(() => myLight.intensity, x => myLight.intensity = x, 0, .5f);
        isOn = false;

        // Disable sprite
        if (mySprite != null) mySprite.enabled = false;

        OnLightStateChanged?.Invoke(this, false);

        AudioManager.PlayTemporarySource(turnOffSFX);
    }
}
