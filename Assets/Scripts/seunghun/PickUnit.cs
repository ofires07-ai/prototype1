using System;
using System.Collections;
using Pathfinding;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
public class PickUnit : MonoBehaviour
{
    // 1. 유닛의 현재 상태를 정의 (Enum)
    public enum UnitState
    {
        Idle,             // 대기
        MovingToPosition, // 좌표로 이동 중
        MovingToSource,   // 광물로 이동 중
        Mining            // 채굴 중
    }
    public UnitState currentState = UnitState.Idle; // 현재 상태
    
    // 2. 참조 변수
    private AIPath aiPath;        // 길찾기 및 이동 담당 (A* 버전)
    private Seeker seeker;          // [추가] 경로 계산을 "요청"하는 컴포넌트
    private MineableResource targetSource;    // [중요] 내가 목표로 삼은 '특정' 광물
    private MiningSpot currentMiningSpot;        // [추가] 나의 '물리적' 타겟 (주차 공간)
    public SourceManager sourceManager;
    private Animator animator;
    
    // [추가] 유닛이 선택되었을 때 켜질 시각 효과 (예: 초록색 원)
    public GameObject selectionVisual;
    private Rigidbody2D rb;         // [추가] 리지드바디 참조
    private Collider2D unitCollider; // [추가] 콜라이더 참조
    private CrimerAbility myAbility;
    
    // [추가] 막힘 감지를 위한 변수
    private float stuckTimer = 0f;
    // 1.5초 이상 속도가 0이면 '막혔다'고 판단
    private const float STUCK_TIME_THRESHOLD = 1.0f;
    
    [Header("UI")]
    private TextMeshProUGUI nameTagText;
    
    void Awake()
    {
        aiPath = GetComponent<AIPath>();
        seeker = GetComponent<Seeker>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        unitCollider = GetComponent<Collider2D>();
        myAbility = GetComponent<CrimerAbility>();
        nameTagText = GetComponentInChildren<TextMeshProUGUI>(true);
        
        if (sourceManager == null)
        {
            sourceManager = FindObjectOfType<SourceManager>();
        }
    }
    void Start()
    {
        Deselect();
        if (myAbility != null)
        {
            myAbility.ApplyAbility();
            nameTagText.text = myAbility.AbilityName;
        }
        else
        {
            nameTagText.text = "일반 죄수"; // 능력이 없을 때
            Debug.LogWarning(name + "에게 Ability가 없습니다! (기본 유닛)");
        }
    }
    
    void Update()
    {
        // 1. FSM: 'switch'가 현재 상태에 따라 올바른 함수를 호출합니다.
        switch (currentState)
        {
            case UnitState.Idle:
                UpdateIdleState();
                break;
                
            case UnitState.MovingToPosition:
            case UnitState.MovingToSource:
                UpdateMovingState(); // ⬅️ 두 이동 상태를 하나로 통합
                break;

            case UnitState.Mining:
                UpdateMiningState();
                break;
        }
        
        // 2. 애니메이션은 FSM이 결정한 '최종 상태'를 기반으로 매번 업데이트됩니다.
        UpdateAnimation();
    }
    
    // --- FSM 상태별 함수들 ---

    private void UpdateIdleState()
    {
        // 할 일 없음 (대기)
        stuckTimer = 0f; // 막힘 타이머 리셋
    }

    private void UpdateMovingState()
    {
        // 1. 이동 중일 때만 좌우 반전
        HandleSpriteFlip();

        // 2. 목적지에 도착했는지 체크
        if (HasArrivedAtDestination())
        {
            stuckTimer = 0f; // 도착했으므로 타이머 리셋
            
            if (currentState == UnitState.MovingToSource)
            {
                Debug.Log("[PickUnit] 광물 도착! 채굴 시작 호출");
                StartMining(); // 광물이었으면 채굴 시작
            }
            else // MovingToPosition
            {
                Debug.Log("[PickUnit] 목적지 도착! Idle로 전환");
                currentState = UnitState.Idle;
                aiPath.isStopped = true;
                aiPath.SetPath(null);
            }
        }
        // 3. [막힘 감지] 도착 안 했는데 속도가 0에 가까우면
        else if (aiPath.velocity.magnitude < 0.1f && !aiPath.pathPending)
        {
            stuckTimer += Time.deltaTime; 
            if (stuckTimer > STUCK_TIME_THRESHOLD)
            {
                Debug.LogWarning("[PickUnit] 이동 중 막혔습니다! 스팟을 반납하고 Idle로 전환합니다.");
                
                // 'MovingToSource' 상태였다면 스팟을 "반납"해야 함
                if (currentState == UnitState.MovingToSource)
                {
                    ReleaseCurrentMiningSpot(); 
                }
                
                currentState = UnitState.Idle;
                aiPath.isStopped = true;
                stuckTimer = 0f;
            }
        }
        // 4. 정상 이동 중이면 타이머 리셋
        else
        {
            stuckTimer = 0f;
        }
    }
    
