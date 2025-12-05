/*
 * FlagMover.cs
 * -------------------
 * [리팩토링됨] 깃발 이동 완료 시 FlagManager.UpdateRallyPoint()를 호출하여
 * 모든 유닛에게 "방송"하도록 수정되었습니다.
 */

using UnityEngine;

public class FlagMover : MonoBehaviour
{
    // --- 1. 싱글톤 설정 ---
    public static FlagMover Instance;

    // --- 2. 이동 관련 정보 (상태 변수) ---
    [Header("이동 관련 정보 (실시간)")]
    private GameObject movingFlag;
    private Vector3 towerCenter;
    private float placementRadius;
    private GameObject rangeIndicator;

    [Header("현재 상태 변수")]
    private bool isMoving = false;
    private bool justStarted = false;
    private Collider2D movingFlagCollider;

    // --- 3. 초기화 ---
    private void Awake()
    {
        Instance = this;
    }

    // --- 4. 외부 호출 함수 (Public Methods) ---

    /// <summary>
    /// [외부 호출] 깃발 이동 모드를 시작합니다.
    /// (주로 'FlagClickHandler.cs'의 OnMouseDown()에서 호출됩니다.)
    /// </summary>
    public void StartMove(GameObject flag, Vector3 towerPos, float radius, GameObject rangeObj)
    {
        if (flag == null) return;

        movingFlag = flag;
        towerCenter = towerPos;
        placementRadius = radius;
        rangeIndicator = rangeObj;

        // --- [★ 핵심 로직 1] 깃발 콜라이더 임시 비활성화 ---
        movingFlagCollider = movingFlag.GetComponent<Collider2D>();
        
        if (movingFlagCollider != null)
        {
            movingFlagCollider.enabled = false;
        }

        // --- 상태 변수 설정 ---
        isMoving = true;      // "지금 이동 중이다"
        justStarted = true;   // "방금 이동 시작했다"

        // --- 설치 가능 범위 표시 (시각화) ---
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = towerCenter;
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }

        Debug.Log($"[FlagMover.StartMove] 깃발 이동 시작. 기준점={towerCenter}, 반경={radius}");
    }

    /// <summary>
    /// [내부 호출] 깃발 이동(설치)을 완료하고 상태를 초기화합니다.
    /// (Update 함수에서 설치 클릭이 감지되었을 때 호출됩니다.)
    /// </summary>
    public void FinishMove(GameObject flag)
    {
        if (flag == null) return;

        var sr = flag.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }

        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);

        // --- 상태 변수 및 참조 초기화 ---
        // (주의: 깃발 콜라이더를 켜기 전에 '방송'을 먼저 해야 하므로,
        //  movingFlag = null; 과 isMoving = false; 는 맨 끝으로 이동)
        // movingFlag = null;
        // isMoving = false; 


        // --- [✨ 1. 핵심 수정!] ---
        // 깃발이 새로운 위치(flag.transform)로 "갱신"되었다고 FlagManager에게 "방송"을 요청합니다.
        // 그러면 FlagManager가 이 방송을 구독하는 모든 유닛들(HY_UnitMovement)을 깨웁니다.
        if (FlagManager.Instance != null)
        {
            FlagManager.Instance.UpdateRallyPoint(flag.transform);
        }
        else
        {
            Debug.LogError("[FlagMover] FlagManager.Instance가 null이라 깃발 이동 방송을 할 수 없습니다!");
        }
        // -------------------------

        // --- [★ 핵심 로직 2] 깃발 콜라이더 다시 활성화 ---
        if (movingFlagCollider != null)
        {
            movingFlagCollider.enabled = true;
        }
        movingFlagCollider = null;
        
        // --- 상태 변수 초기화 (맨 마지막에 수행) ---
        movingFlag = null;
        isMoving = false; 

        Debug.Log("[FlagMover.FinishMove] 깃발 설치 완료 및 방송 요청: " + flag.transform.position);
    }


    // --- 5. 매 프레임 실행되는 로직 ---
    private void Update()
    {
        if (!isMoving) return;

        if (justStarted)
        {
            justStarted = false;
            return; // ★ 시작 클릭이 설치 클릭으로 오인되는 것 방지
        }

        // --- 마우스 위치 계산 및 깃발 위치 업데이트 ---
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // [✨ 핵심 수정: 위치 제한 로직] -----------------------------------
        // 타워 중심에서 마우스까지의 벡터(방향과 거리)를 구합니다.
        Vector3 offset = mousePos - towerCenter;

        // 벡터의 길이가 placementRadius(반경)를 넘지 못하게 자릅니다(Clamp).
        // 즉, 마우스가 원 안에 있으면 그대로 두고, 원 밖으로 나가면 원의 가장자리에 위치시킵니다.
        Vector3 clampedOffset = Vector3.ClampMagnitude(offset, placementRadius);

        // 최종 위치 결정 (중심점 + 제한된 벡터)
        Vector3 finalPosition = towerCenter + clampedOffset;

        if (movingFlag != null)
            movingFlag.transform.position = mousePos;

        // [✨ 수정] 설치 가능 여부
        // 이제 깃발이 무조건 원 안(또는 경계)에 있으므로, 거리상의 설치 불가 조건은 사라집니다.
        // 다만, 혹시 다른 장애물 체크 등이 필요할 수 있으니 변수는 남겨둡니다.
        //bool canPlace = true;

        // --- 설치 가능 여부 검사 (반경 체크) ---
        float distance = Vector3.Distance(mousePos, towerCenter);
        bool canPlace = (distance <= placementRadius);

        // --- 시각적 피드백 (깃발 색상 변경) ---
        if (movingFlag != null)
        {
            var sr = movingFlag.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = canPlace ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        // --- 입력 처리 (깃발 설치) ---
        // '설치 가능한 영역'(canPlace == true)에서 '좌클릭'이 발생했다면,
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            // 깃발 이동(설치) 완료 처리를 합니다.
            FinishMove(movingFlag);
        }
    }
}