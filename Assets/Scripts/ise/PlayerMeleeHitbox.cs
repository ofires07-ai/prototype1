using UnityEngine;

public class PlayerMeleeHitbox : MonoBehaviour // ⬅️ 클래스 이름 변경
{
    public int damage = 0; 
    private bool hasHitTarget = false; 

    void Start()
    {
        Destroy(gameObject, 0.1f);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitTarget || collision.CompareTag("MeleeHitbox")) return; 
        
        // [ ✨ 핵심: 오직 Enemy 태그만 공격합니다. 
        if (collision.CompareTag("Tower") || collision.CompareTag("Flag")) return;
        if (collision.CompareTag("Enemy")) 
        {
            hasHitTarget = true; 
            // 데미지 적용은 상대방 스크립트가 처리합니다.
            Destroy(gameObject); 
        }
    }
}