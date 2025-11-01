/*
FlagManager.cs

[요약]
- 이 스크립트는 '스폰 타워'가 건설된 직후, '최초의' 깃발을 설치하는 과정을 전담하는 관리자입니다.
- 'TowerBuildManager' 같은 다른 스크립트가 타워 건설을 완료하면,
  이 스크립트의 'StartFlagPlacement' 함수를 호출해서 깃발 설치 모드를 '시작'시킵니다.
- 이 스크립트는 씬(Scene)에 있는 @Managers 같은 하나의 게임 오브젝트에 붙여서 '싱글톤'으로 사용합니다.

[참고]
- 깃발 '이동' 로직은 'FlagMover.cs'가 담당합니다.
- 깃발 '클릭' 감지 및 이동 모드 시작은 'FlagClickHandler.cs'가 담당합니다.
*/

using UnityEngine;

public class FlagManager : MonoBehaviour
{
    // 💡 싱글톤 인스턴스입니다.
    // 다른 어떤 스크립트에서든 'FlagManager.Instance'라는 '주소'로
    // 이 스크립트의 기능(예: StartFlagPlacement)을 바로 호출할 수 있습니다.
    public static FlagManager Instance;

    // [인스펙터 연결 필요] (유니티 에디터에서 끌어다 넣어야 함)
    [Header("프리팹")]
    // 씬(Scene)에 '복제'해서 생성할 깃발의 '원본' 프리팹입니다.
    public GameObject flagPrefab; 

    // [인스펙터 연결 필요] (유니티 에디터에서 끌어다 넣어야 함)
    [Header("기존 오브젝트")]
    // 깃발을 설치할 수 있는 범위를 시각적으로 보여주는 '동그란 원' 스프라이트 오브젝트입니다.
    // (FlagMover 스크립트도 이 오브젝트를 재사용합니다)
    public GameObject rangeIndicator;

    [Header("설정")]
    // 깃발을 설치할 수 있는 최대 반경 (타워 중심으로부터)
    public float placementRadius = 3f;

    [Header("현재 상태 변수 (내부 관리용)")]
    // 깃발 설치의 기준점이 될 '스폰 타워'의 위치
    private Vector3 towerPosition;
    // 마우스를 따라다니는 '미리보기' 또는 '고스트' 깃발
    private GameObject flagGhost;
    // "지금 깃발 설치 중인가?"를 확인하는 스위치 (true일 때만 Update 로직 작동)
    private bool isPlacing = false;
    // 🖱️ [중요] 클릭 쿨타임 (0보다 크면 클릭 무시)
    private float clickCooldown = 0f;

    // --- 1. 초기화 ---

    // 게임이 시작될 때(또는 이 오브젝트가 활성화될 때) 딱 한 번 실행됩니다.
    private void Awake()
    {
        // 'Instance'라는 공용 주소에 '나 자신(this)'을 등록합니다.
        Instance = this;
    }

    // --- 2. 매 프레임 실행되는 로직 ---

