using UnityEngine;
using System.Linq; 

/// <summary>
/// [2D 최종본 + 애니메이션] 타워의 포탑(Turret) 회전 및 발사 로직
/// 1. 2D 반경 내의 'Collider2D' 적을 감지합니다.
/// 2. 가장 가까운 적을 향해 'turretHead'를 (Z축 기준으로) 부드럽게 회전시킵니다.
/// 3. 적을 조준 중일 때 'firePoint'에서 'bulletPrefab'을 발사합니다.
/// 4. [✨ 추가] 발사 시 'fireTriggerName' 애니메이션 트리거를 발동시킵니다.
/// </summary>
public class HY_TankController : MonoBehaviour
{
    [Header("--- 타워 회전 설정 ---")]
    [Tooltip("회전시킬 포탑(Turret_Head) 오브젝트를 연결해주세요.")]
    public Transform turretHead;
    [Tooltip("적이 감지되는 최대 반경입니다.")]
    public float detectionRadius = 10f;
    [Tooltip("포탑이 적을 향해 회전하는 속도입니다.")]
    public float rotationSpeed = 5f;
    [Tooltip("감지할 'Enemy' 레이어를 선택해주세요.")]
    public LayerMask enemyLayer;

    [Header("--- 타워 발사 설정 ---")]
    [Tooltip("발사할 총알 프리펩을 연결해주세요.")]
    public GameObject bulletPrefab;
    [Tooltip("총알이 발사될 위치 (TurretHead의 자식인 'FirePoint')")]
    public Transform firePoint; 
    [Tooltip("발사 쿨타임 (초)")]
    public float fireCooldown = 0.5f;
    [Tooltip("총알이 날아가는 힘")]
    public float bulletForce = 10f;

    // --- [✨ 애니메이션 변수 추가] ---
    [Header("--- 애니메이션 설정 ---")]
    [Tooltip("애니메이션 컨트롤러가 있는 Animator 컴포넌트")]
    public Animator animator; 
    [Tooltip("발사 시 사용할 트리거 파라미터의 이름 (Animator에 설정한 'shot')")]
    public string fireTriggerName = "shot";
    // ---

    // 현재 추적하고 있는 타겟(적)
    private Transform currentTarget;
    // 마지막 발사 시간 (쿨타임 계산용)
    private float lastFireTime;

    void Update()
    {
        FindTarget();

        if (currentTarget != null)
        {
            RotateTowardsTarget();
            TryFire();
        }
    }

    /// <summary>
    /// [2D 감지] 'detectionRadius' 반경 내에서 가장 가까운 'Collider2D'를 찾습니다.
    /// </summary>
    void FindTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);

        if (enemies.Length == 0)
        {
            currentTarget = null;
            return;
        }

        currentTarget = enemies
            .OrderBy(enemy => Vector2.Distance(transform.position, enemy.transform.position))
            .FirstOrDefault()
            .transform;
    }

    /// <summary>
    /// [2D 회전] 'turretHead'를 'currentTarget' 방향으로 (Z축 기준) 부드럽게 회전시킵니다.
    /// </summary>
    void RotateTowardsTarget()
    {
        if (turretHead == null || currentTarget == null) return; 

        Vector3 directionToTarget = currentTarget.position - turretHead.position;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        
        // 스프라이트가 아래(Y-)를 보는 것을 기준으로 180도 보정 (+90f)
        float targetAngle = angle + 90f; 

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        
        turretHead.rotation = Quaternion.Slerp(
            turretHead.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    /// <summary>
    /// 발사 쿨타임을 체크합니다.
    /// </summary>
    void TryFire()
    {
        // [기존 로직]
        if (Time.time - lastFireTime > fireCooldown)
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    /// <summary>
    /// 실제 총알을 발사하고 [✨애니메이션을 트리거]합니다.
    /// </summary>
    void Fire()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab 또는 Fire Point가 인스펙터에 연결되지 않았습니다!");
            return;
        }

        // --- [✨✨✨ 애니메이션 로직 추가!] ---
        // 1. 애니메이터가 연결되어 있고, 트리거 이름이 설정되어 있다면
        if (animator != null && !string.IsNullOrEmpty(fireTriggerName))
        {
            // 2. "shot" 트리거를 발동시킵니다. (애니메이션 재생!)
            animator.SetTrigger(fireTriggerName);
        }
        // ---

        // (이하는 기존 발사 로직)
        Vector2 dir = (currentTarget.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; 
            rb.AddForce(dir * bulletForce, ForceMode2D.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}