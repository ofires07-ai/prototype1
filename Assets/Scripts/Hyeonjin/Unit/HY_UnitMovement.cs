// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;

// /// <summary>
// /// [리팩토링됨] FlagManager의 깃발 이동 방송(Event)을 구독합니다.
// /// </summary>
// public class HY_UnitMovement : MonoBehaviour
// {
//     // ... (모든 변수 선언은 동일) ...
//     [Header("이동 설정")]
//     [SerializeField] private float moveSpeed = 3f;
//     [SerializeField] private float stoppingDistance = 0.5f;
//     [SerializeField] private float rotationSpeed = 5f;

//     [Header("웨이포인트 설정")]
//     [SerializeField] private bool autoFindCircles = true;
//     [SerializeField] private Transform[] manualWaypoints;

//     [Header("애니메이션 설정")]
//     [SerializeField] private Animator animator;
//     [SerializeField] private string walkParameterName = "isWalking";
//     [SerializeField] private string idleParameterName = "isIdle";


//     [Header("컴포넌트")]
//     public HY_Scanner scanner;

//     private List<Transform> waypoints = new List<Transform>();
//     private int currentWaypointIndex = 0;
//     private bool isMoving = false;
//     private bool hasReachedFinalDestination = false;
//     private Transform rallyPointTarget;


//     private HY_Player playerUnit;
//     private ProductionTower ownerTower;
//     void Start()
//     {
//         // ... (컴포넌트 자동 찾기 로직 동일) ...
//         if (animator == null) animator = GetComponent<Animator>();
//         if (scanner == null) scanner = GetComponent<HY_Scanner>();
//         if (playerUnit == null) playerUnit = GetComponent<HY_Player>();

//         // ... (웨이포인트 설정 로직 동일) ...
//         if (autoFindCircles)
//         {
//             FindAndSortCircles();
//         }
//         else if (manualWaypoints != null && manualWaypoints.Length > 0)
//         {
//             waypoints = new List<Transform>(manualWaypoints);
//         }

//         // ... (이동 시작 조건 확인 로직 동일) ...
//         if (waypoints.Count > 0)
//         {
//             isMoving = true;
//             SetWalkingAnimation(true);
//         }
//         else
//         {
//             Debug.LogWarning($"[UnitMovement] {name}: 웨이포인트를 찾을 수 없습니다! (깃발을 기다립니다)");
//         }

//         // --- [✨ 핵심 추가] ---
//         // FlagManager의 "방송"을 구독(Subscribe)합니다.
//         // 깃발이 움직일 때마다 HandleRallyPointMoved 함수가 호출됩니다.
//         FlagManager.OnRallyPointUpdated += HandleRallyPointMoved;
//     }

//     // [✨ 핵심 추가 2] 주인을 등록하는 함수
//     public void SetOwnerTower(ProductionTower tower)
//     {
//         ownerTower = tower;
//     }


//     // [✨ 핵심 수정] 깃발 방송을 받아도 '내 주인'의 깃발인지 확인합니다.
//     private void HandleRallyPointMoved(Transform newFlag)
//     {
//         // 1. 주인이 등록되어 있고, 
//         // 2. 새로 들어온 깃발(newFlag)이 내 주인의 현재 깃발(ownerTower.RallyPoint)과 일치할 때만
//         if (ownerTower != null && newFlag == ownerTower.RallyPoint)
//         {
//             // 이 깃발은 '내'가 따라야 할 깃발이 맞습니다.
//             // "수면 상태"에서 깨어납니다.
//             SetRallyPoint(newFlag);
//         }
//         // else: 이 깃발은 '다른 타워'의 깃발 업데이트입니다. "무시"합니다.
//     }
//     /// <summary>
//     /// [✨ 핵심 추가]
//     /// 유닛이 파괴될 때(OnDestroy) 호출됩니다.
//     /// 방송 구독을 취소(Unsubscribe)하여 메모리 누수를 방지합니다.
//     /// </summary>
//     void OnDestroy()
//     {
//         FlagManager.OnRallyPointUpdated -= HandleRallyPointMoved;
//     }

//     // /// <summary>
//     // /// [✨ 핵심 추가]
//     // /// FlagManager의 방송(OnRallyPointUpdated)을 받으면 실행되는 함수입니다.
//     // /// </summary>
//     // private void HandleRallyPointMoved(Transform newFlag)
//     // {
//     //     // 깃발이 새로(또는 다시) 설정되었으므로
//     //     // SetRallyPoint를 호출하여 "수면 상태"에서 깨어납니다.
//     //     SetRallyPoint(newFlag);
//     // }

