using UnityEngine;
using System.Linq; 

/// <summary>
/// [RPG 타워로 수정됨]
/// 1. 적을 감지하고 포탑을 회전시킵니다.
/// 2. [수정] 'HomingMissile' 프리펩을 발사하고, 'target'을 설정해줍니다.
/// </summary>
public class TurretController : MonoBehaviour
{
    [Header("--- 타워 회전 설정 ---")]
    public Transform turretHead;
    public float detectionRadius = 10f;
    public float rotationSpeed = 5f;
    public LayerMask enemyLayer;

    // --- [✨ 발사 설정 수정] ---
    [Header("--- 타워 발사 설정 ---")]
    [Tooltip("발사할 [유도 미사일] 프리펩을 연결해주세요.")]
    public GameObject bulletPrefab; // (RPGMissile_Prefab을 여기에 연결)

    [Tooltip("총알이 발사될 위치 (TurretHead의 자식인 'FirePoint')")]
    public Transform firePoint; 

    [Tooltip("발사 쿨타임 (초)")]
    public float fireCooldown = 1.5f; // RPG는 쿨타임이 좀 더 길겠죠?

    // [✨ 제거] bulletForce는 미사일 스크립트가 담당하므로 필요 없습니다.
    // public float bulletForce = 10f; 

    [Header("--- 애니메이션 설정 ---")]
    public Animator animator; 
    public string fireTriggerName = "shot";

    private Transform currentTarget;
    private float lastFireTime;

    // (Update, FindTarget, RotateTowardsTarget 함수는 이전과 100% 동일합니다)
    // ... (Update, FindTarget, RotateTowardsTarget 함수 복사 붙여넣기) ...

    void Update()
    {
        FindTarget();
        if (currentTarget != null)
        {
            RotateTowardsTarget();
            TryFire();
        }
    }

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
        float targetAngle = angle + 90f; // (아래쪽 보는 스프라이트 기준)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void TryFire()
    {
        if (Time.time - lastFireTime > fireCooldown)
        {
            Fire(); 
            lastFireTime = Time.time; 
        }
    }

    /// <summary>
    /// [✨✨✨ 핵심 수정!]
    /// 총알에 힘을 가하는 대신, 미사일 스크립트에 타겟을 알려줍니다.
    /// </summary>
    void Fire()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab 또는 Fire Point가 연결되지 않았습니다!");
            return;
        }

        // 애니메이션 트리거 (동일)
        if (animator != null && !string.IsNullOrEmpty(fireTriggerName))
        {
            animator.SetTrigger(fireTriggerName);
        }

        // 1. 미사일 생성 (총구 위치, 총구의 현재 각도)
        //    (미사일은 어차피 타겟을 향해 꺾을 거라, 현재 총구 각도로 쏴줍니다)
        GameObject missileGO = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        // 2. 미사일에서 'HomingMissile' 스크립트를 가져옵니다.
        HomingMissile missileScript = missileGO.GetComponent<HomingMissile>();
        
        // 3. [가장 중요!] 미사일 스크립트가 존재하고, 우리 타겟도 존재한다면
        if (missileScript != null && currentTarget != null)
        {
            // 4. "미사일아! 너의 타겟은 'currentTarget'이야!" 라고 알려줍니다.
            missileScript.SetTarget(currentTarget);
        }

        // 5. [✨ 제거] Rigidbody2D와 AddForce 로직은 이제 필요 없습니다.
        //    미사일이 스스로 날아갑니다.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}