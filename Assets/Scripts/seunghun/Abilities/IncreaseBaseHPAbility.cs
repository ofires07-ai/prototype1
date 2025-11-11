using UnityEngine;

public class IncreaseBaseHPAbility : CrimerAbility
{
    public override void ApplyAbility()
    {
        GameManager.Instance.maxBaseHealth += 5;
    }
}
