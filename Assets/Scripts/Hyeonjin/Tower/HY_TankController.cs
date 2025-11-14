using UnityEngine;
using System.Linq; // 가장 가까운 적을 찾기 위해 Linq를 사용합니다.

/// <summary>
/// [2D 최종본] 타워의 포탑(Turret) 회전 및 발사 로직
/// 1. 2D 반경 내의 'Collider2D' 적을 감지합니다.
/// 2. 가장 가까운 적을 향해 'turretHead'를 (Z축 기준으로) 부드럽게 회전시킵니다.
/// 3. 적을 조준 중일 때 'firePoint'에서 'bulletPrefab'을 발사합니다.
/// </summary>
public class TurretController : MonoBehaviour
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

    // --- [✨ HY_Tower에서 가져온 새 변수들] ---
    [Header("--- 타워 발사 설정 ---")]
    [Tooltip("발사할 총알 프리펩을 연결해주세요.")]
    public GameObject bulletPrefab;

    [Tooltip("총알이 발사될 위치 (TurretHead의 자식인 'FirePoint')")]
    public Transform firePoint; // 2단계에서 만든 'FirePoint' 자식 오브젝트

    [Tooltip("발사 쿨타임 (초)")]
    public float fireCooldown = 0.5f;

    [Tooltip("총알이 날아가는 힘")]
    public float bulletForce = 10f;

    // 현재 추적하고 있는 타겟(적)
    private Transform currentTarget;
    // 마지막 발사 시간 (쿨타임 계산용)
    private float lastFireTime;

    // --- Unity 생명주기 함수 ---

    void Update()
    {
        // 1. 적을 찾습니다.
        FindTarget();

        // 2. 타겟(적)이 있는지 확인
        if (currentTarget != null)
        {
            // 2-1. 타겟이 있다면, 그쪽을 향해 회전합니다.
            RotateTowardsTarget();

            // 2-2. 타겟이 있다면, 발사를 시도합니다.
            TryFire();
        }
    }

    // --- 핵심 로직 함수 ---

    /// <summary>
    /// [2D 감지] 'detectionRadius' 반경 내에서 가장 가까운 'Collider2D'를 찾습니다.
    /// (이전 코드와 동일)
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
    /// (이전 코드와 동일, '반대 방향' 문제 해결됨)
    /// </summary>
    void RotateTowardsTarget()
    {
        // 타겟이 없거나 포탑이 없으면 회전하지 않음 (Update에서 이미 검사했지만, 이중 안전장치)
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

    // --- [✨ HY_Tower의 발사 로직을 가져와서 수정한 함수들] ---

    /// <summary>
    /// 발사 쿨타임을 체크합니다.
    /// </summary>
    void TryFire()
    {
        // (현재 시간 - 마지막 발사 시간)이 쿨타임보다 클 때만 발사
        if (Time.time - lastFireTime > fireCooldown)
        {
            Fire(); // 발사!
            lastFireTime = Time.time; // 마지막 발사 시간을 지금 시간으로 갱신
        }
    }

    /// <summary>
    /// 실제 총알을 발사합니다.
    /// </summary>
    void Fire()
    {
        // 총알 프리펩이나 총구가 설정되지 않았으면 경고 후 종료 (안전장치)
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab 또는 Fire Point가 인스펙터에 연결되지 않았습니다!");
            return;
        }

        // 1. 발사 방향 계산 (총구에서 -> 타겟으로)
        //    (HY_Tower는 타워 중심에서 계산했지만, 우린 '총구'에서 계산합니다)
        Vector2 dir = (currentTarget.position - firePoint.position).normalized;
        
        // 2. 발사 각도 계산 (총알 스프라이트가 타겟을 보게 함)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        // 3. 총알 생성 (총알 프리펩, '총구 위치', '총알 각도')
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        
        // 4. 총알에 물리(Rigidbody2D)가 있는지 확인
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; // 2D 총알은 중력이 필요 없습니다.
            
            // 5. 계산된 방향(dir)으로 총알에 '한 번에' 강한 힘(Impulse)을 줍니다.
            rb.AddForce(dir * bulletForce, ForceMode2D.Impulse);
        }
    }

    // (기즈모는 씬 뷰에만 표시되므로 3D 그대로 사용해도 됩니다)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}