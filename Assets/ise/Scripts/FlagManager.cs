/*
FlagManager.cs
- 깃발 설치 모드 관리
- StartFlagPlacement(Vector3 towerPos): 타워 위치 기준 깃발 설치 모드 시작
- PlaceFlag(Vector3 position): 깃발 설치 확정
- ExitPlacementMode(): 설치 모드 종료
- StartMoveFlag(GameObject flag): 깃발 이동 모드 시작
- FinishMove(GameObject flag): 이동 후 깃발 설치 확정 처리
*/

using UnityEngine;
using UnityEngine.EventSystems;

public class FlagManager : MonoBehaviour
{
    public static FlagManager Instance; // 싱글톤

    [Header("프리팹")]
    public GameObject flagPrefab; // 깃발 프리팹

    [Header("기존 오브젝트")]
    public GameObject rangeIndicator; // FlagBuildRange 오브젝트

    [Header("설정")]
    public float placementRadius = 3f; // 설치 가능 반경

    private Vector3 towerPosition; // 현재 타워 위치
    private GameObject flagGhost; // 설치 중 고스트 깃발
    private bool isPlacing = false; // 설치 모드 여부
    private float clickCooldown = 0f; // 클릭 쿨타임

    private void Awake() => Instance = this;

    private void Update()
    {
        if (!isPlacing) return; // 설치 모드가 아니면 종료

        // 클릭 쿨타임 감소
        if (clickCooldown > 0f)
        {
            clickCooldown -= Time.deltaTime;
            return;
        }

        // 마우스 위치
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // 고스트 위치 업데이트
        if (flagGhost != null)
            flagGhost.transform.position = mouseWorldPos;

        // 설치 가능 범위 체크
        float dist = Vector3.Distance(mouseWorldPos, towerPosition);
        bool canPlace = dist <= placementRadius;

        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = canPlace ? new Color(1f,1f,1f,1f) : new Color(1f,1f,1f,0.3f); // 범위 안/밖 색상

        // 좌클릭 설치
        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceFlag(mouseWorldPos);

        // 우클릭 무시 (설치 모드에서는 아무 동작 없음)
    }

    // 깃발 설치 모드 시작
    public void StartFlagPlacement(Vector3 towerPos)
    {
        towerPosition = towerPos;
        isPlacing = true;
        clickCooldown = 0.2f; // 진입 후 바로 클릭 방지
        Debug.Log($"[FlagManager] StartFlagPlacement called with towerPos={towerPos}");
        flagGhost = Instantiate(flagPrefab, towerPosition, Quaternion.identity); // 고스트 생성
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f,1f,1f,0.4f);

        // 범위 표시
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = towerPosition;
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f); // 반지름 기준 스케일
        }
    }

    // 깃발 설치 확정
    private void PlaceFlag(Vector3 position)
    {
        GameObject newFlag = Instantiate(flagPrefab, position, Quaternion.identity); // 실제 깃발 설치
        newFlag.layer = LayerMask.NameToLayer("Flag"); // 레이어 설정

        var sr = newFlag.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white; // 확정 색상

        // 클릭 핸들러 추가
        var handler = newFlag.AddComponent<FlagClickHandler>();
        handler.Initialize(towerPosition, placementRadius);

        ExitPlacementMode(); // 설치 모드 종료
    }

    // 설치 모드 종료
    private void ExitPlacementMode()
    {
        isPlacing = false;

        if (flagGhost != null) Destroy(flagGhost); // 고스트 삭제
        if (rangeIndicator != null) rangeIndicator.SetActive(false); // 범위 숨김
    }

    // 이동 후 깃발 설치 확정 처리
    public void FinishMove(GameObject flag)
    {
        if (flag == null) return;

        var sr = flag.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        var handler = flag.GetComponent<FlagClickHandler>();
        if (handler != null)
        {
            // 이동 후에도 해당 깃발이 속한 타워 위치를 그대로 사용
            handler.Initialize(handler.GetTowerPosition(), placementRadius);
        }
    }


    // 깃발 클릭 후 이동 모드 시작
    public void StartMoveFlag(GameObject flag)
    {
        if (flag == null) return;

        var handler = flag.GetComponent<FlagClickHandler>();
        if (handler == null) return;

        FlagMover.Instance.StartMove(flag, handler.GetTowerPosition(), handler.GetPlacementRadius(), rangeIndicator);
    }
}
