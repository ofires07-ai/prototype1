/*
 * FlagMover.cs
 * -------------------
 * 깃발 '이동' 모드를 전담하는 싱글톤 매니저 스크립트입니다.
 * 씬(Scene)에 있는 하나의 오브젝트(예: @Managers)에 단 하나만 존재해야 합니다.
 * * 동작 순서:
 * 1. 'FlagClickHandler'가 이 스크립트의 StartMove()를 호출합니다. (깃발 클릭 시)
 * 2. Update()가 활성화되어 깃발이 마우스를 따라다니고, 설치 가능 여부를 시각적으로 표시합니다.
 * 3. 사용자가 마우스를 다시 클릭(좌클릭)하면 FinishMove()가 호출되어 깃발을 재배치(설치)하고 이동 모드를 종료합니다.
 */

using UnityEngine;

// MonoBehaviour를 상속받아 Unity 씬의 게임 오브젝트에 부착될 수 있습니다.
public class FlagMover : MonoBehaviour
{
    // --- 1. 싱글톤 설정 ---

    // 이 클래스의 유일한 인스턴스(객체)를 저장하는 static 변수입니다.
    // 'static'이므로, 'FlagMover.Instance' 형태로 다른 스크립트(예: FlagClickHandler)에서
    // 씬에 존재하는 FlagMover 컴포넌트에 쉽게 접근할 수 있습니다.
    public static FlagMover Instance;

    // --- 2. 이동 관련 정보 (상태 변수) ---

    [Header("이동 관련 정보 (실시간)")]
    // [현재 이동 중인 깃발] StartMove()에서 전달받은 깃발 게임 오브젝트입니다.
    private GameObject movingFlag;
    
    // [이동 기준점] 현재 이동 중인 깃발이 속한 '타워'의 중심 위치입니다.
    private Vector3 towerCenter;
    
    // [이동 제한 반경] 깃발을 'towerCenter'로부터 설치할 수 있는 최대 거리입니다.
    private float placementRadius;
    
    // [시각 보조 도구] 설치 가능 반경을 시각적으로 보여주는 '원' 모양의 게임 오브젝트입니다.
    private GameObject rangeIndicator;

    [Header("현재 상태 변수")]
    // 현재 '이동 모드'인지 확인하는 상태 플래그(flag)입니다.
    // true일 때만 Update()에서 깃발 이동 로직이 실행됩니다.
    private bool isMoving = false;
    
    // [★ 중요] 이동 모드 '시작 직후' 프레임인지 확인하는 변수입니다.
    // true인 이유: 깃발을 '클릭'해서 이동을 시작할 때의 그 클릭(Input.GetMouseButtonDown(0))이,
    // Update()문의 '설치' 클릭으로 오인되어 깃발이 바로 그 자리에 설치되는 것을 방지합니다.
    private bool justStarted = false;

    // [★ 중요] 이동 중인 깃발의 2D 콜라이더를 임시로 저장하는 변수입니다.
    // 깃발 이동 중에는 깃발의 콜라이더를 잠시 비활성화(off)하여,
    // 깃발이 또 클릭되는 것(FlagClickHandler의 OnMouseDown)을 막는 데 사용됩니다.
    private Collider2D movingFlagCollider;

    // --- 3. 초기화 ---

    /// <summary>
    /// Unity 엔진에 의해 스크립트가 활성화될 때 '한 번' 호출됩니다.
    /// (Start 함수보다 먼저 호출됩니다)
    /// </summary>
    private void Awake()
    {
        // 'Instance' 변수에 자기 자신(이 스크립트 컴포넌트)을 할당합니다.
        // 이로써 싱글톤 설정이 완료됩니다.
        Instance = this;
    }

