using System;
using System.Collections.Generic;
using UnityEngine;

public class Source : MineableResource
{
    
    [Header("일반 자원 종속성")]
    // 이 자원을 채굴하기 위해 먼저 채굴해야 하는 부모 자원
    // 인스펙터 창에서 드래그 앤 드롭으로 연결!
    public Source parentSource;
    
    [Header("특수 자원")]
    public SpecialSource attachedSpecialSource; // 붙어있는 특수 자원
    
    [Header("채굴 지점")]
    // [추가] 'Source'가 직접 'miningSpots'를 소유합니다.
    public List<MiningSpot> miningSpots;
    
// [핵심] 부모의 'abstract' 메서드를 'override'로 구현
    public override bool CanStartMining()
    {
        // 1. 부모가 없으면 (루트 노드)
        if (parentSource == null) return true;
        
        // 2. 부모가 있으면, 부모가 채굴 중이어야 함
        return parentSource.IsMining();
    }
    
    // [핵심] 부모의 abstract 메서드를 'override'로 구현
    public override MiningSpot GetClosestMiningSpot(Vector3 unitPosition)
    {
        if (miningSpots == null || miningSpots.Count == 0)
        {
            Debug.LogError(name + "에 miningSpots가 설정되지 않았습니다!");
            return null; // ⬅️ [수정] null 반환
        }

        MiningSpot bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        // 자신의 'miningSpots' 리스트를 순회
        foreach (MiningSpot spot in miningSpots)
        {
            // [핵심] 1. "비어있는" 스팟인지 먼저 확인합니다.
            if (!spot.isOccupied)
            {
                // 2. 비어있는 스팟 중에서 가장 가까운지 확인합니다.
                float dSqrToTarget = (spot.transform.position - unitPosition).sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = spot;
                }
            }
        }
        
        // (만약 모든 스팟이 꽉 찼다면 'bestTarget'은 null이 됩니다)
        return bestTarget;
    }
}
