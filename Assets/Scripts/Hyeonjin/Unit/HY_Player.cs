using System;
using UnityEngine;
using Pathfinding; // A* Pathfinding Project를 사용하기 위해 추가

[RequireComponent(typeof(AIPath), typeof(Seeker))] // AIPath와 Seeker 컴포넌트가 필수임을 명시
public class Player : MonoBehaviour
{
    public Transform destination;
    
    AIPath aiPath; // AIPath 컴포넌트를 저장할 변수
    Animator anim;
    Scanner scanner;
    
    public GameObject bulletObj;
    public float fireCooldown = 0.5f;
    float lastFireTime;

    void Awake()
    {
        aiPath = GetComponent<AIPath>();
        anim = GetComponent<Animator>();
        scanner = GetComponent<Scanner>();
    }

    void Update() // FixedUpdate에서 Update로 변경하여 매 프레임 상태를 확인
    {
        // AIPath가 목적지로 이동하도록 설정
        if (destination != null)
        {
            aiPath.destination = destination.position;
        }

        Transform target = scanner != null ? scanner.nearestTarget : null;

        if (target != null)
        {
            // 적이 있으면 이동을 멈춤
            aiPath.canMove = false;
            
            // 쿨타임 내에만 발사
            if (Time.time - lastFireTime > fireCooldown)
            {
                Fire(target);
                lastFireTime = Time.time;
            }
        }
        else
        {
            // 적이 없으면 이동을 다시 시작
            aiPath.canMove = true;
        }

        // 애니메이터에 속도 전달
        // AIPath.velocity는 월드 공간에서의 속도 벡터입니다.
        float animSpeed = aiPath.velocity.magnitude;
        anim.SetFloat("Speed", animSpeed);
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