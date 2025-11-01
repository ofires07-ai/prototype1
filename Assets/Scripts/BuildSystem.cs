using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용을 위해 추가
using System.Collections.Generic; // List<T> 사용을 위해 추가
using System.Linq; // LINQ는 사용하지 않도록 제거 (오류 방지)

[System.Serializable]
public class BuildButtonUI // public 클래스로 분리
{
    // 1. 타워/유닛 아이콘을 표시할 이미지 (버튼 상단)
    public Image unitImage; 
    // 2. 자원 1 (아이콘과 텍스트) - 버튼 하단
    public Image cost1Icon;
    public TextMeshProUGUI cost1Text;
    // 3. 자원 2 (아이콘과 텍스트) - 버튼 하단
    public Image cost2Icon;
    public TextMeshProUGUI cost2Text;
    
    [HideInInspector] public int unitIndex; 
}

public class BuildSystem : MonoBehaviour
{
    // 1단계: 버튼 UI 요소들을 Inspector에서 연결하기 위한 Helper 클래스 정의
    [System.Serializable]
    public class BuildButtonUI
    {
        // 1. 타워/유닛 아이콘을 표시할 이미지 (버튼 상단)
        public Image unitImage; 

        // 2. 자원 1 (아이콘과 텍스트) - 버튼 하단
        public Image cost1Icon;
        public TextMeshProUGUI cost1Text;

        // 3. 자원 2 (아이콘과 텍스트) - 버튼 하단
        public Image cost2Icon;
        public TextMeshProUGUI cost2Text;
        
        // 이 버튼이 어떤 유닛의 인덱스를 가지고 있는지 저장합니다.
        [HideInInspector] public int unitIndex; 
    }
    
    // UI 버튼에 연결할 유닛 정보 (Scriptable Object 또는 클래스로 관리 권장)
    [System.Serializable]
    public class UnitData
    {
        public GameObject prefab;
        
        [Tooltip("Resource Costs: [T1, T2, T3, T4, T5]")]
        public int[] costs = new int[5]; // <--- 크기 5로 변경
    
        public string unitType;
    
        public Sprite unitIcon; 
    
        // T1 ~ T4 필드는 그대로 유지합니다.
        [Tooltip("비용 표시를 위한 자원 아이콘 (T1, T2, T3, T4, T5 순서)")]
        public Sprite resource1Icon; 
        public Sprite resource2Icon; 
        public Sprite resource3Icon; 
        public Sprite resource4Icon; 
    
        // ✅ T5 자원 아이콘 필드
        public Sprite resource5Icon; // T5 자원 아이콘
    
        // 편의를 위해 아이콘 배열을 만듭니다.
        public Sprite GetIconForTier(int tier)
        {
            switch (tier)
            {
                case 0: return resource1Icon; // T1
                case 1: return resource2Icon; // T2
                case 2: return resource3Icon; // T3
                case 3: return resource4Icon; // T4
                case 4: return resource5Icon;
                default: return null;
            }
        }
    }

    [Header("소환 지점")]
    public Transform playerSpawnPoint; 
    
    [Header("구매 가능 유닛 목록")]
    public UnitData[] availableUnits; 
    
    // **새로 추가:** Hierarchy의 버튼들과 연결할 UI 목록
    [Header("빌드 버튼 UI 연결")]
    public BuildButtonUI[] buildButtons; 

    // 현재 플레이어가 선택한 유닛 프리팹 (배치 준비 상태)
    private GameObject _unitToBuild; 
    
    // ❌ 크기를 5로 변경하여 UnitData 및 GameManager와 일치시킵니다.
    private int[] _unitCosts = new int[5]; 
    
    private string _selectedUnitType;
    
    void Start()
    {
        // 게임 시작 시 모든 버튼의 UI를 초기화하고 연결합니다.
        RefreshAllButtonUI();
    }
    
