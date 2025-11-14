using UnityEngine;
using System.Linq; // 가장 가까운 적을 찾기 위해 Linq를 사용합니다.

/// <summary>
/// [2D 물리 전용] 탑다운 뷰 타워의 포탑(Turret) 회전 로직
/// 1. 2D 반경 내의 'Collider2D' 적을 감지합니다.
/// 2. 가장 가까운 적을 향해 'turretHead'를 (Z축 기준으로) 부드럽게 회전시킵니다.
/// </summary>
public class HY_TankController : MonoBehaviour
{
    [Header("타워 설정")]
    [Tooltip("회전시킬 포탑(Turret_Head) 오브젝트를 연결해주세요.")]
    public Transform turretHead;

    [Tooltip("적이 감지되는 최대 반경입니다.")]
    public float detectionRadius = 10f;

    [Tooltip("포탑이 적을 향해 회전하는 속도입니다.")]
    public float rotationSpeed = 5f;

    [Tooltip("감지할 'Enemy' 레이어를 선택해주세요.")]
    public LayerMask enemyLayer;

    // 현재 추적하고 있는 타겟(적)
    private Transform currentTarget;

    void Update()
    {
        // 1. 매 프레임마다 타겟을 찾습니다. (2D 감지)
        FindTarget();

        // 2. 타겟이 있다면, 그쪽을 향해 회전합니다. (2D 회전)
        RotateTowardsTarget();
    }

    /// <summary>
    /// [✨ 2D 수정] 3D 대신 2D 물리(Physics2D)를 사용해 적을 찾습니다.
    /// </summary>
    void FindTarget()
    {
        // Physics.OverlapSphere (3D) 대신 Physics2D.OverlapCircleAll (2D) 사용
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);

        if (enemies.Length == 0)
        {
            currentTarget = null;
            return;
        }

        // 가장 가까운 적을 찾습니다.
        currentTarget = enemies
            .OrderBy(enemy => Vector2.Distance(transform.position, enemy.transform.position))
            .FirstOrDefault()
            .transform;
    }

    /// <summary>
    /// [✨ 2D 수정] 3D 회전(LookRotation) 대신 2D 각도(Atan2)를 사용해 회전합니다.
    /// </summary>
    void RotateTowardsTarget()
    {
        if (turretHead == null || currentTarget == null)
        {
            return; // 타겟이나 포탑이 없으면 정지
        }

        // --- 3. [탑다운 2D 회전 핵심 로직] ---

        // 3-1. 포탑에서 타겟을 향하는 방향 벡터(Vector3)를 계산합니다.
        Vector3 directionToTarget = currentTarget.position - turretHead.position;

        // 3-2. 방향 벡터를 이용해 2D 각도(Angle)를 계산합니다.
        //      Mathf.Atan2는 (y, x) 순서로 인자를 받습니다.
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;

        // 3-3. [중요] 스프라이트가 기본적으로 '위쪽'(Y+)을 바라본다고 가정합니다.
        //      Atan2는 '오른쪽'(X+)을 0도로 계산하므로, 90도를 빼서 보정해줍니다.
        float targetAngle = angle + 90f;

        // 3-4. Quaternion.Euler를 사용해 Z축 회전값을 만듭니다. (X:0, Y:0, Z:targetAngle)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        // 3-5. Slerp를 사용해 부드럽게 회전시킵니다.
        turretHead.rotation = Quaternion.Slerp(
            turretHead.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    // (기즈모는 씬 뷰에만 표시되므로 3D 그대로 사용해도 됩니다)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}