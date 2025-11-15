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
        GameManager.Instance.initialTimeBetweenWaves += 60;
    }
}
