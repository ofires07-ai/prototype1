using System;
using System.Collections;
using Pathfinding;
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
    
    void Awake()
    {
        aiPath = GetComponent<AIPath>();
        seeker = GetComponent<Seeker>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>(); // [추가]
        unitCollider = GetComponent<Collider2D>(); // [추가]

        if (sourceManager == null)
        {
            sourceManager = FindObjectOfType<SourceManager>();
        }
    }
    void Start()
    {
        Deselect();
    }
    
    void Update()
    {
        // 1. 상태별 행동 처리
        // FSM (Finite State Machine, 유한 상태 기계)
        switch (currentState)
        {
            case UnitState.Idle:
                // 할 일 없음 (대기)
                break;
            case UnitState.MovingToPosition:
                // 목적지에 도착했는지 체크
                if (HasArrivedAtDestination())
                {
                    Debug.Log("[PickUnit] 목적지 도착! Idle로 전환");
                    currentState = UnitState.Idle; // 도착했으면 대기 상태로 변경
                    // [핵심 수정] Idle이 될 때 AIPath를 명확히 멈춥니다.
                    aiPath.isStopped = true;
                    aiPath.SetPath(null); // 경로도 깨끗이 지웁니다.
                }
                HandleSpriteFlip();
                break;
                
            case UnitState.MovingToSource:
                // [중요] '목표 광물'에 도착했는지 체크
                if (HasArrivedAtDestination())
                {
                    Debug.Log("[PickUnit] 광물 도착! 채굴 시작 호출");
                    StartMining(); // 도착했으면 채굴 시작
                }
                HandleSpriteFlip();
                break;

            case UnitState.Mining:
                LookAtTarget(targetSource.transform.position);
                // ✅ FIX: null check 추가
                if (targetSource == null)
                {
                    Debug.LogWarning("[PickUnit] targetSource가 null입니다. Idle로 전환합니다.");
                    currentState = UnitState.Idle;
                    break;
                }

                if (!targetSource.CanStartMining() || targetSource.IsDepleted())
                {
                    // (선택) 통합된 로그 메시지
                    Debug.Log("부모 채굴 중지 또는 자원 고갈로 채굴을 중지합니다.");
                    StopMining();
                    currentState = UnitState.Idle; // 대기 상태로 전환
                }
                break;
        }
        // [수정] 애니메이션을 AIPath 속도 기반으로 업데이트
        UpdateAnimation();
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
                sourceManager.TryActivateSource(targetSource); 
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
                    sourceManager.DeactivateSource(targetSource);
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
}