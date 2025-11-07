using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FR-RU-006: 생산 타워 로직
/// [수정됨] FlagManager의 명령을 받아 유닛 생산을 시작합니다.
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
    public Transform rallyPoint; // [수정됨] 이 변수는 이제 StartProduction을 통해 채워집니다.

    private List<GameObject> producedUnits = new List<GameObject>();
    
    [Tooltip("'미미한 값'의 범위 (Inspector에서 조절)")]    
    public float spawnRadiusOffset = 0.5f; 

    // [추가됨] 생산이 시작되었는지 확인 (중복 시작 방지)
    private bool isProductionStarted = false;

    void Start()
    {
        // 필수 항목 검사
        if (unitPrefab == null)
        {
            Debug.LogError($"[ProductionTower] {gameObject.name}: 'Unit Prefab'이(가) 할당되지 않았습니다.");
            return;
        }

        // [제거됨] FlagManager의 명령을 받을 때까지 자동 시작을 보류합니다.
    }

    /// <summary>
    /// [추가됨] FlagManager가 깃발 설치 완료 시 호출하는 "생산 시작" 명령 함수
    /// </summary>
    public void StartProduction(Transform newRallyPoint)
    {
        // 중복 시작 방지
        if (isProductionStarted) return;

        // 1. 깃발(최종 목적지)을 안전하게 설정합니다.
        this.rallyPoint = newRallyPoint;
        isProductionStarted = true;

        Debug.Log($"[ProductionTower] {name}: 깃발({rallyPoint.name}) 설정을 받고 유닛 생산을 시작합니다.");

        // 2. 이제서야 유닛 생산 코루틴을 '안전하게' 시작합니다.
        StartCoroutine(ProduceUnitsRoutine());
    }

    /// <summary>
    /// 일정 시간마다 유닛을 생성하는 코루틴
    /// </summary>
    private IEnumerator ProduceUnitsRoutine()
    {
        while (true)
        {
            // 1. 리스트 정리
            CleanUpUnitList();

            // 2. 최대 유닛 수 확인
            if (producedUnits.Count < maxUnits)
            {
                // 3. 유닛 생성
                SpawnUnit();
                Debug.LogError($"[ProductionTower] {producedUnits.Count}: 현재 생산된 유닛 수");
            }
            
            // 4. 다음 생산까지 대기
            yield return new WaitForSeconds(productionTime);
        }
    }

    /// <summary>
    /// 유닛 1기를 스폰하고, Circle 경로 + 깃발 위치를 설정합니다.
    /// </summary>
    private void SpawnUnit()
    {
        // --- 1. 스폰 위치 계산 (콜라이더 충돌 방지) ---
        Vector3 basePos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadiusOffset;
        Vector3 finalSpawnPos = new Vector3(basePos.x + randomOffset.x, basePos.y + randomOffset.y, basePos.z);

        // --- 2. 유닛 생성 ---
        GameObject newUnitGO = Instantiate(unitPrefab, finalSpawnPos, Quaternion.identity);
        producedUnits.Add(newUnitGO);

        // --- 3. [핵심 수정] 유닛에게 경로 설정하기 ---
        HY_UnitMovement unitMovement = newUnitGO.GetComponent<HY_UnitMovement>();
        
        if (unitMovement != null)
        {
            // [수정됨] FlagManager에 직접 접근하는 대신,
            // StartProduction에서 안전하게 전달받은 'this.rallyPoint' 변수를 사용합니다.
            Transform currentFlag = this.rallyPoint; 

            if (currentFlag != null)
            {
                unitMovement.SetRallyPoint(currentFlag);
            }
            else
            {
                // (이론상 이 경고는 이제 절대 뜨지 않아야 합니다)
                Debug.LogWarning($"[ProductionTower] this.rallyPoint가 null입니다. (StartProduction이 호출되지 않았거나, 깃발이 null로 전달됨)");
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
        producedUnits.RemoveAll(unit => unit == null);
    }
}