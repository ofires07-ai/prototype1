using System;
using UnityEngine;

public class FasterMiningAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "FasterMining"; } 
    }
    public override void ApplyAbility()
    {
        PickUnit owner = GetComponent<PickUnit>();
        owner.miningSpeed = 1.5f;
    }

    public override MiningTickResult ProcessMiningTick(MineableResource node)
    {
        return new MiningTickResult
        {
            Type = node.resourceType,                   // 타입은 'Special'로 고정
            Amount = node.amountPerTick * 1.5f
        };
    }
}
