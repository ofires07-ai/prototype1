using System;
using UnityEngine;

public class IncreaseBaseHPAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "IncBaseHP"; } 
    }
    public override void ApplyAbility()
    {
        GameManager.Instance.maxBaseHealth += 5;
        GameManager.Instance._currentBaseHealth = GameManager.Instance.maxBaseHealth;
        Debug.Log(AbilityName + "능력을 적용합니다!");
    }
}
