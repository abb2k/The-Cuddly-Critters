using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class ArcEdge : MonoBehaviour
{
    [Min(0.01f)] public float radius = 5f;
    [Range(3,512)] public int segments = 64;
    [Min(0f)] public float edgeRadius = 0.1f;

    [Range(0f, 360f)] public float arcAngle = 180f; // arc length
    [Range(0f, 360f)] public float startAngle = 0f; // rotation offset

    void Reset() => Build();
    void OnValidate() => Build();

    public void Build()
    {
        var ec = GetComponent<EdgeCollider2D>();

        int ptsCount = segments + 1;
        var pts = new Vector2[ptsCount];

        float arcRad = arcAngle * Mathf.Deg2Rad;
        float startRad = startAngle * Mathf.Deg2Rad;

        for (int i = 0; i < ptsCount; i++)
        {
            float t = i / (float)segments;
            float ang = startRad + t * arcRad;
            pts[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
        }

        ec.points = pts;
        ec.edgeRadius = edgeRadius;
    }
}