//     /// <summary>
//     /// [수정됨] Update 로직 (이전 리팩토링과 동일)
//     /// </summary>
//     void Update()
//     {
//         // 1. 멈춤 조건 1: 적 감지
//         if (scanner != null && scanner.nearestTarget != null)
//         {
//             if (isMoving)
//             {
//                 isMoving = false;
//                 SetWalkingAnimation(false);
//             }
//             return;
//         }


//         if (playerUnit.isLive == false)
//         {
//             if (isMoving)
//             {
//                 isMoving = false;
//                 SetWalkingAnimation(false);
//             }
//             return;
//         }

//         // 2. 멈춤 조건 2: 이미 최종 목적지 도착
//         if (hasReachedFinalDestination)
//         {
//             if (isMoving)
//             {
//                 isMoving = false;
//                 SetWalkingAnimation(false);
//             }
//             return;
//         }

//         // 3. (재)시작 조건: 적도 없고, 도착도 안했는데, 멈춰있다면
//         if (!isMoving)
//         {
//             isMoving = true;
//             SetWalkingAnimation(true);
//         }

//         // 4. 이동
//         MoveToCurrentWaypoint();
//     }

//     // ... (FindAndSortCircles, MoveToCurrentWaypoint, OnReachedWaypoint, OnReachedFinalDestination, SetWalkingAnimation, SetWaypoints 함수는 모두 동일) ...

//     // (아래는 동일한 함수들입니다)
//     void FindAndSortCircles()
//     {
//         List<GameObject> circleObjects = FindObjectsOfType<GameObject>().Where(obj => obj.name.StartsWith("Square")).ToList();
//         if (circleObjects.Count == 0)
//         {
//             Debug.LogWarning($"[UnitMovement] {name}: 'Circle'로 시작하는 오브젝트를 찾을 수 없습니다!");
//             return;
//         }
//         waypoints = circleObjects.OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position)).Select(obj => obj.transform).ToList();
//         Debug.Log($"[UnitMovement] {name}: {waypoints.Count}개의 Circle 발견 및 정렬 완료.");
//     }

//     void MoveToCurrentWaypoint()
//     {
//         Transform targetWaypoint = null;
//         if (currentWaypointIndex < waypoints.Count)
//         {
//             targetWaypoint = waypoints[currentWaypointIndex];
//         }
//         else if (rallyPointTarget != null)
//         {
//             targetWaypoint = rallyPointTarget;
//         }
//         else
//         {
//             OnReachedFinalDestination();
//             return;
//         }

//         if (targetWaypoint == null)
//         {
//             if (currentWaypointIndex < waypoints.Count)
//                 currentWaypointIndex++;
//             return;
//         }

//         float distance = Vector3.Distance(transform.position, targetWaypoint.position);
//         if (distance <= stoppingDistance)
//         {
//             if (currentWaypointIndex < waypoints.Count)
//             {
//                 OnReachedWaypoint(currentWaypointIndex);
//                 currentWaypointIndex++;
//             }
//             else
//             {
//                 OnReachedFinalDestination();
//             }
//             return;
//         }
//         Vector3 direction = (targetWaypoint.position - transform.position).normalized;
//         transform.position += direction * moveSpeed * Time.deltaTime;
//     }

//     void OnReachedWaypoint(int waypointIndex)
//     {
//         Debug.Log($"[UnitMovement] {name}: 웨이포인트 {waypoints[waypointIndex].name}에 도착!");
//     }

//     void OnReachedFinalDestination()
//     {
//         if (hasReachedFinalDestination) return;
//         hasReachedFinalDestination = true;
//         isMoving = false;
//         Debug.Log($"[UnitMovement] {name}: 🎯 최종 목적지 도착! 대기 상태로 전환합니다.");
//         SetWalkingAnimation(false);
//     }

//     void SetWalkingAnimation(bool walking)
//     {
//         if (animator == null) return;
//         if (!string.IsNullOrEmpty(walkParameterName))
//         {
//             animator.SetBool(walkParameterName, walking);
//         }
//         if (!string.IsNullOrEmpty(idleParameterName))
//         {
//             animator.SetBool(idleParameterName, !walking);
//         }
//     }

//     public void SetWaypoints(Transform[] newWaypoints)
//     {
//         waypoints = new List<Transform>(newWaypoints);
//         currentWaypointIndex = 0;
//         hasReachedFinalDestination = false;
//         isMoving = waypoints.Count > 0;
//         if (isMoving)
//         {
//             SetWalkingAnimation(true);
//         }
//     }

