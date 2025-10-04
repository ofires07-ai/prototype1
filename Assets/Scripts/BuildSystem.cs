using UnityEngine;
using UnityEngine.UI;

public class BuildSystem : MonoBehaviour
{
    // UI 버튼에 연결할 유닛 정보 (Scriptable Object 또는 클래스로 관리 권장)
    [System.Serializable]
    public class UnitData
    {
        public GameObject prefab;
        public int cost;
        public string unitType; // "타워", "생산", "공격" 등
    }

    [Header("소환 지점")]
    // TODO: 인스펙터에서 플레이어 유닛이 생성될 오브젝트를 여기에 연결해주세요.
    public Transform playerSpawnPoint; 
    
    [Header("구매 가능 유닛 목록")]
    public UnitData[] availableUnits; 

    // 현재 플레이어가 선택한 유닛 프리팹 (배치 준비 상태)
    private GameObject _unitToBuild; 
    private int _unitCost;

    private string _selectedUnitType;
    
    // --- UI 버튼 클릭 이벤트에 연결될 함수 ---
    public void SelectUnitToBuild(int unitIndex)
    {
        if (unitIndex >= 0 && unitIndex < availableUnits.Length)
        {
            _unitToBuild = availableUnits[unitIndex].prefab;
            _unitCost = availableUnits[unitIndex].cost;
        
            // 💡 1. 여기서 타입을 저장해야 합니다! (수정 필요)
            _selectedUnitType = availableUnits[unitIndex].unitType; 
        
            Debug.Log(_unitToBuild.name + " 선택됨. 타입: " + _selectedUnitType);

            // TODO: 커서에 유닛 미리보기 이미지/프리팹을 보여주는 시각적 피드백 구현 (배치형 유닛만)
        }
    
        // 💡 2. 타입이 저장된 후, 소환형 유닛인지 확인 (로직 위치 수정)
        if (_selectedUnitType == "SOLDIER")
        {
            // 소환형 유닛이므로 즉시 소환 지점에 생성
            TrySpawnUnit(playerSpawnPoint.position);
            _unitToBuild = null;
            _selectedUnitType = null;
        } 
        // TOWER 타입은 여기서 함수가 종료되고, Update()에서 마우스 클릭을 기다리게 됩니다.
    }


    // --- 맵 클릭 처리 (타워/생산 유닛 배치) ---
    void Update()
    {
        // 💡 배치형 유닛("TOWER")만 마우스 클릭을 대기해야 합니다.
        // 소환형 유닛("SOLDIER")은 SelectUnitToBuild 함수에서 즉시 생성됩니다.
        if (Input.GetMouseButtonDown(0) && _unitToBuild != null && _selectedUnitType == "TOWER")
        {
            // 1. Raycast를 쏴서 맵 클릭 지점을 가져옵니다.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) 
            {
                Vector3 positionToBuild = hit.point;
            
                // 💡 2. 그리드 정렬 로직 (보완)
                // 맵의 격자(Grid)에 정확히 맞도록 좌표를 정수(Integer) 단위로 처리합니다.
                positionToBuild.x = Mathf.Round(positionToBuild.x);
                positionToBuild.y = Mathf.Round(positionToBuild.y);
                // Z축은 그대로 둡니다.

                // 3. 해당 위치에 배치가 가능한지 확인 (길 위인지, 이미 타워가 있는지 등)
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
                    Debug.Log("이 위치(" + positionToBuild + ")에는 배치할 수 없습니다.");
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
            // "Path" 태그를 가진 콜라이더가 발견되면 배치 불가능
            if (hitCollider.CompareTag("Path"))
            {
                return false; // 배치 불가능
            }
        
            // "Tower" 태그를 가진 콜라이더가 발견되면 (이미 타워가 있다면) 배치 불가능
            if (hitCollider.CompareTag("Tower"))
            {
                return false; // 배치 불가능
            }
        }
        
        return true; 
    }
    
    private void TrySpawnUnit(Vector3 position)
    {
        if (GameManager.Instance.TrySpendGold(_unitCost))
        {
            Instantiate(_unitToBuild, position, Quaternion.identity);
            Debug.Log("유닛 생성 완료: " + _unitToBuild.name);
        }
    }
    
}