

using UnityEngine;

public class FlagManager : MonoBehaviour
{
    // --- [추가됨] C# 이벤트(방송) ---
    // 깃발의 위치가 '설정'되거나 '이동'될 때마다 이 방송을 쏩니다.
    // 모든 유닛(HY_UnitMovement)이 이 방송을 구독(Subscribe)할 것입니다.
    public static event System.Action<Transform> OnRallyPointUpdated;

    public static FlagManager Instance;

    [Header("프리팹")]
    public GameObject flagPrefab;
    
    [Header("현재 상태 (외부 참조용)")]
    public Transform currentSpawnFlag;
    
    [Header("기존 오브젝트")]
    public GameObject rangeIndicator;

    [Header("설정")]
    public float placementRadius = 3f;

    [Header("현재 상태 변수 (내부 관리용)")]
    private Vector3 towerPosition;
    private GameObject flagGhost;
    private bool isPlacing = false;
    private float clickCooldown = 0f;
    
    private ProductionTower currentTowerToActivate; // [이전 리팩토링]

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isPlacing) return;

        if (clickCooldown > 0f)
        {
            clickCooldown -= Time.deltaTime; 
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; 

        // 3. 거리 계산
        float dist = Vector3.Distance(mouseWorldPos, towerPosition);

        /*// [✨ 핵심 수정: 위치 제한 로직] -----------------------------------
        // 타워 중심에서 마우스까지의 벡터를 구하고 길이를 제한합니다.
        Vector3 offset = mouseWorldPos - towerPosition;
        Vector3 clampedOffset = Vector3.ClampMagnitude(offset, placementRadius);

        // 최종 위치 결정 (중심점 + 제한된 벡터)
        Vector3 finalPosition = towerPosition + clampedOffset;*/

        // [✨ 핵심 수정] 마우스가 반경 안에 있을 때만 깃발 위치를 갱신합니다.
        // 반경 밖이라면? -> 아무것도 안 함 (마지막 위치에 깃발이 가만히 멈춰있음)
        if (dist <= placementRadius)
        {
            if (flagGhost != null)
                flagGhost.transform.position = mouseWorldPos; 
        }
        // [✨ 수정] 설치 가능 여부
        // 위치를 강제로 제한했으므로, 마우스가 어디에 있든 항상 설치 가능합니다.
        //bool canPlace = true;

        /*float dist = Vector3.Distance(mouseWorldPos, towerPosition);
        bool canPlace = dist <= placementRadius;*/

       

        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white; 
            //canPlace ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);
        }

        if (Input.GetMouseButtonDown(0)  && clickCooldown <= 0f)
            PlaceFlag(flagGhost.transform.position); 
    }

    /// <summary>
    /// [수정됨] 깃발 '최초' 설치 모드를 시작합니다.
    /// </summary>
    public void StartFlagPlacement(ProductionTower tower)
    {
        currentTowerToActivate = tower;
        towerPosition = tower.transform.position;
        isPlacing = true;        
        clickCooldown = 0.2f;    
        Debug.Log($"[FlagManager] 깃발 설치 모드 시작. 타워: {tower.name}");
        flagGhost = Instantiate(flagPrefab, towerPosition, Quaternion.identity);
        
        var sr = flagGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.4f);

        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = towerPosition;
            rangeIndicator.transform.localScale = Vector3.one * (placementRadius * 2f);
        }
    }

    /// <summary>
    /// [내부] 실제 '진짜' 깃발을 씬에 생성(설치)하고 모드를 종료합니다.
    /// </summary>
    private void PlaceFlag(Vector3 position)
    {
        GameObject newFlag = Instantiate(flagPrefab, position, Quaternion.identity);
        newFlag.layer = LayerMask.NameToLayer("Flag");
        var sr = newFlag.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        // [수정됨] 깃발을 설정하는 '공용 함수'를 호출합니다.
        UpdateRallyPoint(newFlag.transform);

        var handler = newFlag.AddComponent<FlagClickHandler>();
        handler.Initialize(towerPosition, placementRadius);

        // --- [핵심 로직] ---
        if (currentTowerToActivate != null)
        {
            currentTowerToActivate.StartProduction(currentSpawnFlag);
            currentTowerToActivate = null;
        }
        // -------------------------

        ExitPlacementMode();
    }

    /// <summary>
    /// [✨ 새로 추가된 공용 함수]
    /// 깃발의 Transform을 갱신하고, 모든 유닛에게 "방송"합니다.
    /// (FlagMover.cs가 깃발을 옮긴 후 이 함수를 호출해야 합니다)
    /// </summary>
    public void UpdateRallyPoint(Transform newFlagTransform)
    {
        // 1. 관리자의 '현재 깃발' 변수를 갱신합니다.
        currentSpawnFlag = newFlagTransform;

        // 2. "방송"을 쏩니다! (이 이벤트를 구독하는 모든 유닛이 깨어납니다)
        if (OnRallyPointUpdated != null)
        {
            OnRallyPointUpdated(currentSpawnFlag);
        }
        
        Debug.Log($"[FlagManager] 깃발 위치가 갱신되었습니다: {newFlagTransform.name}");
    }

    /// <summary>
    /// [내부] 깃발 설치 모드를 종료하고 모든 상태를 초기화(뒷정리)합니다.
    /// </summary>
    private void ExitPlacementMode()
    {
        isPlacing = false;
        if (flagGhost != null) Destroy(flagGhost);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }
}