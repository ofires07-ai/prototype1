using UnityEngine;

/// <summary>
/// [신규] 유도 미사일 로직
/// 1. 'target'을 설정받습니다.
/// 2. 'target'을 향해 부드럽게 방향을 틉니다.
/// 3. 항상 '앞으로' 날아갑니다.
/// 4. 적과 부딪히면 폭발(파괴)합니다.
/// </summary>
public class HomingMissile : MonoBehaviour
{
    [Header("미사일 설정")]
    public float moveSpeed = 8f;        // 미사일 비행 속도
    public float rotationSpeed = 200f;  // 미사일 회전(방향 전환) 속도
    public GameObject explosionPrefab;  // (선택 사항) 폭발 이펙트 프리펩

    private Transform target;           // 추적할 대상 (타워가 설정해줌)
    private Rigidbody2D rb;

    void Awake()
    {
        // 내 Rigidbody 2D 컴포넌트를 미리 찾아둡니다.
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 타워(TurretController)가 이 함수를 호출해서 타겟을 지정해줍니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // 물리 기반 이동은 FixedUpdate가 더 안정적입니다.
    void FixedUpdate()
    {
        // 1. 타겟이 없거나, 타겟이 (죽어서)사라졌다면
        if (target == null)
        {
            // 그냥 원래 가던 방향(앞)으로 계속 날아갑니다.
            rb.linearVelocity = transform.up * moveSpeed;
            // (N초 후에 자동 파괴되는 로직을 추가하면 더 좋습니다)
            return;
        }

        // --- 2. [핵심] 유도 로직 ---

        // 2-1. 타겟을 향하는 방향 벡터 계산
        Vector2 direction = (Vector2)target.position - rb.position;

        // 2-2. 방향 벡터를 2D 각도(Angle)로 변환
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 2-3. [중요] 미사일 스프라이트가 위(Y+)를 본다고 가정 (-90도 보정)
        float targetAngle = angle - 90f;

        // 2-4. 목표 각도로 부드럽게 회전 (Slerp 대신 MoveRotation 사용)
        rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime));

        // 2-5. 항상 내 '앞'방향(transform.up)으로 전진!
        rb.linearVelocity = transform.up * moveSpeed;
    }

    /// <summary>
    /// 콜라이더(Is Trigger)가 다른 콜라이더와 부딪혔을 때 호출됩니다.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // [중요] 부딪힌 대상이 "Enemy" 레이어인지 확인합니다.
        // (참고: LayerMask.LayerToName(other.gameObject.layer) == "Enemy"와 같습니다)
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // 1. (선택 사항) 폭발 이펙트가 있다면, 현재 위치에 생성
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
            
            // 2. (선택 사항) 적에게 데미지를 줍니다.
            //    (적에게 EnemyHealth.cs 같은 스크립트가 있다면)
            // EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            // if (enemy != null)
            // {
            //     enemy.TakeDamage(50); // RPG 데미지 50!
            // }

            // 3. 미사일 자신을 파괴
            Destroy(gameObject);
        }
    }
}