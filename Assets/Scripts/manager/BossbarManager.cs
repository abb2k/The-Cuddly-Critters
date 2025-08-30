using UnityEngine;

public class BossbarManager : Singleton<BossbarManager>
{
    public BossbarCanvas bossbarCanvas;
    protected override string SingletonName => "BossbarManager";
    protected override void Awake()
    {
        bossbarCanvas = Instantiate(Resources.Load<GameObject>("bossbarCanvas"), transform).GetComponent<BossbarCanvas>();
        bossbarCanvas.HPBar.gameObject.SetActive(false);
    }

    private Enemy attachedEnemy;
    public void AttachToEnemy(Enemy e)
    {
        attachedEnemy = e;
        e.OnHurtEvent += UpdateBar;
        bossbarCanvas.HPBar.gameObject.SetActive(true);
        UpdateBar(e);
    }

    void Update()
    {
        if (attachedEnemy == null)
            bossbarCanvas.HPBar.gameObject.SetActive(false);
    }

    void UpdateBar(Enemy e)
    {
        bossbarCanvas.SetValue(e.Health, e.maxHealth);
    }
}