    // --- UI 업데이트 ---
    private void RefreshAllButtonUI()
    {
        for (int i = 0; i < buildButtons.Length; i++)
        {
            // buildButtons 배열에 연결된 버튼이 유효한 유닛 인덱스를 참조하도록 합니다.
            buildButtons[i].unitIndex = i; 
            
            if (i < availableUnits.Length)
            {
                // 유닛 데이터 가져오기
                UnitData data = availableUnits[i];

                // 1. 유닛 아이콘 설정
                buildButtons[i].unitImage.sprite = data.unitIcon;
                
                // 2. 자원 비용 설정 (최대 2개 자원 조합 표시)
                
                // 비용이 0보다 큰 자원의 티어 인덱스(0~4)를 저장합니다.
                List<int> requiredTiers = new List<int>();
                for(int tier = 0; tier < data.costs.Length; tier++)
                {
                    if (data.costs[tier] > 0)
                    {
                        requiredTiers.Add(tier);
                    }
                }
                
                // --- 10번째 버튼 (인덱스 9) 특수 처리 로직 시작 ---
                if (i == 9) 
                {
                    // T5 (인덱스 4)만 표시하도록 강제
                    if (requiredTiers.Contains(4) && data.costs[4] > 0)
                    {
                        // T5 자원만 Cost1에 표시
                        buildButtons[i].cost1Icon.sprite = data.GetIconForTier(4); 
                        buildButtons[i].cost1Text.text = data.costs[4].ToString();
                        buildButtons[i].cost1Icon.gameObject.SetActive(true);
                        buildButtons[i].cost1Text.gameObject.SetActive(true);
                        
                        // Cost2 숨김 (다른 자원 무시)
                        buildButtons[i].cost2Icon.gameObject.SetActive(false);
                        buildButtons[i].cost2Text.gameObject.SetActive(false);
                    }
                    else
                    {
                        // T5 비용이 없으면 모두 숨김
                        buildButtons[i].cost1Icon.gameObject.SetActive(false);
                        buildButtons[i].cost1Text.gameObject.SetActive(false);
                        buildButtons[i].cost2Icon.gameObject.SetActive(false);
                        buildButtons[i].cost2Text.gameObject.SetActive(false);
                    }
                }
                // --- 일반 버튼 로직 (최대 2개 비용 표시) ---
                else 
                {
                    // 자원 1 (requiredTiers[0]) 설정
                    if (requiredTiers.Count >= 1)
                    {
                        int tier1Index = requiredTiers[0];
                        
                        buildButtons[i].cost1Icon.sprite = data.GetIconForTier(tier1Index);
                        buildButtons[i].cost1Text.text = data.costs[tier1Index].ToString();
                        buildButtons[i].cost1Icon.gameObject.SetActive(true);
                        buildButtons[i].cost1Text.gameObject.SetActive(true);
                    }
                    else // 비용이 없으면 숨김
                    {
                         buildButtons[i].cost1Icon.gameObject.SetActive(false);
                         buildButtons[i].cost1Text.gameObject.SetActive(false);
                    }

                    // 자원 2 (requiredTiers[1]) 설정
                    if (requiredTiers.Count >= 2) 
                    {
                        int tier2Index = requiredTiers[1];
                        
                        buildButtons[i].cost2Icon.sprite = data.GetIconForTier(tier2Index);
                        buildButtons[i].cost2Text.text = data.costs[tier2Index].ToString();
                        buildButtons[i].cost2Icon.gameObject.SetActive(true);
                        buildButtons[i].cost2Text.gameObject.SetActive(true);
                    }
                    else // 두 번째 비용이 없으면 숨김
                    {
                         buildButtons[i].cost2Icon.gameObject.SetActive(false);
                         buildButtons[i].cost2Text.gameObject.SetActive(false);
                    }
                }
                // --- 자원 비용 설정 로직 끝 ---
            }
            else
            {
                // availableUnits 데이터가 부족하면 버튼 비활성화 (선택 사항)
                // 예: buildButtons[i].gameObject.SetActive(false);
            }
        }
    }


