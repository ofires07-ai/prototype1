using UnityEngine;

public class EnemyMeleeHitbox : MonoBehaviour
{
    public int damage = 10; 
    private bool hasHitTarget = false; 

    void Start()
    {
        Destroy(gameObject, 0.1f);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitTarget || collision.CompareTag("MeleeHitbox")) return; 

        // [수정됨] 태그가 "Player"이거나 "Base"이거나 상관없이 체크
        if (collision.CompareTag("Player") || collision.CompareTag("Base")) 
        {
            // 1. 먼저 '유닛(HY_Player)'인지 확인
            HY_Player playerUnit = collision.GetComponent<HY_Player>();
            if (playerUnit != null && playerUnit.isLive)
            {
                playerUnit.TakeDamage(damage);
                hasHitTarget = true;
                Destroy(gameObject);
                return; // 공격 성공했으니 종료
            }

            // 2. 유닛이 아니라면 '기지(SpaceShip)'인지 확인
            // (태그를 Player로 해두셨더라도 여기서 걸러져서 맞게 됩니다)
            SpaceShip baseShip = collision.GetComponent<SpaceShip>();
            if (baseShip != null && !baseShip.isDestroyed)
            {
                baseShip.TakeDamage(damage);
                hasHitTarget = true;
                Destroy(gameObject);
                return; // 공격 성공했으니 종료
            }
        }
    }
}
/*using UnityEngine;

public class EnemyMeleeHitbox : MonoBehaviour // ⬅️ 클래스 이름 변경
{
    public int damage = 0; 
    private bool hasHitTarget = false; 

    void Start()
    {
        Destroy(gameObject, 0.1f);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitTarget || collision.CompareTag("MeleeHitbox")) return; 

        // [ ✨ 핵심: 오직 Player 태그만 공격합니다. ]
        if (collision.CompareTag("Player")) 
        {
            hasHitTarget = true; 
            // 데미지 적용은 상대방 스크립트가 처리합니다.
            Destroy(gameObject); 
        }

        if (collision.CompareTag("Base")) 
        {
            hasHitTarget = true; 
            // 데미지 적용은 상대방 스크립트가 처리합니다.
            Destroy(gameObject); 
        }
    }
}*/