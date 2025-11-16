using System;
using UnityEngine;
using Pathfinding; // A* Pathfinding Project를 사용하기 위해 추가

[RequireComponent(typeof(AIPath), typeof(Seeker))] // AIPath와 Seeker 컴포넌트가 필수임을 명시
public class HY_Player : MonoBehaviour
{
    public Transform destination;
    
    AIPath aiPath; // AIPath 컴포넌트를 저장할 변수
    Animator anim;
    HY_Scanner scanner;
    [Header("아군 유닛 스탯")]
    [SerializeField] public int maxHp = 10;
    private int currentHp;
    public bool isLive = true; // 생존 상태 
    public GameObject bulletObj;
    public GameObject meleeAttackObj; // 근접 공격 프리팹
    public bool isMeleeAttacker = false; // 공격 타입 설정
    public float fireCooldown = 0.5f;
    public float meleeAttackRange = 1.5f; // 근접 공격 범위
    float lastFireTime;
    Vector2 lastDirection = Vector2.down; // 마지막으로 바라본 방향(기본값:아래)
    void Awake()
    {
        aiPath = GetComponent<AIPath>();
        anim = GetComponent<Animator>();
        scanner = GetComponent<HY_Scanner>();

        currentHp = maxHp;
        isLive = true;
    }

    void Update() // FixedUpdate에서 Update로 변경하여 매 프레임 상태를 확인
    {
        if (!isLive) return; // 죽었으면 정지
        // AIPath가 목적지로 이동하도록 설정
        if (destination != null)
        {
            aiPath.destination = destination.position;
        }

        Transform target = scanner != null ? scanner.nearestTarget : null;

        if (target != null)
        {
            if (isMeleeAttacker)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                if (distance <= meleeAttackRange)
                {
                    // 근접 공격 범위 내에 있으면 이동을 멈추고 공격
                    aiPath.canMove = false;
                    
                    if (Time.time - lastFireTime > fireCooldown)
                    {
                        MeleeAttack(target);
                        lastFireTime = Time.time;
                    }
                }
                else
                {
                    // 근접 공격 범위 밖에 있으면 타겟을 향해 이동
                    aiPath.canMove = true;
                    aiPath.destination = target.position;
                }
            }
            else
            {
                // 원거리 공격: 적이 있으면 이동을 멈춤
                aiPath.canMove = false;
                
                // 쿨타임 내에만 공격
                if (Time.time - lastFireTime > fireCooldown)
                {
                    Fire(target);
                    lastFireTime = Time.time;
                }
            }
        }
        else
        {
            // 적이 없으면 원래 목적지로 이동을 다시 시작
            aiPath.canMove = true;
            if (destination != null)
            {
                aiPath.destination = destination.position;
            }
        }
       
        
       // --- [ ✨ 1. 실제 속도/방향 계산 ] ---
        Vector2 currentVelocity = aiPath.velocity; 
        float animSpeed = currentVelocity.magnitude;
        anim.SetFloat("Speed", animSpeed);

        // --- [ ✨ 2. 애니메이션 방향 결정 로직 (수정됨) ] ---

        // 1) 만약 "이동 중"이라면 (Speed > 0.1)
        if (animSpeed > 0.1f)
        {
            // 실제 이동 방향(velocity)을 바라본다
            // (velocity가 0이 아닐 때만 갱신하여 (0,0) 저장을 방지)
            if (currentVelocity.magnitude > 0.01f) 
            {
                 lastDirection = currentVelocity.normalized; 
            }
        }
        // 2) 만약 "멈춰있고" "적이 있다면" (target 변수는 Update 상단에 이미 있음)
        else if (target != null) 
        {
            // 적의 방향을 바라본다
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            lastDirection = dirToTarget;
        }
        // 3) 만약 "멈춰있고" "적이 없다면" (깃발 도착 등)
        //    -> lastDirection을 바꾸지 않습니다. (마지막 방향 유지)
        

        // --- [ ✨ 3. 애니메이터에 최종 방향 전달 ] ---
        anim.SetFloat("moveX", lastDirection.x);
        anim.SetFloat("moveY", lastDirection.y);
    }

    void Fire(Transform target)
    {
        if (bulletObj == null || target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        GameObject bullet = Instantiate(bulletObj, transform.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(dir * 10f, ForceMode2D.Impulse);
        }
        anim.SetTrigger("Attack");
    }

    void MeleeAttack(Transform target)
    {
        if (meleeAttackObj == null || target == null) return;
        // 근접 공격 프리팹을 플레이어 위치에 생성
        Instantiate(meleeAttackObj, transform.position, Quaternion.identity);
        Debug.Log("Melee Attack!"); // 근접 공격 시 로그 출력
        anim.SetTrigger("Attack");

        
    }

    void OnDrawGizmosSelected()
    {
        // 근접 공격 범위를 시각적으로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
    }

    public void TakeDamage(int damageAmount)
    {
        if (!isLive) return; // 이미 죽었으면 데미지 안 받음

        currentHp -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage! Current HP: " + currentHp);

        // (선택) 피격 애니메이션
        // anim.SetTrigger("Hit");

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die(); // 체력이 0 이하면 사망
        }
        // 플레이어가 데미지를 받았을 때 처리할 로직
        Debug.Log("Player took " + damageAmount + " damage!");

    }
    private void Die() // 추가(IS)
    {
        if (!isLive) return; // 중복 사망 방지
        isLive = false;

        Debug.Log("Player Died (최초 1회 실행)");
        // 1. 죽음 애니메이션 재생
        anim.SetTrigger("Die");

        // 2. A* 이동 AI 정지
        //aiPath.canMove = false;
        //aiPath.enabled = false; // AIPath 컴포넌트 자체를 꺼버림

        // 3. 이 스크립트(뇌) 정지
        //this.enabled = false;

        // 4. 물리/충돌 중지
        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        rb.linearVelocity = Vector2.zero; // 현재 속도 0으로 만듦
        rb.isKinematic = true;          // 물리 엔진 무시 (스크립트로만 제어 가능하도록)
    }
        // 5. 오브젝트 파괴 (Die 애니메이션 재생 시간 기다리기)
        Destroy(gameObject, 1.5f);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        /*EnemyMeleeHitbox hitbox = collision.GetComponent<EnemyMeleeHitbox>();
        if (hitbox != null)
        {
            if (!collision.CompareTag("MeleeHitbox")) // <-- 이 부분은 선택사항이지만 권장됩니다.
            {
            // 3. 내 자신의 TakeDamage를 호출
            TakeDamage(hitbox.damage);
            }
        }*/
        EnemyMeleeHitbox enemyHitbox = collision.GetComponent<EnemyMeleeHitbox>();
    
        if (enemyHitbox != null)
        {
        // 1. 적군 히트박스(EnemyMeleeHitbox)와 충돌했는지 확인되면
        
        // 2. 데미지를 입고 함수를 종료합니다.
        // (AoE 방지/파괴 로직은 EnemyMeleeHitbox 스크립트에서 처리됩니다.)
        TakeDamage(enemyHitbox.damage);
        return; 
        }
    }
}