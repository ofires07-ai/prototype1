using UnityEngine;
using System.Linq; // 가장 가까운 적을 찾기 위해 Linq를 사용합니다.

/// <summary>
/// 탑다운 뷰 타워의 포탑(Turret) 회전 로직을 담당합니다.
/// 1. 반경 내의 적을 감지합니다.
/// 2. 가장 가까운 적을 향해 'turretHead'를 부드럽게 회전시킵니다.
/// </summary>
public class TurretController : MonoBehaviour
{
    [Header("타워 설정")]
    [Tooltip("회전시킬 포탑(Turret_Head) 오브젝트를 연결해주세요.")]
    public Transform turretHead; // 2단계에서 만든 'TurretHead' 자식 오브젝트

    [Tooltip("적이 감지되는 최대 반경입니다.")]
    public float detectionRadius = 10f;

    [Tooltip("포탑이 적을 향해 회전하는 속도입니다.")]
    public float rotationSpeed = 5f;

    [Tooltip("감지할 'Enemy' 레이어를 선택해주세요.")]
    public LayerMask enemyLayer;

    // 현재 추적하고 있는 타겟(적)
    private Transform currentTarget;

    // --- Unity 생명주기 함수 ---

    void Update()
    {
        // 1. 매 프레임마다 타겟을 찾습니다.
        FindTarget();

        // 2. 타겟이 있다면, 그쪽을 향해 회전합니다.
        RotateTowardsTarget();
    }

    // --- 핵심 로직 함수 ---

    /// <summary>
    /// 'detectionRadius' 반경 내에서 'enemyLayer'에 속한 가장 가까운 적을 찾습니다.
    /// </summary>
    void FindTarget()
    {
        // 1. 3D 물리 엔진을 사용해 내 위치(transform.position)를 중심으로
        //    'detectionRadius' 반경 내에 있는 'enemyLayer' 콜라이더를 모두 찾습니다.
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        // 2. 적이 한 명도 없으면
        if (enemies.Length == 0)
        {
            currentTarget = null; // 타겟 없음
            return;
        }

        // 3. 적이 있다면, Linq의 OrderBy를 사용해 '가장 가까운' 적을 찾습니다.
        // (적과 나의 거리를 기준으로 오름차순 정렬한 뒤, 첫 번째 적을 가져옴)
        currentTarget = enemies
            .OrderBy(enemy => Vector3.Distance(transform.position, enemy.transform.position))
            .FirstOrDefault()
            .transform;
    }

    /// <summary>
    /// 'turretHead'를 'currentTarget' 방향으로 부드럽게 회전시킵니다.
    /// </summary>
    void RotateTowardsTarget()
    {
        // 1. 인스펙터에서 'turretHead'를 연결했는지 확인 (안전장치)
        if (turretHead == null)
        {
            Debug.LogError("Turret Head가 할당되지 않았습니다! 인스펙터 창을 확인해주세요.");
            return;
        }

        // 2. 타겟(적)이 없으면 회전할 필요가 없으므로 함수 종료
        if (currentTarget == null)
        {
            // (참고) 여기에 타겟이 없을 때 포탑이 다시 정면을 보도록 하는
            // '기본 방향으로 복귀' 로직을 추가할 수도 있습니다.
            return;
        }

        // --- 3. [탑다운 뷰 회전 핵심 로직] ---

        // 3-1. 포탑에서 타겟을 향하는 방향 벡터(Vector3)를 계산합니다.
        //      (타겟 위치 - 내 위치)
        Vector3 directionToTarget = currentTarget.position - turretHead.position;

        // 3-2. [★매우 중요★]
        //      이 코드는 탑다운(위에서 아래로 보는) 뷰 기준입니다.
        //      포신이 땅이나 하늘을 보지 않도록(숙이지 않도록) Y축 값을 강제로 0으로 고정합니다.
        directionToTarget.y = 0;

        // 3-3. 이 방향(directionToTarget)을 바라보는 회전값(Quaternion)을 계산합니다.
        //      (directionToTarget이 (0,0,0)이 아닐 때만 계산)
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // 3-4. 현재 회전값(turretHead.rotation)에서
            //      목표 회전값(targetRotation)까지
            //      'Time.deltaTime * rotationSpeed'의 속도로 부드럽게(Slerp) 회전시킵니다.
            turretHead.rotation = Quaternion.Slerp(
                turretHead.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    /// <summary>
    /// (선택 사항) 유니티 씬(Scene) 뷰에서 타워를 선택했을 때
    /// 'detectionRadius' (감지 반경)을 빨간색 원으로 보여줍니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}