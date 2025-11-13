using UnityEngine;

// [중요] 'abstract'는 이 클래스가 직접 씬에 배치되지 않고,
// 자식들에 의해 상속되어야 함을 의미합니다.
public abstract class MineableResource : MonoBehaviour
{
    [Header("공통 자원 정보")] 
    public ResourceType resourceType;
    private bool isMining = false;
    public float amountPerTick = 1f;
        
    // [중요] SourceManager 참조는 이제 부모가 가집니다.
    protected SourceManager sourceManager;

    protected virtual void Start()
    {
        // 씬에 있는 SourceManager를 자동으로 찾아 연결
        sourceManager = FindObjectOfType<SourceManager>();
    }

    // --- 공통 메서드 ---

    public bool IsMining()
    {
        return isMining;
    }

    // [수정] SourceManager가 이제 'MineableResource'를 받습니다.
    public void StartMining()
    { 
        isMining = true;
    }

    public void StopMining()
    {
        isMining = false;
    }

    // [핵심] 채굴 가능 여부 확인 로직은
    // 자식 클래스(Source, SpecialSource)가 반드시 직접 구현하도록 'abstract'로 만듭니다.
    public abstract bool CanStartMining();
    
    // [새 메서드] 자식들이 "어떻게" 찾을지는 모르지만,
    // "가장 가까운 채굴 지점을 찾는 기능"이 있어야 함을 강제합니다.
    public abstract MiningSpot GetClosestMiningSpot(Vector3 unitPosition);
    
    // 일반 자원(Source)은 무한하다고 가정하고 기본값 'false'를 반환합니다.
    public virtual bool IsDepleted()
    {
        return false;
    }
}