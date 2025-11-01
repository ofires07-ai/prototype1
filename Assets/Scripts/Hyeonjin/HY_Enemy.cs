using UnityEngine;

// Enemy 캐릭터 스크립트
// Target(플레이어)을 추적하고 이동
public class HY_Enemy : MonoBehaviour
{
    public Rigidbody2D rigid;
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D Target;
    public float stoppingDistance = 0.5f; // 멈추는 거리
    public int maxHp = 10; // 최대 체력
    public int currentHp; // 현재 체력

    bool isLive = true; // 생존 상태
    
    void Awake()
    {
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHp = maxHp; // 현재 체력을 최대 체력으로 초기화
    }

    void FixedUpdate()
    {
        if (!isLive) return; // 죽었으면 더 이상 움직이지 않음
        
        if (Target == null) return; // Target이 없으면 실행 안 함

        // 목표까지의 거리 계산
        float distanceToTarget = Vector2.Distance(Target.position, rigid.position);

        // 목표에 충분히 가까워지면 움직임을 멈춤
        if (distanceToTarget <= stoppingDistance)
        {
            rigid.linearVelocity = Vector2.zero;
            anim.SetFloat("Speed", 0); // 애니메이션 속도 0으로 설정
            return;
        }
        
        Vector2 dirVec = Target.position - rigid.position;
        float speed = 2f; // 이동 속도 조절
        Vector2 moveVec = dirVec.normalized * speed;
        rigid.linearVelocity = moveVec;
        anim.speed = 10f; // 애니메이션 속도 조절
        // 애니메이터에 속도 전달
        anim.SetFloat("Speed", rigid.linearVelocity.magnitude);
        
        // 플레이어 방향을 바라보도록 스프라이트 반전
        if (dirVec.x != 0)
        {
            spriteRenderer.flipX = dirVec.x < 0;    
        }
    }

    // Enemy가 피해를 입을 때 호출되는 메서드
    public void TakeDamage(int damage)
    {
        if (!isLive) return; // 이미 죽었으면 피해를 받지 않음

        currentHp -= damage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    // Enemy가 죽을 때 호출되는 메서드
    public void Die()
    {
        isLive = false;
        rigid.linearVelocity = Vector2.zero;
        
        // 죽음 애니메이션이 있다면 트리거
        // anim.SetTrigger("Die");
        
        // 콜라이더 비활성화 (선택사항)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // 오브젝트 파괴 (선택사항)
        Destroy(gameObject, 1f); // 1초 후 파괴
    }

    void LateUpdate()
    {
        // 필요한 후처리 코드 추가
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 Bullet 스크립트를 가지고 있는지 확인
        HY_Bullet bullet = collision.GetComponent<HY_Bullet>();
        if (bullet != null)
        {
            // Bullet과 충돌했으므로 Enemy는 피해를 입고 Bullet은 파괴
            TakeDamage(bullet.damage); // 총알의 데미지를 받아옴
        }
    }
}