using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FR-RU-006: 생산 타워 로직
/// 일정 시간마다 자동으로 전투 유닛을 생성(Instantiate)합니다. 
/// </summary>
public class ProductionTower : MonoBehaviour
{
    [Header("유닛 생산 설정")]
    [Tooltip("생성할 전투 유닛의 프리팹 (Prefab)")]
    public GameObject unitPrefab;

    [Tooltip("유닛 생산에 걸리는 시간 (초)")]
    public float productionTime = 5.0f;

    [Tooltip("이 타워가 동시에 유지할 수 있는 최대 유닛 수")]
    public int maxUnits = 5;

    [Header("유닛 이동 설정 (FR-RU-007)")]
    [Tooltip("유닛이 생성될 위치 (지정하지 않으면 타워 위치)")]
    public Transform spawnPoint;

    [Tooltip("생성된 유닛이 이동할 집결 지점 ('깃발')")]    
    public Transform rallyPoint;

    // 이 타워가 생성한 유닛들을 추적하는 리스트
    private List<GameObject> producedUnits = new List<GameObject>();
    
     [Tooltip("'미미한 값'의 범위 (Inspector에서 조절)")]    
    public float spawnRadiusOffset = 0.5f; 

    void Start()
    {
        // 필수 항목 검사
        if (unitPrefab == null)
        {
            Debug.LogError($"[ProductionTower] {gameObject.name}: 'Unit Prefab'이(가) 할당되지 않았습니다. 유닛 생산을 시작할 수 없습니다.");
            return;
        }

        if (rallyPoint == null)
        {
            Debug.LogWarning($"[ProductionTower] {gameObject.name}: 'Rally Point'(깃발)가 설정되지 않았습니다. 유닛이 생성 위치에 머무를 수 있습니다.");
        }
        
        // 게임이 시작되면 유닛 생산 코루틴을 자동으로 시작 
        StartCoroutine(ProduceUnitsRoutine());
    }

    /// <summary>
    /// 일정 시간마다 유닛을 생성하는 코루틴
    /// </summary>
    private IEnumerator ProduceUnitsRoutine()
    {
        // 이 타워가 활성화되어 있는 동안 무한 반복
        while (true)
        {
            // 1. 유닛 리스트 정리 (이미 파괴된 유닛 제거)
            CleanUpUnitList();

            // 2. 최대 유닛 수 확인
            if (producedUnits.Count < maxUnits)
            {
                // 3. 유닛 생성
                SpawnUnit();
            }

            Debug.LogError($"[ProductionTower] {producedUnits.Count}: 현재 생산된 유닛 수");
            
            // 4. 다음 생산까지 대기
            yield return new WaitForSeconds(productionTime);
        }
    }

    /// <summary>
    /// 유닛을 생성하고 설정하는 함수
    /// </summary>
    // (이 스크립트는 'ProductionTower' 또는 'UnitSpawner'라고 가정합니다)

// ... (unitPrefab, spawnPoint 등 변수 선언) ...


/// <summary>
/// 유닛 1기를 스폰하고, Circle 경로 + 깃발 위치를 설정합니다.
/// </summary>
private void SpawnUnit()
{
    // --- 1. 스폰 위치 계산 (콜라이더 충돌 방지) ---
    // 기본 생성 위치 결정
    Vector3 basePos = (spawnPoint != null) ? spawnPoint.position : transform.position;
    // 미미한 랜덤값 적용
    Vector2 randomOffset = Random.insideUnitCircle * spawnRadiusOffset;
    Vector3 finalSpawnPos = new Vector3(basePos.x + randomOffset.x, basePos.y + randomOffset.y, basePos.z);

    // --- 2. 유닛 생성 ---
    // (이제 밀려남 없이 안전한 위치에 스폰됩니다)
    GameObject newUnitGO = Instantiate(unitPrefab, finalSpawnPos, Quaternion.identity);

    // (리스트 추가 등... 기존 로직)
    // producedUnits.Add(newUnitGO);

    // --- 3. [핵심] 유닛에게 경로 설정하기 ---
    
    // (A) 생성된 유닛의 UnitMovement 스크립트를 가져옵니다.
    HY_UnitMovement unitMovement = newUnitGO.GetComponent<HY_UnitMovement>();
    
    if (unitMovement != null)
    {
        // (B) [중요] FlagManager의 '싱글톤 인스턴스'에 접근하여
        //     '현재 깃발'의 Transform을 가져옵니다.
        //     (FlagManager.cs)
        Transform currentFlag = FlagManager.Instance.currentSpawnFlag; 

        // (C) 깃발이 (아직) 생성되지 않았을 수도 있으니 확인합니다.
        if (currentFlag != null)
        {
            // (D) 유닛의 SetRallyPoint 함수를 호출하여
            //     "Circle 경로 + 깃발"의 최종 경로를 완성시킵니다.
            unitMovement.SetRallyPoint(currentFlag);
        }
        else
        {
            Debug.LogWarning($"[ProductionTower] FlagManager.Instance.currentSpawnFlag가 null이라 깃발을 설정할 수 없습니다. (유닛이 Circle 경로만 이동합니다)");
        }
    }
    else
    {
        Debug.LogWarning($"[ProductionTower] 생성된 유닛 {newUnitGO.name}에 UnitMovement 스크립트가 없습니다.");
    }
}

    /// <summary>
    /// 리스트를 순회하며 파괴된 (null) 유닛들을 제거합니다.
    /// </summary>
    private void CleanUpUnitList()
    {
        // 리스트에서 null (파괴된) 유닛들을 제거
        producedUnits.RemoveAll(unit => unit == null);
    }
}