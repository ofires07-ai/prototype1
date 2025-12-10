using System;
using UnityEngine;
using Pathfinding; // ⬅️ AIPath를 사용하려면 "using Pathfinding;"이 필수입니다.

// [핵심] Ability를 상속받음
public class FasterMovementAbility : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "빠른 이동"; } 
    }
    public float speedMultiplier = 1.5f; // 50% 더 빠르게
    
    public override void ApplyAbility()
    {
        // [핵심]
        // "나는 나(이 컴포넌트)를 가지고 있는 '몸통(GameObject)'이 누군지
        //  GetComponent로 직접 찾으면 돼!"
        PickUnit owner = GetComponent<PickUnit>();
        
        if (owner == null)
        {
            Debug.LogError("Ability_FasterMovement가 PickUnit이 없는 곳에 붙어있습니다!");
            return;
        }

        // 'owner' (PickUnit 게임오브젝트)에게서 AIPath 컴포넌트를 찾습니다.
        AIPath aiPath = owner.GetComponent<AIPath>();

        // 'owner'의 속도를 변경합니다.
        if (aiPath != null)
        {
            aiPath.maxSpeed *= speedMultiplier;
            Debug.Log(owner.name + "의 능력 활성화! 이동 속도 증가!");
        }
    }
}
