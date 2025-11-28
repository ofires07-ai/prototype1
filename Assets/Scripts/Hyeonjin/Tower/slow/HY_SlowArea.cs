using UnityEngine;
using System.Collections.Generic;

public class HY_SlowArea : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("감속 비율 (0.5 = 50% 속도로 느려짐)")]
    [SerializeField] private float slowFactor = 0.5f;

    // 현재 슬로우를 걸고 있는 적들을 기억하는 리스트 (타워가 팔리거나 꺼질 때를 대비)
    private List<ISlowable> affectedEnemies = new List<ISlowable>();

    // 1. 적이 범위 내로 '들어왔을 때' (한 번만 실행됨)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // [개선] ISlowable 인터페이스를 가진 모든 컴포넌트를 찾습니다.
        ISlowable enemy = collision.GetComponent<ISlowable>();

        if (enemy != null)
        {
            // 적에게 슬로우 적용
            enemy.ApplySlow(slowFactor);
            
            // 관리 명단에 추가
            affectedEnemies.Add(enemy);
            
            Debug.Log($"{collision.name} 감속 적용!");
        }
    }

    // 2. 적이 범위 밖으로 '나갔을 때' (한 번만 실행됨)
    private void OnTriggerExit2D(Collider2D collision)
    {
        ISlowable enemy = collision.GetComponent<ISlowable>();

        if (enemy != null && affectedEnemies.Contains(enemy))
        {
            // 적의 슬로우 해제
            enemy.RemoveSlow();
            
            // 관리 명단에서 제외
            affectedEnemies.Remove(enemy);
            Debug.Log($"{collision.name} 감속 해제!");
        }
    }

    // 3. 만약 타워가 파괴되거나 비활성화되면(업그레이드 등) 잡고 있던 적들을 다 풀어줘야 함
    private void OnDisable()
    {
        foreach (ISlowable enemy in affectedEnemies)
        {
            // 적이 죽어서 null이 아닐 때만 해제
            if (enemy != null)
            {
                enemy.RemoveSlow();
            }
        }
        affectedEnemies.Clear();
    }
}