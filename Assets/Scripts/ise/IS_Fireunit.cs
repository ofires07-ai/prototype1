using System;
using UnityEngine;
using Pathfinding; // A* Pathfinding 사용

// AIPath와 Seeker 컴포넌트가 없으면 자동으로 추가해줌
[RequireComponent(typeof(AIPath), typeof(Seeker))] 
public class IS_Fireunit : MonoBehaviour
{
    [Header("경로 탐색")]
    public Transform destination; // 목적지 (필요 시)
    
    // 컴포넌트 참조 변수들
    AIPath aiPath; 
    Animator anim;
    HY_Scanner scanner;

    [Header("유닛 스탯")]
    [SerializeField] public int maxHp = 10;
    private int currentHp;
    public bool isLive = true; 

    // [✨ 핵심] 애니메이션 이름을 통해 쿨타임을 자동 계산하기 위한 변수
    [Tooltip("애니메이터에 있는 공격 클립 이름 (예: Attack, Fire)")]
    [SerializeField] private string attackClipName = "Attack"; 

    [Header("공격 설정")]
    public GameObject bulletObj;      // 원거리용 총알 (화염방사기는 안 씀)
    public GameObject meleeAttackObj; // 근접용 이펙트 (필요 시)
    public bool isMeleeAttacker = false; // 화염방사기는 이걸 체크(True)해야 함!
    
    // 공격 쿨타임 (Start함수에서 애니메이션 길이에 맞춰 자동으로 변경됨)
    public float fireCooldown = 0.5f; 
    public float meleeAttackRange = 1.5f; // 공격 사거리
    
    // 내부 쿨타임 계산용 변수
    float lastFireTime;
    Vector2 lastDirection = Vector2.down; 

    void Awake()
    {
        // 컴포넌트 가져오기
        aiPath = GetComponent<AIPath>();
        anim = GetComponent<Animator>();
        scanner = GetComponent<HY_Scanner>();

        currentHp = maxHp;
        isLive = true;
    }

    // [✨ 추가] 게임 시작 시 애니메이션 길이를 찾아 쿨타임 자동 설정
    void Start()
    {
        // 1. 애니메이터에서 공격 관련 클립 길이를 찾아옴
        float animDuration = GetAttackClipDuration();

        // 2. 찾았다면 그 길이 + 0.1초로 쿨타임 설정 (애니메이션 끊김 방지)
        if (animDuration > 0f)
        {
            fireCooldown = animDuration + 0.1f;
            // 디버그용 로그 (콘솔에서 확인 가능)
            Debug.Log($"[IS_Fireunit] {name} 쿨타임 자동 설정 완료: {fireCooldown:F2}초");
        }
        else
        {
            Debug.LogWarning($"[IS_Fireunit] {name}: '{attackClipName}' 클립을 못 찾아 기본 쿨타임({fireCooldown})을 사용합니다.");
        }
    }

    // [✨ 추가] 애니메이션 클립 길이를 찾아주는 도구 함수
    private float GetAttackClipDuration()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return 0f;