    // --- 4. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출] 깃발 이동 모드를 시작합니다.
    /// (주로 'FlagClickHandler.cs'의 OnMouseDown()에서 호출됩니다.)
    /// </summary>
    /// <param name="flag">이동시킬 깃발 게임 오브젝트</param>
    /// <param name="towerPos">깃발이 속한 타워의 위치 (설치 반경의 기준점)</param>
    /// <param name="radius">설치 가능 반경</param>
    /// <param name="rangeObj">반경을 표시할 시각 오브젝트 (RangeIndicator)</param>
    public void StartMove(GameObject flag, Vector3 towerPos, float radius, GameObject rangeObj)
    {
        // 방어 코드: 만약 전달받은 깃발이 null(없음)이면, 아무것도 하지 않고 함수를 종료합니다.
        if (flag == null) return;

        // 이동에 필요한 정보들을 클래스 멤버 변수에 저장합니다.
        movingFlag = flag; //이동시킬 깃발 게임 오브젝트
        towerCenter = towerPos; //깃발이 속한 타워의 위치 (설치 반경의 기준점)
        placementRadius = radius; //설치 가능 반경
        rangeIndicator = rangeObj; //반경을 표시할 시각 오브젝트

        // --- [★ 핵심 로직 1] 깃발 콜라이더 임시 비활성화 ---
        // 깃발에 부착된 Collider2D 컴포넌트를 가져와서 'movingFlagCollider' 변수에 저장합니다.
        movingFlagCollider = movingFlag.GetComponent<Collider2D>();
        
        if (movingFlagCollider != null) // 콜라이더가 존재한다면
        {
            // 콜라이더를 비활성화(끕니다).
            // [이유]: 만약 콜라이더가 켜져 있으면, 깃발이 마우스를 따라다니다가 
            // 깃발 위에서 또 클릭이 발생했을 때, FlagClickHandler의 OnMouseDown이 
            // 또 호출되어 StartMove가 '재귀적으로' 호출되는 심각한 오류가 발생합니다.
            // 이를 방지하기 위해 이동이 끝날 때까지 콜라이더를 꺼둡니다.
            movingFlagCollider.enabled = false;
        }

        // --- 상태 변수 설정 ---
        isMoving = true;      // "지금 이동 중이다"라고 상태를 변경
        justStarted = true;   // "방금 이동 시작했다"라고 플래그를 켬 (Update에서 사용됨)

        // --- 설치 가능 범위 표시 (시각화) ---
        if (rangeIndicator != null) // 범위 표시기 오브젝트가 지정되었다면
        {
            rangeIndicator.SetActive(true); // 표시기를 씬에 보이게 함
            rangeIndicator.transform.position = towerCenter; // 표시기의 위치를 타워 중심으로 이동
            
            // 표시기의 스케일(크기)을 설정합니다.
            // Unity의 원(Circle) Sprite는 기본적으로 지름 1짜리입니다.
            // 따라서 스케일을 '반지름 * 2' (즉, 지름)로 설정해야 원하는 반경이 표시됩니다.
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }

        // 디버그 로그를 남겨서 개발자가 현재 상태를 파악할 수 있게 돕습니다.
        Debug.Log($"[FlagMover.StartMove] 깃발 이동 시작. 기준점={towerCenter}, 반경={radius}");
    }

    /// <summary>
    /// [내부 호출] 깃발 이동(설치)을 완료하고 상태를 초기화합니다.
    /// (Update 함수에서 설치 클릭이 감지되었을 때 호출됩니다.)
    /// </summary>
    /// <param name="flag">설치를 완료한 깃발 (movingFlag)</param>
    public void FinishMove(GameObject flag)
    {
        // 방어 코드: 만약 깃발이 (어떤 이유로든) null이 되었다면 종료합니다.
        if (flag == null) return;

        // 깃발의 SpriteRenderer를 가져옵니다.
        var sr = flag.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 깃발 색상을 최종적으로 원본(완전 불투명, 흰색)으로 되돌립니다.
            // (이동 중에 반투명하게 보였을 수 있으므로)
            sr.color = Color.white;
        }

