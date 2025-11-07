using System;
using UnityEngine;

public class Source : MineableResource
{
    
    [Header("일반 자원 종속성")]
    // 이 자원을 채굴하기 위해 먼저 채굴해야 하는 부모 자원
    // 인스펙터 창에서 드래그 앤 드롭으로 연결!
    public Source parentSource;
    
    [Header("특수 자원")]
    public SpecialSource attachedSpecialSource; // 붙어있는 특수 자원

    public float amountPerTick = 1f;
    
// [핵심] 부모의 'abstract' 메서드를 'override'로 구현
    public override bool CanStartMining()
    {
        // 1. 부모가 없으면 (루트 노드)
        if (parentSource == null) return true;
        
        // 2. 부모가 있으면, 부모가 채굴 중이어야 함
        return parentSource.IsMining();
    }
}
