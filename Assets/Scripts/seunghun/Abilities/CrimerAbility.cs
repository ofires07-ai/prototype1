using UnityEngine;

public abstract class CrimerAbility : MonoBehaviour
{
    public abstract void ApplyAbility();
    
    // "이 능력은 부모-자식 채굴 규칙을 무시할 수 있습니까?"
    // 'virtual'이므로, 자식이 재정의(override)하지 않으면 기본값(false)을 반환합니다.
    public virtual bool CanIgnoreParentRule()
    {
        return false;
    }
}
