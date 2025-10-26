using UnityEngine;
using UnityEngine.EventSystems;


/// FlagManager
/// ----------------------------------------------
/// 1. 타워 설치 완료 후 깃발 설치 모드 관리
/// 2. 고스트 깃발(반투명) 표시 및 깃발 설치 가능 범위 시각화
/// 3. 깃발 설치 가능 범위 내에서 좌클릭 시 깃발 설치
/// 4. 설치 범위를 벗어나면 고스트 투명 처리
/// ----------------------------------------------
public class FlagManager : MonoBehaviour
{
    public static FlagManager Instance;                 // 싱글톤 (전역 접근용)

    [Header("프리팹")]
    public GameObject flagPrefab;                        // 실제 설치될 깃발 프리팹
    public GameObject FlagBuildRangePrefab;              // 타워 주변 반투명 원 프리팹 (범위 시각화)

    [Header("설정")]
    public float placementRadius = 3f;                  // Inspector에서 조절 가능한 설치 반경

    private GameObject flagGhost;                        // 마우스를 따라다니는 고스트 깃발
    private GameObject FlagBuildRange;                   // 설치 가능 범위 표시 오브젝트
    private bool isPlacing = false;                      // 깃발 설치 모드 여부
    private Vector3 towerPosition;                       // 기준 타워 위치

    private void Awake()
    {
        Instance = this;                                 // 싱글톤 초기화
    }

    private void Update()
    {
        if (!isPlacing) return;                          // 설치 모드가 아니면 리턴

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // 마우스 월드 좌표 계산
        mousePos.z = 0f;                                 // z축 고정

        if (flagGhost != null)
            flagGhost.transform.position = mousePos;     // 고스트 깃발 위치 갱신

        float distance = Vector3.Distance(mousePos, towerPosition); // 타워 기준 거리 계산
        bool canPlace = distance <= placementRadius;     // 범위 안이면 설치 가능

        // 고스트 색상 변경 (범위 안 선명 / 범위 밖 투명)
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = canPlace ? new Color(1f,1f,1f,1f) : new Color(1f,1f,1f,0.3f);

        // 좌클릭 → 설치
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return; // UI 클릭 시 무시
            if (canPlace) PlaceFlag(mousePos);                          // 범위 내 설치
        }

        // 우클릭 → 설치 취소
        if (Input.GetMouseButtonDown(1))
            CancelFlagPlacement();
    }

    /// <summary>
    /// 깃발 설치 모드 진입
    /// </summary>
    public void EnterFlagPlacementMode()
    {
        if (isPlacing) return;
        isPlacing = true;

        flagGhost = Instantiate(flagPrefab);             // 고스트 깃발 생성
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1f,1f,1f,0.5f);       // 반투명

        ShowPlacementRange();                            // 설치 가능 범위 표시
    }

    /// <summary>
    /// 기준 타워 위치 설정
    /// </summary>
    public void SetTowerPosition(Vector3 pos)
    {
        towerPosition = pos;
    }

    /// <summary>
    /// 실제 깃발 설치
    /// </summary>
    private void PlaceFlag(Vector3 position)
    {
        Instantiate(flagPrefab, position, Quaternion.identity); // 깃발 생성
        CancelFlagPlacement();                                   // 설치 모드 종료
    }

    /// <summary>
    /// 설치 취소 / 모드 종료
    /// </summary>
    public void CancelFlagPlacement()
    {
        isPlacing = false;

        if (flagGhost != null) Destroy(flagGhost);             // 고스트 제거
        HidePlacementRange();                                   // 범위 제거
    }

    /// <summary>
    /// 설치 가능 범위 시각화
    /// </summary>
    private void ShowPlacementRange()
    {
        if (FlagBuildRangePrefab == null) return;

        FlagBuildRange = Instantiate(FlagBuildRangePrefab, towerPosition, Quaternion.identity);

        float diameter = placementRadius * 2f;                 // Inspector 값 기준 크기
        FlagBuildRange.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    /// <summary>
    /// 설치 범위 표시 제거
    /// </summary>
    private void HidePlacementRange()
    {
        if (FlagBuildRange != null)
        {
            Destroy(FlagBuildRange);
            FlagBuildRange = null;
        }
    }
}
