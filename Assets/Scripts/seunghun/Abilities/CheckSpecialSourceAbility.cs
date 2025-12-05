using System;
using UnityEngine;

public class CheckSpecialSourceAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "MultiplySS"; } 
    }
    // [추가] 인벤토리 매니저의 참조를 저장할 변수
    private InventoryManager inventoryManager;
    public override void ApplyAbility()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        Debug.Log(AbilityName + "능력을 적용합니다!");
        
    }
    
    public override MiningTickResult ProcessMiningTick(MineableResource node)
    {
        return new MiningTickResult
        {
            Type = node.resourceType,                   // 타입은 'Special'로 고정
            Amount = node.amountPerTick * (inventoryManager.GetTotalAmount(ResourceType.Special) + 1) // 양은 '기본량 * 배율'
        };
    }
}
