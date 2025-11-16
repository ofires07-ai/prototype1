using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// [통합 AI 스크립트 - 원거리 유닛 리팩토링]
/// 1. 웨이포인트(순찰)와 스캐너(추적) 로직은 유지.
/// 2. 전투(HandleCombat) 로직이 원거리(rangedAttackRange) 기준으로 변경됨.
/// 3. 공격 이벤트(Event_PerformAttack)가 근접 히트박스 대신 [GrowingOrb] 프리펩을 발사함.
/// 4. 피격 시 스턴(경직) 로직 포함.
/// </summary>
public class HY_Ranged_EnemyUnitMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("기본 순찰 속도")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("웨이포인트에 도달했다고 판단하는 거리")]
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("전투 설정")]
    [Tooltip("적을 발견했을 때의 추격 속도")]
    [SerializeField] private float chaseSpeed = 4f;
    
    // [✨ 수정] 근접 'attackRange' -> 원거리 'rangedAttackRange'
    [Tooltip("이 거리 안으로 들어오면 공격을 시작합니다 (원거리용 8~10 추천)")]
    [SerializeField] private float rangedAttackRange = 8f; 

    // [✨ 수정] 'enemyMeleeHitboxPrefab' -> 'orbPrefab'
    [Tooltip("적이 소환할 [성장하는 구체] 프리펩 (GrowingOrb.cs 포함)")]
    [SerializeField] private GameObject orbPrefab; 
    
    [Tooltip("구체가 발사될 위치 (없으면 유닛 위치)")]
    [SerializeField] private Transform firePoint;

    [Header("체력 설정")]
    [SerializeField] private int maxHp = 10;
    private int currentHp;
    private bool isLive = true;

    [Header("웨이포인트 설정")]
    [SerializeField] private bool autoFindCircles = true;
    [SerializeField] private Transform[] manualWaypoints;
    public string NodeName;

    [Header("컴포넌트 (자동 찾기)")]
    [SerializeField] private Animator animator;
    public HY_Scanner scanner; // AI의 '눈' 역할
    private SpriteRenderer spriteRenderer;

    // --- 내부 관리 변수 ---
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool hasReachedFinalDestination = false;
    private bool isStunned = false;
    private bool deathReported = false;
    public string enemyID;

    // [✨ 추가] 공격 이벤트를 위해 현재 타겟을 클래스 변수로 저장
    private Transform currentTarget; 

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

        // 2. 웨이포인트(순찰 경로) 설정 (로직 동일)
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
             Debug.LogWarning($"[AI] {name}: '{NodeName}'으로 시작하는 웨이포인트를 찾을 수 없습니다! (제자리 대기)");
        }
    }

    /// <summary>
    /// AI의 메인 두뇌 (매 프레임 실행)
    /// </summary>
    void Update()
    {
        // [✨ 수정] 죽었거나 '스턴' 상태면 아무것도 하지 않음 (중복 체크 정리)
        if (!isLive || isStunned)
        {
            animator.SetBool("isLive", isLive);
            return;
        }

        // [✨ 수정] 스캐너의 타겟을 클래스 변수 'currentTarget'에 저장
        currentTarget = scanner.nearestTarget;

        if (currentTarget != null)
        {
            // 2. 적이 있다! (전투 모드: 추격 또는 공격)
            HandleCombat(currentTarget);
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

        // [✨ 수정] 'attackRange' -> 'rangedAttackRange'
        if (distance > rangedAttackRange)
        {
            // --- 1. 추격 (Chase) --- (로직 동일)
            transform.position += direction * chaseSpeed * Time.deltaTime;
            animator.SetFloat("Speed", chaseSpeed);
            
            // [✨ 수정] 좌우 반전 활성화
            HandleSpriteFlip(direction.x);
        }
        else
        {
            // --- 2. 공격 (Attack) --- (로직 동일)
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Attack");
            
            // [✨ 수정] 좌우 반전 활성화
            HandleSpriteFlip(direction.x);
        }
    }

    /// <summary>
    /// (순찰) 웨이포인트를 따라 이동합니다. (로직 동일)
    /// </summary>
    void HandlePatrol()
    {
        if (hasReachedFinalDestination || waypoints.Count == 0)
        {
            animator.SetFloat("Speed", 0);
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            currentWaypointIndex++;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetWaypoint.position);

        if (distance <= stoppingDistance)
        {
            OnReachedWaypoint(currentWaypointIndex);
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Count)
            {
                OnReachedFinalDestination();
            }
            return;
        }

        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        animator.SetFloat("Speed", moveSpeed);
        animator.SetFloat("moveX", direction.x);
        animator.SetFloat("moveY", direction.y);
        
        // [✨ 수정] 좌우 반전 활성화
        HandleSpriteFlip(direction.x);
    }

    /// <summary>
    /// 이동 방향(x)에 따라 스프라이트를 좌우로 뒤집습니다. (로직 동일)
    /// </summary>
    void HandleSpriteFlip(float directionX)
    {
        if (spriteRenderer == null) return;

        if (directionX > 0.01f) // 오른쪽
        {
            spriteRenderer.flipX = false;
        }
        else if (directionX < -0.01f) // 왼쪽
        {
            spriteRenderer.flipX = true;
        }
    }

    // --- 체력 및 피격 로직 (로직 동일) ---

    public void TakeDamage(int damage)
    {
        if (!isLive) return;
        currentHp -= damage;
        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void ApplyStun(float duration)
    {
        if (!isLive) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        animator.SetFloat("Speed", 0);
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    public void Die()
    {
        if (!isLive) return;
        isLive = false;
        currentHp = 0;

        animator.SetTrigger("Die");
        if (deathReported) return;
        deathReported = true;

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.OnMonsterDied(enemyID);

        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        Destroy(gameObject, 2.0f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HY_Bullet bullet = collision.GetComponent<HY_Bullet>();
        if (bullet != null)
        {
            TakeDamage(bullet.damage);
            Destroy(bullet.gameObject);
        }
        PlayerMeleeHitbox melee = collision.GetComponent<PlayerMeleeHitbox>();
        if (melee != null)
        {
            TakeDamage(melee.damage);
        }
    }
    // --- 웨이포인트 관련 헬퍼 함수 (로직 동일) ---

    void OnReachedWaypoint(int waypointIndex)
    {
        // Debug.Log($"[AI] {name}: 웨이포인트 {waypoints[waypointIndex].name}에 도착!");
    }

    void OnReachedFinalDestination()
    {
        hasReachedFinalDestination = true;
        animator.SetFloat("Speed", 0);
        Debug.Log($"[AI] {name}: 🎯 최종 목적지 도착! 순찰을 중단합니다.");
    }

    void FindAndSortCircles()
    {
        // (Greedy 경로 구성 로직 전체 동일)
        List<Transform> circleList = FindObjectsOfType<Transform>()
            .Where(t => t != null && t.gameObject != null && t.gameObject.name.StartsWith(NodeName))
            .ToList();

        if (circleList.Count == 0)
        {
            waypoints = new List<Transform>();
            Debug.Log($"[AI] {name}: '{NodeName}' 웨이포인트가 없습니다.");
            return;
        }

        Vector3 currentPos = transform.position;
        var remaining = new List<Transform>(circleList);
        var ordered = new List<Transform>(circleList.Count);

        while (remaining.Count > 0)
        {
            Transform next = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < remaining.Count; i++)
            {
                Transform cand = remaining[i];
                float sqr = (cand.position - currentPos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    next = cand;
                }
            }
            ordered.Add(next);
            remaining.Remove(next);
            currentPos = next.position;
        }
        waypoints = ordered;
        Debug.Log($"[AI] {name}: Greedy 방식으로 {waypoints.Count}개의 'Circle' 웨이포인트 경로 구성 완료.");
    }

    void OnDrawGizmos()
    {
        // (Gizmos 로직 전체 동일)
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

    // --- [✨✨✨ 핵심 수정 ✨✨✨] ---
    /// <summary>
    /// (애니메이션 이벤트에서 호출됨)
    /// 'GrowingOrb' 프리펩을 발사하고 타겟을 설정합니다.
    /// </summary>
    public void Event_PerformAttack()
    {
        Debug.Log($"[AI] {name}이(가) Event_PerformAttack()를 호출 (원거리 공격)!");
        
        // 1. 소환할 [구체] 프리펩이 설정되어 있는지 확인
        if (orbPrefab == null)
        {
            Debug.LogError(name + ": orbPrefab이 설정되지 않았습니다!");
            return;
        }

        // 2. 공격 애니메이션이 시작된 후 타겟이 사라졌는지 확인
        if (currentTarget == null) 
        {
            Debug.LogWarning(name + ": 구체를 발사하려 했으나 타겟이 사라졌습니다.");
            return;
        }

        // 3. 구체 발사 위치 결정 (FirePoint가 있으면 거기, 없으면 내 위치)
        Vector3 spawnPosition = (firePoint != null) ? firePoint.position : transform.position;
        Quaternion spawnRotation = (firePoint != null) ? firePoint.rotation : transform.rotation;

        // 4. 구체를 '소환(Instantiate)'
        GameObject orbGO = Instantiate(orbPrefab, spawnPosition, spawnRotation);

        // 5. 구체에서 'GrowingOrb' 스크립트를 가져옵니다.
        //    (스크립트 이름이 GrowingOrb.cs라고 가정)
        GrowingOrb orbScript = orbGO.GetComponent<GrowingOrb>();

        // 6. [가장 중요!] 구체 스크립트가 존재한다면
        if (orbScript != null)
        {
            // 7. "구체야! 너의 타겟은 'currentTarget'이야!" 라고 알려줍니다.
            orbScript.SetTarget(currentTarget);
            Debug.Log($"[AI] {name}이(가) {currentTarget.name}을(를) 향해 구체를 발사합니다!");
        }
        else
        {
            Debug.LogError($"[AI] {name}: 발사한 구체 프리펩에 GrowingOrb.cs 스크립트가 없습니다!");
        }
    }
}