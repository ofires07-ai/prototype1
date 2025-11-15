using System;
using UnityEngine;

public class JustMiningAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "IgnorePSource"; } 
    }
    public override void ApplyAbility()
    {
        Debug.Log("부모 자원 무시 능력 활성화!");
    }

    // [핵심] 부모의 'CanIgnoreParentRule' 함수를 재정의(override)합니다.
    public override bool CanIgnoreParentRule()
    {
        return true;
    }
}
