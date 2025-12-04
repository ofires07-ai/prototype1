using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HY_SlowArea : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("감속 비율 (0.5 = 50% 속도로 느려짐)")]
    [SerializeField] public float slowFactor = 0.5f;

    [Header("데미지 설정")]
    [Tooltip("틱당 데미지")]
    [SerializeField] public int damage = 1;

    [Tooltip("데미지 간격")]
    [SerializeField] public float damageInterval = 1.0f;

    private List<ISlowable> affectedEnemies = new List<ISlowable>();
    private Coroutine damageCoroutine;

    // [중요] 여기에 있던 slowDebuffCount는 적 스크립트로 이동합니다! 여기선 삭제하세요.

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ISlowable enemy = collision.GetComponent<ISlowable>();

        if (enemy != null)
        {
            if (!affectedEnemies.Contains(enemy))
            {
                affectedEnemies.Add(enemy);
                enemy.ApplySlow(slowFactor); // 적에게 "너 느려져!" 명령
            }

            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DamageRoutine());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        ISlowable enemy = collision.GetComponent<ISlowable>();

        if (enemy != null && affectedEnemies.Contains(enemy))
        {
            RemoveEnemy(enemy); // 적에게 "너 이제 나가도 돼(슬로우 해제 시도)" 명령
        }
    }

    private IEnumerator DamageRoutine()
    {
        while (affectedEnemies.Count > 0)
        {
            yield return new WaitForSeconds(damageInterval);

            for (int i = affectedEnemies.Count - 1; i >= 0; i--)
            {
                ISlowable target = affectedEnemies[i];

                if (target == null || (target as MonoBehaviour) == null) 
                {
                    affectedEnemies.RemoveAt(i);
                    continue;
                }
                target.TakeDamage(damage);
            }
        }
        damageCoroutine = null;
    }

    private void RemoveEnemy(ISlowable enemy)
    {
        enemy.RemoveSlow();
        affectedEnemies.Remove(enemy);
    }

    private void OnDisable()
    {
        foreach (ISlowable enemy in affectedEnemies)
        {
            if (enemy != null && (enemy as MonoBehaviour) != null)
            {
                enemy.RemoveSlow();
            }
        }
        affectedEnemies.Clear();
        damageCoroutine = null;
    }
}