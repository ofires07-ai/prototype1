using UnityEngine;

// 이 스크립트는 몬스터 프리팹에 연결되어야 합니다.
public class Enemy_Y : MonoBehaviour
{
    // 1. 웨이브 관리를 위해 SpawnManager가 할당할 몬스터의 타입 ID
    // 이 필드가 있어야 SpawnManager가 몬스터를 생성할 때 ID를 할당할 수 있습니다.
    [HideInInspector] // Inspector에 표시할 필요가 없으므로 숨깁니다.
    public string enemyID = "Default"; 

    // 2. 몬스터의 기본 스탯
    public int currentHP = 5;
    public int attackDamage = 1;

    // TODO: 몬스터 이동 속도, Pathfinding 로직 등이 여기에 추가됩니다.
    
    void Start()
    {
        // 몬스터의 초기 설정
    }

    // --- 데미지 처리 ---
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            Die();
        }
        // TODO: 피격 시 비주얼/사운드 피드백 추가
    }

    // --- 사망 처리 ---
    private void Die()
    {
        // 몬스터가 죽었을 때 SpawnManager에 해당 타입의 카운트를 줄이도록 알립니다.
        if (GameManager.Instance != null && GameManager.Instance.spawnManager != null)
        {
             GameManager.Instance.spawnManager.OnMonsterDied(enemyID);
        }
        
        // TODO: 자원 드랍 로직 (GameManager.Instance.AddResource 등)
        
        Destroy(gameObject);
    }
    
    // --- 기지 도착 처리 (예시: Collider를 트리거로 설정) ---
    private void OnTriggerEnter(Collider other)
    {
        // 몬스터가 플레이어의 기지에 도착했을 때
        if (other.CompareTag("Base")) 
        {
            // 기지에 피해를 입히고 자폭
            GameManager.Instance.TakeDamage(attackDamage);
            Destroy(gameObject); 
        }
    }
    
    // 2D 환경을 사용한다면 OnTriggerEnter2D를 사용해야 합니다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Base"))
        {
            GameManager.Instance.TakeDamage(attackDamage);
            Destroy(gameObject);
        }
    }
}