    private void UpdateMiningState()
    {
        stuckTimer = 0f; // 채굴 중이므로 타이머 리셋

        // 1. 'null'인지 "먼저" 확인
        if (targetSource == null)
        {
            Debug.LogWarning("[PickUnit] targetSource가 null입니다. Idle로 전환합니다.");
            StopMining(); 
            currentState = UnitState.Idle;
            return; // 'break' 대신 'return'을 써서 즉시 Update() 종료
        }

        // --- 이 아래부터는 targetSource가 'null'이 아님이 보장됩니다 ---

        // 2. 타겟 바라보기
        LookAtTarget(targetSource.transform.position);

        // 3. 모든 중지 조건(고갈, 부모)을 한 번에 검사
        bool isDepleted = targetSource.IsDepleted();
        bool canMine = targetSource.CanStartMining();

        if ((!canMine && !CanIgnoreParentRule()) || isDepleted)
        {
            // (로그 메시지)
            if (isDepleted) Debug.Log(targetSource.name + " 고갈로 채굴 중지.");
            else Debug.Log("부모 노드 중지로 채굴 중지.");
            
            StopMining();
            currentState = UnitState.Idle; 
        }
    }
    
    // [새 함수] Mining 상태일 때, '목표 위치'를 바라보도록 스프라이트를 뒤집습니다.
    private void LookAtTarget(Vector3 targetPosition)
    {
        // 유닛의 X위치와 타겟의 X위치를 비교합니다.
        float horizontalDirection = targetPosition.x - transform.position.x;
        
        // SetSpriteFlip 함수를 호출하여 스케일을 변경합니다.
        SetSpriteFlip(horizontalDirection);
    }

    // [새 함수] HandleSpriteFlip에서 스케일 변경 로직만 분리한 헬퍼 함수
    // (이걸로 HandleSpriteFlip과 LookAtTarget이 코드를 공유합니다)
    private void SetSpriteFlip(float horizontalDirection)
    {
        // 0.1f보다 작으면 (거의 정중앙이면) 방향을 바꾸지 않습니다.
        if (Mathf.Abs(horizontalDirection) > 0.1f)
        {
            Vector3 newScale = transform.localScale;

            if (horizontalDirection > 0)
            {
                // 오른쪽을 바라봄 (X 스케일 1)
                newScale.x = 1f;
            }
            else
            {
                // 왼쪽을 바라봄 (X 스케일 -1)
                newScale.x = -1f;
            }
            transform.localScale = newScale;
        }
    }
    
    private void HandleSpriteFlip()
    {
        // AIPath의 수평 속도를 가져옵니다.
        float horizontalVelocity = aiPath.velocity.x;
        
        // SetSpriteFlip 함수를 호출하여 스케일을 변경합니다.
        SetSpriteFlip(horizontalVelocity);
    }
    
    // [새로 추가] 애니메이션을 실제 이동 상태에 따라 업데이트
    private void UpdateAnimation()
    {
        // ✅ FIX: animator null check 추가
        if (animator == null) return;
        
        // AIPath의 실제 속도를 기반으로 걷기 여부 판단
        bool isMoving = aiPath.velocity.magnitude > 0.1f && !aiPath.isStopped;
        bool isMining = currentState == UnitState.Mining;
        
        // unitState Integer 파라미터 업데이트 (Animator Controller가 이걸 사용한다면)
        animator.SetInteger("unitState", (int)currentState);
        
        // Bool 파라미터들도 설정 (더 직관적)
        // Animator Controller에 이 파라미터들을 추가하세요:
        // - isWalking (Bool)
        // - isMining (Bool)
        animator.SetBool("isWalking", isMoving);
        animator.SetBool("isMining", isMining);
        
    }
    
