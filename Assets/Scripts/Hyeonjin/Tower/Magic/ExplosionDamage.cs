using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    [Header("폭발 설정")]
    [Tooltip("폭발이 일어날 범위 (원의 반지름)")]
    public float explosionRadius = 3f;

    [Tooltip("폭발이 입힐 데미지")]
    public int damage = 25;

    [Tooltip("데미지를 입힐 대상의 레이어 (반드시 'Enemy'로 설정!)")]
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
            Debug.Log(hits.Length + "개의 적을 폭발 범위 안에서 감지!"); // [✨ 디버깅용]
        }

        // 2. 찾은 모든 콜라이더(적들)에게 데미지와 스턴을 줍니다.
        foreach (Collider2D enemyCollider in hits)
        {
            // 3. 콜라이더에서 적의 체력 스크립트(HY_EnemyUnitMovement)를 가져옵니다.
            HY_EnemyUnitMovement enemy = enemyCollider.GetComponent<HY_EnemyUnitMovement>();

            // 4. 스크립트를 찾았다면, TakeDamage와 ApplyStun을 호출합니다.
            if (enemy != null)
            {
                // [✨ 수정!] 데미지 주기 + 스턴 주기
                enemy.TakeDamage(damage);
                enemy.ApplyStun(stunDuration);
            }
            else
            {
                // [✨ 디버깅용] 
                Debug.LogWarning("감지된 콜라이더(" + enemyCollider.name + ")에 HY_EnemyUnitMovement 스크립트가 없습니다.");
            }
        }
    }

    // (디버깅용) 씬(Scene) 뷰에서 폭발 범위를 시각적으로 보여줍니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}