using System;
using UnityEngine;

public abstract class CrimerAbility : MonoBehaviour
{
    public virtual string AbilityName 
    {
        get { return "기본 능력"; } 
    }
    public abstract void ApplyAbility();
    
    // "이 능력은 부모-자식 채굴 규칙을 무시할 수 있습니까?"
    // 'virtual'이므로, 자식이 재정의(override)하지 않으면 기본값(false)을 반환합니다.
    public virtual bool CanIgnoreParentRule()
    {
        return false;
    }
    
    // "이번 틱의 채굴 '결과(원장)'를 'MiningTickResult' 객체로 반환해!"
    public virtual MiningTickResult ProcessMiningTick(MineableResource currentResourceNode)
    {
        // [기본 행동] (능력 없는 유닛)
        // "그냥 네가 붙어있는 자원의 '타입'과 '기본 채굴량'을 반환할게."
        return new MiningTickResult
        {
            Type = currentResourceNode.resourceType,
            Amount = currentResourceNode.amountPerTick
        };
    }
}
