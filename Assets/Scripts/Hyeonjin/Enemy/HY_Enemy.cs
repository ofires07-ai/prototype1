using UnityEngine;

// Enemy 캐릭터 스크립트
// Target(플레이어)을 추적하고 이동
public class HY_Enemy : MonoBehaviour
{
    public Rigidbody2D rigid;
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D Target;

    // 🔹 SpawnManager에 보고할 적 타입 ID (Wave의 enemyID와 동일해야 함)
    public string enemyID;

    public float stoppingDistance = 0.5f; // 멈추는 거리
    public int maxHp = 10; // 최대 체력
    public int currentHp; // 현재 체력

    bool isLive = true; // 생존 상태
    bool deathReported = false; // 🔹 중복 보고 방지

    void Awake()
    {
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHp = maxHp; // 현재 체력을 최대 체력으로 초기화
    }

    void FixedUpdate()
    {
        if (!isLive) return;        
        if (Target == null) return;

        float distanceToTarget = Vector2.Distance(Target.position, rigid.position);

        if (distanceToTarget <= stoppingDistance)
        {
            rigid.linearVelocity = Vector2.zero;
            anim.SetFloat("Speed", 0);
            return;
        }
        
        Vector2 dirVec = Target.position - rigid.position;
        float speed = 2f;
        Vector2 moveVec = dirVec.normalized * speed;
        rigid.linearVelocity = moveVec;
        anim.speed = 10f;
        anim.SetFloat("Speed", rigid.linearVelocity.magnitude);
        
        if (dirVec.x != 0)
        {
            spriteRenderer.flipX = dirVec.x < 0;    
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isLive) return;

        currentHp -= damage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (deathReported) return;       // 🔹 혹시 여러 번 호출돼도 한 번만 처리
        deathReported = true;

        // 🔹 SpawnManager에 사망 보고(타입별 카운트 감소)
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.OnMonsterDied(enemyID);

        isLive = false;
        rigid.linearVelocity = Vector2.zero;

        // 콜라이더 비활성화 (선택)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 오브젝트 파괴
        Destroy(gameObject, 1f);
    }

    void LateUpdate() { }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HY_Bullet bullet = collision.GetComponent<HY_Bullet>();
        if (bullet != null)
        {
            TakeDamage(bullet.damage);

            // 🔹 총알이 여러 프레임 동안 계속 맞추지 않도록 즉시 파괴 권장
            Destroy(bullet.gameObject);
        }
    }
}
