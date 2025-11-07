using UnityEngine;

[DisallowMultipleComponent]
public class HY_FogRevealer : MonoBehaviour
{
    [Tooltip("이 유닛의 시야 반경(월드 단위)")]
    public float visionRadius = 3.5f;

    [Tooltip("이 주기(초)마다 안개를 갱신")]
    public float revealInterval = 0.1f;

    private float t;

    void Update()
    {
        if (HY_FogOfWar.Instance == null) return;
        t += Time.deltaTime;
        if (t >= revealInterval)
        {
            HY_FogOfWar.Instance.RevealCircle(transform.position, visionRadius);
            t = 0f;
        }
    }
}
