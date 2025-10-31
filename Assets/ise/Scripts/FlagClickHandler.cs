/*
FlagClickHandler.cs (수정됨)
- 설치된 깃발 클릭 시 FlagMover로 이동 모드 '직접' 전달
- Initialize(Vector3 towerPos, float radius): 타워 위치 및 설치 반경 초기화
- GetTowerPosition(), GetPlacementRadius(): FlagMover에서 필요로 하는 타워 정보 전달
- OnMouseDown(): 클릭 시 FlagMover로 이동 모드 전달
*/

using UnityEngine;

public class FlagClickHandler : MonoBehaviour
{
    private Vector3 towerCenter; // 이 깃발이 속한 타워 위치
    private float placementRadius; // 설치 가능 반경

    // 초기화: 깃발이 속한 타워 위치와 반경을 전달
    public void Initialize(Vector3 towerPos, float radius)
    {
        towerCenter = towerPos;
        placementRadius = radius;
    }

    // FlagMover에 타워 위치 전달
    public Vector3 GetTowerPosition() => towerCenter;

    // FlagMover에 설치 반경 전달
    public float GetPlacementRadius() => placementRadius;

    // [수정] 깃발 클릭 시 FlagMover의 이동 모드 직접 시작
    private void OnMouseDown()
    {
        if (!Input.GetMouseButtonDown(0)) return; // 좌클릭이 아니면 무시

        // [수정] FlagManager를 거치지 않고 FlagMover를 직접 호출
        if (FlagMover.Instance != null && FlagManager.Instance != null)
        {
            // FlagManager가 가지고 있는 범위 표시기(rangeIndicator)를 가져와서 전달
            GameObject indicator = FlagManager.Instance.rangeIndicator;
            
            FlagMover.Instance.StartMove(gameObject, towerCenter, placementRadius, indicator);
        }
    }
}