        // 애니메이터에 등록된 모든 클립을 뒤짐
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            // 이름에 Attack이나 Fire가 포함된 클립을 찾음
            if (clip.name.Contains("Attack") || clip.name.Contains("attack") || 
                clip.name.Contains("Fire") || clip.name.Contains("fire"))
            {
                return clip.length; // 길이 반환
            }
        }
        return 0f; // 못 찾음
    }

    void Update()
    {
        if (!isLive) return; // 죽었으면 동작 정지

        // 목적지 설정 (Scanner가 없거나 타겟이 없을 때)
        if (destination != null)
        {
            aiPath.destination = destination.position;
        }

        // 1. 스캐너로 가장 가까운 적 찾기
        Transform target = scanner != null ? scanner.nearestTarget : null;

        if (target != null) // 적이 있다면
        {
            if (isMeleeAttacker) // [화염방사기 / 근접 유닛]
            {
                float distance = Vector2.Distance(transform.position, target.position);
                
                // 사거리 안에 들어왔는지 확인
                if (distance <= meleeAttackRange)
                {
                    // 이동 멈춤
                    aiPath.canMove = false;
                    
                    // [✨ 핵심 수정] 쿨타임 체크! (이제 난사되지 않음)
                    if (Time.time - lastFireTime > fireCooldown)
                    {
                        MeleeAttack(target); // 공격 함수 호출
                        lastFireTime = Time.time; // 마지막 공격 시간 갱신
                    }
                }
                else
                {
                    // 사거리 밖이면 다시 이동
                    aiPath.canMove = true;
                    aiPath.destination = target.position;
                }
            }
            else // [원거리 유닛]
            {
                aiPath.canMove = false;
                if (Time.time - lastFireTime > fireCooldown)
                {
                    Fire(target);
                    lastFireTime = Time.time;
                }
            }
        }
        else // 적이 없다면
        {
            // 다시 이동 가능하게 하고 원래 목적지로
            aiPath.canMove = true;
            if (destination != null)
            {
                aiPath.destination = destination.position;
            }
        }
        
        // --- 애니메이션 속도 및 방향 설정 ---
        Vector2 currentVelocity = aiPath.velocity; 
        float animSpeed = currentVelocity.magnitude;
        anim.SetFloat("Speed", animSpeed);

        // 움직이는 중이면 이동 방향을 봄
        if (animSpeed > 0.1f)
        {
            if (currentVelocity.magnitude > 0.01f) 
            {
                 lastDirection = currentVelocity.normalized; 
            }
        }
        // 멈춰서 공격 중이면 적을 봄
        else if (target != null) 
        {
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            lastDirection = dirToTarget;
        }

        // 애니메이터에 방향 전달 (Blend Tree용)
        anim.SetFloat("moveX", lastDirection.x);
        anim.SetFloat("moveY", lastDirection.y);
    }

    // 원거리 공격 함수
    void Fire(Transform target)
    {
        if(!isLive) return;
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

    // 근접(화염방사) 공격 함수
    void MeleeAttack(Transform target)
    {
        if(!isLive) return;

        // [참고] 화염방사기는 보통 애니메이션에서 콜라이더를 켜는 방식이므로
        // Instantiate가 필요 없을 수 있습니다. 
        // 만약 필요하다면 아래 주석을 해제하세요.
        /*
        if (meleeAttackObj != null) 
        {
             Instantiate(meleeAttackObj, transform.position, Quaternion.identity);
        }
        */
        
        Debug.Log("Melee Attack! (화염 발사)"); 
        anim.SetTrigger("Attack"); // 애니메이션 실행 -> 애니메이션이 화염 콜라이더를 켬
    }

    // 에디터에서 사거리 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
    }

    // 데미지 받는 함수
    public void TakeDamage(int damageAmount)
    {
        if (!isLive) return; 

        currentHp -= damageAmount;
        Debug.Log("IS_Fireunit 피격! 남은 체력: " + currentHp);

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die(); 
        }
    }

    // 사망 처리 함수
    private void Die() 
    {
        if (!isLive) return; 
        isLive = false;

        Debug.Log("IS_Fireunit 사망");
        
        // 애니메이션 및 물리 정리
        anim.ResetTrigger("Attack");
        if(aiPath != null) aiPath.enabled = false; 

        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.isKinematic = true;        
        }
        
        anim.SetTrigger("Die");
        
        // 1.5초 뒤 오브젝트 삭제
        Destroy(gameObject, 1.5f);
    }

    // 충돌 감지 (적의 공격을 맞았을 때)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyMeleeHitbox enemyHitbox = collision.GetComponent<EnemyMeleeHitbox>();
        if (enemyHitbox != null)
        {
            TakeDamage(enemyHitbox.damage);
        }
    }
}