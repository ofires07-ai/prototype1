using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HY_Scanner))] // 스캐너가 없으면 자동으로 추가
public class HY_SlowTower : MonoBehaviour
{
    [Header("타워 설정")]
    [Tooltip("감속 비율 (0.75 = 75% 속도)")]
    [SerializeField] private float slowFactor = 0.75f;

    private HY_Scanner scanner;
    
    // "직전 프레임"까지 슬로우를 걸고 있던 적들의 목록
    private HashSet<HY_EnemyUnitMovement> previousTargets = new HashSet<HY_EnemyUnitMovement>();

    void Start()
    {
        scanner = GetComponent<HY_Scanner>();
        
        // 스캐너의 범위를 타워의 공격/효과 범위로 설정 (필요시)
        // scanner.scanRage = 3.0f; 
    }

    void Update()
    {
        // 1. 스캐너가 감지한 물체들을 가져옵니다.
        RaycastHit2D[] hits = scanner.targets;
        
        // "현재 프레임"에 범위 안에 있는 적들의 목록 (중복 방지용 HashSet 사용)
        HashSet<HY_EnemyUnitMovement> currentTargets = new HashSet<HY_EnemyUnitMovement>();

        // 2. 스캔된 대상들 중에서 적(Enemy) 컴포넌트만 추출
        if (hits != null)
        {
            foreach (RaycastHit2D hit in hits)
            {
                // 충돌체가 없거나 자기 자신이 감지된 경우 패스
                if (hit.collider == null) continue;

                HY_EnemyUnitMovement enemy = hit.collider.GetComponent<HY_EnemyUnitMovement>();
                
                // 적 스크립트가 있고, 살아있는 경우만 처리
                if (enemy != null)
                {
                    currentTargets.Add(enemy);
                }
            }
        }

        // 3. [핵심 로직] 범위에서 "나간" 적 처리
        // (어제는 있었는데, 오늘은 없는 애들 = 나간 애들)
        foreach (HY_EnemyUnitMovement enemy in previousTargets)
        {
            // 적이 죽어서(null) 사라졌을 수도 있으니 체크
            if (enemy != null && !currentTargets.Contains(enemy))
            {
                enemy.RemoveSlow(); // 슬로우 해제
            }
        }

        // 4. [핵심 로직] 범위에 "있는" 적 처리
        // (오늘 있는 애들 = 슬로우 걸어야 할 애들)
        foreach (HY_EnemyUnitMovement enemy in currentTargets)
        {
            if (enemy != null)
            {
                enemy.ApplySlow(slowFactor); // 슬로우 적용 (반복 호출돼도 변수 덮어쓰기라 괜찮음)
            }
        }

        // 5. 현재 목록을 다음 프레임 비교를 위해 저장
        previousTargets = currentTargets;
    }
    
    // 타워가 파괴되거나 비활성화될 때, 잡고 있던 애들을 놔줘야 함
    void OnDisable()
    {
        foreach (HY_EnemyUnitMovement enemy in previousTargets)
        {
            if (enemy != null) enemy.RemoveSlow();
        }
        previousTargets.Clear();
    }
}