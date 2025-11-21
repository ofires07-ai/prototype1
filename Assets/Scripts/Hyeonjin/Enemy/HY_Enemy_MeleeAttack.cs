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
        Debug.Log("MeleeAttack!");
        // 1. Player인지 확인
        HY_Player player = collision.GetComponent<HY_Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
            // (선택) 한 번 공격으로 하나의 대상만 때리고 싶다면 여기서 return;
        }

        // 2. SpaceShip인지 확인 (Player가 아니었다면 이어서 실행됨)
        SpaceShip spaceship = collision.GetComponent<SpaceShip>();
        if (spaceship != null)
        {
            spaceship.TakeDamage(damage);
        }
    }
}