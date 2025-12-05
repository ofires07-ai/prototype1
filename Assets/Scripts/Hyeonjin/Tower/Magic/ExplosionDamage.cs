using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    [Header("폭발 설정")]
    [Tooltip("폭발이 일어날 범위 (원의 반지름)")]
    public float explosionRadius = 3f;

    [Tooltip("폭발이 입힐 데미지")]
    public int damage = 25;

    [Tooltip("데미지를 입힐 대상의 레이어 (반드시 'Enemy'로 설정!)")]
    // [중요] 이 LayerMask를 유니티 Inspector에서 'Enemy'로 설정해야 합니다.
    public LayerMask enemyLayer;

    [Tooltip("폭발에 맞은 적의 경직(스턴) 시간 (초)")]
    public float stunDuration = 0.5f;

    // 이 오브젝트가 생성되는 즉시(Start) 1회 실행됩니다.
    void Start()
    {
        // [✨ 중요!] 인스펙터에서 Enemy Layer가 설정되었는지 확인
        if (enemyLayer.value == 0) // LayerMask가 'Nothing'일 때 value는 0입니다.
        {
            Debug.LogError("ExplosionDamage: 'Enemy Layer'가 설정되지 않았습니다! (Nothing)");
            return;
        }
        
        // 1. 폭발 범위 내의 모든 'enemyLayer' 콜라이더를 찾습니다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);

        if (hits.Length > 0)
        {
            Debug.Log(hits.Length + "개의 적을 폭발 범위 안에서 감지!"); 
        }

        // 2. 찾은 모든 콜라이더(적들)에게 데미지와 스턴을 줍니다.
        foreach (Collider2D enemyCollider in hits)
        {
            // 3-1. 근접 유닛 스크립트 시도 (기존 로직)
            HY_EnemyUnitMovement meleeEnemy = enemyCollider.GetComponent<HY_EnemyUnitMovement>();

            if (meleeEnemy != null)
            {
                // 근접 유닛에게 데미지 및 스턴 적용
                meleeEnemy.TakeDamage(damage);
                meleeEnemy.ApplyStun(stunDuration);
                continue; // 다음 콜라이더로 이동
            }

            // 3-2. 원거리 유닛 스크립트 시도 (추가된 로직)
            HY_Ranged_EnemyUnitMovement rangedEnemy = enemyCollider.GetComponent<HY_Ranged_EnemyUnitMovement>();
            
            if (rangedEnemy != null)
            {
                // 원거리 유닛에게 데미지 및 스턴 적용
                rangedEnemy.TakeDamage(damage);
                rangedEnemy.ApplyStun(stunDuration);
                continue; // 다음 콜라이더로 이동
            }
            
            // 4. 두 스크립트 모두 찾을 수 없는 경우 경고
            Debug.LogWarning("감지된 콜라이더(" + enemyCollider.name + ")에 데미지 스크립트(HY_EnemyUnitMovement 또는 HY_Ranged_EnemyUnitMovement)가 없습니다.");
        }
    }

    // (디버깅용) 씬(Scene) 뷰에서 폭발 범위를 시각적으로 보여줍니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}