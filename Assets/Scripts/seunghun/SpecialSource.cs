using UnityEngine;

// [수정] MonoBehaviour -> MineableResource 상속
public class SpecialSource : MineableResource
{
    [Header("특수 자원 종속성")]
    public Source attachedNormalSource; // 내가 붙어있는 일반 자원
    [Header("해당 특수자원 총량")]
    public int capacity;
    
    // [핵심] 부모의 'abstract' 메서드를 'override'로 구현
    public override bool CanStartMining()
    {
        // [질문에 대한 답] 네, 이렇게 일반 자원의 메서드를 호출하면 됩니다!
        // 이 특수 자원의 채굴 가능 여부는,
        // 이 자원이 붙어있는 "일반 자원"의 채굴 가능 여부와 같습니다.
        
        if (attachedNormalSource == null) return false;
        
        return attachedNormalSource.CanStartMining();
    }
}