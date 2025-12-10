using System;
using UnityEngine;

public class ExtendsPreparingTimeAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "준비시간 증가"; } 
    }
    public override void ApplyAbility()
    {
        GameManager.Instance.initialTimeBetweenWaves += 30;
        GameManager.Instance.timerText.color = Color.green;
        Debug.Log(AbilityName + "능력을 적용합니다!");
    }
}
