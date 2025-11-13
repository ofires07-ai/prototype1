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

        // 애니메이터에 속도 전달
        float animSpeed = aiPath.velocity.magnitude;
        anim.SetFloat("Speed", animSpeed);
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
        aiPath.canMove = false;
        aiPath.enabled = false; // AIPath 컴포넌트 자체를 꺼버림

        // 3. 이 스크립트(뇌) 정지
        this.enabled = false;

        // 4. 물리/충돌 중지
        GetComponent<Collider2D>().enabled = false;

        // 5. 오브젝트 파괴 (Die 애니메이션 재생 시간 기다리기)
        Destroy(gameObject, 2.5f);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        MeleeHitbox hitbox = collision.GetComponent<MeleeHitbox>();
        if (hitbox != null)
        {
            if (!collision.CompareTag("MeleeHitbox")) // <-- 이 부분은 선택사항이지만 권장됩니다.
            {
            // 3. 내 자신의 TakeDamage를 호출
            TakeDamage(hitbox.damage);
            }
        }
    }
}