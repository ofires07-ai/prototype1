/*
TowerBuildManager.cs (통합)

[요약]
- '고스트'(미리보기)를 보여주며 모든 종류의 '타워 건설' 과정을 관리하는 총책임자입니다.
- 이 스크립트는 씬(Scene)에 있는 @Managers 같은 하나의 게임 오브젝트에 붙여서 '싱글톤'으로 사용합니다.

[작동 방식]
1. 외부 (예: BuildMenuUI.cs)에서 'A 타워 건설 버튼'을 누릅니다.
2. 버튼 스크립트가 'TowerBuildManager.Instance.EnterBuildMode(A타워_프리팹, 콜백함수)'를 호출합니다.
3. 이 스크립트는 'isBuilding = true'로 바꾸고, A타워 고스트를 생성해 마우스를 따라다니게 합니다.
4. Update()에서 실시간으로 "여기에 건설 가능한가?" (태그, 레이어 검사)를 확인합니다.
5. 유저가 '설치 가능'한 곳을 '클릭'하면 'BuildTower()'가 실행됩니다.
6. 'BuildTower()'는 진짜 A 타워를 생성하고, 저장해뒀던 '콜백함수'를 실행시킵니다.
7. 'ExitBuildMode()'를 호출하여 건설 모드를 종료(뒷정리)합니다.
*/

using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 방지를 위해 (EventSystem.current.IsPointerOverGameObject() 사용)

public class TowerBuildManager : MonoBehaviour
{
    // 💡 싱글톤 인스턴스입니다.
    // 'TowerBuildManager.Instance'라는 '공용 주소'를 통해
    // 다른 스크립트(예: UI 버튼)에서 이 스크립트의 기능(EnterBuildMode)을 쉽게 호출할 수 있습니다.
    public static TowerBuildManager Instance;

    [Header("고스트 오브젝트 (실시간 관리)")]
    // 마우스를 따라다니는 '미리보기' 타워 오브젝트
    private GameObject towerGhost;

    [Header("건설 정보 (외부에서 전달받음)")]
    // 현재 건설할 타워의 '원본' 프리팹 (EnterBuildMode에서 전달받음)
    private GameObject towerPrefab;
    // 타워 건설이 '완료'되었을 때 호출할 '예약 함수' (콜백).
    // (예: '스폰 타워' 건설 완료 시 -> 'FlagManager.Instance.StartFlagPlacement'를 호출하도록 예약)
    // (예: '방어 타워' 건설 완료 시 -> 아무것도 안 하도록 null을 전달)
    private System.Action<Vector3> onBuildComplete; // <Vector3>는 건설된 위치를 전달하기 위함

    [Header("현재 상태 변수")]
    // "지금 타워 건설 중인가?"를 확인하는 스위치 (true일 때만 Update 로직 실행)
    private bool isBuilding = false;
    // "지금 마우스 위치에 건설이 가능한가?" (Update에서 매 프레임 갱신됨)
    // 이 변수를 사용해 고스트 색상을 바꾸고, 클릭 시 건설 여부를 결정합니다.
    private bool canBuildHere = false;

    // --- 1. 초기화 ---

    private void Awake()
    {
        // 'Instance'라는 공용 주소에 '나 자신(this)'을 등록합니다.
        Instance = this;
    }

    // --- 2. 매 프레임 실행되는 로직 ---

