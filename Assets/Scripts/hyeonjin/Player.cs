using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rigid;
    Animator anim;
    Scanner scanner;
    public GameObject bulletObj;
    public float fireCooldown = 0.5f;
    float lastFireTime;

    void Awake()
    {
    rigid = GetComponent<Rigidbody2D>();
    anim = GetComponent<Animator>();
    scanner = GetComponent<Scanner>();
    }

    void FixedUpdate()
    {
    float speed = 2f; // 이동 속도 조절
    Vector2 moveVec = Vector2.right * speed;
    // Scanner에서 nearestTarget(=nearestEnemy)이 있으면 멈춤
    if (scanner != null && scanner.nearestTarget != null)
    {
        rigid.linearVelocity = Vector2.zero;
        // 쿨타임 내에만 발사
        if (Time.time - lastFireTime > fireCooldown)
        {
            Fire(scanner.nearestTarget);
            lastFireTime = Time.time;
        }
    }
    else
    {
        rigid.linearVelocity = moveVec;
    }
    // 애니메이터에 속도 전달
    float animSpeed = rigid.linearVelocity.magnitude;
    if (animSpeed <= 0.01f) animSpeed = 0f;
    anim.SetFloat("Speed", animSpeed);
    }

    void LateUpdate()
    {
    // ...existing code...

    }

    void Fire(Transform target)
    {
        if (bulletObj == null || target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        GameObject bullet = Instantiate(bulletObj, transform.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(dir * 10f, ForceMode2D.Impulse);
        }
    }
}
