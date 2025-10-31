/*
FlagMover.cs
- 깃발 이동 모드 관리
- StartMove(GameObject flag, Vector3 towerPos, float radius, GameObject indicator):
    깃발 이동 모드 시작
- Update(): 마우스 따라 이동, 색상 표시, 범위 표시, 좌클릭 설치
- StopMove(): 이동 모드 종료, 설치 확정, 색상/범위 처리
*/

using UnityEngine;

public class FlagMover : MonoBehaviour
{
    public static FlagMover Instance; // 싱글톤

    private GameObject movingFlag; // 현재 이동 중인 깃발
    private Vector3 towerPosition; // 깃발이 속한 타워 위치
    private float placementRadius; // 설치 가능 반경
    private bool ignoreFirstClick = true; // 이동 시작 직후 0.2초 클릭 방지
    private GameObject rangeIndicator; // 범위 표시 오브젝트 (FlagBuildRange)

    private void Awake()
    {
        Instance = this;
        
    }


    private void Update()
    {
        if (movingFlag == null)
        {
            if (rangeIndicator != null)
                rangeIndicator.SetActive(false); // 이동 깃발 없으면 범위 표시 끄기
            return;
        }

        // 마우스 위치 받아오기
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = towerPosition.z; // z값 맞춤 (2D)

        // 타워 기준 거리 계산
        float distance = Vector3.Distance(mousePos, towerPosition);
        bool canPlace = distance <= placementRadius; // 설치 가능 여부 판단
       
        // 깃발 따라다니기
        movingFlag.transform.position = mousePos;

        // 색상: 범위 안이면 불투명, 범위 밖이면 반투명
        var sr = movingFlag.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = canPlace ? new Color(1f,1f,1f,1f) : new Color(1f,1f,1f,0.3f);

        // 범위 표시 업데이트
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = towerPosition;
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f); // 반지름 기준 스케일
        }

        if (ignoreFirstClick) return; // 이동 직후 클릭 무시

        // 좌클릭: 설치 확정
        if (Input.GetMouseButtonDown(0) && canPlace)
            StopMove();

        // 우클릭: 이동 모드에서는 무시
    }

    // 이동 모드 시작
    public void StartMove(GameObject flag, Vector3 towerPos, float radius, GameObject indicator)
    {
        movingFlag = flag; // 이동할 깃발
        towerPosition = towerPos;
        placementRadius = radius;
        rangeIndicator = indicator;
        ignoreFirstClick = true; 
        Invoke(nameof(AllowClick), 0.2f); // 첫 클릭 0.2초 무시

        // 범위 표시 초기화
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = towerPosition;
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }
        
    }

    // 첫 클릭 허용
    private void AllowClick() => ignoreFirstClick = false;

    // 이동 모드 종료 + 설치 확정
    public void StopMove()
    {
        if (movingFlag == null) return;

        // 설치 확정 색상
        var sr = movingFlag.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;

        // 범위 표시 끄기
        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);

        // FlagManager 통해 깃발 설치 확정 처리
        if (FlagManager.Instance != null)
            FlagManager.Instance.FinishMove(movingFlag);

        movingFlag = null; // 이동 깃발 초기화
    }
}
