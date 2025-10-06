using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PickUnit : MonoBehaviour
{
    public Source targetSource;
    public float moveSpeed = 5f;
    public float miningPerHit = 10f;
    public float miningSpeed = 2f;
    public float stoppingDistance = 0.1f; // 목표 지점 도착 판정 거리
    
    private bool isMining = false;
    private bool isMoving = true;
    private Vector3 _targetPosition;
    private Coroutine miningCoroutine;

    void Start()
    {
        _targetPosition = transform.position;
    }

    void Update()
    {
        HandleRightClick();
        if (isMoving)
        {
            MoveToTarget();
        }
    }

    void HandleRightClick()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        
        // 1. 마우스 위치를 화면 좌표로 가져옵니다.
        Vector3 mousePos = Input.mousePosition;

        // 2. z값을 카메라와의 거리로 설정해 주어야 올바르게 변환됩니다.
        mousePos.z = 10.0f; 

        // 3. 화면 좌표를 월드 좌표로 변환하여 _targetPosition에 저장합니다.
        _targetPosition = Camera.main.ScreenToWorldPoint(mousePos);

        // (선택) 2D 게임처럼 z값이 변하지 않게 하려면 아래 코드를 추가하세요.
        _targetPosition.z = transform.position.z;
        
        // 이동 시작
        isMoving = true;
        
        // 이동 시작 시 채굴 중지
        if (isMining)
        {
            StopMining();
        }
    }

    void MoveToTarget()
    {
        // 현재 위치에서 목표 위치로 이동
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
        
        // 목표 지점에 도착했는지 거리로 확인 (부동소수점 오차 고려)
        if (Vector3.Distance(transform.position, _targetPosition) < stoppingDistance)
        {
            isMoving = false;
        }
    }

    // 채굴을 시작하는 메서드
    void StartMining()
    {
        // 이동 중이면 채굴하지 않음
        if (isMoving)
        {   
            Debug.Log("이동중이라 채굴을 시작히자 않습니다!");
            return;
        }
        
        // 이미 채굴 중이 아닌 경우에만 새로 시작
        if (miningCoroutine == null)
        {
            isMining = true; // 채굴 상태를 true로 설정
            miningCoroutine = StartCoroutine(MiningSequence()); // 채굴 코루틴 시작
            Debug.Log($"채굴 시작! Source 남은 용량: {targetSource.capacity}");
        }
    }

    // 채굴을 중지하는 메서드
    void StopMining()
    {
        // 채굴 코루틴이 실행 중인 경우
        if (miningCoroutine != null)
        {
            StopCoroutine(miningCoroutine); // 코루틴 중지
            miningCoroutine = null; // 코루틴 참조 초기화
            isMining = false; // 채굴 상태를 false로 설정
            Debug.Log("채굴 중지!");
        }
    }

    // 실제 채굴 작업을 수행하는 코루틴
    IEnumerator MiningSequence()
    {
        // 광물이 존재하고 용량이 0보다 큰 동안 반복
        while (targetSource != null && targetSource.capacity > 0)
        {
            // miningSpeed초 동안 대기 (채굴 속도 조절)
            yield return new WaitForSeconds(miningSpeed);

            // 대기 후에도 광물이 유효한지 다시 확인
            if (targetSource != null && targetSource.capacity > 0)
            {
                // 광물의 용량을 miningPerHit만큼 감소
                targetSource.capacity -= miningPerHit;
                Debug.Log($"채굴 중... 남은 용량: {targetSource.capacity}");
            }
        }

        // 광물이 다 떨어지면 채굴 중지
        Debug.Log("광물 고갈!");
        StopMining();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        isMoving = false;
    }

    // PickUnit이 다른 오브젝트와 계속 충돌 중일 때 호출되는 메서드
    private void OnCollisionStay2D(Collision2D other)
    {
        // 디버그: 충돌한 오브젝트 이름 출력
        Debug.Log($"충돌 중인 오브젝트: {other.gameObject.name}");
        
        // 충돌한 오브젝트에서 Source 컴포넌트를 찾음
        Source source = other.gameObject.GetComponent<Source>();
        
        // Source가 여전히 null이면
        if (source == null)
        {
            Debug.LogWarning($"'{other.gameObject.name}' 오브젝트에 Source 컴포넌트가 없습니다!");
            return;
        }
        
        // Source가 존재하고 용량이 0보다 큰 경우
        if (source.capacity > 0)
        {
            targetSource = source; // 타겟 광물 설정
            StartMining(); // 채굴 시작 (내부에서 isMoving 체크)
        }
    }

    // PickUnit이 다른 오브젝트와 충돌이 끝났을 때 호출되는 메서드
    private void OnCollisionExit2D(Collision2D other)
    {
        // 충돌한 오브젝트에서 Source 컴포넌트를 찾음 (부모/자식 포함)
        Source source = other.gameObject.GetComponent<Source>();
        if (source == null)
        {
            source = other.gameObject.GetComponentInParent<Source>();
        }
        if (source == null)
        {
            source = other.gameObject.GetComponentInChildren<Source>();
        }
        
        if (source != null)
        {
            StopMining(); // 채굴 중지
            targetSource = null; // 타겟 광물 초기화
        }
    }
}