using UnityEditor.Experimental.GraphView;
using UnityEngine;

// [수정] MonoBehaviour -> MineableResource 상속
public class SpecialSource : MineableResource
{
    [Header("특수 자원 종속성")]
    public Source attachedNormalSource; // 내가 붙어있는 일반 자원
    [Header("해당 특수자원 총량")]
    public float capacity = 1f;
    public float remaining;
    
    public float amountPerTick = 0.1f;
    
    // [핵심] 부모의 'abstract' 메서드를 'override'로 구현
    public override bool CanStartMining()
    {
        // [질문에 대한 답] 네, 이렇게 일반 자원의 메서드를 호출하면 됩니다!
        // 이 특수 자원의 채굴 가능 여부는,
        // 이 자원이 붙어있는 "일반 자원"의 채굴 가능 여부와 같습니다.
        
        if (attachedNormalSource == null) return false;
        
        return attachedNormalSource.CanStartMining();
    }
    
    // [핵심] "빌려오는" 로직 구현
    public override MiningSpot GetClosestMiningSpot(Vector3 unitPosition)
    {
        if (attachedNormalSource == null)
        {
            Debug.LogError(name + "에 attachedNormalSource가 연결되지 않았습니다! 임시로 중심점을 반환합니다.");
            return null;
        }

        // [핵심] 나는 스팟이 없으니, "내가 붙어있는 일반 자원"의
        // GetClosestMiningSpot 함수를 대신 호출해서 그 결과를 돌려줍니다.
        return attachedNormalSource.GetClosestMiningSpot(unitPosition);
    }
    
    // [핵심] 부모의 IsDepleted 함수를 '재정의'합니다.
    public override bool IsDepleted()
    {
        // "나의 남은 양(remaining)이 0 이하면 '고갈'된 것입니다."
        return remaining <= 0;
    }

    public void Start()
    {
        base.Start();
        remaining = capacity;
    }
}