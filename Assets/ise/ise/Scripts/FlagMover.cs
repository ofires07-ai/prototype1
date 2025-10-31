/*
FlagMover.cs
- 깃발 '이동' 모드를 전담하는 스크립트입니다.
- 'FlagClickHandler'로부터 호출되어 깃발 이동을 시작하고, 
  두 번째 클릭으로 깃발을 재배치(설치)합니다.
- 이 스크립트는 씬(Scene)에 있는 하나의 오브젝트(예: @Managers)에 붙여서 사용합니다.
*/

using UnityEngine;

public class FlagMover : MonoBehaviour
{
    // 싱글톤 인스턴스: 다른 스크립트(주로 FlagClickHandler)에서 접근
    public static FlagMover Instance;

    [Header("이동 관련 정보 (실시간)")]
    // 현재 이동 중인 깃발 오브젝트 (StartMove에서 받아옴)
    private GameObject movingFlag;
    // 이동 중인 깃발이 속한 '타워'의 중심 위치
    private Vector3 towerCenter;
    // 깃발을 설치할 수 있는 최대 '반경'
    private float placementRadius;
    // 설치 가능 반경을 시각적으로 보여주는 '원' 오브젝트
    private GameObject rangeIndicator;

    [Header("현재 상태 변수")]
    // 현재 '이동 모드'인지 확인하는 상태 변수
    private bool isMoving = false;
    // [중요] 이동 모드 '시작 직후' 프레임인지 확인하는 변수
    // (이동 시작 클릭이 '설치' 클릭으로 오인되는 것을 방지)
    private bool justStarted = false;

    // [중요] 이동 중인 깃발의 콜라이더를 임시 저장
    // (이동 중에 깃발이 또 클릭되는 것을 막기 위해 잠시 비활성화할 용도)
    private Collider2D movingFlagCollider;

    // --- 1. 초기화 ---

    private void Awake() => Instance = this;

    // --- 2. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출] 깃발 이동 모드를 시작합니다.
    /// (주로 'FlagClickHandler.cs'의 OnMouseDown()에서 호출됨)
    /// </summary>
    /// <param name="flag">이동시킬 깃발 오브젝트</param>
    /// <param name="towerPos">깃발이 속한 타워의 위치 (설치 반경의 기준점)</param>
    /// <param name="radius">설치 가능 반경</param>
    /// <param name="rangeObj">반경을 표시할 시각 오브젝트 (RangeIndicator)</param>
    public void StartMove(GameObject flag, Vector3 towerPos, float radius, GameObject rangeObj)
    {
        if (flag == null) return;

        // 이동할 대상과 정보 저장
        movingFlag = flag;
        towerCenter = towerPos;
        placementRadius = radius;
        rangeIndicator = rangeObj;

        // --- [핵심 로직 1] 콜라이더 비활성화 ---
        // 깃발의 콜라이더를 가져옴
        movingFlagCollider = movingFlag.GetComponent<Collider2D>();
        if (movingFlagCollider != null)
        {
            // 콜라이더를 끔. 
            // 이유: 이동 중에 깃발이 또 클릭되면 FlagClickHandler가 StartMove를
            //      '또' 호출하는 무한 루프에 빠짐. 이를 방지.
            movingFlagCollider.enabled = false;
        }

        // 상태 변수 설정
        isMoving = true;
        justStarted = true; // "방금 이동 시작함" 플래그 켜기

        // 설치 가능 범위 표시
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = towerCenter;
            // 스케일은 지름 기준이므로 반지름 * 2
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }

        Debug.Log($"[FlagMover.StartMove] towerCenter={towerCenter}, radius={radius}");
    }

    /// <summary>
    /// [내부 호출] 깃발 이동(설치)을 완료합니다.
    /// </summary>
    /// <param name="flag">설치를 완료한 깃발</param>
    public void FinishMove(GameObject flag)
    {
        if (flag == null) return;

        // 깃발 색상을 최종적으로 불투명하게 설정
        var sr = flag.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        // 범위 표시기 숨김
        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);

        // 상태 변수 및 참조 초기화
        movingFlag = null;
        isMoving = false;

        // --- [핵심 로직 2] 콜라이더 활성화 ---
        // 비활성화했던 깃발의 콜라이더를 다시 '활성화'
        if (movingFlagCollider != null)
        {
            // 이유: 이동이 끝났으므로, 나중에 이 깃발을 '다시' 클릭해서 
            //      이동시킬 수 있어야 함.
            movingFlagCollider.enabled = true;
        }
        movingFlagCollider = null; // 참조 초기화

        Debug.Log("[FlagMover.FinishMove] Flag placed at " + flag.transform.position);
    }


    // --- 3. 매 프레임 실행되는 로직 ---

    private void Update()
    {
        // 이동 모드가 아니면 종료
        if (!isMoving) return;

        // [중요] 'justStarted' 로직
        // StartMove가 호출된 '첫 프레임'에는 클릭 검사를 건너뜀
        if (justStarted)
        {
            justStarted = false; // 플래그를 끄고
            return; // 즉시 Update 종료 (아래의 클릭 검사를 실행하지 않음)
        }

        // --- 마우스 위치 계산 및 깃발 이동 ---
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // 깃발이 마우스를 따라다니게 함
        if (movingFlag != null)
            movingFlag.transform.position = mousePos;

        // --- 설치 가능 여부 검사 (반경) ---
        float distance = Vector3.Distance(mousePos, towerCenter);
        bool canPlace = distance <= placementRadius; // 반경 내에 있는지 확인

        // 깃발 색상 업데이트 (시각적 피드백)
        if (movingFlag != null)
        {
            var sr = movingFlag.GetComponent<SpriteRenderer>();
            if (sr != null)
                // 설치 가능하면 불투명(White), 불가능하면 반투명(Grayish)
                sr.color = canPlace ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }

        // --- 입력 처리 (설치) ---
        // [중요] 깃발의 콜라이더가 꺼져있으므로, 이 클릭은 
        // FlagClickHandler가 아닌 오직 FlagMover의 Update만이 감지할 수 있음.
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            // 설치 가능 영역에서 좌클릭 시 이동 완료
            FinishMove(movingFlag);
        }
    }
}