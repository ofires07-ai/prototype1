using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourceManager : MonoBehaviour
{
    // [중요] 현재 활발하게 채굴 중인 자원들만 담는 리스트
    private List<MineableResource> activeSources = new List<MineableResource>();
    
    // 자원 총량을 관리할 인벤토리 (인스펙터에서 연결)
    public InventoryManager inventoryManager;

    [Header("채굴 설정")]
    public float collectionInterval = 1.0f; // 1초마다 수집
    private float tickTimer = 0f;

    void Update()
    {
        // 1. 타이머
        tickTimer += Time.deltaTime;

        // 2. 수집 주기 도달
        if (tickTimer >= collectionInterval)
        {
            tickTimer -= collectionInterval;
            CollectResources();
        }
    }

    private void CollectResources()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            MineableResource resource = activeSources[i];
            float amountToMine; // 이번 틱에 실제 채굴할 양

            // 1. [핵심] 이 자원이 'SpecialSource' 타입인지 확인합니다.
            if (resource is SpecialSource specialSource)
            {
                // [수정] (남은 용량)과 (틱당 채굴량) 중 더 작은 값을 선택
                amountToMine = Mathf.Min(specialSource.amountPerTick, (float) specialSource.capacity);

                // 2. 실제 채굴한 만큼만 용량 감소
                specialSource.remaining -= amountToMine;

                // 3. 용량이 0 이하가 되었는지 확인
                if (specialSource.remaining <= 0)
                {
                    Debug.Log(specialSource.name + " (특수 자원)이 고갈되었습니다. specialSource.capacity: " + specialSource.capacity);
                    specialSource.StopMining();
                    activeSources.RemoveAt(i);
                    Destroy(specialSource.gameObject);
                    inventoryManager.AddResource(resource.resourceType, (int) specialSource.capacity);
                }
            }
            else // 'resource'가 'Source' (일반 자원) 타입일 경우
            {
                Source s = resource as Source;
                // 일반 자원은 용량이 무한하므로, 틱당 채굴량만큼 캡니다.
                amountToMine = s.amountPerTick;
                inventoryManager.AddResource(resource.resourceType, (int) amountToMine);
            }
        }
    }
    
// [수정] DeactivateSource 함수
    public void DeactivateSource(MineableResource resourceToDeactivate)
    {
        // [중요] 이 함수는 이제 코루틴을 멈추는 역할도 합니다.
        if (resourceToDeactivate == null) return;

        // 1. 리소스의 상태를 '채굴 중 아님'으로 변경
        // (이것만으로도 대기 중인 코루틴이 활성화를 중단합니다.)
        resourceToDeactivate.StopMining(); 
        
        // 2. 이미 활성 리스트에 있다면 제거
        if (activeSources.Contains(resourceToDeactivate))
        {
            activeSources.Remove(resourceToDeactivate);
            Debug.Log(resourceToDeactivate.name + " 자원을 활성 리스트에서 제거합니다.");
        }
    }
    
    // [변경] 이 함수는 플레이어가 특정 자원을 클릭할 때 호출됩니다.
    public void TryActivateSource(MineableResource targetSource)
    {
        // 1. 기본 검사
        if (targetSource.IsMining() || !targetSource.CanStartMining())
        {
            Debug.Log("이미 채굴 중이거나, 부모가 비활성화되어 채굴을 시작할 수 없습니다.");
            
            // [중요] PickUnit이 Mining 상태로 고정되는 것을 방지
            // 만약 PickUnit이 Mining 상태로 전환된 직후 이 검사에 실패했다면,
            // PickUnit의 상태를 다시 Idle로 돌려야 할 수도 있습니다. (현재 PickUnit 코드는 스스로 처리함)
            return;
        }

        // 2. [핵심] 즉시 활성화하는 대신, 코루틴을 시작합니다.
        StartCoroutine(ActivateResourceAfterDelay(targetSource));
    }
    // [새 함수] 활성화 지연 코루틴
    private IEnumerator ActivateResourceAfterDelay(MineableResource resourceToActivate)
    {
        Debug.Log(resourceToActivate.name + "의 활성화를 시작합니다... (" + resourceToActivate.activationTime + "초 대기)");

        // 1. 자원의 상태를 '채굴 중'으로 "먼저" 설정합니다.
        //    (PickUnit은 Mining 상태이며, 다른 유닛이 중복 활성화를 시도하지 못하도록)
        resourceToActivate.StartMining();

        // 2. 설정된 활성화 시간만큼 대기합니다.
        yield return new WaitForSeconds(resourceToActivate.activationTime);

        // 3. [매우 중요] 대기가 끝난 후, 유닛이 그 사이에 떠나지 않았는지 "재확인"합니다.
        //    만약 유닛이 떠났다면, DeactivateSource가 호출되어 isMining이 false가 되었을 것입니다.
        if (resourceToActivate.IsMining() && resourceToActivate.CanStartMining())
        {
            // 4. 모든 조건이 맞으면, "실제 채굴" 리스트에 추가합니다.
            Debug.Log(resourceToActivate.name + " 활성화 완료! 실제 채굴을 시작합니다.");
            activeSources.Add(resourceToActivate);
        }
        else
        {
            // (유닛이 떠났거나, 그 사이에 부모가 멈췄음)
            Debug.Log(resourceToActivate.name + " 활성화가 대기 중에 취소되었습니다.");
        }
    }
}
