/// <summary>
/// 감속 효과를 적용할 수 있는 모든 객체가 구현해야 하는 인터페이스입니다.
/// </summary>
public interface ISlowable
{
    void ApplySlow(float ratio);
    void RemoveSlow();
    void TakeDamage(int damage);
    
}