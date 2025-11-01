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
    private Source targetSource;    // [중요] 내가 목표로 삼은 '특정' 광물
    public SourceManager sourceManager;
    private Animator animator;
    
    // [추가] 유닛이 선택되었을 때 켜질 시각 효과 (예: 초록색 원)
    public GameObject selectionVisual;

    void Start()
    {
        aiPath = GetComponent<AIPath>();
        seeker = GetComponent<Seeker>(); // [추가]
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
                // 현재 유닛과 타겟 광물 사이의 실제 거리를 계산합니다.
                float distanceToTarget = Vector3.Distance(transform.position, targetSource.transform.position);

                // [핵심] '도착 판정 거리' (1.0)보다 멀어지면 채굴 중지
                // (살짝 밀리는 건 괜찮도록 1.5배 정도의 여유 버퍼를 줍니다)
                if (distanceToTarget > aiPath.endReachedDistance * 1.5f) 
                {
                    Debug.Log("광물에서 너무 멀어져 채굴을 중지합니다!");
                    StopMining();
                    currentState = UnitState.Idle; // 대기 상태로 전환
                }
                break;
        }
        
        // [수정] 애니메이션을 AIPath 속도 기반으로 업데이트
        UpdateAnimation();
    }
    
    // [새로 추가] 애니메이션을 실제 이동 상태에 따라 업데이트
    private void UpdateAnimation()
    {
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
    
    // (행동 1) 특정 광물을 목표로 설정하고 이동
    public void SetTargetSource(Source source)
    {
        Debug.Log("[PickUnit] SetTargetSource 호출: " + source.name);
        StopMining();
        targetSource = source; // 나의 목표 광물로 저장
        
        aiPath.destination = targetSource.transform.position; // 목표 광물 위치로 이동
        aiPath.isStopped = false;
        // [핵심 수정] 수동으로 경로 계산을 "시작"시킵니다.
        seeker.StartPath(transform.position, targetSource.transform.position);
        
        currentState = UnitState.MovingToSource; // 상태 변경
        Debug.Log("[PickUnit] 상태 변경: MovingToSource");
    }
    
    // (행동 2) 특정 위치로 이동
    public void MoveToPosition(Vector3 position)
    {
        Debug.Log("[PickUnit] MoveToPosition 호출: " + position);
        StopMining();
        targetSource = null; // 광물 목표 해제 (그냥 땅으로 가는 것이므로)
        
        aiPath.destination = position; // 클릭한 '땅' 좌표로 이동
        aiPath.isStopped = false;
        // [핵심 수정] 수동으로 경로 계산을 "시작"시킵니다.
        seeker.StartPath(transform.position, position);
        
        currentState = UnitState.MovingToPosition; // 상태 변경
    }
    
    // (행동 3) 채굴 시작 (목표 광물에 도착했을 때)
    public void StartMining()
    {
        Debug.Log("[PickUnit] StartMining 실행!");
        aiPath.isStopped = true;  // 이동 정지
        aiPath.SetPath(null);     // 현재 경로 초기화
        if (!targetSource.CanStartMining()) return;
        
        currentState = UnitState.Mining; // 상태 변경

        // [중요] 이전 대화의 SourceManager에게 이 광물을 활성화하라고 요청
        if (targetSource != null && sourceManager != null)
        {
            Debug.Log(targetSource.name + "에서 채굴을 시작합니다!");
            sourceManager.TryActivateSource(targetSource); 
        }
        else
        {
            Debug.LogError("[PickUnit] targetSource 또는 sourceManager가 null입니다!");
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

            // SourceManager에게 이 광물을 비활성화하라고 요청
            if (targetSource != null && sourceManager != null)
            {
                sourceManager.DeactivateSource(targetSource);
            }
            // 에러가 나도 이미 isStopped = false가 되었으므로
            // 다음 목적지가 설정되면 이동합니다.
        }
    }

    // AIPath가 목적지에 도착했는지 확인하는 헬퍼 함수
    private bool HasArrivedAtDestination()
    {
        // [디버그] 도착 체크 상태를 로그로 출력
        bool arrived = aiPath.reachedEndOfPath;
        
        // [추가] 거리 기반 체크도 함께 사용 (더 안정적)
        if (!arrived && targetSource != null)
        {
            float distance = Vector3.Distance(transform.position, aiPath.destination);
            // aiPath의 endReachedDistance 값 사용 (기본값 보통 0.2~1.0)
            arrived = distance < aiPath.endReachedDistance + 0.5f;
            
            if (arrived)
            {
                Debug.Log($"[PickUnit] 거리 기반 도착 감지: {distance}");
            }
        }
        
        return arrived;
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