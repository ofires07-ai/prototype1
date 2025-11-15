using UnityEngine;

/// <summary>
/// [✨ 리팩토링] 성장하는 유도 구체
/// [수정] 크기 조절(Grow) 로직이 제거되고, 'Animator'가 Scale을 제어합니다.
/// 
/// 1. 'target'을 설정받아 쫓아갑니다.
/// 2. (Animator가 크기를 키웁니다)
/// 3. 적과 부딪히면, '현재 Animator가 설정한 크기(localScale.x)'를 폭발 범위로 사용합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
// [✨ 추가!] Animator가 Scale을 제어하므로 Animator가 반드시 필요합니다.
[RequireComponent(typeof(Animator))] 
public class GrowingOrb : MonoBehaviour
{
    [Header("유도 설정")]
    public float moveSpeed = 8f;        // 비행 속도
    public float rotationSpeed = 200f;  // 회전 속도
    private Transform target;           // 추적할 대상
    private Rigidbody2D rb;

    // [✨ 삭제!] --- 성장 설정 변수들 제거 ---
    // public float growthRate = 0.5f;
    // public float maxScale = 5f;

    [Header("폭발 설정 (가변 범위)")]
    public int damage = 25;             
    public LayerMask targetLayer;       // 데미지를 입힐 대상 (아군/플레이어)
    public GameObject explosionPrefab;  // 시각적 폭발 이펙트 프리펩

    [Header("생존 시간 설정")]
    [Tooltip("구체가 최대 생존할 시간 (초). 이 시간이 지나면 자동 파괴됩니다.")]
    public float maxLifetime = 5.0f; // 스캐너 범위와 속도를 고려해 설정

    private bool hasHit = false; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true; 
        
        // [✨ 추가!] Animator 컴포넌트가 Scale을 제어하도록 설정
        // (선택 사항: Animator가 Rigidbody 위치에 영향을 주지 않도록 함)
        GetComponent<Animator>().applyRootMotion = false;


        // [✨ 핵심 추가!]
        // 이 오브젝트가 5초(maxLifetime) 뒤에 자동으로 파괴되도록 예약합니다.
        // 만약 5초 전에 적과 부딪힌다면, OnTriggerEnter2D의 Destroy(gameObject)가
        // 먼저 실행되어 이 예약은 자동으로 취소됩니다.
        Destroy(gameObject, maxLifetime);
    }

    /// <summary>
    /// 적 유닛(HY_EnemyUnitMovement)이 이 함수를 호출해서 타겟을 지정해줍니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void FixedUpdate()
    {
        // [✨ 삭제!] 1. 성장 로직(Grow()) 호출 제거
        // Grow(); 

        // 2. 타겟이 없으면 그냥 앞으로만 날아감 (로직 동일)
        if (target == null)
        {
            rb.linearVelocity = transform.up * moveSpeed;
            return;
        }

        // 3. 유도 로직 (로직 동일)
        Vector2 direction = (Vector2)target.position - rb.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float targetAngle = angle - 90f; 
        rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime));
        rb.linearVelocity = transform.up * moveSpeed;
    }

    // [✨ 삭제!] --- Grow() 함수 전체 제거 ---
    // void Grow() { ... }

    /// <summary>
    /// [✨ 로직 동일!] 
    /// 충돌 시 '현재 크기(Animator가 설정한)'로 범위 데미지를 줍니다.
    /// 이 함수는 수정할 필요가 없습니다.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 1. 부딪힌 대상이 'targetLayer'인지 확인 (로직 동일)
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            hasHit = true;

            // 2. [!!!] 현재 내 크기(transform.localScale.x)를 폭발 범위(Radius)로 사용
            // 애니메이터가 이 값을 5로 키웠다면 5가 되고, 10으로 키웠다면 10이 됩니다.
            float currentExplosionRadius = transform.localScale.x;
            
            // 3. 현재 크기(범위) 내의 모든 'targetLayer' 콜라이더를 찾음 (로직 동일)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentExplosionRadius, targetLayer);
            
            Debug.Log($"구체 폭발! (애니메이션 크기: {currentExplosionRadius})로 {hits.Length}개의 타겟 감지!");

            // 4. 찾은 모든 타겟에게 데미지 (로직 동일)
            foreach (Collider2D hitTarget in hits)
            {
                // (!!주의!!) 아군/플레이어의 체력 스크립트로 변경해야 합니다.
                HY_Player player = hitTarget.GetComponent<HY_Player>(); // (임시)
                if (player != null)
                {
                    Debug.Log(player.name + "에게 데미지 " + damage + "!");
                }
            }

            // 5. 시각적 폭발 이펙트 소환 (로직 동일)
            if (explosionPrefab != null)
            {
                GameObject explosionGO = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                explosionGO.transform.localScale = transform.localScale;
            }

            // 6. 구체(자신) 파괴 (로직 동일)
            Destroy(gameObject);
        }
    }
}