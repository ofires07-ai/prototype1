using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // 자원 총량을 저장하는 딕셔너리 (데이터베이스 역할)
    // (예: {ResourceType.Iron: 150, ResourceType.Gold: 20})
    public Dictionary<ResourceType, int> resourceTotals = new Dictionary<ResourceType, int>();

    // SourceManager가 호출할 함수
    public void AddResource(ResourceType type, int amount)
    {
        // 1. 딕셔너리에 해당 자원이 없으면 키(key) 생성
        if (!resourceTotals.ContainsKey(type))
        {
            resourceTotals[type] = 0;
        }

        // 2. 자원량 추가
        resourceTotals[type] += amount;

        // 3. UI 업데이트 (이벤트 방식 추천)
        Debug.Log(type + " 자원 " + amount + " 획득! (총: " + resourceTotals[type] + ")");
        // UIManager.Instance.UpdateResourceUI(type, resourceTotals[type]);
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