using UnityEngine;

public class HY_MeleeAttack : MonoBehaviour
{
    public int damage = 1; // 근접 공격의 데미지
    public float duration = 0.2f; // 공격 판정 시간

    void Start()
    {
        // 지정된 시간 후에 오브젝트 파괴
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            HY_Enemy enemy = collision.GetComponent<HY_Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