        // 범위 표시기(rangeIndicator)를 다시 숨깁니다.
        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);

        // --- 상태 변수 및 참조 초기화 ---
        movingFlag = null; // "현재 이동 중인 깃발 없음"으로 초기화
        isMoving = false;  // "이동 모드 아님"으로 상태 변경

        // --- [★ 핵심 로직 2] 깃발 콜라이더 다시 활성화 ---
        // StartMove에서 비활성화했던 깃발의 콜라이더를 다시 '활성화'(켭니다).
        if (movingFlagCollider != null)
        {
            // [이유]: 이동이 끝났으므로, 나중에 사용자가 이 깃발을 '다시' 클릭해서 
            // 이동시킬 수 있어야 합니다. 콜라이더가 꺼져 있으면 클릭이 감지되지 않습니다.
            movingFlagCollider.enabled = true;
        }
        // 콜라이더 참조 변수도 초기화합니다.
        movingFlagCollider = null;

        Debug.Log("[FlagMover.FinishMove] 깃발 설치 완료: " + flag.transform.position);
    }


    // --- 5. 매 프레임 실행되는 로직 ---

    /// <summary>
    /// Unity 엔진에 의해 '매 프레임'마다 호출되는 함수입니다.
    /// 깃발 이동 로직의 실시간 처리를 담당합니다.
    /// </summary>
    private void Update()
    {
        // [가드(Guard) 로직]
        // 현재 '이동 모드'(isMoving)가 아니라면, 
        // 아래의 복잡한 로직을 실행할 필요가 없으므로 즉시 함수를 종료합니다. (성능 최적화)
        if (!isMoving) return;

        // [★ 중요] 'justStarted' 플래그 검사
        // 만약 '방금 이동 시작한 프레임'(justStarted == true)이라면,
        if (justStarted)
        {
            justStarted = false; // 플래그를 다시 끔 (다음 프레임부터는 이 로직을 타지 않도록)
            return; // ★ 즉시 Update 함수를 종료합니다.
            // [이유]: 이 프레임은 깃발을 '클릭'한 프레임입니다.
            // 만약 여기서 return하지 않고 아래의 '설치' 클릭 검사(Input.GetMouseButtonDown(0))로
            // 넘어가면, '시작 클릭'이 '설치 클릭'으로 인식되어 깃발이 즉시 제자리에 설치됩니다.
        }

        // --- 마우스 위치 계산 및 깃발 위치 업데이트 ---
        // 현재 마우스 커서의 화면 좌표(Screen Position)를 
        // 게임 월드 좌표(World Position)로 변환합니다.
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f; // 2D 게임이므로 Z축 좌표는 0으로 고정합니다.

        // 깃발이 마우스를 따라다니도록 위치를 매 프레임 업데이트합니다.
        if (movingFlag != null)
            movingFlag.transform.position = mousePos;

        // --- 설치 가능 여부 검사 (반경 체크) ---
        // 현재 마우스 위치(mousePos)와 타워 중심(towerCenter) 사이의 거리를 계산합니다.
        float distance = Vector3.Distance(mousePos, towerCenter);
        
        // 계산된 거리가 설정된 '반경'(placementRadius) 이내인지 확인합니다.
        bool canPlace = (distance <= placementRadius); // 반경 내에 있으면 true, 벗어나면 false

        // --- 시각적 피드백 (깃발 색상 변경) ---
        if (movingFlag != null)
        {
            var sr = movingFlag.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // C#의 삼항 연산자 사용
                // canPlace가 true이면 : 완전 불투명(Color.white)
                // canPlace가 false이면 : 반투명 회색 (new Color(1f, 1f, 1f, 0.3f))
                // (알파(a) 채널을 0.3f로 주어 반투명하게 만듭니다)
                sr.color = canPlace ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        // --- 입력 처리 (깃발 설치) ---
        // [★ 중요] 
        // 현재 깃발의 콜라이더는 '비활성화'된 상태입니다.
        // 따라서 이 좌클릭(GetMouseButtonDown(0))은 깃발 자체가 감지하는 것이 아니라,
        // 오직 이 'FlagMover'의 Update 문에서만 감지할 수 있습니다.
        
        // 만약 '설치 가능한 영역'(canPlace == true)에서 '좌클릭'이 발생했다면,
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            // 깃발 이동(설치) 완료 처리를 합니다.
            FinishMove(movingFlag);
        }
    }
}