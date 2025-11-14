using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FR-RU-006: 생산 타워 로직
/// [수정] 깃발(RallyPoint)을 외부에서 수정하지 못하도록 보호합니다.
/// [수정] 스폰된 유닛에게 자신(타워)을 '주인(Owner)'으로 등록시킵니다.
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

    [Tooltip("'미미한 값'의 범위 (Inspector에서 조절)")]
    public float spawnRadiusOffset = 0.5f;

    // --- 깃발(RallyPoint) 보안 ---
    // 1. 실제 데이터는 private 변수에 저장
    private Transform _rallyPoint;

    // 2. 외부에는 이 '속성(Property)'을 공개
    // 외부에서 '읽기(get)'는 가능하지만, '쓰기(set)'는 오직 이 스크립트 내부에서만 가능!
    public Transform RallyPoint
    {
        get { return _rallyPoint; }
        private set { _rallyPoint = value; }
    }
    // ---

    // 이 타워가 생산한 유닛들을 관리하는 리스트
    private List<GameObject> producedUnits = new List<GameObject>();

    // 생산 코루틴의 중복 시작을 방지하기 위한 플래그
    private bool isProductionStarted = false;

    void Start()
    {
        // 필수 항목 검사
        if (unitPrefab == null)
        {
            Debug.LogError($"[ProductionTower] {gameObject.name}: 'Unit Prefab'이(가) 할당되지 않았습니다.");
            // unitPrefab이 없으면 이 컴포넌트의 작동을 멈춥니다.
            this.enabled = false;
            return;
        }

        // 깃발 관리자(FlagManager)의 명령을 받을 때까지 자동 시작을 보류합니다.
    }

    /// <summary>
    /// [핵심] FlagManager가 깃발 설치/이동 시 호출하는 "생산 시작/업데이트" 명령 함수
    /// </summary>
    public void StartProduction(Transform newRallyPoint)
    {
        // 1. 깃발(최종 목적지)을 안전하게 설정합니다.
        // (깃발이 이동하면 이 값이 업데이트됩니다)
        this.RallyPoint = newRallyPoint;

        // 2. 생산 코루틴이 '처음' 호출될 때만 코루틴을 시작합니다.
        if (!isProductionStarted)
        {
            isProductionStarted = true;
            Debug.Log($"[ProductionTower] {name}: 깃발({RallyPoint.name}) 설정을 받고 유닛 생산을 시작합니다.");
            StartCoroutine(ProduceUnitsRoutine());
        }
        // else: 이미 생산 중이라면 깃발 위치만 업데이트되고,
        // 유닛들은 깃발 방송(Event)을 통해 새 위치를 알게 됩니다.
    }

    /// <summary>
    /// 일정 시간마다 유닛을 생성하는 코루틴
    /// </summary>
    private IEnumerator ProduceUnitsRoutine()
    {
        // 이 타워가 활성화되어 있는 동안 계속 반복
        while (true)
        {
            // 1. 리스트 정리 (파괴된 유닛 제거)
            CleanUpUnitList();

            // 2. 최대 유닛 수 확인
            if (producedUnits.Count < maxUnits)
            {
                // 3. 유닛 생성
                SpawnUnit();
                Debug.Log($"[ProductionTower] {producedUnits.Count + 1}: 현재 생산된 유닛 수"); // +1은 방금 스폰했기 때문
            }

            // 4. 다음 생산까지 대기
            yield return new WaitForSeconds(productionTime);
        }
    }

    /// <summary>
    /// 유닛 1기를 스폰하고, 경로와 "주인"을 설정합니다.
    /// </summary>
    private void SpawnUnit()
    {
        // --- 1. 스폰 위치 계산 (유닛끼리 겹치지 않게 약간 랜덤) ---
        Vector3 basePos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadiusOffset;
        Vector3 finalSpawnPos = new Vector3(basePos.x + randomOffset.x, basePos.y + randomOffset.y, basePos.z);

        // --- 2. 유닛 생성 및 리스트 추가 ---
        GameObject newUnitGO = Instantiate(unitPrefab, finalSpawnPos, Quaternion.identity);
        producedUnits.Add(newUnitGO);

        // --- 3. 유닛에게 정보 전달 ---
        HY_UnitMovement unitMovement = newUnitGO.GetComponent<HY_UnitMovement>();

        if (unitMovement != null)
        {
            Transform currentFlag = this.RallyPoint;

            if (currentFlag != null)
            {
                // [정보 전달 1] "너의 목적지는 여기(currentFlag)야"
                unitMovement.SetRallyPoint(currentFlag);

                // [정보 전달 2] "그리고 너의 주인(Owner)은 바로 나(this)야"
                unitMovement.SetOwnerTower(this);
            }
            else
            {
                Debug.LogWarning($"[ProductionTower] {name}: 깃발(RallyPoint)이 null이라 유닛에게 전달하지 못했습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[ProductionTower] 생성된 유닛 {newUnitGO.name}에 HY_UnitMovement 스크립트가 없습니다.");
        }
    }

    /// <summary>
    /// 리스트를 순회하며 파괴된 (null이 된) 유닛들을 제거합니다.
    /// </summary>
    private void CleanUpUnitList()
    {
        // 리스트에서 'null' (파괴되었거나 사라진) 항목을 모두 제거
        producedUnits.RemoveAll(unit => unit == null);
    }
}