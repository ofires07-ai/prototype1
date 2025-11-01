using System;
using UnityEngine;

public class Source : MonoBehaviour
{
    // 1. 자원 정보 (데이터)
    [Header("자원 정보")]
    public ResourceType resourceType; // 이 자원의 종류 (예: Iron)
    public int amountPerTick = 1;     // 1회 수집 시 생산량
    
    [Header("트리 종속성")]
    // 이 자원을 채굴하기 위해 먼저 채굴해야 하는 부모 자원
    // 인스펙터 창에서 드래그 앤 드롭으로 연결!
    public Source parentSource;
    
    // 2. 상태 (State)
    private bool isMining = false;    // 현재 채굴 중인지?

    // 3. SourceManager가 호출할 함수들
    // [중요] 채굴이 가능한 상태인지 스스로 확인하는 함수
    public bool CanStartMining()
    {
        // 1. 부모가 없으면 (parentSource가 null이면) 루트 노드이므로 항상 가능
        if (parentSource == null)
        {
            return true;
        }

        // 2. 부모가 있다면, 부모가 채굴 중(isMining)이어야만 가능
        return parentSource.IsMining();
    }
    
    // 채굴 시작 시 SourceManager가 호출
    public void StartMining()
    {
        isMining = true;
        // (선택) 채굴 중임을 나타내는 시각 효과 (e.g., 파티클 켜기)
    }
    
    // 채굴 중지 시 SourceManager가 호출
    public void StopMining()
    {
        isMining = false;
        // (선택) 시각 효과 끄기
    }
    
    public bool IsMining()
    {
        return isMining;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