    // // "SourceManager야, 내가 이번에 캘 자원은 이거야!"
    // public ResourceType GetMinedResourceType()
    // {
    //     // (안전장치) targetSource가 없으면 일단 기본값 반환
    //     if (targetSource == null) return ResourceType.Tier1; // (기본 자원 타입으로 변경)
    //
    //     if (myAbility == null)
    //     {
    //         // 능력이 없으면 (기본 유닛), 붙어있는 자원 타입을 반환
    //         return targetSource.resourceType; 
    //     }
    //     
    //     // 내 능력이 결정한 자원 타입을 반환
    //     // (이때 'targetSource'를 인자로 넘겨서, 기본 능력이 참조할 수 있게 함)
    //     return myAbility.GetMinedResourceType(targetSource);
    // }
    // [새 헬퍼 함수]
    // "SourceManager야, 내 '최종 작업 원장'을 받아!"
    public MiningTickResult GetMiningTickResult()
    {
        if (targetSource == null) 
            return new MiningTickResult { Type = ResourceType.Tier1, Amount = 0 }; // 비상 탈출

        if (myAbility == null)
        {
            // 능력 없음 (기본 행동)
            return new MiningTickResult 
            {
                Type = targetSource.resourceType, 
                Amount = targetSource.amountPerTick 
            };
        }
        
        // 내 능력이 계산한 '최종 원장'을 반환
        return myAbility.ProcessMiningTick(targetSource);
    }
    
    // (행동 1) 특정 광물을 목표로 설정하고 이동 (방탄 버전)
    public void SetTargetSource(MineableResource resource)
    {
        // [핵심] 'null'이 들어오면 크래시를 내기 전에 즉시 함수를 중단!
        if (resource == null)
        {
            Debug.LogError("[PickUnit] SetTargetSource가 'null' 리소스를 받았습니다! 이동을 취소합니다.");
            // (이 경우, 유닛은 StopMining만 실행되고 Idle 상태로 돌아가게 됩니다)
            StopMining(); 
            currentState = UnitState.Idle;
            return;
        }

        Debug.Log("[PickUnit] SetTargetSource 호출: " + resource.name);
        
        try
        {
            StopMining();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PickUnit] StopMining 중 에러 발생: {e.Message}");
        }
        
        targetSource = resource; 
        // 1. "비어있는 가장 가까운 주차 공간"을 광물에게 물어봅니다.
        MiningSpot targetSpot = targetSource.GetClosestMiningSpot(transform.position);

