/*
BuildMenuUI.cs
- UI 버튼 클릭 이벤트를 받아, 'TowerBuildManager'에게 실제 타워 건설을 요청합니다.
- 이 스CRIPT는 'Canvas'나 'BuildMenuPanel' 등 UI 관련 오브젝트에 붙여서 사용합니다.

[설정 방법]
1.  이 스크립트를 UI 오브젝트(예: BuildMenuPanel)에 추가합니다.
2.  인스펙터(Inspector) 창에서 'Defense Tower Prefab'과 'Spawn Tower Prefab' 슬롯에
    프로젝트에 있는 각 타워 프리팹 원본을 드래그하여 연결합니다.
3.  '방어 타워 건설 버튼'의 OnClick() 이벤트에 이 스크립트의 'OnClickDefenseTowerButton()' 함수를 연결합니다.
4.  '스폰 타워 건설 버튼'의 OnClick() 이벤트에 이 스크립트의 'OnClickSpawnTowerButton()' 함수를 연결합니다.
*/

using UnityEngine;

public class BuildMenuUI : MonoBehaviour
{
    // [인스펙터 연결 필요]
    // 유니티 에디터의 인스펙터 창에서 연결해야 하는 변수들입니다.
    [Header("타워 프리팹")]
    public GameObject defenseTowerPrefab; // 방어 타워 원본 프리팹
    public GameObject spawnTowerPrefab;   // 스폰 타워 원본 프리팹

    // --- 1. 방어 타워 건설 버튼 ---

    /// <summary>
    /// [UI 버튼 연결] 방어 타워 건설 버튼의 OnClick() 이벤트에 연결할 함수입니다.
    /// </summary>
    public void OnClickDefenseTowerButton()
    {
        // 프리팹이 인스펙터에 연결되었는지 확인
        if (defenseTowerPrefab == null)
        {
            Debug.LogError("[BuildMenuUI] DefenseTowerPrefab이 연결되지 않았습니다.");
            return;
        }

        // 'TowerBuildManager'의 건설 모드 시작 함수를 호출합니다.
        // - 첫 번째 인자: 'defenseTowerPrefab' (이것을 지어달라고 전달)
        // - 두 번째 인자: 'null' (타워 건설이 끝나면? "아무것도 안 해도 돼" 라는 의미)
        TowerBuildManager.Instance.EnterBuildMode(defenseTowerPrefab, null);
    }

    // --- 2. 스폰 타워 건설 버튼 ---

    /// <summary>
    /// [UI 버튼 연결] 스폰 타워 건설 버튼의 OnClick() 이벤트에 연결할 함수입니다.
    /// </summary>
    public void OnClickSpawnTowerButton()
    {
        // 프리팹이 인스펙터에 연결되었는지 확인
        if (spawnTowerPrefab == null)
        {
            Debug.LogError("[BuildMenuUI] SpawnTowerPrefab이 연결되지 않았습니다.");
            return;
        }

        // 'TowerBuildManager'의 건설 모드 시작 함수를 호출합니다.
        // - 첫 번째 인자: 'spawnTowerPrefab' (이것을 지어달라고 전달)
        // - 두 번째 인자: (pos) => { ... } (람다식 콜백 함수)
        //   (타워 건설이 끝나면? "내가 지금 주는 이 함수를 실행해 줘")
        TowerBuildManager.Instance.EnterBuildMode(spawnTowerPrefab, (pos) =>
        {
            // [콜백 함수 내용]
            // 'pos'는 TowerBuildManager가 타워를 건설한 '실제 위치'입니다.
            // FlagManager가 있다면, 그 'pos' 위치를 기준으로 깃발 설치 모드를 시작합니다.
            if (FlagManager.Instance != null)
            {
                FlagManager.Instance.StartFlagPlacement(pos);
            }
        });
    }
}