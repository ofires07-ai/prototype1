using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [통합 AI 스크립트 - 원거리 유닛 리팩토링]
/// [🔧 수정] Event_PerformAttack에서 구체 타입을 자동으로 감지하도록 개선
/// </summary>
public class HY_Ranged_EnemyUnitMovement : MonoBehaviour, ISlowable
{
    [Header("이동 설정")]
    [Tooltip("기본 순찰 속도")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("웨이포인트에 도달했다고 판단하는 거리")]
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("상태 이상 설정")]
    [Tooltip("현재 속도 배율 (1.0 = 정상, 0.5 = 절반 속도)")]
    [SerializeField] public float speedMultiplier = 1.0f;

    [Header("전투 설정")]
    [Tooltip("적을 발견했을 때의 추격 속도")]
    [SerializeField] private float chaseSpeed = 4f;
    
    [Tooltip("이 거리 안으로 들어오면 공격을 시작합니다 (원거리용 8~10 추천)")]
    [SerializeField] private float rangedAttackRange = 8f; 

    [Tooltip("적이 소환할 구체 프리펩 (GrowingOrb 또는 HY_Orb 스크립트 포함)")]
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

    [SerializeField] private HY_SmoothHealthBar healthBar;
    public HY_Scanner scanner;
    private SpriteRenderer spriteRenderer;

    private float _originalSpeedMultiplier;
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool hasReachedFinalDestination = false;
    private bool isStunned = false;
    private bool deathReported = false;
    public string enemyID;

    private float attackCooldown = 2.0f;
    private float lastAttackTime = 0f;
    private int slowDebuffCount = 0; // [추가] 중첩 카운트
    private Transform currentTarget;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (scanner == null) scanner = GetComponent<HY_Scanner>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentHp = maxHp;
        
       if (healthBar != null)
        {
            healthBar.SetHealth(currentHp, maxHp);
        }

        isLive = true;
        _originalSpeedMultiplier = speedMultiplier; // 원래 속도 배율 저장

        if (spriteRenderer == null)
        {
            Debug.LogError($"[AI] {name}: SpriteRenderer가 없습니다!");
        }

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
             Debug.LogWarning($"[AI] {name}: '{NodeName}'으로 시작하는 웨이포인트를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        if (!isLive || isStunned)
        {
            animator.SetBool("isLive", isLive);
            return;
        }

        currentTarget = scanner.nearestTarget;

        if (currentTarget != null)
        {
            HandleCombat(currentTarget);
        }
        else
        {
            HandlePatrol();
        }
    }

    void HandleCombat(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        Vector3 direction = (target.position - transform.position).normalized;

        if (distance > rangedAttackRange)
        {
            transform.position += direction * chaseSpeed * Time.deltaTime;
            animator.SetFloat("Speed", chaseSpeed);
        }
        else
        {
            animator.SetFloat("Speed", 0);
            
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                animator.SetTrigger("Attack");
                Debug.Log($"[AI] {name}: 공격 트리거 실행! (타겟: {target.name})");
            }
        }
        animator.SetFloat("moveX", direction.x);
        animator.SetFloat("moveY", direction.y);
    }

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
        float currentMoveSpeed = moveSpeed * speedMultiplier;
        transform.position += direction * currentMoveSpeed * Time.deltaTime;

        animator.SetFloat("Speed", currentMoveSpeed);
        animator.SetFloat("moveX", direction.x);
        animator.SetFloat("moveY", direction.y);
    }

    void HandleSpriteFlip(float directionX)
    {
        if (spriteRenderer == null) return;

        if (directionX > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (directionX < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isLive) return;
        currentHp -= damage;
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHp, maxHp);
        }
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

    void OnReachedWaypoint(int waypointIndex)
    {
        // Debug.Log($"[AI] {name}: 웨이포인트 도착!");
    }

    void OnReachedFinalDestination()
    {
        hasReachedFinalDestination = true;
        animator.SetFloat("Speed", 0);
        Debug.Log($"[AI] {name}: 🎯 최종 목적지 도착!");
    }

    void FindAndSortCircles()
    {
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
        Debug.Log($"[AI] {name}: {waypoints.Count}개의 웨이포인트 경로 구성 완료.");
    }

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
            Transform currentWaypoint = waypoints[currentWaypointIndex];
            if (currentWaypoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentWaypoint.position);
            }
        }
        
        // 🔧 공격 범위 시각화 추가
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
    }

    /// <summary>
    /// [🔧 핵심 수정!] 애니메이션 이벤트에서 호출
    /// GrowingOrb 또는 HY_Orb를 자동으로 감지하여 발사
    /// </summary>
    public void Event_PerformAttack()
    {
        Debug.Log($"[AI 🎯] {name}: Event_PerformAttack() 호출됨!");
        
        // 1. 프리팹 확인
        if (orbPrefab == null)
        {
            Debug.LogError($"[AI ❌] {name}: orbPrefab이 Inspector에 할당되지 않았습니다!");
            return;
        }

        // 2. 타겟 확인
        if (currentTarget == null) 
        {
            Debug.LogWarning($"[AI ⚠️] {name}: 타겟이 사라졌습니다.");
            return;
        }

        // 3. 발사 위치 결정
        Vector3 spawnPosition = (firePoint != null) ? firePoint.position : transform.position;
        Quaternion spawnRotation = (firePoint != null) ? firePoint.rotation : transform.rotation;

        Debug.Log($"[AI 📍] {name}: 구체 생성 위치: {spawnPosition}");

        // 4. 구체 생성
        GameObject orbGO = Instantiate(orbPrefab, spawnPosition, spawnRotation);
        
        if (orbGO == null)
        {
            Debug.LogError($"[AI ❌] {name}: 구체 생성 실패!");
            return;
        }

        Debug.Log($"[AI ✅] {name}: 구체 생성 성공! (오브젝트: {orbGO.name})");

        // 5. [🔧 핵심 수정!] 구체 타입을 자동으로 감지
        bool targetSet = false;

        // 5-1. GrowingOrb 시도
        GrowingOrb growingOrb = orbGO.GetComponent<GrowingOrb>();
        if (growingOrb != null)
        {
            growingOrb.SetTarget(currentTarget);
            targetSet = true;
            Debug.Log($"[AI ✅] {name}: GrowingOrb 타입 구체 발사! (타겟: {currentTarget.name})");
        }

        // 5-2. HY_Orb 시도
        if (!targetSet)
        {
            HY_Orb hyOrb = orbGO.GetComponent<HY_Orb>();
            if (hyOrb != null)
            {
                hyOrb.SetTarget(currentTarget);
                targetSet = true;
                Debug.Log($"[AI ✅] {name}: HY_Orb 타입 구체 발사! (타겟: {currentTarget.name})");
            }
        }

        // 5-3. 둘 다 없으면 에러
        if (!targetSet)
        {
            Debug.LogError($"[AI ❌] {name}: 구체 프리팹에 'GrowingOrb' 또는 'HY_Orb' 스크립트가 없습니다!");
            Destroy(orbGO);
            return;
        }

        Debug.Log($"[AI 🚀] {name}: 구체가 {currentTarget.name}을 향해 발사되었습니다!");
    }
    public void ApplySlow(float ratio)
    {
        slowDebuffCount++; 

        if (slowDebuffCount == 1 || speedMultiplier > ratio)
        {
            speedMultiplier = ratio;
        }
    }

    public void RemoveSlow()
    {
        slowDebuffCount--;

        if (slowDebuffCount <= 0)
        {
            slowDebuffCount = 0;
            speedMultiplier = _originalSpeedMultiplier;
        }
    }
}
