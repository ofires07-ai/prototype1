/*
FlagManager.cs
- 스폰 타워 건설 직후, 깃발을 '최초'로 설치하는 모드를 관리합니다.
- 'TowerBuildManager'의 콜백 함수를 통해 'StartFlagPlacement'가 호출되면서 시작됩니다.
- 이 스크립트는 씬(Scene)에 있는 하나의 오브젝트(예: @Managers)에 붙여서 사용합니다.
- (깃발 '이동'은 FlagMover가, 깃발 '클릭'은 FlagClickHandler가 담당합니다)
*/

using UnityEngine;

public class FlagManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 다른 스크립트에서 'FlagManager.Instance'로 접근
    public static FlagManager Instance;

    // [인스펙터 연결 필요]
    [Header("프리팹")]
    public GameObject flagPrefab; // 생성할 깃발의 원본 프리팹

    // [인스펙터 연결 필요]
    [Header("기존 오브젝트")]
    // 씬(Scene)에 미리 배치된 범위 표시용 원형 스프라이트 오브젝트
    // (FlagMover도 이 오브젝트를 함께 사용함)
    public GameObject rangeIndicator;

    [Header("설정")]
    // 깃발을 설치할 수 있는 최대 반경 (타워 중심으로부터)
    public float placementRadius = 3f;

    [Header("현재 상태 변수")]
    // 깃발을 설치할 기준점 (스폰 타워의 위치)
    private Vector3 towerPosition;
    // 마우스를 따라다니는 '고스트' 깃발
    private GameObject flagGhost;
    // 현재 '깃발 설치 모드'인지 확인하는 상태 변수
    private bool isPlacing = false;
    // [중요] 클릭 쿨타임 (타워 설치 클릭이 깃발 설치 클릭으로 오인되는 것 방지)
    private float clickCooldown = 0f;

    // --- 1. 초기화 ---

    private void Awake() => Instance = this;

    // --- 2. 매 프레임 실행되는 로직 ---

    private void Update()
    {
        // 깃발 설치 모드가 아니면(isPlacing == false) 즉시 종료
        if (!isPlacing) return;

        // [중요] 'clickCooldown' 로직
        // StartFlagPlacement가 호출된 직후(0.2초)에는 클릭을 무시함
        if (clickCooldown > 0f)
        {
            clickCooldown -= Time.deltaTime; // 쿨타임 감소
            // (쿨다운 중에도 고스트는 움직여야 하므로, return하지 않음)
        }

        // --- 마우스 위치 계산 및 고스트 이동 ---
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // 2D Z좌표 고정

        if (flagGhost != null)
            flagGhost.transform.position = mouseWorldPos; // 고스트가 마우스 위치 추적

        // --- 설치 가능 여부 검사 (반경) ---
        // 마우스와 타워 중심 사이의 거리를 계산
        float dist = Vector3.Distance(mouseWorldPos, towerPosition);
        // 거리가 설정된 반경(placementRadius)보다 작거나 같으면 '설치 가능'
        bool canPlace = dist <= placementRadius;

        // 고스트 색상 업데이트 (시각적 피드백)
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 설치 가능하면 불투명(White), 불가능하면 반투명(Grayish)
            sr.color = canPlace ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);
        }

        // --- 입력 처리 (설치) ---
        // (설치 가능 & 좌클릭 & 쿨타임 종료) 세 조건을 모두 만족하면 설치
        if (Input.GetMouseButtonDown(0) && canPlace && clickCooldown <= 0f)
            PlaceFlag(mouseWorldPos); // 깃발 설치 함수 호출
    }

    // --- 3. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출] 깃발 '최초' 설치 모드를 시작합니다.
    /// (TowerBuildManager의 콜백이 호출)
    /// </summary>
    /// <param name="towerPos">깃발 설치의 기준점이 될 스폰 타워의 위치</param>
    public void StartFlagPlacement(Vector3 towerPos)
    {
        towerPosition = towerPos; // 타워 위치 저장
        isPlacing = true; // 설치 모드 시작
        clickCooldown = 0.2f; // [중요] 0.2초 쿨타임 설정
                               // (타워 설치에 사용된 클릭이 깃발 설치로 이어지는 것 방지)

        Debug.Log($"[FlagManager] StartFlagPlacement called with towerPos={towerPos}");

        // 깃발 고스트 생성
        flagGhost = Instantiate(flagPrefab, towerPosition, Quaternion.identity);
        // 고스트 초기 색상: 반투명
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.4f);

        // 범위 표시기(rangeIndicator) 활성화 및 설정
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true); // 켬
            rangeIndicator.transform.position = towerPosition; // 타워 위치로 이동
            // 스케일은 지름 기준이므로 반지름 * 2
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }
    }

    // --- 4. 내부 실행 함수 (Private Methods) ---

    /// <summary>
    /// 실제 깃발을 씬에 생성(설치)하는 함수
    /// </summary>
    /// <param name="position">설치할 위치 (마우스 클릭 위치)</param>
    private void PlaceFlag(Vector3 position)
    {
        // 'flagPrefab'을 'position' 위치에 실제로 생성
        GameObject newFlag = Instantiate(flagPrefab, position, Quaternion.identity);
        // 깃발 레이어 설정
        newFlag.layer = LayerMask.NameToLayer("Flag");

        // 깃발 색상을 불투명(White)으로 확정
        var sr = newFlag.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        // --- [핵심 로직 3] FlagClickHandler 동적 추가 ---
        // 생성된 깃발 오브젝트(newFlag)에 'FlagClickHandler' 스크립트 컴포넌트를
        // 코드로 '직접' 추가합니다.
        var handler = newFlag.AddComponent<FlagClickHandler>();

        // 추가된 'FlagClickHandler' 스크립트에 이 깃발이 속한
        // '타워 위치'와 '반경' 정보를 전달하여 초기화합니다.
        // (이 정보는 나중에 'FlagMover'가 깃발을 이동시킬 때 사용됨)
        handler.Initialize(towerPosition, placementRadius);

        // 깃발 설치가 완료되었으므로 설치 모드 종료
        ExitPlacementMode();
    }

    /// <summary>
    /// 깃발 설치 모드를 종료하고 모든 상태를 초기화(정리)합니다.
    /// </summary>
    private void ExitPlacementMode()
    {
        isPlacing = false; // 설치 모드 종료

        // 생성했던 'flagGhost' 오브젝트 파괴
        if (flagGhost != null) Destroy(flagGhost);
        // 켰던 'rangeIndicator' 오브젝트 숨김
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }
}