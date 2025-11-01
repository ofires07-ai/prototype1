using UnityEngine;

/// <summary>
/// 유닛의 시야 범위를 관리하고 Fog of War를 업데이트합니다.
/// </summary>
public class UnitVision : MonoBehaviour
{
    [Header("시야 설정")]
    [Tooltip("유닛의 시야 반경")]
    [SerializeField] private float visionRadius = 1000f;
    
    [Tooltip("시야 업데이트 주기 (초)")]
    [SerializeField] private float updateInterval = 0.1f;

    [Header("디버그")]
    [Tooltip("Scene 뷰에서 시야 범위 표시")]
    [SerializeField] private bool showVisionRange = true;

    private FogOfWar fogOfWar;
    private float nextUpdateTime = 0f;

    void Start()
    {
        // FogOfWar 찾기
        fogOfWar = FindObjectOfType<FogOfWar>();
        
        if (fogOfWar == null)
        {
            Debug.LogWarning($"[UnitVision] {name}: FogOfWar 시스템을 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[UnitVision] {name}: FogOfWar 연결 완료 (시야 반경: {visionRadius})");
        }

        // 즉시 첫 업데이트
        UpdateVision();
    }

    void Update()
    {
        if (fogOfWar == null) return;

        // 일정 주기마다 시야 업데이트
        if (Time.time >= nextUpdateTime)
        {
            UpdateVision();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    /// <summary>
    /// 현재 위치의 Fog를 밝게 만듭니다
    /// </summary>
    void UpdateVision()
    {
        if (fogOfWar != null)
        {
            fogOfWar.RevealArea(transform.position, visionRadius);
        }
    }

    /// <summary>
    /// 시야 범위 변경
    /// </summary>
    public void SetVisionRadius(float radius)
    {
        visionRadius = radius;
        UpdateVision();
    }

    /// <summary>
    /// 유닛이 파괴될 때 시야도 제거 (선택적)
    /// </summary>
    void OnDestroy()
    {
        // 선택: 유닛이 사라지면 해당 위치를 다시 어둡게
        // if (fogOfWar != null)
        // {
        //     fogOfWar.ConcealArea(transform.position, visionRadius);
        // }
    }

    // Scene 뷰에서 시야 범위 시각화
    void OnDrawGizmos()
    {
        if (!showVisionRange) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }

    void OnDrawGizmosSelected()
    {
        if (!showVisionRange) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
}