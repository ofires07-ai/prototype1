using System;
using System.Collections.Generic;
using UnityEngine;

public class SourceManager : MonoBehaviour
{
    // [중요] 현재 활발하게 채굴 중인 자원들만 담는 리스트
    private List<Source> activeSources = new List<Source>();
    
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

    // [중요] 활성화된 리스트만 순회, 리스트에 있는 소스는 부모가 채굴 중인지 확인된 소스
    private void CollectResources()
    {
        // 500개가 아닌, 활성화된 10개만 순회
        foreach (Source source in activeSources)
        {
            // InventoryManager에게 자원 추가를 '요청'
            inventoryManager.AddResource(source.resourceType, source.amountPerTick);
        }
    }

    // (예시) 플레이어가 자원을 클릭/해금했을 때 이 함수를 호출
    public void ActivateSource(Source newSource)
    {
        if (newSource.parentSource == null) // 대상 소스가 루트 소스인 경우
        {
            newSource.StartMining();
            activeSources.Add(newSource);
            return;
        }
        if (newSource.IsMining() || !newSource.parentSource.IsMining()) return; // 이미 채굴 중이거나 부모 노드가 채굴 중이 아니면
        
        newSource.StartMining();
        activeSources.Add(newSource);
    }
    
    // (예시) 자원 채굴을 중단할 때
    public void DeactivateSource(Source sourceToStop)
    {
        sourceToStop.StopMining();
        activeSources.Remove(sourceToStop);
    }
    
    // [변경] 이 함수는 플레이어가 특정 자원을 클릭할 때 호출됩니다.
    public void TryActivateSource(Source targetSource)
    {
        // 1. 이미 채굴 중인지 확인
        if (targetSource.IsMining())
        {
            Debug.Log("이미 채굴 중입니다.");
            return;
        }

        // 2. [핵심] 자원 스스로에게 채굴 가능한지 물어봄
        if (targetSource.CanStartMining())
        {
            // 3. 가능하면 활성화
            targetSource.StartMining();
            activeSources.Add(targetSource);
        }
        else
        {
            // 4. 불가능하면 알림
            Debug.Log(targetSource.parentSource.name + " 자원을 먼저 활성화해야 합니다!");
        }
    }
    void Start()
    {
        
    }
    
}
