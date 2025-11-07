// 'HY_Enemy_MeleeAttack.cs' (새 스크립트 또는 복사본)
using UnityEngine;

public class HY_Enemy_MeleeAttack : MonoBehaviour
{
    public int damage = 10; // 적의 공격력
    public float duration = 0.2f; // 공격 판정 시간

    void Start()
    {
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // [수정됨] 적의 공격은 "Player" 태그를 감지해야 합니다!
        if (collision.CompareTag("Player"))
        {
            // [수정됨] 플레이어의 스크립트 (예: HY_Player)를 가져옵니다.
            HY_Player player = collision.GetComponent<HY_Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            // (선택) 한 번만 때리도록, 맞춘 즉시 히트박스를 파괴할 수도 있습니다.
            // Destroy(gameObject); 
        }
    }
}