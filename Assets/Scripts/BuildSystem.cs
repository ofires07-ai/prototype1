using UnityEngine;
using UnityEngine.UI;

public class BuildSystem : MonoBehaviour
{
    // UI 버튼에 연결할 유닛 정보 (Scriptable Object 또는 클래스로 관리 권장)
    [System.Serializable]
    public class UnitData
    {
        public GameObject prefab;
        
        // Gold cost 대신 4가지 자원 비용 배열을 사용합니다.
        [Tooltip("Resource Costs: [T1, T2, T3, T4]")]
        public int[] costs = new int[4]; 
        
        public string unitType; // "TOWER", "SOLDIER" 등
    }

    [Header("소환 지점")]
    // 인스펙터에서 플레이어 유닛이 생성될 오브젝트를 여기에 연결해주세요.
    public Transform playerSpawnPoint; 
    
    [Header("구매 가능 유닛 목록")]
    public UnitData[] availableUnits; 

    // 현재 플레이어가 선택한 유닛 프리팹 (배치 준비 상태)
    private GameObject _unitToBuild; 
    
    // 4가지 자원 비용을 저장할 배열
    private int[] _unitCosts = new int[4]; 

    private string _selectedUnitType;
    
    // --- UI 버튼 클릭 이벤트에 연결될 함수 ---
    public void SelectUnitToBuild(int unitIndex)
    {
        if (unitIndex >= 0 && unitIndex < availableUnits.Length)
        {
            _unitToBuild = availableUnits[unitIndex].prefab;
            // 비용 배열을 저장합니다.
            _unitCosts = availableUnits[unitIndex].costs;
        
            _selectedUnitType = availableUnits[unitIndex].unitType; 
        
            Debug.Log(_unitToBuild.name + " selected. Type: " + _selectedUnitType);

            // TODO: 커서에 유닛 미리보기 이미지/프리팹을 보여주는 시각적 피드백 구현 (배치형 유닛만)
        }
    
        // 소환형 유닛("SOLDIER")은 즉시 소환 지점에 생성 시도
        if (_selectedUnitType == "SOLDIER")
        {
            TrySpawnUnit(playerSpawnPoint.position);
            _unitToBuild = null;
            _selectedUnitType = null;
        } 
        // TOWER 타입은 Update()에서 마우스 클릭을 기다립니다.
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
        // 기존의 TrySpendGold 대신 TrySpendMultipleResources를 사용합니다.
        if (GameManager.Instance.TrySpendMultipleResources(_unitCosts))
        {
            Instantiate(_unitToBuild, position, Quaternion.identity);
            Debug.Log("Unit spawned successfully: " + _unitToBuild.name);
        }
        // 자원 부족 시 GameManager 내부에서 Debug.Log 메시지를 출력합니다.
    }
    
}