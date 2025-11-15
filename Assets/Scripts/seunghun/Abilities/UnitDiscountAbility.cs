using System;
using UnityEngine;

public class UnitDiscountAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "DiscountUTower"; } 
    }
    public float costMultiplier = 0.8f; // 20% 할인

    public override void ApplyAbility()
    {
        // "게임 매니저님, 저 20% 할인 능력 있어요!"
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUnitCostModifier(costMultiplier);
            Debug.Log("유닛 할인 활성화! 유닛 비용 20% 할인 적용.");
        }
    }
}
