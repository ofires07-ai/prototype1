using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HY_Orb : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 8f;      // 날아가는 속도
    public float rotateSpeed = 200f;  // 유도 회전 속도 (클수록 급커브 가능)
    public int damage = 10;           // 공격력
    public float lifeTime = 5f;       // 최대 생존 시간 (못 맞추면 사라짐)

    private Transform target;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 지정된 시간이 지나면 자동으로 사라짐 (메모리 관리)
        Destroy(gameObject, lifeTime); 
    }

    // 적 유닛이 호출해주는 함수: "이 녀석을 쫓아가라!"
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void FixedUpdate()
    {
        // 1. 타겟이 존재하면 유도(Homing) 비행
        if (target != null)
        {
            // 타겟 방향 계산
            Vector2 direction = (Vector2)target.position - rb.position;
            direction.Normalize();

            // 회전 (Cross Product를 이용해 타겟을 바라보도록 회전)
            float rotateAmount = Vector3.Cross(direction, transform.up).z;
            rb.angularVelocity = -rotateAmount * rotateSpeed;

            // 앞으로 전진
            rb.linearVelocity = transform.up * moveSpeed;
        }
        else
        {
            // 2. 타겟이 사라졌으면(죽었거나 놓침) 그냥 직진
            rb.linearVelocity = transform.up * moveSpeed;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 태그로 아군인지 확인 (플레이어 유닛 or 기지)
        // 만약 아군 유닛의 태그가 "Player"가 아니라면 여기를 수정하세요.
        if (collision.CompareTag("Player") || collision.CompareTag("Base"))
        {
            // 1. 플레이어 유닛 데미지 처리
            // (플레이어 스크립트 이름이 HY_Player라고 가정)
            HY_Player player = collision.GetComponent<HY_Player>();
            if (player != null) 
            {
                player.TakeDamage(damage);
            }

            // 2. 기지(우주선) 데미지 처리
            // (기지 스크립트 이름이 HY_Spaceship이라고 가정)
            SpaceShip spaceship = collision.GetComponent<SpaceShip>();
            if (spaceship != null) 
            {
                //spaceship.TakeDamage(damage);
            }

            // 3. (선택) 충돌 이펙트 생성
            // Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            // 4. 구체 삭제 (임무 완수)
            Destroy(gameObject);
        }
        Debug.Log($"💥 구체 충돌! 부딪힌 대상: {collision.name} (태그: {collision.tag}) / IsTrigger: {collision.isTrigger}");
    }
    
}