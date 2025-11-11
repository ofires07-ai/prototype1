using UnityEngine;

public class TowerDiscountAbility : CrimerAbility
{
    public float costMultiplier = 0.8f; // 20% 할인

    public override void ApplyAbility()
    {
        // "게임 매니저님, 저 20% 할인 능력 있어요!"
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterTowerCostModifier(costMultiplier);
            Debug.Log("타워 할인 활성화! 타워 비용 20% 할인 적용.");
        }
    }
}
