using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic; 
using System.Linq; // 💡(참고) 10번째 버튼 로직에서 .Contains()를 쓰려면 Linq가 필요할 수 있습니다.

[System.Serializable]
public class BuildButtonUI // 💡(참고) BuildSystem 클래스 외부에 이미 정의되어 있다면 이 부분은 삭제하세요.
{
    public Image unitImage; 
    public Image cost1Icon;
    public TextMeshProUGUI cost1Text;
    public Image cost2Icon;
    public TextMeshProUGUI cost2Text;
    
    [HideInInspector] public int unitIndex; 
}

public class BuildSystem : MonoBehaviour
{
    // 💡(참고) 외부에 BuildButtonUI가 없다면, 이 클래스 정의를 사용하세요.
    // [System.Serializable]
    // public class BuildButtonUI
    // { ... }
    
    [System.Serializable]
    public class UnitData
    {
        public GameObject prefab;
        
        [Tooltip("Resource Costs: [T1, T2, T3, T4, T5]")]
        public int[] costs = new int[5]; 
    
        public string unitType;
    
        public Sprite unitIcon; 
    
        public Sprite resource1Icon; 
        public Sprite resource2Icon; 
        public Sprite resource3Icon; 
        public Sprite resource4Icon; 
        public Sprite resource5Icon; 
    
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
    
    [Header("빌드 버튼 UI 연결")]
    public BuildButtonUI[] buildButtons; 

    private GameObject _unitToBuild; 
    private int[] _unitCosts = new int[5]; 
    private string _selectedUnitType;
    
    void Start()
    {
        RefreshAllButtonUI();
    }
    
    private void RefreshAllButtonUI()
    {
        for (int i = 0; i < buildButtons.Length; i++)
        {
            buildButtons[i].unitIndex = i; 
            
            if (i < availableUnits.Length)
            {
                UnitData data = availableUnits[i];
                buildButtons[i].unitImage.sprite = data.unitIcon;
                
                List<int> requiredTiers = new List<int>();
                for(int tier = 0; tier < data.costs.Length; tier++)
                {
                    if (data.costs[tier] > 0)
                    {
                        requiredTiers.Add(tier);
                    }
                }
                
                if (i == 9) // 10번째 버튼 특수 처리
                {
                    // T5 (인덱스 4)만 표시
                    // (참고: .Contains()를 쓰려면 using System.Linq;가 필요합니다)
                    if (requiredTiers.Contains(4) && data.costs[4] > 0) 
                    {
                        buildButtons[i].cost1Icon.sprite = data.GetIconForTier(4); 
                        buildButtons[i].cost1Text.text = data.costs[4].ToString();
                        buildButtons[i].cost1Icon.gameObject.SetActive(true);
                        buildButtons[i].cost1Text.gameObject.SetActive(true);
                        
                        buildButtons[i].cost2Icon.gameObject.SetActive(false);
                        buildButtons[i].cost2Text.gameObject.SetActive(false);
                    }
                    else
                    {
                        buildButtons[i].cost1Icon.gameObject.SetActive(false);
                        buildButtons[i].cost1Text.gameObject.SetActive(false);
                        buildButtons[i].cost2Icon.gameObject.SetActive(false);
                        buildButtons[i].cost2Text.gameObject.SetActive(false);
                    }
                }
                else // 일반 버튼 로직
                {
                    if (requiredTiers.Count >= 1)
                    {
                        int tier1Index = requiredTiers[0];
                        buildButtons[i].cost1Icon.sprite = data.GetIconForTier(tier1Index);
                        buildButtons[i].cost1Text.text = data.costs[tier1Index].ToString();
                        buildButtons[i].cost1Icon.gameObject.SetActive(true);
                        buildButtons[i].cost1Text.gameObject.SetActive(true);
                    }
                    else 
                    {
                         buildButtons[i].cost1Icon.gameObject.SetActive(false);
                         buildButtons[i].cost1Text.gameObject.SetActive(false);
                    }

                    if (requiredTiers.Count >= 2) 
                    {
                        int tier2Index = requiredTiers[1];
                        buildButtons[i].cost2Icon.sprite = data.GetIconForTier(tier2Index);
                        buildButtons[i].cost2Text.text = data.costs[tier2Index].ToString();
                        buildButtons[i].cost2Icon.gameObject.SetActive(true);
                        buildButtons[i].cost2Text.gameObject.SetActive(true);
                    }
                    else
                    {
                         buildButtons[i].cost2Icon.gameObject.SetActive(false);
                         buildButtons[i].cost2Text.gameObject.SetActive(false);
                    }
                }
            }
        }
    }


