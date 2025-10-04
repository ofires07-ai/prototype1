using UnityEngine;

public class Enemy : MonoBehaviour
{
    // --- 팀원이 채워야 할 부분 ---
    [Header("Enemy 스탯")]
    public float maxHealth = 10f;
    public float moveSpeed = 1f;
    public int goldReward = 10;
    // ----------------------------
    
    private float currentHealth;
    private int currentWaypointIndex = 0;
    private PathManager pathManager;

    void Start()
    {
        currentHealth = maxHealth;
        // 경로 관리자 찾기 (Scene에 하나만 있다고 가정)
        pathManager = FindObjectOfType<PathManager>(); 
        if (pathManager == null) Debug.LogError("PathManager를 찾을 수 없습니다!");

        // GameManager에 자신을 등록 (웨이브 관리용)
        GameManager.Instance.activeEnemies.Add(this);
    }

    void Update()
    {
        // **팀원이 구현할 이동 로직:**
        // if (currentWaypointIndex < pathManager.GetPathLength())
        // {
        //     Vector3 targetPos = pathManager.GetWaypointPosition(currentWaypointIndex);
        //     transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        //
        //     if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        //     {
        //         currentWaypointIndex++;
        //     }
        // }
        // else
        // {
        //     // 최종 목적지 도달 로직
        //     GameManager.Instance.TakeDamage(1); // 기지 피해
        //     Die(); 
        // }
    }

    // 공격 유닛/타워로부터 피해를 입을 때 호출
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        GameManager.Instance.AddGold(goldReward); // 자원 획득
        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.EnemyDied(this); // 웨이브 관리자에게 사망 통보
        }
        
        Destroy(gameObject);
    }
}