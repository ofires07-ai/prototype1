using UnityEngine;
using System; 
using System.Linq; 
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // 자원 총량을 저장하는 딕셔너리 (데이터베이스 역할)
    // (예: {ResourceType.Iron: 150, ResourceType.Gold: 20})
    public Dictionary<ResourceType, int> resourceTotals = new Dictionary<ResourceType, int>();

    void Start()
{
    // ✅ 핵심: 게임 시작 시 모든 ResourceType에 대해 딕셔너리를 초기화합니다.
    if (resourceTotals.Count == 0)
    {
        // Enum.GetValues를 사용하려면 파일 상단에 'using System;'이 있어야 합니다.
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (!resourceTotals.ContainsKey(type))
            {
                resourceTotals.Add(type, 0);
            }
        }
    }
    
    // InventoryManager 초기화가 끝났으므로, GameManager에게 UI 갱신을 요청합니다.
    // 이 호출은 GameManager의 Start()가 끝난 후 호출되도록 보장되어야 합니다.
    if (GameManager.Instance != null)
    {
        GameManager.Instance.UpdateResourceUI();
    }
}

    public void RemoveResource(ResourceType type, int amount)
{
    if (resourceTotals.ContainsKey(type))
    {
        resourceTotals[type] -= amount;
        if (resourceTotals[type] < 0) resourceTotals[type] = 0; // 안전 장치
        
        // 💡 GameManager의 UI 업데이트 함수를 호출하여 화면을 갱신합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateResourceUI();
        }
    }
}

    // SourceManager가 호출할 함수
    public void AddResource(ResourceType type, int amount)
    {
        // 1. 딕셔너리에 해당 자원이 없으면 키(key) 생성
        if (!resourceTotals.ContainsKey(type))
        {
            resourceTotals[type] = 100;
        }

        // 2. 자원량 추가
        resourceTotals[type] += amount;

        // 3. UI 업데이트 (이벤트 방식 추천)
        Debug.Log(type + " 자원 " + amount + " 획득! (총: " + resourceTotals[type] + ")");
        // UIManager.Instance.UpdateResourceUI(type, resourceTotals[type]);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateResourceUI();
        }
    }

    // (선택) 특정 자원 총량을 반환하는 함수
    public int GetTotalAmount(ResourceType type)
    {
        if (resourceTotals.TryGetValue(type, out int amount))
        {
            return amount;
        }
        return 0;
    }
}