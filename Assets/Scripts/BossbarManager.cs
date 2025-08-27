using UnityEngine;

public class BossbarManager : Singleton<BossbarManager>
{
    private BossbarCanvas bossbarCanvas;
    void Start()
    {
        bossbarCanvas = Instantiate(Resources.Load<GameObject>("bossbarCanvas"), transform).GetComponent<BossbarCanvas>();
        bossbarCanvas.gameObject.SetActive(false);
    }

    private Enemy attachedEnemy;
    public void AttachToEnemy(Enemy e)
    {
        attachedEnemy = e;
        e.OnHurtEvent += UpdateBar;
        bossbarCanvas.gameObject.SetActive(true);
        UpdateBar(e);
    }

    void Update()
    {
        if (attachedEnemy == null)
            bossbarCanvas.gameObject.SetActive(false);
    }

    void UpdateBar(Enemy e)
    {
        bossbarCanvas.SetValue(e.Health, e.maxHealth);
    }
}
