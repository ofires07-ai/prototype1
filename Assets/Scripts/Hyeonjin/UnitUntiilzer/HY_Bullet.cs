using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject firePrefab;
    public float rotationSpeed = 10f;  // 회전 속도

    void Update()
    {
        // 2D 게임에서 Z축을 기준으로 회전
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime * 360f);
    }

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

    void OnBecameInvisible()
    {
        // 화면 밖으로 나가면 총알 제거
        Destroy(gameObject);
    }
}
