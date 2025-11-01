using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 웨이포인트 시스템과 타겟 감지를 통합한 유닛 이동 스크립트.
/// 타겟이 감지되면 이동을 멈추고, 그렇지 않으면 웨이포인트를 따라 이동합니다.
/// </summary>
public class HY_UnitMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("웨이포인트 설정")]
    [Tooltip("Circle로 시작하는 오브젝트들을 자동으로 찾아 웨이포인트로 사용")]
    [SerializeField] private bool autoFindCircles = true;
    [Tooltip("수동으로 웨이포인트를 지정 (autoFindCircles가 false일 때)")]
    [SerializeField] private Transform[] manualWaypoints;

    [Header("애니메이션 설정")]
    [Tooltip("Animator 컴포넌트 (자동으로 찾습니다)")]
    [SerializeField] private Animator animator;
    [Tooltip("걷기 애니메이션 파라미터 이름")]
    [SerializeField] private string walkParameterName = "isWalking";
    [Tooltip("대기 애니메이션 파라미터 이름")]
    [SerializeField] private string idleParameterName = "isIdle";

    [Header("컴포넌트")]
    [Tooltip("Scanner 컴포넌트 (자동으로 찾습니다)")]
    public Scanner scanner;

    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private bool hasReachedFinalDestination = false;

    void Start()
    {
        // 컴포넌트 자동 찾기
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (scanner == null)
        {
            scanner = GetComponent<Scanner>();
        }

        // 웨이포인트 설정
        if (autoFindCircles)
        {
            FindAndSortCircles();
        }
        else if (manualWaypoints != null && manualWaypoints.Length > 0)
        {
            waypoints = new List<Transform>(manualWaypoints);
            Debug.Log($"[UnitMovement] {name}: 수동 웨이포인트 {waypoints.Count}개 사용");
        }

        // 이동 시작 조건 확인
        if (waypoints.Count > 0)
        {
            isMoving = true;
            SetWalkingAnimation(true);
            Debug.Log($"[UnitMovement] {name}: 총 {waypoints.Count}개의 웨이포인트를 거쳐 이동 시작!");
        }
        else
        {
            Debug.LogWarning($"[UnitMovement] {name}: 웨이포인트를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        // 타겟이 감지되면 즉시 움직임을 멈춤
        if (scanner != null && scanner.nearestTarget != null)
        {
            if (isMoving)
            {
                isMoving = false;
                SetWalkingAnimation(false);
                Debug.Log($"[UnitMovement] {name}: 타겟({scanner.nearestTarget.name}) 감지! 이동을 중단합니다.");
            }
            return;
        }
        // 타겟이 사라졌고, 움직이고 있지 않다면 다시 웨이포인트 이동 시작
        else if (!isMoving && !hasReachedFinalDestination)
        {
            isMoving = true;
            SetWalkingAnimation(true);
            Debug.Log($"[UnitMovement] {name}: 타겟 없음. 웨이포인트 이동을 재개합니다.");
        }

        // 이동 중이 아니거나 최종 목적지에 도달했다면 return
        if (!isMoving || hasReachedFinalDestination)
        {
            return;
        }

        // 웨이포인트 이동
        if (waypoints.Count > 0)
        {
            MoveToCurrentWaypoint();
        }
        else
        {
            // 움직일 곳이 없으면 정지
            isMoving = false;
            SetWalkingAnimation(false);
        }
    }

    /// <summary>
    /// Scene에서 "Circle"로 시작하는 모든 오브젝트를 찾아 거리순으로 정렬
    /// </summary>
    void FindAndSortCircles()
    {
        List<GameObject> circleObjects = FindObjectsOfType<GameObject>()
            .Where(obj => obj.name.StartsWith("Circle"))
            .ToList();

        if (circleObjects.Count == 0)
        {
            Debug.LogError($"[UnitMovement] {name}: 'Circle'로 시작하는 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 현재 위치에서 가까운 순서대로 정렬
        waypoints = circleObjects
            .OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position))
            .Select(obj => obj.transform)
            .ToList();

        Debug.Log($"[UnitMovement] {name}: {waypoints.Count}개의 Circle 발견 및 정렬 완료.");
    }

    /// <summary>
    /// 현재 웨이포인트를 향해 이동
    /// </summary>
    void MoveToCurrentWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Count)
        {
            OnReachedFinalDestination();
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            currentWaypointIndex++;
            return;
        }

        // 목표까지의 거리 계산
        float distance = Vector3.Distance(transform.position, targetWaypoint.position);

        // 목표에 도착했는지 확인
        if (distance <= stoppingDistance)
        {
            OnReachedWaypoint(currentWaypointIndex);
            currentWaypointIndex++;
            return;
        }

        // 이동
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // // 회전 (2D)
        // if (direction != Vector3.zero)
        // {
        //     float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //     Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90);
        //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        // }
    }

    /// <summary>
    /// 웨이포인트에 도착했을 때 호출
    /// </summary>
    void OnReachedWaypoint(int waypointIndex)
    {
        Debug.Log($"[UnitMovement] {name}: 웨이포인트 {waypoints[waypointIndex].name}에 도착!");
    }

    /// <summary>
    /// 최종 목적지에 도착했을 때 호출
    /// </summary>
    void OnReachedFinalDestination()
    {
        if (hasReachedFinalDestination) return;

        hasReachedFinalDestination = true;
        isMoving = false;
        Debug.Log($"[UnitMovement] {name}: 🎯 최종 목적지 도착! 대기 상태로 전환합니다.");
        SetWalkingAnimation(false);
    }

    /// <summary>
    /// 걷기/대기 애니메이션 제어
    /// </summary>
    void SetWalkingAnimation(bool walking)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(walkParameterName))
        {
            animator.SetBool(walkParameterName, walking);
        }
        if (!string.IsNullOrEmpty(idleParameterName))
        {
            animator.SetBool(idleParameterName, !walking);
        }
    }

    /// <summary>
    /// 외부에서 웨이포인트를 설정하는 함수
    /// </summary>
    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = new List<Transform>(newWaypoints);
        currentWaypointIndex = 0;
        hasReachedFinalDestination = false;
        isMoving = waypoints.Count > 0;
        
        if (isMoving)
        {
            SetWalkingAnimation(true);
        }
    }

    /// <summary>
    /// 외부에서 단일 집결지(Rally Point)를 설정하는 함수
    /// </summary>
    public void SetRallyPoint(Transform newRallyPoint)
    {
        if (newRallyPoint == null) return;

        autoFindCircles = false; 
        waypoints.Clear();
        waypoints.Add(newRallyPoint);
        
        currentWaypointIndex = 0;
        hasReachedFinalDestination = false;
        isMoving = true;
        SetWalkingAnimation(true);
        
        Debug.Log($"[UnitMovement] {name}: 새로운 집결지 '{newRallyPoint.name}' 설정 완료.");
    }

    /// <summary>
    /// 이동 일시정지/재개
    /// </summary>
    public void SetMoving(bool moving)
    {
        isMoving = moving;
        SetWalkingAnimation(moving);
    }

    // Scene 뷰에서 경로 시각화
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        if (!hasReachedFinalDestination && currentWaypointIndex < waypoints.Count)
        {
            Transform currentTarget = waypoints[currentWaypointIndex];
            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
    }
}
