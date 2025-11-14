using UnityEngine;

public class FasterMiningAbility : CrimerAbility
{
    public override void ApplyAbility()
    {
    }

    public override MiningTickResult ProcessMiningTick(MineableResource node)
    {
        return new MiningTickResult
        {
            Type = node.resourceType,                   // 타입은 'Special'로 고정
            Amount = node.amountPerTick * 1.2f
        };
    }
}
