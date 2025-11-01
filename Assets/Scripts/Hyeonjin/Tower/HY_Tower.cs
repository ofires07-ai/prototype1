using UnityEngine;

public class HY_Tower : MonoBehaviour
{
    [Header("Tower Settings")]
    public GameObject bulletObj;           // 발사할 총알 프리팹
    public float fireCooldown = 0.5f;      // 발사 쿨타임
    public float bulletForce = 10f;        // 총알 발사 힘
    public float rotationSpeed = 10f;      // 총알 회전 속도

    private HY_Scanner scanner;               // 적 감지용 스캐너
    private float lastFireTime;            // 마지막 발사 시간
    private Animator anim;                 // 애니메이터 컴포넌트

    void Awake()
    {
        scanner = GetComponent<HY_Scanner>();
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (scanner != null && scanner.nearestTarget != null)
        {
            // 적이 감지되면 해당 방향으로 회전
            Vector2 direction = scanner.nearestTarget.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // 쿨타임 내에만 발사
            if (Time.time - lastFireTime > fireCooldown)
            {
                Fire(scanner.nearestTarget);
                lastFireTime = Time.time;
            }
        }

        // 애니메이터가 있다면 상태 업데이트
        if (anim != null)
        {
            anim.SetBool("IsShooting", scanner != null && scanner.nearestTarget != null);
        }
    }

    void Fire(Transform target)
    {
        if (bulletObj == null || target == null) return;

        Vector2 dir = (target.position - transform.position).normalized;
        
        // 발사 방향에 따른 회전 각도 계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        // 총알 생성 및 컴포넌트 설정
        GameObject bullet = Instantiate(bulletObj, transform.position, Quaternion.Euler(0, 0, angle));
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.gravityScale = 0f;  // 2D에서 중력 영향 제거
            rb.AddForce(dir * bulletForce, ForceMode2D.Impulse);
            
            // 총알 회전을 위한 Bullet 스크립트 추가
           // StartBulletRotation(bullet);
        }
    }

    private void StartBulletRotation(GameObject bullet)
    {
        // Bullet 스크립트가 없다면 추가
  
      HY_Bullet bulletComponent = bullet.GetComponent<HY_Bullet>();
        if (bulletComponent == null)
        {
            bulletComponent = bullet.AddComponent<HY_Bullet>();
        }
        
        // 회전 속도 설정
        //bulletComponent.rotationSpeed = rotationSpeed;
    }
}

