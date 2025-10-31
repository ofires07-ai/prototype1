/*
TowerBuildManager.cs (통합)
- 모든 타워 설치 고스트 표시 및 타워 설치 관리
- 이 스크립트는 씬(Scene)에 있는 하나의 오브젝트(예: @Managers)에 붙여서 사용합니다.
- 외부(예: BuildMenuUI.cs)에서는 TowerBuildManager.Instance.EnterBuildMode()를 호출하여
  타워 건설을 시작합니다.
*/

using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 방지를 위해 필요

public class TowerBuildManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 다른 스크립트에서 'TowerBuildManager.Instance'로 쉽게 접근 가능
    public static TowerBuildManager Instance;

    [Header("고스트 오브젝트 (실시간 관리)")]
    // 마우스를 따라다니는 '고스트' (미리보기) 오브젝트
    private GameObject towerGhost;

    [Header("건설 정보 (외부에서 전달받음)")]
    // 현재 건설할 타워의 원본 프리팹 (EnterBuildMode에서 전달받음)
    private GameObject towerPrefab;
    // 타워 건설이 '완료'되었을 때 호출할 함수 (콜백). 
    // (예: 스폰 타워가 깃발 설치를 시작해야 할 때 사용)
    private System.Action<Vector3> onBuildComplete;

    [Header("현재 상태 변수")]
    // 현재 '건설 모드'인지 확인하는 상태 변수 (true일 때만 Update 로직 실행)
    private bool isBuilding = false;
    // 현재 마우스 위치에 건설이 '가능한지' 확인하는 상태 변수 (Update에서 매 프레임 갱신됨)
    private bool canBuildHere = false;

    // --- 1. 초기화 ---

    private void Awake()
    {
        // 싱글톤 인스턴스 초기화
        Instance = this;
    }

    // --- 2. 매 프레임 실행되는 로직 ---

    // TowerBuildManager.cs의 Update 함수 (수정된 버전)
    private void Update()
    {
        // 건설 모드가 아니면(isBuilding == false) 아무것도 하지 않고 즉시 종료
        if (!isBuilding) return;

        // --- 마우스 위치 계산 ---
        // 화면상 마우스 위치(Input.mousePosition)를 실제 게임 월드 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // 2D 게임이므로 Z좌표를 0으로 강제 고정

        // --- 고스트 위치 업데이트 ---
        // 고스트 오브젝트가 마우스를 실시간으로 따라다니게 함
        if (towerGhost != null)
            towerGhost.transform.position = mouseWorldPos;

        // --- 건설 가능 여부 실시간 검사 ---
        // [최적화] 매 프레임 검사 결과를 canBuildHere 변수에 저장하여 중복 계산 방지

        canBuildHere = false; // 기본값은 '설치 불가'
        bool onBuildableArea = false; // '설치 가능 영역' 위에 있는지
        bool hasTower = false; // '이미 타워'가 있는지

        // 현재 마우스 위치의 '모든' 콜라이더를 가져옴 (OverlapPointAll)
        Collider2D[] allHits = Physics2D.OverlapPointAll(mouseWorldPos);

        // 1. 가져온 콜라이더 중 'Buildable' (설치 가능) 태그가 있는지 검사
        foreach (var h in allHits)
        {
            if (h.CompareTag("Buildable"))
            {
                onBuildableArea = true; // 설치 가능 영역임을 확인
                break; // 하나라도 찾으면 검사 종료
            }
        }

        // 2. 'Buildable' 영역일 때만, 그 위에 'Tower'가 이미 있는지 추가 검사
        if (onBuildableArea)
        {
            foreach (var h in allHits)
            {
                // [중요] 레이어 마스크를 사용해 "Tower" 레이어를 가진 오브젝트 검사
                if (h.gameObject.layer == LayerMask.NameToLayer("Tower"))
                {
                    hasTower = true; // 이미 타워가 있음을 확인
                    break; // 하나라도 찾으면 검사 종료
                }
            }

            // 3. 'Buildable'이면서(O) 'Tower'가 없을 때(X)만 최종 건설 가능
            if (!hasTower)
                canBuildHere = true;
        }
        // --- 검사 끝 ---


        // --- [개선] 고스트 색상 업데이트 (시각적 피드백) ---
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (canBuildHere)
                // 설치 가능: 불투명 (흰색)
                sr.color = new Color(1f, 1f, 1f, 1f);
            else if (onBuildableArea) // (hasTower가 true라는 의미)
                // 설치 영역이지만 불가 (타워가 이미 있음): 붉은 반투명
                sr.color = new Color(1f, 0.5f, 0.5f, 0.4f);
            else
                // 설치 불가 영역 (Buildable 태그 없음): 기본 반투명
                sr.color = new Color(1f, 1f, 1f, 0.5f);
        }

        // --- 입력 처리 ---

        // [개선] 좌클릭 설치 (canBuildHere 변수 사용)
        // 건설 가능한 곳(canBuildHere == true)에서 좌클릭 시
        if (Input.GetMouseButtonDown(0) && canBuildHere)
        {
            // [중요] UI 클릭 방지: 마우스가 UI(버튼 등) 위에 있으면 건설 로직을 중단
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // 타워 건설 실행
            BuildTower(mouseWorldPos);
        }

        // 우클릭: 건설 모드 취소
        if (Input.GetMouseButtonDown(1))
            ExitBuildMode();
    }

    // --- 3. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출] 타워 건설 모드를 시작합니다.
    /// (예: UI의 '타워 건설 버튼'에서 이 함수를 호출)
    /// </summary>
    /// <param name="prefab">설치할 타워의 원본 프리팹</param>
    /// <param name="onBuildCompleteCallback">건설 완료 후 실행할 함수 (없으면 null)</param>
    public void EnterBuildMode(GameObject prefab, System.Action<Vector3> onBuildCompleteCallback)
    {
        // 이미 건설 중이거나, 전달받은 프리팹이 없으면 무시
        if (isBuilding || prefab == null) return;

        isBuilding = true; // 건설 모드 시작
        towerPrefab = prefab; // 설치할 프리팹 저장
        onBuildComplete = onBuildCompleteCallback; // 콜백 함수 저장

        // 고스트 생성
        towerGhost = Instantiate(towerPrefab);
        // 고스트 초기 색상: 반투명
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.5f);
    }

    /// <summary>
    /// [외부/내부] 건설 모드를 종료하고 모든 상태를 초기화합니다.
    /// (우클릭 취소 또는 건설 완료 시 호출됨)
    /// </summary>
    public void ExitBuildMode()
    {
        isBuilding = false; // 건설 모드 종료

        // 남아있는 고스트 오브젝트 파괴
        if (towerGhost != null) Destroy(towerGhost);

        // 참조 초기화
        towerPrefab = null;
        onBuildComplete = null;
    }

    // --- 4. 내부 실행 함수 (Private Methods) ---

    /// <summary>
    /// 실제 타워를 씬에 생성하는 함수
    /// </summary>
    /// <param name="position">설치할 위치 (마우스 클릭 위치)</param>
    private void BuildTower(Vector3 position)
    {
        // 전달받은 'position'에 'towerPrefab'을 복제(생성)
        GameObject newTower = Instantiate(towerPrefab, position, Quaternion.identity);

        // [중요] 생성된 타워의 레이어를 'Tower'로 설정
        // (이래야 다음 건설 시 'hasTower' 검사에 걸림)
        newTower.layer = LayerMask.NameToLayer("Tower");

        // [중요] 타워가 반투명하게 생성되는 것을 방지.
        // (고스트의 Alpha값을 쓰지 않도록 색상/투명도를 '불투명'으로 강제 리셋)
        var sr = newTower.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        Debug.Log($"[TowerBuildManager] {newTower.name} built at {position}");

        // [중요] EnterBuildMode에서 받아둔 콜백 함수(onBuildComplete) 실행.
        // (만약 null이 아니면)
        onBuildComplete?.Invoke(position);

        // 건설이 완료되었으므로 건설 모드 종료
        ExitBuildMode();
    }
}