using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [통합 AI 스크립트]
/// 웨이포인트(순찰)와 스캐너(추적/공격)를 모두 처리하며,
/// 체력, 죽음, 애니메이션(Speed, Attack, Die) 제어를 담당합니다.
/// 
/// [필수 Animator Parameters]
/// - Speed (Float)
/// - Attack (Trigger)
/// - Die (Trigger)
/// 
/// [필수 컴포넌트]
/// - Animator, HY_Scanner, SpriteRenderer, Collider2D
/// </summary>
public class HY_EnemyUnitMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [Header("이동 설정")]
    [Tooltip("기본 순찰 속도")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("웨이포인트에 도달했다고 판단하는 거리")]
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("전투 설정")]
    [Tooltip("적을 발견했을 때의 추격 속도")]
    [SerializeField] private float chaseSpeed = 4f;
    [Tooltip("이 거리 안으로 들어오면 공격을 시작합니다")]
    [SerializeField] private float attackRange = 1.0f; 
    [Tooltip("공격 데미지 (필요한 경우)")]
    [SerializeField] private int attackDamage = 1; // (예시)

    [Header("체력 설정")]
    [SerializeField] private int maxHp = 10;
    private int currentHp;
    private bool isLive = true;

    [Header("웨이포인트 설정")]
    [Tooltip("Scene에서 'spaceship'으로 시작하는 오브젝트를 자동으로 찾아 순서대로 정렬")]
    [SerializeField] private bool autoFindCircles = true;
    [Tooltip("수동 웨이포인트 (autoFindCircles가 false일 때)")]
    [SerializeField] private Transform[] manualWaypoints;

    [Header("컴포넌트 (자동 찾기)")]
    [SerializeField] private Animator animator;
    public HY_Scanner scanner; // AI의 '눈' 역할
    private SpriteRenderer spriteRenderer; // 좌우 반전용

    // --- 내부 관리 변수 ---
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool hasReachedFinalDestination = false;

    // (SpawnManager에 사망 보고가 필요하다면 HY_Enemy처럼 enemyID 변수 추가)
    bool deathReported = false; // 사망 보고 중복 방지
    public string enemyID; 


    void Start()
    {
        // 1. 컴포넌트 자동 찾기 및 초기화
        if (animator == null) animator = GetComponent<Animator>();
        if (scanner == null) scanner = GetComponent<HY_Scanner>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        
        currentHp = maxHp; // 체력 초기화
        isLive = true;

        if (spriteRenderer == null)
        {
            Debug.LogError($"[AI] {name}: SpriteRenderer가 없습니다! 좌우 반전(flipX)을 할 수 없습니다.");
        }

        // 2. 웨이포인트(순찰 경로) 설정
        if (autoFindCircles)
        {
            FindAndSortCircles();
        }
        else if (manualWaypoints != null && manualWaypoints.Length > 0)
        {
            waypoints = new List<Transform>(manualWaypoints);
            Debug.Log($"[AI] {name}: 수동 웨이포인트 {waypoints.Count}개 사용");
        }

        if (waypoints.Count == 0 && autoFindCircles)
        {
             Debug.LogWarning($"[AI] {name}: 'spaceship'으로 시작하는 웨이포인트를 찾을 수 없습니다! (제자리 대기)");
        }
    }

    /// <summary>
    /// AI의 메인 두뇌 (매 프레임 실행)
    /// </summary>
    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (!isLive)
        {
            animator.SetBool("isLive", false);
            return;
        }   

        // 1. "눈" (스캐너)으로 적을 찾음
        Transform target = scanner.nearestTarget;

        if (target != null)
        {
            // 2. 적이 있다! (전투 모드: 추격 또는 공격)
            HandleCombat(target);
        }
        else
        {
            // 3. 적이 없다! (순찰 모드)
            HandlePatrol();
        }
    }

    /// <summary>
    /// (전투) 타겟을 추격하거나 공격합니다.
    /// </summary>
    void HandleCombat(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        Vector3 direction = (target.position - transform.position).normalized;

        if (distance > attackRange)
        {
            // --- 1. 추격 (Chase) ---
            // "적이 죽으면... 다시 움직이고" (새로운 적을 향해)
            transform.position += direction * chaseSpeed * Time.deltaTime;
            
            // 애니메이션: 'Walk' 상태 재생 (Speed > 0.1)
            animator.SetFloat("Speed", chaseSpeed);
            
            // 방향: 좌우 반전 (상하 이동 시 마지막 좌우 방향 유지)
            HandleSpriteFlip(direction.x);
        }
        else
        {
            // --- 2. 공격 (Attack) ---
            // "가까워 지면 공격을 하고"
            // (1) 멈춤 (Animator FSM이 'Idle' 상태로 가게 함)
            animator.SetFloat("Speed", 0); 
            
            // (2) 공격 트리거 발동 (FSM이 'Idle' -> 'Attack'으로 즉시 전환)
            animator.SetTrigger("Attack"); 
            
            // (3) 방향: 적을 바라보도록 좌우 반전
            HandleSpriteFlip(direction.x);
        }
    }

    /// <summary>
    /// (순찰) 웨이포인트를 따라 이동합니다.
    /// </summary>
    void HandlePatrol()
    {
        // "적이 죽으면... 없으면 다시 움직이고"
        if (hasReachedFinalDestination || waypoints.Count == 0)
        {
            // 멈춤 (모든 순찰 완료 또는 순찰 경로 없음)
            animator.SetFloat("Speed", 0); // 'Idle' 상태로
            return;
        }

        // --- 1. 목표 웨이포인트 설정 ---
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) 
        {
            // (혹시 웨이포인트가 파괴된 경우)
            currentWaypointIndex++;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetWaypoint.position);

        // --- 2. 웨이포인트 도착 ---
        if (distance <= stoppingDistance)
        {
            OnReachedWaypoint(currentWaypointIndex);
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Count)
            {
                // 최종 목적지 도착
                OnReachedFinalDestination();
            }
            return;
        }

        // --- 3. 이동 ---
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 애니메이션: 'Walk' 상태 재생
        animator.SetFloat("Speed", moveSpeed);
        
        // 방향: 좌우 반전
        HandleSpriteFlip(direction.x);
    }

    /// <summary>
    /// 이동 방향(x)에 따라 스프라이트를 좌우로 뒤집습니다.
    /// </summary>
    void HandleSpriteFlip(float directionX)
    {
        if (spriteRenderer == null) return;

        // directionX가 0이 아닐 때만 방향을 바꿉니다.
        // (0이면, 즉 수직 이동 시에는 마지막 방향을 유지합니다)
        if (directionX > 0.01f) // 오른쪽
        {
            spriteRenderer.flipX = false;
        }
        else if (directionX < -0.01f) // 왼쪽
        {
            spriteRenderer.flipX = true;
        }
    }

    // --- 체력 및 피격 로직 (HY_Enemy에서 가져옴) ---

    /// <summary>
    /// 외부(총알 등)에서 호출하여 데미지를 입힙니다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!isLive) return; // 이미 죽었으면 무시

        currentHp -= damage;
        // (선택) 여기서 피격 애니메이션 트리거
        // animator.SetTrigger("Hit"); 

        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    public void Die()
    {
        if (!isLive) return; // 중복 사망 방지
        isLive = false;
        currentHp = 0;

        // 1. 죽음 애니메이션 재생
        animator.SetTrigger("Die");
        if (deathReported) return;       // 중복 보고 방지
        deathReported = true;

        // 스폰 매니저에 사망 보고
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.OnMonsterDied(enemyID);

        // 2. 물리/충돌 중지
        GetComponent<Collider2D>().enabled = false;
        // Rigidbody2D가 있다면 비활성화
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // 3. 오브젝트 파괴 (Die 애니메이션 재생 시간 기다리기)
        // (Tip: 애니메이션 클립의 실제 길이만큼 설정하는 것이 좋음)
        Destroy(gameObject, 2.0f); 
    }

    /// <summary>
    /// 총알 등과의 충돌 감지
    /// </summary>
    void OnTriggerEnter2D(Collider2D collision)
    {
        // (총알 스크립트 이름이 HY_Bullet이라고 가정)
        HY_Bullet bullet = collision.GetComponent<HY_Bullet>();
        if (bullet != null)
        {
            TakeDamage(bullet.damage);
            
            // 총알이 관통형이 아니라면 즉시 파괴
            Destroy(bullet.gameObject);
        }
    }


    // --- 웨이포인트 관련 헬퍼 함수 ---

    void OnReachedWaypoint(int waypointIndex)
    {
        // Debug.Log($"[AI] {name}: 웨이포인트 {waypoints[waypointIndex].name}에 도착!");
        // (필요시 이곳에서 사운드 재생 등)
    }

    void OnReachedFinalDestination()
    {
        hasReachedFinalDestination = true;
        animator.SetFloat("Speed", 0); // 멈춤
        Debug.Log($"[AI] {name}: 🎯 최종 목적지 도착! 순찰을 중단합니다.");
    }

    /// <summary>
    /// Scene에서 "Circle"으로 시작하는 모든 오브젝트를 찾아 거리순으로 정렬
    /// </summary>
    void FindAndSortCircles()
    {
        // 1) 후보 수집: 이름이 "Circle"로 시작하는 오브젝트들
    //    (태그를 쓰신다면 FindGameObjectsWithTag("Circle")로 대체 가능)
    List<Transform> circleList = FindObjectsOfType<Transform>()
        .Where(t => t != null && t.gameObject != null && t.gameObject.name.StartsWith("Circle"))
        .ToList();

    if (circleList.Count == 0)
    {
        waypoints = new List<Transform>();
        Debug.Log($"[AI] {name}: 'Circle' 웨이포인트가 없습니다.");
        return;
    }

    // 2) Greedy 경로 구성: 현재 위치에서 가장 가까운 것을 하나씩 고름
    Vector3 currentPos = transform.position; // 시작점: 적군 스폰 위치(현재 유닛 위치)
    var remaining = new List<Transform>(circleList);
    var ordered = new List<Transform>(circleList.Count);

    while (remaining.Count > 0)
    {
        Transform next = null;
        float bestSqr = float.PositiveInfinity;

        // 남은 후보들 중 현재 위치와의 거리가 가장 짧은 것 선택
        for (int i = 0; i < remaining.Count; i++)
        {
            Transform cand = remaining[i];
            // sqrMagnitude로 루트 연산을 피해서 성능 최적화
            float sqr = (cand.position - currentPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                next = cand;
            }
        }

        // 선택된 후보를 경로에 추가하고, 현재 위치를 갱신
            ordered.Add(next);
            remaining.Remove(next);
            currentPos = next.position;
        }

        waypoints = ordered;

        Debug.Log($"[AI] {name}: Greedy 방식으로 {waypoints.Count}개의 'Circle' 웨이포인트 경로 구성 완료.");
    }

    // Scene 뷰에서 경로 시각화 (디버깅용)
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

    // --- (기존 스크립트의 SetRallyPoint 등은 제거됨. 필요하면 추가) ---
}