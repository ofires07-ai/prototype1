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
        if (resourceToDeactivate == null) return;

        // 1. 리소스의 상태를 '채굴 중 아님'으로 변경
        resourceToDeactivate.StopMining(); 
        
        // 2. 이미 활성 리스트에 있다면 제거
        if (activeSources.Contains(resourceToDeactivate))
        {
            activeSources.Remove(resourceToDeactivate);
            Debug.Log(resourceToDeactivate.name + " 자원을 활성 리스트에서 제거합니다.");
        }
    }
    
    public void TryActivateSource(MineableResource targetSource, PickUnit unit)
    {
        // 1. 기본 검사
        if (targetSource.IsMining())
        {
            Debug.Log("이미 채굴 중입니다!");
            return;
        }
        
        bool canIgnoreRules = unit.CanIgnoreParentRule();
        // 3. (A) 유닛이 규칙을 무시할 수 있거나,
        //    (B) 자원이 원래 채굴 가능한 상태라면 -> 통과
        if (canIgnoreRules || targetSource.CanStartMining())
        {
            if (canIgnoreRules && !targetSource.CanStartMining())
            {
                Debug.Log(unit.name + "이(가) 규칙을 무시하고 " + targetSource.name + " 채굴을 시작합니다!");
            }
            
            // 4. 가능하면 활성화
            targetSource.StartMining();
            activeSources.Add(targetSource);
        }
        else
        {
            // 유닛이 규칙 무시 능력도 없고, 자원도 잠겨있음
            Debug.LogWarning(targetSource.name + " 자원은 현재 채굴 불가능합니다 (부모 미활성).");
            // (이 경우, PickUnit의 Update() FSM이 자동으로 Mining 상태를 중지시킬 것입니다)
        }
    }
}
