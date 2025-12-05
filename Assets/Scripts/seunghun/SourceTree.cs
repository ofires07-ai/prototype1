using UnityEngine;
using System.Collections.Generic;

public class SourceTree : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("생성할 특수 자원의 프리팹을 연결하세요.")]
    public GameObject specialResourcePrefab;

    [Header("Managed Resources")]
    // 하위의 모든 자원들을 관리하는 배열
    public Source[] childResources;

    void Awake()
    {
        PlaceSpecialResource();
    }

    private void PlaceSpecialResource()
    {
        childResources = GetComponentsInChildren<Source>();
        // 2. 첫 번째 자원(인덱스 0)을 제외하고, 1번부터 끝까지 중에서 랜덤 인덱스 추첨
        int randomIndex = Random.Range(1, childResources.Length);
        
        // 당첨된 자원
        Source selectedResource = childResources[randomIndex];

        if (selectedResource != null && specialResourcePrefab != null)
        {
            // 3. 특수 자원 생성 (위치는 당첨된 자원의 위치)
            GameObject specialObj = Instantiate(specialResourcePrefab, selectedResource.transform.position, Quaternion.identity);
            specialObj.transform.localScale = Vector3.one * 2f;
            // 4. 스크립트 가져오기
            SpecialSource specialComponent = specialObj.GetComponent<SpecialSource>();

            if (specialComponent != null)
            {
                // A. 특수 자원 -> 일반 자원 연결
                specialComponent.attachedNormalSource = selectedResource;
                // B. 일반 자원 -> 특수 자원 연결
                selectedResource.attachedSpecialSource = specialComponent;

                Debug.Log($"[SourceTree] 특수 자원이 '{selectedResource.name}'에 배치되었습니다.");
            }
        }
    }
}