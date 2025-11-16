using UnityEngine;
using System.Linq;
using System.Collections; // [✨ 리팩토링] 코루틴을 사용하기 위해 추가!

/// <summary>
/// [RPG 타워로 수정됨]
/// 1. [✨] 스폰 애니메이션 시간만큼 대기 후, 적을 감지하고 포탑을 회전시킵니다.
/// 2. 'HomingMissile' 프리펩을 발사하고, 'target'을 설정해줍니다.
/// </summary>
public class TurretController : MonoBehaviour
{
    [Header("--- 타워 회전 설정 ---")]
    public Transform turretHead;
    public float detectionRadius = 10f;
    public float rotationSpeed = 5f;
    public LayerMask enemyLayer;

    [Header("--- 타워 발사 설정 ---")]
    public GameObject bulletPrefab; 
    public Transform firePoint; 
    public float fireCooldown = 1.5f;

    // --- [✨ 리팩토링] 스폰 애니메이션 관련 ---
    [Header("--- 스폰 설정 ---")]
    [Tooltip("스폰 애니메이션이 끝나고 발사를 시작할 때까지의 대기 시간 (초)")]
    public float spawnAnimationTime = 4.0f; // 예: 스폰 애니메이션이 2초라고 가정
    private bool isReadyToFire = false; // 스폰이 끝나 'Idle' 상태가 되었는지 확인

    [Header("--- 애니메이션 설정 ---")]
    public Animator animator; 
    public string fireTriggerName = "shot";

    private Transform currentTarget;
    private float lastFireTime;

    // --- [✨ 리팩토링] 시작 시 코루틴 호출 ---
    void Start()
    {
        // 1. 발사 준비 플래그를 'false'로 초기화 (기본값)
        isReadyToFire = false;

        // 2. "SpawnDelayRoutine" 코루틴을 시작시킴
        StartCoroutine(SpawnDelayRoutine());
    }

    /// <summary>
    /// [✨ 리팩토링] 스폰 애니메이션 시간만큼 대기하는 코루틴
    /// </summary>
    IEnumerator SpawnDelayRoutine()
    {
        // 1. 'spawnAnimationTime' (예: 2초) 만큼 기다립니다.
        //    이 시간 동안 터렛은 아무것도 쏘지 않습니다.
        yield return new WaitForSeconds(spawnAnimationTime);

        // 2. 대기 시간이 끝나면, 터렛을 "Idle" 상태로 변경합니다.
        //    이제부터 발사할 준비가 되었다고 플래그를 'true'로 설정합니다.
        Debug.Log("터렛 스폰 완료. 발사 준비!");
        isReadyToFire = true;
    }

    // Update 함수는 그대로 두되, TryFire 내부에서 막힙니다.
    void Update()
    {
        // [✨] 발사 준비가 안 되었다면(스폰 중이라면) 타겟 찾기/회전을 아예 안 할 수도 있습니다.
        // if (!isReadyToFire) return;
        // (하지만 스폰 중에도 적을 조준하는 연출을 원한다면 아래 로직을 그대로 둬도 됩니다)

        FindTarget();
        if (currentTarget != null)
        {
            RotateTowardsTarget();
            TryFire(); // TryFire는 매번 호출되지만,
        }
    }
    
    // (FindTarget, RotateTowardsTarget 함수는 이전과 100% 동일)
    void FindTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);
        if (enemies.Length == 0) { currentTarget = null; return; }
        currentTarget = enemies.OrderBy(e => Vector2.Distance(transform.position, e.transform.position)).FirstOrDefault().transform;
    }

    void RotateTowardsTarget()
    {
        if (turretHead == null || currentTarget == null) return; 
        Vector3 dir = currentTarget.position - turretHead.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float targetAngle = angle + 90f;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    // --- [✨ 리팩토링] 발사 조건 수정 ---
    void TryFire()
    {
        // --- [✨✨✨ 핵심 추가!] ---
        // 1. 빌드 매니저가 존재하고(null이 아니고) && 빌드 매니저가 건설 모드(isBuilding) 중이라면
        if (TowerBuildManager.Instance != null && TowerBuildManager.Instance.GetIsBuilding())
        {
            // 2. 지금은 공격할 수 없음. 즉시 종료.
            return;
        }
        // --- [핵심 추가 끝] ---


        // [기존 로직] (스폰 대기 또는 쿨타임 중이라면)
        if (!isReadyToFire || (Time.time - lastFireTime <= fireCooldown))
        {
            return;
        }

        Fire();
        lastFireTime = Time.time;
    }

    // (Fire 함수, OnDrawGizmosSelected 함수는 이전과 100% 동일)
    void Fire()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab 또는 Fire Point가 연결되지 않았습니다!");
            return;
        }

        if (animator != null && !string.IsNullOrEmpty(fireTriggerName))
        {
            animator.SetTrigger(fireTriggerName);
        }

        GameObject missileGO = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        HomingMissile missileScript = missileGO.GetComponent<HomingMissile>();
        
        if (missileScript != null && currentTarget != null)
        {
            // HomingMissile 스크립트에 SetTarget이 public이어야 합니다.
            // (보내주신 HomingMissile.cs에 public void SetTarget이 있으므로 OK)
            missileScript.SetTarget(currentTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}