    // Update는 매 프레임(아주 빠른 속도)마다 계속 호출됩니다.
    private void Update()
    {
        // 1. 깃발 설치 모드(isPlacing)가 아니라면 (즉, false라면),
        //    (예: 평상시 상태라면) Update()의 나머지 로직을 전혀 실행하지 않고 즉시 종료합니다.
        //    (컴퓨터 자원을 아끼는 효율적인 코드입니다)
        if (!isPlacing) return;

        // 2. [중요] 클릭 쿨타임 로직
        //    (타워를 '건설'할 때 사용한 클릭이 깃발을 '설치'하는 클릭으로
        //     바로 이어지지 않도록 막아주는 0.2초의 안전장치입니다)
        if (clickCooldown > 0f)
        {
            // 쿨타임이 아직 남아있다면(0보다 크다면),
            // 매 프레임 시간(Time.deltaTime)만큼 쿨타임을 감소시킵니다.
            clickCooldown -= Time.deltaTime; 
            // (참고: 쿨타임 중에도 고스트는 움직여야 하므로, 여기서 return하면 안 됩니다!)
        }

        // --- (여기부터는 isPlacing == true 일 때만 실행됨) ---

        // 3. 마우스 위치 계산
        // '카메라' 기준의 2D 화면 좌표(Input.mousePosition)를
        // '게임 월드' 기준의 3D (여기선 2D) 좌표로 변환합니다.
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // 2D 게임이므로 z좌표는 0으로 고정합니다.

        // 4. '고스트' 깃발이 있다면 (null이 아니라면),
        //    그 위치를 방금 계산한 마우스 위치로 계속 업데이트합니다. (마우스 따라다니기)
        if (flagGhost != null)
            flagGhost.transform.position = mouseWorldPos; 

        // 5. 설치 가능 여부 검사 (거리 계산)
        // 마우스 위치와 타워 위치 사이의 '직선 거리'를 계산합니다.
        float dist = Vector3.Distance(mouseWorldPos, towerPosition);
        // 이 거리가 우리가 설정한 반경(placementRadius)보다 '작거나 같으면' '설치 가능' (true)
        bool canPlace = dist <= placementRadius;

        // 6. 시각적 피드백 (고스트 색상 변경)
        // 고스트 깃발의 SpriteRenderer 컴포넌트(색상/이미지 담당)를 가져옵니다.
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null) // (혹시 SpriteRenderer가 없는 경우를 대비한 방어 코드)
        {
            // 설치 가능(canPlace == true)하면: 완전 불투명 (흰색, Alpha=1f)
            // 설치 불가능(canPlace == false)하면: 반투명 (회색빛, Alpha=0.3f)
            // (삼항 연산자: '조건 ? A : B' -> 조건이 참이면 A, 거짓이면 B를 반환)
            sr.color = canPlace ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);
        }

        // 7. [최종] 깃발 설치 입력 처리
        // (1) 마우스 왼쪽 버튼을 '누른 순간' (GetMouseButtonDown(0))
        // (2) '설치 가능한' 위치이고 (canPlace == true)
        // (3) '쿨타임이 끝났다면' (clickCooldown <= 0f)
        // => 이 세 가지 조건을 *모두* 만족할 때만 깃발을 설치합니다.
        if (Input.GetMouseButtonDown(0) && canPlace && clickCooldown <= 0f)
            PlaceFlag(mouseWorldPos); // 깃발 설치 함수 호출!
    }

    // --- 3. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출용] 깃발 '최초' 설치 모드를 시작합니다.
    /// (예: TowerBuildManager에서 스폰 타워 건설 직후 이 함수를 호출합니다)
    /// </summary>
    /// <param name="towerPos">깃발 설치의 기준점이 될 스폰 타워의 위치</param>
    public void StartFlagPlacement(Vector3 towerPos)
    {
        towerPosition = towerPos; // 1. 기준점(스폰 타워) 위치를 '내부 변수'에 저장합니다.
        isPlacing = true;         // 2. "지금 설치 중" 스위치를 켭니다. (Update 로직이 돌기 시작!)
        clickCooldown = 0.2f;     // 3. [중요] 0.2초 쿨타임을 설정합니다.
                                  // (이 함수를 호출한 '타워 건설 클릭'이 '깃발 설치 클릭'으로 오인되는 것을 방지)

        Debug.Log($"[FlagManager] 깃발 설치 모드 시작. 타워 위치={towerPos}");

        // 4. '고스트' 깃발(미리보기)을 생성합니다.
        // (Instantiate: '프리팹'을 '위치'와 '회전값'으로 복제하여 씬에 생성하는 함수)
        flagGhost = Instantiate(flagPrefab, towerPosition, Quaternion.identity);
        
        // 5. 고스트 깃발의 초기 색상을 반투명하게 설정합니다.
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.4f);

        // 6. 범위 표시용 '원' 오브젝트를 활성화하고 설정합니다.
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true); // 1) 원을 보이게 합니다.
            rangeIndicator.transform.position = towerPosition; // 2) 원의 중심을 타워 위치로 옮깁니다.
            // 3) 원의 크기를 조절합니다.
            //    (Scale은 '지름' 기준입니다. 우리가 가진 placementRadius는 '반지름'이므로 * 2를 해줘야 함)
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }
    }

    // --- 4. 내부 실행 함수 (Private Methods) ---

    /// <summary>
    /// [내부] 실제 '진짜' 깃발을 씬에 생성(설치)하고 모드를 종료합니다.
    /// </summary>
    /// <param name="position">설치할 위치 (마우스 클릭 위치)</param>
    private void PlaceFlag(Vector3 position)
    {
        // 1. '진짜' 깃발 오브젝트를 'flagPrefab' 원본을 바탕으로
        //    클릭한 'position' 위치에 생성합니다.
        GameObject newFlag = Instantiate(flagPrefab, position, Quaternion.identity);
        
        // 2. 깃발의 레이어를 "Flag"로 설정합니다.
        //    (이래야 나중에 FlagClickHandler 같은 스크립트가 클릭을 감지하기 쉬워집니다)
        newFlag.layer = LayerMask.NameToLayer("Flag");

        // 3. 깃발 색상을 '완전한 불투명'(흰색)으로 확정합니다.
        var sr = newFlag.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        // --- [핵심 로직] 깃발에 클릭 핸들러(담당자) 붙이기 ---
        
        // 4. 방금 생성한 '진짜 깃발(newFlag)'에
        //    'FlagClickHandler' 스크립트(컴포넌트)를 코드로 '동적' 추가합니다.
        //    (프리팹에 미리 붙여두는 게 아니라, 생성 시점에 붙이는 방식)
        var handler = newFlag.AddComponent<FlagClickHandler>();

        // 5. 방금 추가한 'handler' 스크립트에 필요한 '초기 정보'를 전달합니다.
        //    (이 깃발이 어느 타워에 속해있는지(towerPosition),
        //     얼마나 움직일 수 있는지(placementRadius)를 알려줍니다)
        //    (이 정보는 나중에 FlagMover가 깃발을 이동시킬 때 사용합니다)
        handler.Initialize(towerPosition, placementRadius);

        // 6. 깃발 설치가 완료되었으므로, '설치 모드'를 종료(뒷정리)합니다.
        ExitPlacementMode();
    }

    /// <summary>
    /// [내부] 깃발 설치 모드를 종료하고 모든 상태를 초기화(뒷정리)합니다.
    /// </summary>
    private void ExitPlacementMode()
    {
        isPlacing = false; // 1. "설치 중" 스위치를 끕니다. (Update 로직이 멈춤)

        // 2. 마우스를 따라다니던 '고스트' 깃발을 파괴(Destroy)합니다. (더 이상 필요 없으므로)
        if (flagGhost != null) Destroy(flagGhost);
        
        // 3. 켜뒀던 '범위 표시 원'을 다시 숨깁니다(비활성화).
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }
}