using UnityEngine;

// Enemy 캐릭터 스크립트
// 기본 목표(우주선)를 향해 이동하다가, 스캐너로 감지된 적(플레이어)을 우선 공격합니다.
public class HY_Enemy : MonoBehaviour
{
    // --- 1. 적의 현재 상태 정의 ---
    public enum EnemyState
    {
        MovingToSpaceship, // 우주선으로 이동 중
        AttackingPlayer    // 플레이어 감지 및 공격 중
    }
    private EnemyState currentState; // 현재 AI 상태

    [Header("컴포넌트")]
    public Rigidbody2D rigid;
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    private HY_Scanner scanner; // '눈' 역할을 할 스캐너

    [Header("목표물 설정")]
    public Transform Target; // <<< AI가 현재 추적 중인 '활성 목표'
    public Transform spaceshipTarget; // <<< [필수] Inspector에서 우주선 오브젝트를 연결해야 합니다.

    [Header("적 스탯")]
    public string enemyID; // 스폰 매니저 보고용 ID
    public float speed = 2f;
    public float stoppingDistance = 0.5f; // 멈추는 거리
    public int maxHp = 10; // 최대 체력
    public int currentHp; // 현재 체력
    bool isLive = true; // 생존 상태
    bool deathReported = false; // 사망 보고 중복 방지

    [Header("즉각 공격 설정 (1번 방식)")]
    public int damage = 10;           // 1회 공격력
    public float attackRate = 1.5f;   // 공격 속도 (1.5초에 한 번)
    private float nextAttackTime = 0f; // 다음 공격이 가능한 시간

    void Awake()
    {
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        scanner = GetComponent<HY_Scanner>(); // 내장된 스캐너 컴포넌트 찾기

        currentHp = maxHp; // 체력 초기화

        // --- 4. AI 초기 상태 설정 ---
        currentState = EnemyState.MovingToSpaceship; // 처음엔 우주선으로 이동

        // 우주선이 설정되었다면, 첫 목표로 지정
        if (spaceshipTarget != null)
        {
            Target = spaceshipTarget;
        }
        else
        {
            Debug.LogWarning(gameObject.name + "의 spaceshipTarget이 설정되지 않았습니다!");
        }
    }

    void FixedUpdate()
    {
        if (!isLive) return; // 죽었다면 아무것도 하지 않음

        // --- 1. "두뇌" : 상태 및 목표물 결정 ---
        if (scanner.nearestTarget != null)
        {
            // '눈'이 플레이어를 감지함 => "플레이어 공격" 상태로 즉시 변경
            currentState = EnemyState.AttackingPlayer;
            Target = scanner.nearestTarget; // 활성 목표를 플레이어로 변경
        }
        else
        {
            // '눈'이 플레이어를 감지하지 못함 (범위 밖이거나, 플레이어가 죽음)
            if (currentState == EnemyState.AttackingPlayer)
            {
                // 방금까지 플레이어를 쫓았다면, "우주선 이동" 상태로 복귀
                currentState = EnemyState.MovingToSpaceship;
                Target = spaceshipTarget; // 활성 목표를 다시 우주선으로 변경
            }
        }

        // --- 2. "몸" : 결정된 상태에 따라 행동 ---

        // 활성 목표(Target)가 아예 없으면 (우주선도 설정 안 됐으면) 멈춤
        if (Target == null)
        {
            rigid.linearVelocity = Vector2.zero;
            anim.SetFloat("Speed", 0);
            return;
        }

        // '활성 목표'까지의 거리 계산
        float distanceToTarget = Vector2.Distance((Vector2)Target.position, rigid.position);

        // --- (A) 공격/정지 범위 안에 들어왔을 때 ---
        if (distanceToTarget <= stoppingDistance)
        {
            rigid.linearVelocity = Vector2.zero; // 멈춤
            anim.SetFloat("Speed", 0);

            // "플레이어 공격" 상태일 때만 공격 로직을 실행
            if (currentState == EnemyState.AttackingPlayer)
            {
                // 쿨다운(attackRate)이 다 찼는지 확인
                if (Time.time >= nextAttackTime)
                {
                    Attack(); // 즉시 공격!
                    nextAttackTime = Time.time + attackRate; // 다음 공격 시간 갱신
                }
            }
            // (참고: 만약 '우주선 이동' 상태로 우주선에 도착하면, 
            //  공격 로직이 없으므로 그냥 멈춰있게 됩니다.)

            return; // 멈췄으니 더 이상 이동 로직은 실행 안 함
        }

        // --- (B) 범위 밖에 있을 때 (목표를 향해 이동) ---
        Vector2 dirVec = (Vector2)Target.position - rigid.position;
        Vector2 moveVec = dirVec.normalized * speed;
        rigid.linearVelocity = moveVec;

        // (참고: 이 값은 1f가 표준입니다. 10배속 재생 중입니다!)
        anim.speed = 10f;

        anim.SetFloat("Speed", rigid.linearVelocity.magnitude);

        // 이동 방향에 따라 스프라이트 좌우 반전
        if (dirVec.x != 0)
        {
            spriteRenderer.flipX = dirVec.x < 0;
        }
    }

    // --- (이 아래는 우리가 이전에 만든, 잘 작동하는 함수들입니다) ---

    // '즉각 공격' 함수
    public void Attack()
    {
        if (Target == null) return; // 방어 코드

        // 1. 공격 애니메이션 재생 (Animator에 "Attack" Trigger가 설정되어 있어야 함)
        anim.SetTrigger("Attack");

        // 2. 실제 데미지 주기
        // (플레이어에게 HY_Player 스크립트가 있다고 가정)
        HY_Player player = Target.GetComponent<HY_Player>();

        if (player != null)
        {
            player.TakeDamage(damage);
            Debug.Log(gameObject.name + "가 " + Target.name + "을(를) 공격! 데미지: " + damage);
        }
    }

    // 데미지 받는 함수
    public void TakeDamage(int damage)
    {
        if (!isLive) return;

        currentHp -= damage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    // 죽음 처리 함수
    public void Die()
    {
        if (deathReported) return;       // 중복 보고 방지
        deathReported = true;

        // 스폰 매니저에 사망 보고
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.OnMonsterDied(enemyID);

        isLive = false; // AI 정지
        rigid.linearVelocity = Vector2.zero; // 물리 정지

        // 콜라이더 비활성화 (시체가 길을 막지 않도록)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // TODO: 여기에 죽음 애니메이션 Trigger를 넣어도 좋습니다.
        // anim.SetTrigger("Die"); 

        // 1초 뒤에 오브젝트 파괴 (죽음 애니메이션 재생 시간)
        Destroy(gameObject, 1f);
    }

    void LateUpdate() { }

    // 총알(Bullet)에 맞았을 때 데미지를 받는 함수
    void OnTriggerEnter2D(Collider2D collision)
    {
        // (참고: 이 로직은 적이 '총알'에 맞는 로직이며,
        //  적이 '근접 공격'을 하는 로직과는 별개입니다.)
        HY_Bullet bullet = collision.GetComponent<HY_Bullet>();
        if (bullet != null)
        {
            TakeDamage(bullet.damage);
            Destroy(bullet.gameObject); // 총알 즉시 파괴
        }
    }
}