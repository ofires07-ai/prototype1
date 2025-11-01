using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FR-RU-006: 생산 타워 로직
/// 일정 시간마다 자동으로 전투 유닛을 생성(Instantiate)합니다. 
/// </summary>
public class MakingTower : MonoBehaviour
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

            // 4. 다음 생산까지 대기
            yield return new WaitForSeconds(productionTime);
        }
    }

    /// <summary>
    /// 유닛을 생성하고 설정하는 함수
    /// </summary>
    private void SpawnUnit()
    {
        // 유닛 생성 위치 결정
        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : transform.position;

        // 전투 유닛을 생성(Instantiate)
        GameObject newUnitGO = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        // 생성된 유닛을 리스트에 추가
        producedUnits.Add(newUnitGO);

        // UnitMovement 스크립트가 있는지 확인
        HY_UnitMovement unitMovement = newUnitGO.GetComponent<HY_UnitMovement>();
        if (unitMovement != null && rallyPoint != null)
        {
            // 유닛에게 집결지(깃발) 위치를 설정해줍니다.
            unitMovement.SetRallyPoint(rallyPoint);
            Debug.Log($"[ProductionTower] {newUnitGO.name}에게 rally point 설정 완료: {rallyPoint.position}");
        }
        else if (unitMovement == null)
        {
            Debug.LogWarning($"[ProductionTower] 생성된 유닛 {newUnitGO.name}에 HY_UnitMovement 스크립트가 없습니다. 유닛이 이동하지 않을 수 있습니다.");
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