//     /// <summary>
//     /// [수정됨] 이 함수는 이제 방송(Event)에 의해서도 호출됩니다.
//     /// </summary>
//     public void SetRallyPoint(Transform newRallyPoint)
//     {
//         if (newRallyPoint == null) return;
//         rallyPointTarget = newRallyPoint;

//         // 💡 [핵심] "수면 상태"에서 깨어나도록 리셋합니다.
//         hasReachedFinalDestination = false;
//         isMoving = true;
//         SetWalkingAnimation(true);
//         Debug.Log($"[UnitMovement] {name}: 새로운 집결지({newRallyPoint.name}) 설정. 이동을 시작합니다.");
//     }

//     public void SetMoving(bool moving)
//     {
//         isMoving = moving;
//         SetWalkingAnimation(moving);
//     }

//     void OnDrawGizmos()
//     {
//         if (waypoints == null) return;
//         Gizmos.color = Color.yellow;
//         for (int i = 0; i < waypoints.Count - 1; i++)
//         {
//             if (waypoints[i] != null && waypoints[i + 1] != null)
//                 Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
//         }
//     }
// }


using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [리팩토링됨] FlagManager의 깃발 이동 방송(Event)을 구독합니다.
/// [수정됨] 스폰 시 '주변'의 웨이포인트만 탐색합니다.
/// [수정됨] 깃발 이동 시, 남은 웨이포인트를 '포기'하고 즉시 깃발로 향합니다.
/// </summary>
public class HY_UnitMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("웨이포인트 설정")]
    [SerializeField] private bool autoFindCircles = true;
    [SerializeField] private Transform[] manualWaypoints;

    // --- [✨ 핵심 추가 1] ---
    [Header("근접 웨이포인트 설정 (AutoFind)")]
    [Tooltip("autoFindCircles=true일 때, 유닛 주변에서 웨이포인트를 탐색할 반경")]
    [SerializeField] private float searchRadius = 15f;
    [Tooltip("웨이포인트로 사용할 'Square' 오브젝트들의 레이어 (반드시 설정!)")]
    [SerializeField] private LayerMask waypointLayer;
    // ---

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
    
    private HY_Player playerUnit;
    
    // [✨ 핵심 추가 2] 이 유닛을 스폰한 "주인 타워"
    private ProductionTower ownerTower;

    void Start()
    {
        // --- 컴포넌트 자동 찾기 ---
        if (animator == null) animator = GetComponent<Animator>();
        if (scanner == null) scanner = GetComponent<HY_Scanner>();
        if (playerUnit == null) playerUnit = GetComponent<HY_Player>();

        // --- [✨ 핵심 수정 3] ---
        // 스폰 시 '주변'의 웨이포인트를 '즉시' 탐색합니다.
        if (autoFindCircles)
        {
            // 씬 전체(FindObjectsOfType) 대신, '근처'만 탐색(OverlapSphere)
            FindAndSortNearbyCircles(); 
        }
        else if (manualWaypoints != null && manualWaypoints.Length > 0)
        {
            waypoints = new List<Transform>(manualWaypoints);
        }
        // ---

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
        
        // --- [✨ 핵심 추가 4] ---
        // FlagManager의 "방송"을 구독(Subscribe)합니다.
        // (이 로직은 이전과 동일하게 유지됩니다)
        FlagManager.OnRallyPointUpdated += HandleRallyPointMoved;
    }

    void OnDestroy()
    {
        // 방송 구독 취소 (메모리 누수 방지)
        FlagManager.OnRallyPointUpdated -= HandleRallyPointMoved;
    }

    /// <summary>
    /// [✨ 핵심 수정 5]
    /// 깃발 방송(OnRallyPointUpdated)을 받으면 실행되는 함수입니다.
    /// "내 주인"의 깃발이 맞는지 확인합니다.
    /// </summary>
    private void HandleRallyPointMoved(Transform newFlag)
    {
        // 1. 주인이 등록되어 있고, 
        // 2. 새로 들어온 깃발(newFlag)이 내 주인의 현재 깃발(ownerTower.RallyPoint)과 일치할 때만
        if (ownerTower != null && newFlag == ownerTower.RallyPoint)
        {
            // "수면 상태"에서 깨어나고, 깃발 이동으로 처리(true)
            SetRallyPoint(newFlag, true);
        }
        // else: 이 깃발은 '다른 타워'의 깃발 업데이트이므로 "무시"합니다.
    }

    void Update()
    {
        // (Update 로직은 이전과 동일 - 적 감지, 사망 시 정지, 목적지 도착 시 정지)

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
        
        if(playerUnit.isLive == false)
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
    // [HY_UnitMovement.cs]

    // --- [✨ 핵심 교체] ---
    /// <summary>
    /// 'FindAndSortCircles' 대신 'OverlapCircleAll' (2D)을 사용해 '근처'의 웨이포인트만 찾습니다.
    /// </summary>
    void FindAndSortNearbyCircles()
    {
        if (waypointLayer.value == 0) // LayerMask가 설정되지 않았다면
        {
            Debug.LogError($"[UnitMovement] {name}: 'Waypoint Layer'가 인스펙터에 설정되지 않았습니다!");
            return;
        }

        // [✨ 수정!] Physics(3D) -> Physics2D (2D)
        // OverlapSphere(3D) -> OverlapCircleAll(2D)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius, waypointLayer);
        
        if (colliders.Length == 0)
        {
            Debug.LogWarning($"[UnitMovement] {name}: {searchRadius}m 반경 내 'Waypoint' 레이어의 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 찾은 콜라이더들을 '거리 순'으로 정렬하여 'waypoints' 리스트에 저장
        waypoints = colliders
            .OrderBy(col => Vector2.Distance(transform.position, col.transform.position)) // [✨ 수정!] Vector3 -> Vector2
            .Select(col => col.transform)
            .ToList();
            
        Debug.Log($"[UnitMovement] {name}: {waypoints.Count}개의 근처 웨이포인트 발견 및 정렬 완료.");
    }
    void MoveToCurrentWaypoint()
    {
        // (이하 로직은 이전과 동일합니다)
        Transform targetWaypoint = null;
        if (currentWaypointIndex < waypoints.Count)
        {
            // 1순위: Square 웨이포인트
            targetWaypoint = waypoints[currentWaypointIndex];
        }
        else if (rallyPointTarget != null)
        {
            // 2순위: 최종 깃발(RallyPoint)
            targetWaypoint = rallyPointTarget;
        }
        else
        {
            // 모두 도착
            OnReachedFinalDestination();
            return;
        }
        
        // (널 체크 등 방어 코드)
        if (targetWaypoint == null)
        {
            if (currentWaypointIndex < waypoints.Count)
                currentWaypointIndex++;
            return;
        }

        // (이동 및 도착 처리)
        float distance = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distance <= stoppingDistance)
        {
            if (currentWaypointIndex < waypoints.Count)
            {
                OnReachedWaypoint(currentWaypointIndex);
                currentWaypointIndex++; // 다음 웨이포인트로
            }
            else
            {
                OnReachedFinalDestination(); // 최종 목적지(깃발) 도착
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
        // (이하 로직은 이전과 동일합니다)
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

    // (SetWaypoints 함수는 수동 설정을 위해 남겨둡니다)
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
    /// [✨ 핵심 수정 6]
    /// 깃발이 '이동'했는지(isFlagMove) 여부를 받습니다.
    /// </summary>
    public void SetRallyPoint(Transform newRallyPoint, bool isFlagMove = false)
    {
        if (newRallyPoint == null) return;
        rallyPointTarget = newRallyPoint;

        // "수면 상태" (이미 도착)에서 깨어나도록 리셋
        if (hasReachedFinalDestination)
        {
            hasReachedFinalDestination = false;
        }

        // [✨ 핵심] 깃발 '이동'으로 이 함수가 호출되었다면 (isFlagMove == true),
        // 사용자의 요청대로 남은 Square 경로를 '포기'하고
        // 인덱스를 끝으로 보내 바로 깃발로 향하게 합니다.
        if (isFlagMove)
        {
            currentWaypointIndex = waypoints.Count;
        }
        // 'isFlagMove'가 false라면 (스폰 시 ProductionTower가 호출)
        // currentWaypointIndex (기본값 0)를 건드리지 않으므로,
        // 유닛은 Start()에서 찾은 Square 경로를 정상적으로 따릅니다.

        isMoving = true;
        SetWalkingAnimation(true);
        Debug.Log($"[UnitMovement] {name}: 새로운 집결지({newRallyPoint.name}) 설정. 이동을 시작합니다.");
    }
    
    // [✨ 핵심 추가 7] (이전과 동일)
    /// <summary>
    /// ProductionTower가 스폰 시 "내가 네 주인이다"라고 알려주는 함수
    /// </summary>
    public void SetOwnerTower(ProductionTower tower)
    {
        ownerTower = tower;
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
        SetWalkingAnimation(moving);
    }
    
    // (기즈모 로직 동일)
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