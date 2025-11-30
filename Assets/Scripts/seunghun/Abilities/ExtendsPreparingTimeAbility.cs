using System;
using UnityEngine;

public class ExtendsPreparingTimeAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "ExtendsPrep"; } 
    }
    public override void ApplyAbility()
    {
        GameManager.Instance.initialTimeBetweenWaves += 30;
        Debug.Log(AbilityName + "능력을 적용합니다!");
    }
}
