using UnityEngine;

public class ExtendsPreparingTimeAbility : CrimerAbility
{
    public override void ApplyAbility()
    {
        GameManager.Instance.initialTimeBetweenWaves += 60;
    }
}
