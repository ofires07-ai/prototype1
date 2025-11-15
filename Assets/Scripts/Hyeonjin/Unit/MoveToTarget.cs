using UnityEngine;

/// <summary>
/// 이 스크립트가 붙은 오브젝트를 'target'으로 지정된 위치까지
/// 'moveSpeed'의 속도로 매 프레임 이동시킵니다.
/// </summary>
public class MoveToTarget : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("이동할 목표 지점 (Transform)")]
    public Transform target; // 1. 목표가 될 오브젝트 (예: 플레이어, 빈 오브젝트)

    [Tooltip("초당 이동 속도")]
    public float moveSpeed = 5f; // 2. 이동 속도

    // 매 프레임마다 호출됩니다.
    void Update()
    {
        // --- 1. 방어 코드 ---
        // 목표(target)가 설정되지 않았다면 (예: 파괴되었거나, 인스펙터에서 빼먹음)
        // 에러를 띄우지 말고 그냥 함수를 종료합니다.
        if (target == null)
        {
            return;
        }

        // --- 2. 이동 방향 및 거리 계산 ---
        // 이번 프레임에 이동할 수 있는 최대 거리
        // (Time.deltaTime을 곱해서 컴퓨터 성능과 관계없이 일정한 속도를 보장)
        float step = moveSpeed * Time.deltaTime; 

        // --- 3. [핵심] 이동 실행 ---
        // Vector3.MoveTowards(현재 위치, 목표 위치, 최대 이동 거리);
        // 'transform.position'을 'target.position'으로 'step'만큼 이동시킵니다.
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);
    }

    /// <summary>
    /// [선택 사항] 외부에서 이 프리펩의 타겟을 설정해줄 수 있게 하는 함수
    /// 예: HomingMissile.cs의 SetTarget(Transform newTarget)과 동일한 역할
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}