   // --- UI 버튼 클릭 이벤트에 연결될 함수 ---
    /// <summary>
    /// [✨ 이 함수 전체를 교체하세요]
    /// UI 버튼 클릭 시 호출됩니다. 
    /// TOWER 타입은 TowerBuildManager에게 배치를 위임하고,
    /// SOLDIER 타입은 즉시 소환합니다.
    /// </summary>
    /// <param name="unitIndex">클릭된 버튼에 해당하는 availableUnits의 인덱스</param>
    public void SelectUnitToBuild(int unitIndex)
    {
        // 0. 유효성 검사: 인덱스가 배열 범위를 벗어나지 않았는지 확인
        if (unitIndex < 0 || unitIndex >= availableUnits.Length)
        {
            Debug.LogError($"[BuildSystem] 잘못된 UnitIndex({unitIndex})가 요청되었습니다.");
            return;
        }

        // 1. 선택한 유닛의 원본 데이터(ScriptableObject 등)를 가져옵니다.
        UnitData selectedData = availableUnits[unitIndex];
        
        // 2. 이 함수에서 사용할 핵심 정보들을 임시 변수에 저장합니다.
        //    (콜백 함수가 이 변수들을 '캡처'하여 사용합니다)
        GameObject prefabToBuild = selectedData.prefab;
        string unitType = selectedData.unitType;
        
        // 3. 비용 배열을 '깊은 복사'하여 저장합니다. (매우 중요)
        //    (자원 체크는 나중에 '콜백'에서 하므로, 지금 시점의 비용을 '복사본'으로 저장해야 함)
        int[] costsToSpend = new int[selectedData.costs.Length];
        System.Array.Copy(selectedData.costs, costsToSpend, selectedData.costs.Length);
        
        Debug.Log($"[BuildSystem] {prefabToBuild.name} 선택됨. 타입: {unitType}");

        
        // 4. 유닛 타입에 따라 분기합니다.
        
        // [기존 로직: SOLDIER]
        // "SOLDIER" 타입은 즉시 소환을 시도합니다.
        if (unitType == "SOLDIER")
        {
            // TrySpawnUnit 함수는 _unitToBuild와 _unitCosts 변수를 사용하므로,
            // 이 변수들에 임시로 값을 할당합니다.
            _unitToBuild = prefabToBuild;
            _unitCosts = costsToSpend; 
            
            // 소환 시도
            TrySpawnUnit(playerSpawnPoint.position);
            
            // 사용 후 즉시 초기화 (다른 로직과 꼬이지 않도록)
            _unitToBuild = null;
            _unitCosts = new int[5]; // 배열 초기화
        } 
        
        // [✨ 수정된 연동 로직: TOWER]
        // "TOWER" 타입은 TowerBuildManager의 건설 모드를 시작시킵니다.
        // (참고: 스폰 타워의 unitType도 "TOWER"로 설정해야 합니다.)
        else if (unitType == "TOWER")
        {
            // [핵심 1] TowerBuildManager가 건설을 완료했을 때 실행할 '콜백 함수'를 즉석에서(람다식으로) 만듭니다.
            // 이 콜백 함수는 'buildPosition'을 인자로 받습니다.
            System.Action<Vector3> onBuildCallback = (buildPosition) =>
            {
                // 이 코드는 TowerBuildManager가 '이미' 타워를 씬에 생성한 직후에 호출됩니다.
                
                // 1. 자원 소모를 시도합니다. (저장해둔 costsToSpend 사용)
                if (GameManager.Instance.TrySpendMultipleResources(costsToSpend))
                {
                    // 2. [자원 소모 성공]
                    Debug.Log($"[BuildSystem] {prefabToBuild.name} 건설 완료 및 자원 소모 성공.");
                    
                    // 3. [연동] 만약 이 타워가 '스폰 타워'라면 FlagManager를 호출합니다.
                    //    (여기서는 unitIndex == 0 인 경우를 스폰 타워로 '가정'합니다.)
                    //    (만약 스폰 타워가 0번이 아니면, 이 '0'을 실제 인덱스로 수정해야 합니다.)
                    if (unitIndex == 0 && FlagManager.Instance != null) 
                    {
                        Debug.Log("[BuildSystem] 스폰 타워로 확인됨. FlagManager 호출.");
                        FlagManager.Instance.StartFlagPlacement(buildPosition);
                    }
                }
                else
                {
                    // 4. [자원 소모 실패] (롤백 처리)
                    // TowerBuildManager가 이미 타워를 지었으므로, 다시 '파괴'해야 합니다.
                    Debug.LogWarning($"[BuildSystem] 자원 부족! 방금 생성된 {prefabToBuild.name}을(를) 파괴합니다.");
                    
                    // 2D 물리 시스템(TowerBuildManager 기준)으로 방금 생성된 타워를 찾습니다.
                    // (레이어 마스크를 사용하여 "Tower" 레이어만 검색)
                    int towerLayerMask = 1 << LayerMask.NameToLayer("Tower");
                    Collider2D[] hits = Physics2D.OverlapPointAll(buildPosition, towerLayerMask);
                    
                    foreach (var hit in hits)
                    {
                        // 프리팹 이름이 같은지 확인하여 정확한 타워만 파괴
                        // (Instantiate하면 이름 뒤에 (Clone)이 붙으므로 StartsWith 사용)
                        if (hit.gameObject.name.StartsWith(prefabToBuild.name))
                        {
                            Destroy(hit.gameObject); // 찾은 타워를 파괴
                            break;
                        }
                    }
                }
            }; // --- 콜백 함수 정의 끝 ---

            // [핵심 2] TowerBuildManager의 건설 모드를 시작시킵니다.
            //    프리팹과 방금 만든 '콜백 함수'를 전달합니다.
            TowerBuildManager.Instance.EnterBuildMode(prefabToBuild, onBuildCallback);

            // 6. [중요] BuildSystem의 Update()가 이 타워를 다시 배치하지 않도록 
            //    _unitToBuild를 null로 설정합니다.
            //    (이미 TowerBuildManager에게 작업을 위임했으므로)
            _unitToBuild = null; 
            _selectedUnitType = null;
        }
        else
        {
            // "SOLDIER"도 "TOWER"도 아닌, 알 수 없는 unitType일 경우
            Debug.LogWarning($"[BuildSystem] {prefabToBuild.name}의 unitType('{unitType}')을 처리할 수 없습니다.");
            _unitToBuild = null;
            _selectedUnitType = null;
        }
    }

