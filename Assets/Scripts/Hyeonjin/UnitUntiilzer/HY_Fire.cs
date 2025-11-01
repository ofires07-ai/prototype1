using UnityEngine;

// Fire 프리팹의 메인 스크립트
// Enemy를 감지하고 제거하는 역할
public class Fire : MonoBehaviour
{
    public float lifetime = 3f; // 불의 지속 시간
    public float checkInterval = 0.1f; // 감지 주기(초)
    private float timer;

    void Start()
    {
        Destroy(gameObject, lifetime); // 일정 시간 후 Fire 자동 제거
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            KillEnemiesInArea();
        }
    }

    // 불의 시각적 크기만큼 OverlapBox로 Enemy 감지 및 제거
    void KillEnemiesInArea()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector2 size = sr != null ? (Vector2)sr.bounds.size : Vector2.one;
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size, 0f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemyScript = hit.GetComponent<Enemy>();
                if (enemyScript != null)
                    enemyScript.Die();
                Destroy(hit.gameObject);
            }
        }
    }
}