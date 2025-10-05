using UnityEngine;

// Enemy 캐릭터 스크립트
// Target(플레이어)을 추적하고 이동
public class Enemy : MonoBehaviour
{
    public Rigidbody2D rigid;
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D Target;

    bool isLive = true; // 생존 상태
    
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (!isLive) return; // 죽었으면 더 이상 움직이지 않음
        
        if (Target == null) return; // Target이 없으면 실행 안 함
        
        Vector2 dirVec = Target.position - rigid.position;
        float speed = 2f; // 이동 속도 조절
        Vector2 moveVec = dirVec.normalized * speed;
        rigid.linearVelocity = moveVec;
        
        // 애니메이터에 속도 전달
        anim.SetFloat("Speed", rigid.linearVelocity.magnitude);
        
        // 플레이어 방향을 바라보도록 스프라이트 반전
        if (dirVec.x != 0)
        {
            spriteRenderer.flipX = dirVec.x < 0;    
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
    }

    void LateUpdate()
    {
        // 필요한 후처리 코드 추가
    }
}