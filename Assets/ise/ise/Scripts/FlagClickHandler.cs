/*
 * FlagClickHandler.cs
 * -------------------
 * 이 스크립트는 게임 내에 '설치된' 깃발(Flag) 게임 오브젝트에 부착됩니다.
 * 사용자가 이 깃발을 클릭했을 때의 동작을 정의합니다.
 *
 * 주요 기능:
 * 1. 깃발이 속한 타워의 위치(towerCenter)와 설치 가능 반경(placementRadius) 정보를 저장합니다.
 * 2. 깃발이 클릭되면(OnMouseDown), 'FlagMover' 싱글톤을 직접 호출하여
 * 깃발 이동 모드(StartMove)를 시작시킵니다.
 */

using UnityEngine;

// MonoBehaviour를 상속받아 Unity 게임 오브젝트에 컴포넌트로 부착될 수 있습니다.
public class FlagClickHandler : MonoBehaviour
{
    // 이 깃발이 종속된(설치된) 타워의 중심 위치를 저장하는 변수입니다.
    // 깃발을 이동시킬 때 이 위치를 기준으로 반경 제한을 둡니다.
    private Vector3 towerCenter; 

    // 깃발이 'towerCenter'로부터 떨어질 수 있는 최대 거리를 저장하는 변수입니다.
    private float placementRadius; 

    /// <summary>
    /// 깃발 오브젝트가 생성되거나 초기화될 때 외부(예: FlagManager)에서 호출하는 함수입니다.
    /// 이 깃발이 알아야 할 필수 정보(타워 위치, 반경)를 설정합니다.
    /// </summary>
    /// <param name="towerPos">이 깃발이 속한 타워의 월드 좌표</param>
    /// <param name="radius">이 깃발이 타워로부터 이동할 수 있는 최대 반경</param>
    public void Initialize(Vector3 towerPos, float radius)
    {
        towerCenter = towerPos;
        placementRadius = radius;
    }

    /// <summary>
    /// 이 깃발이 속한 타워의 위치를 반환합니다. (Getter 함수)
    /// FlagMover가 이동 제한의 기준점으로 사용하기 위해 호출할 수 있습니다.
    /// </summary>
    /// <returns>저장된 타워의 중심 위치 (Vector3)</returns>
    public Vector3 GetTowerPosition() => towerCenter; // C# 6.0 이상 'Expression-bodied member'

    /// <summary>
    /// 이 깃발의 설치 가능 반경을 반환합니다. (Getter 함수)
    /// FlagMover가 이동 제한 반경으로 사용하기 위해 호출할 수 있습니다.
    /// </summary>
    /// <returns>저장된 설치 가능 반경 (float)</returns>
    public float GetPlacementRadius()
    {
      return placementRadius;  
    }  // C# 6.0 이상 'Expression-bodied member'

    /// <summary>
    /// Unity 엔진에 의해 자동으로 호출되는 메시지 함수입니다.
    /// 이 스크립트가 부착된 게임 오브젝트의 Collider가 마우스 커서에 의해 클릭(눌리는 순간)되면 실행됩니다.
    /// </summary>
    private void OnMouseDown()
    {
        // OnMouseDown은 모든 마우스 버튼 클릭에 반응할 수 있습니다.
        // 따라서 'Input.GetMouseButtonDown(0)'을 통해 '좌클릭'이 발생한 바로 그 프레임인지 
        // 한 번 더 확인하여, 우클릭 등 다른 입력을 무시합니다. (0 = 좌클릭, 1 = 우클릭, 2 = 휠클릭)
        if (!Input.GetMouseButtonDown(0)) return; 

        // [핵심 로직] 깃발 이동 시작
        // FlagMover와 FlagManager의 싱글톤 인스턴스가 현재 씬에 존재하는지 확인합니다.
        // (Null 참조 오류 방지)
        if (FlagMover.Instance != null && FlagManager.Instance != null)
        {
            // FlagManager가 관리하는 '범위 표시기(rangeIndicator)' 오브젝트의 참조를 가져옵니다.
            // 이 표시기는 깃발을 옮길 때 설치 가능한 범위를 시각적으로 보여주는 데 사용됩니다.
            GameObject indicator = FlagManager.Instance.rangeIndicator;
            
            // FlagMover의 StartMove 함수를 호출하여 깃발 이동 모드를 활성화합니다.
            // 필요한 모든 정보를 인자로 전달합니다.
            FlagMover.Instance.StartMove(
                gameObject,        // [이동할 대상] : 이 스크립트가 붙어있는 '바로 이 깃발' 오브젝트
                towerCenter,       // [이동 기준점] : 깃발이 속한 타워의 위치
                placementRadius,   // [이동 제한 반경] : 타워 위치로부터의 최대 거리
                indicator          // [시각 보조 도구] : 이동 가능 범위를 보여줄 '범위 표시기' 오브젝트
            );
        }
        // 만약 FlagMover.Instance 또는 FlagManager.Instance가 null이라면
        // (즉, 씬에 해당 매니저가 없다면) 아무 동작도 하지 않습니다.
    }
}