        // [핵심] 2. 스팟을 찾았는지 확인
        if (targetSpot != null)
        {
            // 3. [예약] 스팟을 찾았으면, 내가 찜합니다.
            targetSpot.isOccupied = true;
            currentMiningSpot = targetSpot; // 내 스팟으로 저장

            // 4. AIPath/Seeker 설정
            aiPath.isStopped = false;
            seeker.StartPath(transform.position, currentMiningSpot.transform.position, OnPathComplete);
            
            currentState = UnitState.MovingToSource; 
        }
        else
        {
            // 5. [예외] 빈 스팟이 없음
            Debug.Log(resource.name + "에 빈 채굴 지점이 없습니다!");
            currentState = UnitState.Idle; // 이동 포기
        }
    }
    
    // 현재 스팟을 '반납'하는 헬퍼 함수
    private void ReleaseCurrentMiningSpot()
    {
        if (currentMiningSpot != null)
        {
            Debug.Log("Mining Spot을 반납합니다!");
            currentMiningSpot.isOccupied = false; // "이제 비었어요"
            currentMiningSpot = null;
        }
    }
    
    // (행동 2) 특정 위치로 이동
    public void MoveToPosition(Vector3 position)
    {
        Debug.Log("[PickUnit] MoveToPosition 호출: " + position);
        
        // ✅ FIX: 안전하게 채굴 중지
        try
        {
            StopMining();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PickUnit] StopMining 중 에러 발생: {e.Message}");
        }
        
        targetSource = null; // 광물 목표 해제 (그냥 땅으로 가는 것이므로)
        
        // ✅ FIX: AIPath 설정 순서 수정
        aiPath.isStopped = false;
        
        // [핵심 수정] Seeker를 먼저 호출 - AIPath가 자동으로 경로를 따라감
        seeker.StartPath(transform.position, position, OnPathComplete);
        
        currentState = UnitState.MovingToPosition; // 상태 변경
    }
    
    // ✅ NEW: 경로 계산 완료 콜백 (크래시 방지)
    private void OnPathComplete(Path p)
    {
        if (p.error)
        {
            Debug.LogError($"[PickUnit] 경로 계산 실패: {p.errorLog}");
            currentState = UnitState.Idle;
            aiPath.isStopped = true;
            // 스팟을 "반납(예약 취소)"해야 합니다.
            ReleaseCurrentMiningSpot();
            return;
        }
        
        Debug.Log("[PickUnit] 경로 계산 완료!");
        // AIPath가 자동으로 경로를 따라가므로 추가 작업 불필요
    }
    
    // (행동 3) 채굴 시작 (목표 광물에 도착했을 때)
    public void StartMining()
    {
        Debug.Log("[PickUnit] StartMining 실행! SourceManager에 활성화 요청...");
        aiPath.isStopped = true;  // 이동 정지
        aiPath.SetPath(null);     // 현재 경로 초기화
        
        // ✅ FIX: null check 강화
        if (targetSource == null)
        {
            Debug.LogError("[PickUnit] targetSource가 null입니다!");
            currentState = UnitState.Idle;
            return;
        }
        
        currentState = UnitState.Mining; // 상태 변경 (일단 '채굴 중'으로)
        
        // [핵심 1] 나 자신을 'Obstacle' 레이어로 변경
        gameObject.layer = LayerMask.NameToLayer("Obstacle");
        // [핵심 2] 물리적으로도 '벽'이 되도록 Static으로 변경
        rb.bodyType = RigidbodyType2D.Static;
        // [핵심 3] A*에게 "내 위치"만 실시간으로 갱신하라고 명령
        // (A*는 'Obstacle' 레이어를 감지하고 이 위치를 '길 없음(빨간색)'으로 만듦)
        if (unitCollider != null)
        {
            AstarPath.active.UpdateGraphs(unitCollider.bounds);
        }

        // SourceManager에게 활성화를 요청
        if (sourceManager != null)
        {
            try
            {
                // SourceManager가 규칙을 체크하고 활성화할 것입니다.
                sourceManager.TryActivateSource(targetSource, this); 
            }
            catch (Exception e)
            {
                Debug.LogError($"[PickUnit] TryActivateSource 중 에러 발생: {e.Message}");
                currentState = UnitState.Idle;
            }
        }
        else
        {
            Debug.LogError("[PickUnit] sourceManager가 null입니다!");
            currentState = UnitState.Idle;
        }
    }
    
    // [수정된 함수] 채굴을 중지하고 AIPath를 다시 활성화합니다.
    public void StopMining()
    {
        // "현재" 상태가 채굴 중일 때만 중지 로직을 실행
        if (currentState == UnitState.Mining)
        {
            ReleaseCurrentMiningSpot();
            Debug.Log("채굴을 중지하고 이동 준비.");
            
            // [핵심 1] 나 자신을 다시 'Unit' 레이어로 복구
            gameObject.layer = LayerMask.NameToLayer("Unit");
            // [핵심 2] 다시 움직일 수 있도록 Dynamic으로 변경
            rb.bodyType = RigidbodyType2D.Dynamic;
            // [핵심 3] A*에게 "내 위치"를 다시 갱신하라고 명령
            // (A*는 'Unit' 레이어를 무시하고 이 위치를 '길 있음(파란색)'으로 만듦)
            if (unitCollider != null)
            {
                 AstarPath.active.UpdateGraphs(unitCollider.bounds);
            }

            // [중요] AIPath의 멈춤 상태를 "먼저" 해제합니다.
            // 이렇게 하면 StopMining에서 에러가 나도 이동은 가능해집니다.
            aiPath.isStopped = false;

            // ✅ FIX: 안전하게 DeactivateSource 호출
            if (targetSource != null && sourceManager != null)
            {
                try
                {
                    sourceManager.DeactivateSource(this);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PickUnit] DeactivateSource 중 에러 발생: {e.Message}");
                }
            }
            // 에러가 나도 이미 isStopped = false가 되었으므로
            // 다음 목적지가 설정되면 이동합니다.
        }
    }

    // AIPath가 목적지에 도착했는지 확인하는 헬퍼 함수
    private bool HasArrivedAtDestination()
    {
        // "Recalculate Path Automatically"를 껐으므로,
        // 이 플래그는 도착 시 true가 되고 리셋되지 않아 가장 신뢰할 수 있습니다.
        return aiPath.reachedEndOfPath;
    }
    
    // 매니저가 이 유닛을 "선택"했을 때 호출할 함수
    public void Select()
    {
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(true);
        }
    }

    // 매니저가 이 유닛을 "선택 해제"했을 때 호출할 함수
    public void Deselect()
    {
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(false);
        }
    }
    
    // "내가 규칙 무시 능력을 가졌는지?"를 반환합니다.
    public bool CanIgnoreParentRule()
    {
        if (myAbility == null) return false;
        return myAbility.CanIgnoreParentRule();
    }
}