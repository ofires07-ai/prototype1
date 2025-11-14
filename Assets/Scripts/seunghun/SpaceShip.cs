using System.Collections.Generic;
using UnityEngine;

public class SpaceShip : MonoBehaviour
{
    
    public CrimerManager crimerManager; 
    [Header("죄수 스폰 설정")]
    [Tooltip("추첨에서 뽑힌 죄수 5명의 프리팹 리스트, initSelectedPrisoner를 통한 초기화 필수")]
    public List<PickUnit> selectedPrisonerPrefabs; // ⬅️ (추첨 시스템이 이 리스트를 채워야 함)
    [Tooltip("죄수들이 '생성'될 위치 (우주선 문 앞)")]
    public Transform spaceshipSpawnPoint; 
    [Tooltip("죄수들이 '이동할' 최종 도착 지점들 (5개)")]
    public List<Transform> rallyPoints; 

    public void Init()
    {
        if (crimerManager == null)
        {
            crimerManager = GetComponent<CrimerManager>();
        }
        
        selectedPrisonerPrefabs = crimerManager.GetCurrentDraft();
    }
    
    private void SpawnAndMovePrisoners()
    {
        if (selectedPrisonerPrefabs == null || selectedPrisonerPrefabs.Count == 0)
        {
            Debug.LogError("스폰할 죄수가 없습니다!");
            return;
        }

        for (int i = 0; i < selectedPrisonerPrefabs.Count; i++)
        {
            // [핵심 수정]
            // 1. 유닛을 스폰합니다.
            // Instantiate는 'PickUnit' 프리팹을 넣으면,
            // 생성된 오브젝트의 'PickUnit' 컴포넌트를 "바로" 반환해줍니다.
            PickUnit pickUnitScript = Instantiate(
                selectedPrisonerPrefabs[i], // ⬅️ PickUnit 프리팹
                rallyPoints[i].position, 
                rallyPoints[i].rotation
            );
/*  !!!!!!!!!!!!!!!!!!!!나중에 구현할 코드임. 지우기 금지!!!!!!!!!!!!!!!!
            // 2. [수정] GetComponent가 필요 없어졌습니다.
            if (pickUnitScript == null)
            {
                Debug.LogError(selectedPrisonerPrefabs[i].name + " 스폰에 실패했습니다.");
                continue; 
            }

            // 3. 도착 지점 할당
            int spotIndex = i % rallyPoints.Count;
            Vector3 destination = rallyPoints[spotIndex].position;

            // 4. 이동 명령
            pickUnitScript.MoveToPosition(destination);
            */
        } 
    }
}
