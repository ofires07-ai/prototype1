using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [리팩토링됨] FlagManager의 깃발 이동 방송(Event)을 구독합니다.
/// </summary>
public class HY_UnitMovement : MonoBehaviour
{
    // ... (모든 변수 선언은 동일) ...
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("웨이포인트 설정")]
    [SerializeField] private bool autoFindCircles = true;
    [SerializeField] private Transform[] manualWaypoints;

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkParameterName = "isWalking";
    [SerializeField] private string idleParameterName = "isIdle";
    

    [Header("컴포넌트")]
    public HY_Scanner scanner;

    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private bool hasReachedFinalDestination = false;
    private Transform rallyPointTarget;


    void Start()
    {
        // ... (컴포넌트 자동 찾기 로직 동일) ...
        if (animator == null) animator = GetComponent<Animator>();
        if (scanner == null) scanner = GetComponent<HY_Scanner>();

        // ... (웨이포인트 설정 로직 동일) ...
        if (autoFindCircles)
        {
            FindAndSortCircles();
        }
        else if (manualWaypoints != null && manualWaypoints.Length > 0)
        {
            waypoints = new List<Transform>(manualWaypoints);
        }

        // ... (이동 시작 조건 확인 로직 동일) ...
        if (waypoints.Count > 0)
        {
            isMoving = true;
            SetWalkingAnimation(true);
        }
        else
        {
            Debug.LogWarning($"[UnitMovement] {name}: 웨이포인트를 찾을 수 없습니다! (깃발을 기다립니다)");
        }
        
        // --- [✨ 핵심 추가] ---
        // FlagManager의 "방송"을 구독(Subscribe)합니다.
        // 깃발이 움직일 때마다 HandleRallyPointMoved 함수가 호출됩니다.
        FlagManager.OnRallyPointUpdated += HandleRallyPointMoved;
    }

    /// <summary>
    /// [✨ 핵심 추가]
    /// 유닛이 파괴될 때(OnDestroy) 호출됩니다.
    /// 방송 구독을 취소(Unsubscribe)하여 메모리 누수를 방지합니다.
    /// </summary>
    void OnDestroy()
    {
        FlagManager.OnRallyPointUpdated -= HandleRallyPointMoved;
    }

    /// <summary>
    /// [✨ 핵심 추가]
    /// FlagManager의 방송(OnRallyPointUpdated)을 받으면 실행되는 함수입니다.
    /// </summary>
    private void HandleRallyPointMoved(Transform newFlag)
    {
        // 깃발이 새로(또는 다시) 설정되었으므로
        // SetRallyPoint를 호출하여 "수면 상태"에서 깨어납니다.
        SetRallyPoint(newFlag);
    }

    /// <summary>
    /// [수정됨] Update 로직 (이전 리팩토링과 동일)
    /// </summary>
    void Update()
    {
        // 1. 멈춤 조건 1: 적 감지
        if (scanner != null && scanner.nearestTarget != null)
        {
            if (isMoving)
            {
                isMoving = false;
                SetWalkingAnimation(false);
            }
            return; 
        }

        // 2. 멈춤 조건 2: 이미 최종 목적지 도착
        if (hasReachedFinalDestination)
        {
            if (isMoving) 
            {
                isMoving = false; 
                SetWalkingAnimation(false); 
            }
            return; 
        }

        // 3. (재)시작 조건: 적도 없고, 도착도 안했는데, 멈춰있다면
        if (!isMoving)
        {
            isMoving = true;
            SetWalkingAnimation(true);
        }

        // 4. 이동
        MoveToCurrentWaypoint();
    }

    // ... (FindAndSortCircles, MoveToCurrentWaypoint, OnReachedWaypoint, OnReachedFinalDestination, SetWalkingAnimation, SetWaypoints 함수는 모두 동일) ...
    
    // (아래는 동일한 함수들입니다)
    void FindAndSortCircles()
    {
        List<GameObject> circleObjects = FindObjectsOfType<GameObject>().Where(obj => obj.name.StartsWith("Circle")).ToList();
        if (circleObjects.Count == 0)
        {
            Debug.LogWarning($"[UnitMovement] {name}: 'Circle'로 시작하는 오브젝트를 찾을 수 없습니다!");
            return;
        }
        waypoints = circleObjects.OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position)).Select(obj => obj.transform).ToList();
        Debug.Log($"[UnitMovement] {name}: {waypoints.Count}개의 Circle 발견 및 정렬 완료.");
    }

    void MoveToCurrentWaypoint()
    {
        Transform targetWaypoint = null;
        if (currentWaypointIndex < waypoints.Count)
        {
            targetWaypoint = waypoints[currentWaypointIndex];
        }
        else if (rallyPointTarget != null)
        {
            targetWaypoint = rallyPointTarget;
        }
        else
        {
            OnReachedFinalDestination();
            return;
        }

        if (targetWaypoint == null)
        {
            if (currentWaypointIndex < waypoints.Count)
                currentWaypointIndex++;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distance <= stoppingDistance)
        {
            if (currentWaypointIndex < waypoints.Count)
            {
                OnReachedWaypoint(currentWaypointIndex);
                currentWaypointIndex++;
            }
            else
            {
                OnReachedFinalDestination();
            }
            return;
        }
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    void OnReachedWaypoint(int waypointIndex)
    {
        Debug.Log($"[UnitMovement] {name}: 웨이포인트 {waypoints[waypointIndex].name}에 도착!");
    }

    void OnReachedFinalDestination()
    {
        if (hasReachedFinalDestination) return;
        hasReachedFinalDestination = true;
        isMoving = false;
        Debug.Log($"[UnitMovement] {name}: 🎯 최종 목적지 도착! 대기 상태로 전환합니다.");
        SetWalkingAnimation(false);
    }

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
    /// [수정됨] 이 함수는 이제 방송(Event)에 의해서도 호출됩니다.
    /// </summary>
    public void SetRallyPoint(Transform newRallyPoint)
    {
        if (newRallyPoint == null) return;
        rallyPointTarget = newRallyPoint;

        // 💡 [핵심] "수면 상태"에서 깨어나도록 리셋합니다.
        hasReachedFinalDestination = false;
        isMoving = true;
        SetWalkingAnimation(true);
        Debug.Log($"[UnitMovement] {name}: 새로운 집결지({newRallyPoint.name}) 설정. 이동을 시작합니다.");
    }
    
    public void SetMoving(bool moving)
    {
        isMoving = moving;
        SetWalkingAnimation(moving);
    }
    
    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
