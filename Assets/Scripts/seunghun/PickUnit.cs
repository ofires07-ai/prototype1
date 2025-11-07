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
    public SourceManager sourceManager;
    private Animator animator;
    
    // [추가] 유닛이 선택되었을 때 켜질 시각 효과 (예: 초록색 원)
    public GameObject selectionVisual;

    void Start()
    {
        aiPath = GetComponent<AIPath>();
        seeker = GetComponent<Seeker>();
        animator = GetComponent<Animator>();
        
        // (선택) SourceManager를 자동으로 찾아 연결
        if (sourceManager == null)
        {
            sourceManager = FindObjectOfType<SourceManager>();
        }

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
                break;
                
            case UnitState.MovingToSource:
                // [중요] '목표 광물'에 도착했는지 체크
                if (HasArrivedAtDestination())
                {
                    Debug.Log("[PickUnit] 광물 도착! 채굴 시작 호출");
                    StartMining(); // 도착했으면 채굴 시작
                }
                break;

            case UnitState.Mining:
                // ✅ FIX: null check 추가
                if (targetSource == null)
                {
                    Debug.LogWarning("[PickUnit] targetSource가 null입니다. Idle로 전환합니다.");
                    currentState = UnitState.Idle;
                    break;
                }
                
                float distanceToTarget = Vector3.Distance(transform.position, targetSource.transform.position);

                if (distanceToTarget > aiPath.endReachedDistance * 1f || !targetSource.CanStartMining())
                {
                    // (선택) 통합된 로그 메시지
                    Debug.Log("조건 미달(거리 이탈 또는 부모 중지)로 채굴을 중지합니다.");
                    StopMining();
                    currentState = UnitState.Idle; // 대기 상태로 전환
                }
                break;
        }
        
        HandleSpriteFlip();
        // [수정] 애니메이션을 AIPath 속도 기반으로 업데이트
        UpdateAnimation();
    }
    
    private void HandleSpriteFlip()
    {
        // AIPath의 수평 속도를 가져옵니다.
        float horizontalVelocity = aiPath.velocity.x;

        // 0.1f보다 작으면 (거의 멈췄으면) 방향을 바꾸지 않습니다 (깜빡임 방지).
        if (Mathf.Abs(horizontalVelocity) > 0.1f)
        {
            // 현재 스케일 값을 가져옵니다.
            Vector3 newScale = transform.localScale;

            if (horizontalVelocity > 0.1f)
            {
                // 오른쪽으로 이동: X 스케일을 1로 (양수)
                newScale.x = 1f;
            }
            else if (horizontalVelocity < -0.1f)
            {
                // 왼쪽으로 이동: X 스케일을 -1로 (음수)
                newScale.x = -1f;
            }

            // 변경된 스케일 값을 적용합니다.
            transform.localScale = newScale;
        }
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
        aiPath.isStopped = false;
        
        // 'resource'가 null이 아님이 보장된 상태에서 호출
        seeker.StartPath(transform.position, targetSource.transform.position, OnPathComplete);
        
        currentState = UnitState.MovingToSource; 
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
         
        // [수정] CanStartMining() 체크를 여기서 삭제합니다!
        
        currentState = UnitState.Mining; // 상태 변경 (일단 '채굴 중'으로)

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
            Debug.Log("채굴을 중지하고 이동 준비.");

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