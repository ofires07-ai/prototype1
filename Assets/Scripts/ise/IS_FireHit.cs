using UnityEngine;
using System.Collections.Generic;

public class IS_FireHit : MonoBehaviour
{
    [Header("공격 설정")]
    public int damage = 1;
    public float damageInterval = 0.2f; // 데미지 주는 간격 (0.2초마다 틱 데미지)

    // 적마다 마지막으로 데미지 입은 시간을 기록할 딕셔너리
    private Dictionary<GameObject, float> damageTimers = new Dictionary<GameObject, float>();

    void OnEnable()
    {
        // 켜질 때마다 타이머 초기화 (새로운 공격 시작)
        damageTimers.Clear();
    }

    // 닿아있는 동안 계속 호출됨
    void OnTriggerStay2D(Collider2D collision)
    {
        // 적 태그 확인
        if (collision.CompareTag("Enemy"))
        {
            GameObject enemy = collision.gameObject;

            // 1. 처음 만난 적이거나, 2. 데미지 간격(쿨타임)이 지났으면 데미지 적용
            if (!damageTimers.ContainsKey(enemy) || Time.time >= damageTimers[enemy] + damageInterval)
            {
                // 적 스크립트 가져오기 (HY_EnemyUnitMovement라고 가정)
                HY_EnemyUnitMovement enemyScript = enemy.GetComponent<HY_EnemyUnitMovement>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(damage);
                    
                    // 타이머 갱신 (현재 시간 기록)
                    if (damageTimers.ContainsKey(enemy))
                        damageTimers[enemy] = Time.time;
                    else
                        damageTimers.Add(enemy, Time.time);
                        
                    // Debug.Log($"{enemy.name}에게 화염 데미지!");
                }
            }
        }
    }
}