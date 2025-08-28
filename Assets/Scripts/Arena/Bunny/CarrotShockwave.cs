using DG.Tweening;
using UnityEngine;

public class CarrotShockwave : MonoBehaviour
{
    [SerializeField] private GameObject carrotPrefab;
    [SerializeField] private float distPerCarrot;
    [SerializeField] private float timePerCarrot;
    [SerializeField] private float maxHight;
    [SerializeField] private float maxDist;
    [SerializeField] private float carrotStayTime;
    [SerializeField] private DamageInfo damage;

    private int carrotsSpawned;

    void Start()
    {
        StartShock();
    }

    public void StartShock()
    {
        int carrotAmount = (int)(maxDist / distPerCarrot);
        float heightStep = maxHight / carrotAmount;

        carrotsSpawned = carrotAmount * 2;

        int i = 0;

        DOTween.Sequence()
            .AppendCallback(() =>
            {
                var carrotRight = Instantiate(carrotPrefab).GetComponent<FallingCarrot>();
                carrotRight.Setup(damage, transform.position + Vector3.right * distPerCarrot * i + Vector3.up * heightStep * i - Vector3.up, Vector2.zero, false);
                carrotRight.transform.eulerAngles = new Vector3(0, 0, 180);

                var carrotLeft = Instantiate(carrotPrefab).GetComponent<FallingCarrot>();
                carrotLeft.Setup(damage, transform.position + Vector3.left * distPerCarrot * i + Vector3.up * heightStep * i - Vector3.up, Vector2.zero, false);
                carrotLeft.transform.eulerAngles = new Vector3(0, 0, 180);

                RunStaySeq(carrotRight);
                RunStaySeq(carrotLeft);
            })
            .AppendInterval(timePerCarrot)
            .AppendCallback(() => i++).SetLoops(carrotAmount);
    }

    void RunStaySeq(FallingCarrot carrot)
    {
        DOTween.Sequence()
            .AppendInterval(carrotStayTime)
            .AppendCallback(() =>
            {
                Destroy(carrot.gameObject);
                carrotsSpawned--;
                if (carrotsSpawned == 0)
                    Destroy(gameObject);
            });
    }
}
