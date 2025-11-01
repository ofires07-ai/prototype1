using UnityEngine;

/// <summary>
/// A* 없이 간단한 직선 이동으로 circle을 향해 이동합니다.
/// </summary>
public class HY_SimpleUnitMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.5f;
    
    private Transform target;
    private bool hasTarget = false;

    void Start()
    {
        // Circle 찾기
        GameObject circleObj = GameObject.Find("Circle");
        if (circleObj != null)
        {
            target = circleObj.transform;
            hasTarget = true;
            Debug.Log($"[SimpleUnitMovement] {name}: Circle을 찾았습니다! 위치: {target.position}");
        }
        else
        {
            Debug.LogError($"[SimpleUnitMovement] {name}: Circle을 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        if (!hasTarget || target == null) return;

        // circle까지의 거리
        float distance = Vector3.Distance(transform.position, target.position);

        // 아직 도착하지 않았으면 이동
        if (distance > stoppingDistance)
        {
            // 방향 계산
            Vector3 direction = (target.position - transform.position).normalized;
            
            // 이동
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // 디버그 정보 (1초마다)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[SimpleUnitMovement] {name}: circle까지 거리 = {distance:F2}m, 현재 위치 = {transform.position}");
            }
        }
        else
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[SimpleUnitMovement] {name}: circle에 도착했습니다!");
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasTarget = true;
    }

    // Scene 뷰에서 경로 표시
    void OnDrawGizmos()
    {
        if (target != null && hasTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}