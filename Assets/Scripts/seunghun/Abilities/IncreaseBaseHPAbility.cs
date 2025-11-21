using System;
using UnityEngine;

public class IncreaseBaseHPAbility : CrimerAbility
{
    // 증가시킬 체력량 (인스펙터에서 조절 가능하게 만들어 유연성 확보)
    [SerializeField] private int increaseAmount = 5;
    public override string AbilityName 
    {
        get { return "IncBaseHP"; } 
    }
    public override void ApplyAbility()
    {
        // 1. 씬에서 SpaceShip 오브젝트를 찾습니다.
        SpaceShip spaceShip = FindObjectOfType<SpaceShip>();

        // 2. SpaceShip이 존재하는지 안전하게 확인합니다.
        if (spaceShip != null)
        {
            // 3. SpaceShip에게 체력 증가를 요청합니다.
            spaceShip.IncreaseMaxHP(increaseAmount);
            Debug.Log($"{AbilityName} 능력 적용: 우주선 최대 체력 +{increaseAmount}");
        }
        else
        {
            Debug.LogError($"[Ability] {AbilityName} 적용 실패: 씬에서 SpaceShip을 찾을 수 없습니다!");
        }
    }
}
