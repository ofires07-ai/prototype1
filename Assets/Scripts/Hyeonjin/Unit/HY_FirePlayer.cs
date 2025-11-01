using UnityEngine;

// FirePlayer 클래스는 Enemy가 감지되면 플레이어 위치에 불(Fire) 오브젝트를 생성하고, Enemy가 불에 닿으면 사라지게 하는 플레이어 캐릭터입니다.
public class HY_FirePlayer : MonoBehaviour
{
    // Rigidbody2D: 2D 물리 엔진에서 속도, 힘 등을 제어하는 컴포넌트
    Rigidbody2D rigid;
    // Animator: 애니메이션 상태를 제어하는 컴포넌트
    Animator anim;
    // Scanner: 주변 Enemy를 감지하는 사용자 정의 컴포넌트
    HY_Scanner scanner;

    // firePrefab: 생성할 Fire(불) 프리팹 오브젝트를 Inspector에서 할당
    public GameObject firePrefab;
    // fireCooldown: 불 생성 쿨타임(초 단위, 연속 생성 방지)
    public float fireCooldown = 0.5f;
    // lastFireTime: 마지막으로 불을 생성한 시간
    float lastFireTime;

    // 컴포넌트 초기화 (게임 오브젝트가 생성될 때 1회 실행)
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>(); // Rigidbody2D 컴포넌트 가져오기
        anim = GetComponent<Animator>();      // Animator 컴포넌트 가져오기
        scanner = GetComponent<HY_Scanner>();    // Scanner 컴포넌트 가져오기
    }

    // FixedUpdate: 물리 연산이 일정한 시간 간격으로 실행됨 (이동, 감지, 발사 등)
    void FixedUpdate()
    {
        float speed = 2f; // 플레이어의 이동 속도
        Vector2 moveVec = Vector2.right * speed; // 오른쪽으로 이동하는 벡터

        // Enemy가 감지되면 플레이어는 멈추고, 쿨타임마다 Fire를 생성
        if (scanner != null && scanner.nearestTarget != null)
        {
            rigid.linearVelocity = Vector2.zero; // 플레이어 멈춤
            if (Time.time - lastFireTime > fireCooldown)
            {
                Fire(scanner.nearestTarget); // Enemy를 향해 Fire
                lastFireTime = Time.time; // 마지막 발사 시간 갱신
            }
        }
        else
        {
            rigid.linearVelocity = moveVec; // Enemy가 없으면 계속 이동
        }

        // 애니메이터에 속도값 전달 (0이면 Idle 애니메이션, 0보다 크면 이동 애니메이션)
        float animSpeed = rigid.linearVelocity.magnitude;
        if (animSpeed <= 0.01f) animSpeed = 0f; // 미세한 값은 0으로 처리
        anim.SetFloat("Speed", animSpeed);
    }

    // Fire: Enemy가 감지되면 플레이어 위치에 Fire 프리팹을 생성하고, 움직이지 않게 고정
    // target 파라미터는 현재 사용하지 않지만, 향후 방향 지정 등에 활용 가능
    void Fire(Transform target)
    {
        if (firePrefab == null) 
        {
            Debug.LogWarning("Fire Prefab이 할당되지 않았습니다!");
            return;
        }
       
        // 플레이어 위치에 Fire 생성
        GameObject fire = Instantiate(firePrefab, transform.position, Quaternion.Euler(0, 90, 0));
        
        Rigidbody2D rb = fire.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Fire가 움직이지 않도록 속도 0
            rb.bodyType = RigidbodyType2D.Kinematic; // 물리적으로 고정
        }
    }
}