    // --- 맵 클릭 처리 (타워/생산 유닛 배치) ---
    void Update()
    {
        // 배치형 유닛("TOWER")만 마우스 클릭을 대기해야 합니다.
        if (Input.GetMouseButtonDown(0) && _unitToBuild != null && _selectedUnitType == "TOWER")
        {
            // 1. Raycast를 쏴서 맵 클릭 지점을 가져옵니다.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) 
            {
                Vector3 positionToBuild = hit.point;
            
                // 2. 그리드 정렬 로직
                positionToBuild.x = Mathf.Round(positionToBuild.x);
                // Y축은 그대로 둡니다.
                positionToBuild.z = Mathf.Round(positionToBuild.z); 

                // 3. 해당 위치에 배치가 가능한지 확인 
                if (IsPlacementValid(positionToBuild))
                {
                    // 4. 통합된 생성 함수 호출 (내부에서 자원 체크 및 생성 처리)
                    TrySpawnUnit(positionToBuild); 
                
                    // 생성 후, 배치 준비 상태 해제
                    _unitToBuild = null; 
                    _selectedUnitType = null;
                }
                else
                {
                    // TODO: 배치 불가능 시 플레이어에게 시각적/청각적 피드백 제공
                    Debug.Log("Placement is invalid at this position (" + positionToBuild + ").");
                }
            }
        }
    }

    // 이 함수의 내부 로직은 맵 디자인에 따라 팀원들이 조정해야 합니다.
    bool IsPlacementValid(Vector3 position)
    {
        // 타워가 차지할 공간 크기 (충돌 확인 반경)
        float overlapRadius = 0.5f; 
    
        // 충돌체를 검사합니다.
        Collider[] hitColliders = Physics.OverlapSphere(position, overlapRadius);

        foreach (var hitCollider in hitColliders)
        {
            // "Path" 태그나 "Tower" 태그가 발견되면 배치 불가능
            if (hitCollider.CompareTag("Path") || hitCollider.CompareTag("Tower"))
            {
                return false; 
            }
        }
        
        return true; 
    }
    
    private void TrySpawnUnit(Vector3 position)
    {
        // 복합 자원 소모 시도 (GameManager의 새로운 함수 호출)
        if (GameManager.Instance.TrySpendMultipleResources(_unitCosts))
        {
            Instantiate(_unitToBuild, position, Quaternion.identity);
            Debug.Log("Unit spawned successfully: " + _unitToBuild.name);
        }
        // 자원 부족 시 GameManager 내부에서 Debug.Log 메시지를 출력합니다.
    }
    
}