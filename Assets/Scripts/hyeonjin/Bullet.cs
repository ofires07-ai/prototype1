using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject firePrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Fire 프리팹 생성 및 y축 -90도 회전
            if (firePrefab != null)
            {
                GameObject fire = Instantiate(firePrefab, collision.transform.position, Quaternion.Euler(0, -90, 0));
            }
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
