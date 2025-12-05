using UnityEngine;

/// <summary>
/// 적 유닛이 발사하는 기본적인 유도/추적 발사체 스크립트입니다.
/// HY_Ranged_EnemyUnitMovement.cs에서 Instantiation 후 Target을 설정합니다.
/// </summary>
public class HY_Orb : MonoBehaviour
{
    [Header("발사체 설정")]
    [Tooltip("구체의 이동 속도")]
    [SerializeField] private float moveSpeed = 10f;
    
    [Tooltip("구체의 공격력")]
    [SerializeField] public int damage = 1;
    
    [Tooltip("구체의 수명 (이 시간 후 자동으로 파괴)")]
    [SerializeField] private float lifeTime = 3f;

    private Transform target; // 추적할 대상 (플레이어)
    private Vector3 initialDirection; // 타겟이 없을 경우 초기 방향
    private bool hasTarget = false;

    void Start()
    {
        // 일정 시간 후 스스로 파괴되도록 설정하여 씬이 지저분해지는 것을 방지합니다.
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// HY_Ranged_EnemyUnitMovement.cs에서 타겟을 설정할 때 호출됩니다. (필수!)
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasTarget = true;
        // 타겟이 설정되면, 타겟을 향한 초기 방향을 계산합니다.
        // 타겟이 이동하더라도 이 발사체는 직선 경로로만 날아가게 할 수 있습니다. (아래 Update 로직 참고)
        if (target != null)
        {
            initialDirection = (target.position - transform.position).normalized;
        }
        else
        {
            // 타겟이 null이면 직선으로 날아갈 방향을 자체적으로 설정합니다 (예: 현재 정면)
            initialDirection = transform.right; // 2D 횡스크롤/탑다운 시 적절하게 변경
        }
    }

    void Update()
    {
        // 구체 이동 로직
        Vector3 movementVector;
        
        if (hasTarget && target != null)
        {
            // 🎯 옵션 1: 추적 (Homing) - 매 프레임 타겟 위치를 향해 방향을 업데이트합니다.
            // movementVector = (target.position - transform.position).normalized;
            
            // 🎯 옵션 2: 직선 발사 (Straight Shot) - 초기 설정된 방향으로만 이동합니다.
            movementVector = initialDirection;
        }
        else
        {
            // 타겟이 없거나 사라진 경우, 초기 방향으로 계속 이동합니다.
            movementVector = initialDirection;
        }

        transform.position += movementVector * moveSpeed * Time.deltaTime;
    }
    
    // 충돌 처리 (2D 게임 기준)
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어 충돌 처리
        // 'Player' 태그를 사용하거나, 플레이어 스크립트를 직접 찾습니다.

        if (collision.CompareTag("Flag") || collision.CompareTag("Tower"))
        {
            return;
        }
        
        if (collision.CompareTag("Player"))
        {
            // 플레이어의 체력 관리 스크립트를 찾아 데미지를 줍니다.
            // 예시: PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(damage);
            // }
            
            HY_Player player = collision.GetComponent<HY_Player>();
            
            player.TakeDamage(damage);

            Debug.Log($"[Orb Hit] {collision.gameObject.name}에 충돌! 데미지: {damage}");
            
            // 충돌 후 발사체는 파괴됩니다.
            Destroy(gameObject);
        }
        
        // 2. 환경 충돌 처리 (선택 사항: 벽이나 장애물에 닿으면 파괴되게 할 경우)
        // if (collision.CompareTag("Wall"))
        // {
        //     Destroy(gameObject);
        // }
    }
}