using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// SpawnTowerBuildManager
/// ----------------------------------------------
/// 1. 타워 설치 모드 관리 (버튼 클릭 시 설치 모드 진입)
/// 2. 고스트 타워 표시 및 설치 가능 영역(Buildable) 하이라이트
/// 3. 설치 가능 영역에서 좌클릭 시 타워 설치
/// 4. 타워 설치 완료 후 FlagManager에게 위치 전달, 깃발 설치 모드 자동 진입
/// 5. 우클릭 시 설치 모드 종료
/// ----------------------------------------------
public class SpawnTowerBuildManager : MonoBehaviour
{
    public static SpawnTowerBuildManager Instance;       // 싱글톤 인스턴스 (전역 접근용)

    [Header("프리팹")]
    public GameObject spawnTowerPrefab;                 // 실제 설치될 타워 프리팹

    private GameObject towerGhost;                      // 마우스를 따라다니는 고스트 타워
    private bool isBuilding = false;                    // 설치 모드 여부

    [Header("설정")]
    public GameObject buildRangePrefab;                 // 타워 주변 반투명 원 (선택적)
    private GameObject buildRangeVisual;                // 클릭 시 범위 표시 오브젝트

    private void Awake()
    {
        Instance = this;                                // 싱글톤 초기화
    }

    private void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // 마우스 월드 좌표 계산
        mouseWorldPos.z = 0f;                           // z축 고정

        // 설치 모드가 아니면 리턴
        if (!isBuilding) return;

        // 고스트 타워 위치 갱신
        if (towerGhost != null)
            towerGhost.transform.position = mouseWorldPos;

        // 마우스가 닿은 Collider 확인
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f,1f,1f,0.5f);       // 기본 흐림
            if (hit != null && hit.CompareTag("Buildable")) // 설치 가능 영역 체크
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);
                bool hasTower = false;
                foreach (var h in hits)
                    if (h.gameObject.layer == LayerMask.NameToLayer("Tower"))
                        hasTower = true;                // 이미 타워 있는지 체크

                sr.color = hasTower ? new Color(1f,1f,1f,0.5f) : new Color(1f,1f,1f,1f); // 타워 없으면 선명
            }
        }

        // 좌클릭 → 타워 설치
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return; // UI 클릭 시 무시

            if (hit != null && hit.CompareTag("Buildable")) // 설치 가능 영역
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);
                bool hasTower = false;
                foreach (var h in hits)
                    if (h.gameObject.layer == LayerMask.NameToLayer("Tower"))
                        hasTower = true;

                if (!hasTower)
                    BuildSpawnTower(hit.transform.position); // 설치
            }
        }

        // 우클릭 → 설치 모드 종료
        if (Input.GetMouseButtonDown(1))
            ExitBuildMode();
    }

    /// <summary>
    /// 설치 모드 진입 (버튼 클릭 시)
    /// </summary>
    public void EnterBuildMode()
    {
        if (isBuilding) return;                          // 이미 설치 모드면 무시
        isBuilding = true;

        HighlightBuildAreas(true);                        // Buildable 영역 하이라이트

        towerGhost = Instantiate(spawnTowerPrefab);      // 고스트 타워 생성
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1f,1f,1f,0.4f);        // 반투명
    }

    /// <summary>
    /// 설치 모드 종료
    /// </summary>
    public void ExitBuildMode()
    {
        isBuilding = false;

        HighlightBuildAreas(false);                       // 하이라이트 제거

        if (towerGhost != null)
            Destroy(towerGhost);                          // 고스트 제거
    }

    /// <summary>
    /// 실제 타워 설치
    /// </summary>
    private void BuildSpawnTower(Vector3 position)
    {
        GameObject newTower = Instantiate(spawnTowerPrefab, position, Quaternion.identity); // 타워 생성
        newTower.layer = LayerMask.NameToLayer("Tower");                                      // 재설치 방지 레이어 설정

        ExitBuildMode();                                                                    // 설치 모드 종료

        // 설치 완료 후 FlagManager에게 위치 전달
        FlagManager.Instance.SetTowerPosition(position);    // 깃발 설치 위치 설정
        FlagManager.Instance.EnterFlagPlacementMode();      // 깃발 설치 모드 진입
    }

    /// <summary>
    /// Buildable 영역 하이라이트
    /// </summary>
    private void HighlightBuildAreas(bool state)
    {
        foreach (var area in GameObject.FindGameObjectsWithTag("Buildable"))
        {
            var sr = area.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = state ? new Color(0,0,1,0.3f) : Color.white; // 설치 가능 영역 색상
        }
    }
}
