using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    // 이 히트박스의 데미지
    public int damage = 5; 

    void Start()
    {
        // 생성되자마자 0.2초 뒤에 스스로 파괴됩니다.
        Destroy(gameObject, 0.2f);
    }
}