   // --- UI 버튼 클릭 이벤트에 연결될 함수 ---
    /// <summary>
    /// [✨ 이 함수만 수정됩니다]
    /// UI 버튼 클릭 시 호출됩니다. 
    /// TOWER 타입은 TowerBuildManager에게 배치를 위임하고,
    /// SOLDIER 타입은 즉시 소환합니다.
    /// </summary>
    public void SelectUnitToBuild(int unitIndex)
    {
        // 0. 유효성 검사
        if (unitIndex < 0 || unitIndex >= availableUnits.Length)
        {
            Debug.LogError($"[BuildSystem] 잘못된 UnitIndex({unitIndex})가 요청되었습니다.");
            return;
        }

        // 1. 선택한 유닛의 원본 데이터
        UnitData selectedData = availableUnits[unitIndex];
        
        // 2. 핵심 정보 임시 변수에 저장
        GameObject prefabToBuild = selectedData.prefab;
        string unitType = selectedData.unitType;
        
        // 3. 비용 배열 '깊은 복사'
        int[] costsToSpend = new int[selectedData.costs.Length];
        System.Array.Copy(selectedData.costs, costsToSpend, selectedData.costs.Length);
        
        Debug.Log($"[BuildSystem] {prefabToBuild.name} 선택됨. 타입: {unitType}");

        
        // 4. 유닛 타입에 따라 분기
        
        // [기존 로직: SOLDIER] (수정 없음)
        if (unitType == "SOLDIER")
        {
            _unitToBuild = prefabToBuild;
            _unitCosts = costsToSpend; 
            
            TrySpawnUnit(playerSpawnPoint.position);
            
            _unitToBuild = null;
            _unitCosts = new int[5]; 
        } 
        
        // [✨ 수정된 연동 로직: TOWER]
        else if (unitType == "TOWER")
        {
            // [핵심 1] '콜백 함수' (람다식)
            System.Action<Vector3> onBuildCallback = (buildPosition) =>
            {
                // 1. 자원 소모 시도
                if (GameManager.Instance.TrySpendMultipleResources(costsToSpend))
                {
                    // 2. [자원 소모 성공]
                    Debug.Log($"[BuildSystem] {prefabToBuild.name} 건설 완료 및 자원 소모 성공.");
                    
                    // 3. [연동]
                    //    [수정됨] FlagManager를 호출하는 방식이 변경되었습니다.
                    if ((unitIndex == 0 || unitIndex == 1 || unitIndex == 2) && FlagManager.Instance != null ) 
                    {
                        // --- [수정된 로직 시작] ---
                        // (Vector3 buildPosition만 아는 상태에서, 방금 생성된 타워를 다시 찾아야 함)
                        
                        int towerLayerMask = 1 << LayerMask.NameToLayer("Tower"); 
                        Collider2D[] hits = Physics2D.OverlapPointAll(buildPosition, towerLayerMask); 
                        ProductionTower foundTower = null;

                        foreach (var hit in hits)
                        {
                            if (hit.gameObject.name.StartsWith(prefabToBuild.name)) 
                            {
                                foundTower = hit.gameObject.GetComponent<ProductionTower>();
                                break; // 찾았음
                            }
                        }

                        if (foundTower != null)
                        {
                            Debug.Log("[BuildSystem] 스폰 타워로 확인됨. FlagManager 호출.");
                            // [수정됨] Vector3가 아닌, ProductionTower 스크립트 자체를 전달합니다.
                            FlagManager.Instance.StartFlagPlacement(foundTower); 
                        }
                        else
                        {
                            Debug.LogWarning($"[BuildSystem] FlagManager를 호출하려 했으나, {buildPosition} 위치에서 타워를 찾지 못했습니다.");
                        }
                        // --- [수정된 로직 끝] ---
                    }
                }
                else
                {
                    // 4. [자원 소모 실패] (롤백 처리 - 수정 없음)
                    Debug.LogWarning($"[BuildSystem] 자원 부족! 방금 생성된 {prefabToBuild.name}을(를) 파괴합니다.");
                    
                    int towerLayerMask = 1 << LayerMask.NameToLayer("Tower");
                    Collider2D[] hits = Physics2D.OverlapPointAll(buildPosition, towerLayerMask);
                    
                    foreach (var hit in hits)
                    {
                        if (hit.gameObject.name.StartsWith(prefabToBuild.name))
                        {
                            Destroy(hit.gameObject); 
                            break;
                        }
                    }
                }
            }; // --- 콜백 함수 정의 끝 ---

            // [핵심 2] TowerBuildManager 호출 (수정 없음)
            TowerBuildManager.Instance.EnterBuildMode(prefabToBuild, onBuildCallback);

            _unitToBuild = null; 
            _selectedUnitType = null;
        }
        else
        {
            Debug.LogWarning($"[BuildSystem] {prefabToBuild.name}의 unitType('{unitType}')을 처리할 수 없습니다.");
            _unitToBuild = null;
            _selectedUnitType = null;
        }
    }

    // --- (Update, IsPlacementValid, TrySpawnUnit 함수는 수정할 필요 없음) ---
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && _unitToBuild != null && _selectedUnitType == "TOWER")
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) 
            {
                Vector3 positionToBuild = hit.point;
                positionToBuild.x = Mathf.Round(positionToBuild.x);
                positionToBuild.z = Mathf.Round(positionToBuild.z); 

                if (IsPlacementValid(positionToBuild))
                {
                    TrySpawnUnit(positionToBuild); 
                    _unitToBuild = null; 
                    _selectedUnitType = null;
                }
                else
                {
                    Debug.Log("Placement is invalid at this position (" + positionToBuild + ").");
                }
            }
        }
    }

    bool IsPlacementValid(Vector3 position)
    {
        float overlapRadius = 0.5f; 
        Collider[] hitColliders = Physics.OverlapSphere(position, overlapRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Path") || hitCollider.CompareTag("Tower"))
            {
                return false; 
            }
        }
        
        return true; 
    }
    
    private void TrySpawnUnit(Vector3 position)
    {
        if (GameManager.Instance.TrySpendMultipleResources(_unitCosts))
        {
            Instantiate(_unitToBuild, position, Quaternion.identity);
            Debug.Log("Unit spawned successfully: " + _unitToBuild.name);
        }
    }
}