    private void Update()
    {
        // 1. 건설 모드(isBuilding)가 아니라면 (즉, false라면),
        //    (예: 평상시 상태라면) Update()의 나머지 로직을 전혀 실행하지 않고 즉시 종료합니다.
        //    (컴퓨터 자원을 아끼는 효율적인 코드입니다)
        if (!isBuilding) return;

        // --- (여기부터는 isBuilding == true 일 때만 실행됨) ---

        // 2. 마우스 위치 계산
        // '카메라' 기준의 2D 화면 좌표(Input.mousePosition)를
        // '게임 월드' 기준의 3D (여기선 2D) 좌표로 변환합니다.
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // 2D 게임이므로 z좌표는 0으로 고정합니다.

        // 3. '고스트' 위치 업데이트
        //    '고스트' 타워가 있다면 (null이 아니라면), 그 위치를 방금 계산한 마우스 위치로 업데이트합니다.
        if (towerGhost != null)
            towerGhost.transform.position = mouseWorldPos;

        // --- 4. 건설 가능 여부 실시간 검사 ---
        // [최적화] 검사 결과를 'canBuildHere' 변수에 저장하여,
        // '고스트 색상 변경'과 '클릭 처리' 두 곳에서 이 결과를 재사용합니다.

        canBuildHere = false; // (1) 일단 '불가능'으로 초기화하고 시작
        bool onBuildableArea = false; // '설치 가능 영역' 위에 있는지?
        bool hasTower = false; // '이미 타워'가 있는지?

        // (2) 현재 마우스 위치(mouseWorldPos)에 겹쳐있는 '모든' 콜라이더를 가져옵니다.
        Collider2D[] allHits = Physics2D.OverlapPointAll(mouseWorldPos);

        // (3) [1단계 검사] "Buildable" 태그가 있는지 검사
        //     가져온 모든 콜라이더(allHits)를 하나씩(h) 검사합니다.
        foreach (var h in allHits)
        {
            // 만약 콜라이더의 태그가 "Buildable"이라면
            if (h.CompareTag("Buildable"))
            {
                onBuildableArea = true; // "설치 가능 영역 위에 있다!"라고 기록
                break; // 1단계 통과. 'Buildable' 하나라도 찾았으면 더 검사할 필요 없이 반복 종료.
            }
        }

        // (4) [2단계 검사] 1단계를 통과했을 때만 "Tower" 레이어가 있는지 추가 검사
        if (onBuildableArea) // "Buildable" 태그가 있는 영역일 때만
        {
            // 다시 한번 모든 콜라이더(allHits)를 검사합니다.
            foreach (var h in allHits)
            {
                // [중요] 이번엔 '레이어(Layer)'를 검사합니다.
                // h.gameObject.layer == LayerMask.NameToLayer("Tower")
                // -> "이 콜라이더(h)의 게임 오브젝트의 레이어가 'Tower' 레이어와 같은가?"
                if (h.gameObject.layer == LayerMask.NameToLayer("Tower"))
                {
                    hasTower = true; // "이미 타워가 있다!"라고 기록
                    break; // 2단계 실패. 타워 하나라도 찾았으면 더 검사할 필요 없이 반복 종료.
                }
            }

            // (5) [최종 판정]
            // "Buildable 위에 있지만 (onBuildableArea == true)"
            // "타워는 없어야 함 (hasTower == false)"
            if (!hasTower) // hasTower가 false일 때만 (즉, '!' 붙었을 때)
            {
                canBuildHere = true; // 최종적으로 "여기에 건설 가능!"
            }
        }
        // --- 검사 끝 ---


        // --- 5. [개선] 고스트 색상 업데이트 (시각적 피드백) ---
        // 고스트 타워의 SpriteRenderer 컴포넌트(색상/이미지 담당)를 가져옵니다.
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null) // (혹시 SpriteRenderer가 없는 경우를 대비한 방어 코드)
        {
            // (1) 최종적으로 건설 가능 (canBuildHere == true)
            if (canBuildHere)
                // 설치 가능: 불투명 (흰색)
                sr.color = new Color(1f, 1f, 1f, 1f);

            // (2) 'Buildable' 위에는 있지만(onBuildableArea=true), '타워가 이미 있어서' 불가능
            else if (onBuildableArea) // (이 경우, hasTower가 true라는 의미)
                // 설치 영역이지만 불가: 붉은 반투명 (경고색)
                sr.color = new Color(1f, 0.5f, 0.5f, 0.4f);

            // (3) 'Buildable' 영역이 아예 아님 (onBuildableArea=false)
            else
                // 설치 불가 영역: 기본 반투명 (회색빛)
                sr.color = new Color(1f, 1f, 1f, 0.5f);
        }

        // --- 6. 입력 처리 ---

        // [개선] 좌클릭 설치 (canBuildHere 변수 사용)
        // (1) 마우스 왼쪽 버튼을 '누른 순간' (GetMouseButtonDown(0))
        // (2) '최종적으로 건설 가능한' 곳(canBuildHere == true)에서 클릭했을 때
        if (Input.GetMouseButtonDown(0) && canBuildHere)
        {
            // [중요] UI 클릭 방지
            // EventSystem.current.IsPointerOverGameObject()
            // -> "혹시 지금 마우스가 UI(버튼, 인벤토리 창 등) 위에 있습니까?"
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // (만약 true라면, 즉 UI 위에 있다면)
                // 타워를 건설하지 않고 즉시 종료합니다. (땅을 클릭한 게 아니라 UI를 클릭한 것이므로)
                return;
            }

