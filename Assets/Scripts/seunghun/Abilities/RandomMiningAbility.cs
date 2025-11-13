using UnityEngine;

public class RandomMiningAbility : CrimerAbility
{
    public override void ApplyAbility()
    {
    }
    
    public override MiningTickResult ProcessMiningTick(MineableResource node)
    {
        var values = System.Enum.GetValues(typeof(ResourceType));
        int randomIndex = Random.Range(0, values.Length - 1); // 'Special' 제외
        
        return new MiningTickResult
        {
            Type = (ResourceType)values.GetValue(randomIndex), // 타입은 랜덤
            Amount = node.amountPerTick               // 양은 그대로
        };
    }
}