            // 위 모든 조건을 통과했으면, 실제 타워 건설 함수 호출!
            BuildTower(mouseWorldPos);
        }

        // 우클릭: 건설 모드 '취소'
        if (Input.GetMouseButtonDown(1))
            ExitBuildMode(); // 뒷정리 함수 호출
    }

    // --- 3. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출용] 타워 건설 모드를 시작합니다.
    /// (예: UI의 '스폰 타워 건설 버튼'에서 이 함수를 호출)
    /// </summary>
    /// <param name="prefab">설치할 타워의 '원본' 프리팹 (예: spawnTowerPrefab)</param>
    /// <param name="onBuildCompleteCallback">
    ///   건설 완료 후 실행할 '예약 함수' (콜백).
    ///   (예: FlagManager.Instance.StartFlagPlacement)
    /// </param>
    public void EnterBuildMode(GameObject prefab, System.Action<Vector3> onBuildCompleteCallback)
    {
        // (방어 코드)
        // 1. 이미 다른 타워를 건설 중이거나 (isBuilding == true)
        // 2. 외부에서 프리팹(prefab)을 안 넘겨줬으면 (null이면)
        // -> 아무것도 하지 않고 무시
        Debug.Log("[TowerBuildManager] EnterBuildMode가 호출되었습니다.");
        if (isBuilding)
        {
            Debug.LogWarning("[TowerBuildManager] 이미 건설 중(isBuilding=true)이라서 무시합니다.");
            return;
        }
        if (prefab == null)
        {
            Debug.LogWarning("[TowerBuildManager] 프리팹이 null이라서 무시합니다.");
            return;
        }

        isBuilding = true; // 1. "지금 건설 중" 스위치를 켭니다. (Update 로직이 돌기 시작!)
        towerPrefab = prefab; // 2. 전달받은 '원본 프리팹'을 저장합니다. (BuildTower에서 써야 함)
        onBuildComplete = onBuildCompleteCallback; // 3. 전달받은 '예약 함수'를 저장합니다. (BuildTower에서 써야 함)

        // 4. '고스트' 타워(미리보기)를 생성합니다.
        towerGhost = Instantiate(towerPrefab);
        // 고스트 모드일때 콜라이더 끄기(밀치기 방지)
        Collider2D[] cols = towerGhost.GetComponentsInChildren<Collider2D>();
        foreach (var col in cols)
        {
            col.enabled = false;
        }

        // (2) 공격 스크립트 끄기 (새로 추가!)
        // HY_TankController가 붙어있다면 끕니다.
        var tankScript = towerGhost.GetComponent<HY_TankController>();
        if (tankScript != null) tankScript.enabled = false;

        // TurretController가 붙어있다면 끕니다.
        var turretScript = towerGhost.GetComponent<TurretController>();
        if (turretScript != null) turretScript.enabled = false;
        
        Animator ghostAnim = towerGhost.GetComponent<Animator>();
        if (ghostAnim != null)
        {
            // 방법 1: (추천) 애니메이션이 없는 상태(T-pose)나 정지 상태가 괜찮다면 끕니다.
            // ghostAnim.enabled = false; 

            // 방법 2: (강력 추천) 스폰 애니메이션이 '땅에서 솟아오르는' 것이라면,
            // 고스트는 이미 솟아오른(완료된) 상태여야 배치하기 편합니다.
            // 따라서 시간을 강제로 흐르게 하여 애니메이션을 끝부분으로 보냅니다.
        
            // (1) 애니메이터를 활성화한 상태에서
            ghostAnim.enabled = true;
        
            // (2) 강제로 10초(충분한 시간)가 지난 것처럼 업데이트를 1회 실행합니다.
            // 이렇게 하면 스폰 애니메이션이 끝나고 Idle 상태가 된 모습이 렌더링 됩니다.
            ghostAnim.Update(10.0f);
        
            // (3) 그 후 애니메이터를 멈춥니다 (CPU 절약 및 모션 고정)
            ghostAnim.enabled = false; 
        }
        HY_SlowArea[] slowAreas = towerGhost.GetComponentsInChildren<HY_SlowArea>();
        foreach (var area in slowAreas)
        {
            // (1) 스크립트를 끕니다 -> Start()가 실행되지 않아 코루틴(장판 펼쳐짐)이 안 돕니다.
            area.enabled = false;

            // (2) 시각적으로도 안 보이게 크기를 0으로 만듭니다. (확실하게 숨김)
            //     만약 고스트 때 '정지된 장판 범위'를 보여주고 싶다면 이 줄을 지우세요.
            area.transform.localScale = Vector3.zero;
        }
        // 5. 고스트 타워의 초기 색상을 반투명하게 설정합니다.
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.5f);
    }

    /// <summary>
    /// [외부/내부] 건설 모드를 종료하고 모든 상태를 초기화(뒷정리)합니다.
    /// (우클릭으로 '취소'하거나, 좌클릭으로 '건설 완료' 시 호출됨)
    /// </summary>
    public void ExitBuildMode()
    {
        isBuilding = false; // 1. "건설 중" 스위치를 끕니다. (Update 로직이 멈춤)

        // 2. 마우스를 따라다니던 '고스트' 타워를 파괴(Destroy)합니다.
        if (towerGhost != null) Destroy(towerGhost);

        // 3. (중요) 저장해뒀던 정보들을 깨끗이 비웁니다(null).
        //    (다음 건설에 이전 정보가 남아있으면 안 되므로)
        towerPrefab = null;
        onBuildComplete = null;
    }

    // --- 4. 내부 실행 함수 (Private Methods) ---

    /// <summary>
    /// [내부] 실제 '진짜' 타워를 씬에 생성하는 함수
    /// </summary>
    /// <param name="position">설치할 위치 (마우스 클릭 위치)</param>
    private void BuildTower(Vector3 position)
    {
        // ✨ [수정 시작] ✨
        // try...finally 구문을 사용하여,
        // 4번(onBuildComplete)에서 오류가 발생하더라도
        // 5번(ExitBuildMode)이 '반드시' 실행되도록 보장합니다.
        try
        {
            // 1. '진짜' 타워 오브젝트를 'towerPrefab' 원본을 바탕으로
            //   클릭한 'position' 위치에 생성합니다.
            GameObject newTower = Instantiate(towerPrefab, position, Quaternion.identity);

            // 2. [중요] 생성된 '진짜' 타워의 레이어를 'Tower'로 설정합니다.
            //    (이 코드가 없으면, 방금 지은 타워 위에 또 타워를 지을 수 있게 됩니다!)
            newTower.layer = LayerMask.NameToLayer("Tower");

            // 3. [중요] 타워가 반투명하게 생성되는 것을 방지합니다.
            //    (고스트는 반투명했지만, '진짜' 타워는 불투명해야 함)
            //    '진짜' 타워의 색상을 '완전한 불투명'(흰색)으로 강제 리셋합니다.
            var sr = newTower.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
            // 3. 방금 생성된 타워에서 HY_Tower 스크립트를 찾습니다.
            HY_Tower towerScript = newTower.GetComponent<HY_Tower>();
            
            // 4. 스크립트가 존재하고, 비활성화되어 있다면
            if (towerScript != null)
            {
                // 5. 스크립트를 '활성화'시킵니다. (체크박스를 켭니다)
                //    이 순간부터 HY_Tower.FixedUpdate()가 작동하기 시작합니다.
                towerScript.enabled = true; 
                Debug.Log($"[TowerBuildManager] {newTower.name}의 HY_Tower.enabled = true로 설정.");
            }
            Debug.Log($"[TowerBuildManager] {newTower.name} built at {position}");

            // 4. [핵심] EnterBuildMode에서 저장해뒀던 '예약 함수(onBuildComplete)'를 실행합니다.
            //    (만약 onBuildComplete가 null이 아니면(?.) -> Invoke(실행)해라)
            //    (만약 스폰 타워였다면, 이 순간 FlagManager의 StartFlagPlacement가 호출됩니다)
            onBuildComplete?.Invoke(position); // (position: 깃발 매니저에게 타워 위치를 알려주기 위함)
        }
        catch (System.Exception ex)
        {
            // 4-1. (선택 사항) 콜백에서 오류가 발생했음을 로그로 남깁니다.
            Debug.LogError($"[TowerBuildManager] onBuildComplete 콜백 실행 중 오류 발생: {ex.Message}");
        }
        finally
        {
            // 5. 건설이 완료되었으므로, 건설 모드를 종료(뒷정리)합니다.
            ExitBuildMode();
        }
        
    }

    /// <summary>
    /// 외부 스크립트(타워 등)가 "지금 건설 중인지" 확인할 수 있도록
    /// 'isBuilding' 변수의 현재 상태를 알려주는 공용 함수입니다.
    /// </summary>
    public bool GetIsBuilding()
    {
        return isBuilding;
    }
}
   