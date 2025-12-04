using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HY_SlowArea : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("감속 비율 (0.5 = 50% 속도로 느려짐)")]
    [SerializeField] public float slowFactor = 0.5f;

    [Header("데미지 설정")]
    [Tooltip("틱당 데미지")]
    [SerializeField] public int damage = 1;

    [Tooltip("데미지 간격")]
    [SerializeField] public float damageInterval = 1.0f;

    [Header("영역 사이클 설정 (Pulse)")]
    [Tooltip("영역이 완전히 펼쳐져 있는 시간 (초)")]
    [SerializeField] private float activeDuration = 5.0f;

    [Tooltip("영역이 펼쳐질 때 걸리는 시간 (작을수록 빨리 커짐)")]
    [SerializeField] private float growTime = 0.5f;

    [Tooltip("영역이 줄어들며 사라지는 시간 (초)")]
    [SerializeField] private float shrinkDuration = 3.0f;

    // 내부 변수
    private List<ISlowable> affectedEnemies = new List<ISlowable>();
    private Coroutine damageCoroutine;
    private Vector3 originalScale; // 시작할 때 원래 크기 저장용

    private void Start()
    {
        // 1. 에디터에서 설정한 원래 크기를 기억해둡니다.
        originalScale = transform.localScale;
        
        // 2. 시작하자마자 크기를 0으로 만들고 사이클 시작
        transform.localScale = Vector3.zero;
        StartCoroutine(AreaCycleRoutine());
    }

    /// <summary>
    /// [신규] 커졌다(5초) -> 작아졌다(3초) -> 반복하는 사이클 코루틴
    /// </summary>
    private IEnumerator AreaCycleRoutine()
    {
        while (true) // 무한 반복
        {
            // --- 1단계: 펼쳐짐 (Grow) ---
            // 0에서 원래 크기(originalScale)까지 growTime 동안 커짐
            yield return StartCoroutine(ChangeScale(Vector3.zero, originalScale, growTime));

            // --- 2단계: 유지 (Active) ---
            // 설정한 시간(5초)에서 커지는 시간(0.5초)을 뺀 만큼 대기
            float waitTime = activeDuration - growTime;
            if (waitTime < 0) waitTime = 0;
            yield return new WaitForSeconds(waitTime);

            // --- 3단계: 축소 (Shrink) ---
            // 원래 크기에서 0까지 shrinkDuration(3초) 동안 작아짐
            yield return StartCoroutine(ChangeScale(originalScale, Vector3.zero, shrinkDuration));

            // (선택사항) 다 줄어들고 나서 잠시 쉬고 싶다면 여기에 WaitForSeconds 추가
            // yield return new WaitForSeconds(1.0f); 
        }
    }

    /// <summary>
    /// [도구] 부드럽게 크기를 변경해주는 함수 (Lerp 사용)
    /// </summary>
    private IEnumerator ChangeScale(Vector3 startSize, Vector3 endSize, float time)
    {
        float elapsedTime = 0f;

        while (elapsedTime < time)
        {
            // 시간을 더해주고
            elapsedTime += Time.deltaTime;
            
            // 현재 진행도(0~1)를 계산
            float t = elapsedTime / time;

            // Lerp(선형 보간)로 부드럽게 크기 적용
            transform.localScale = Vector3.Lerp(startSize, endSize, t);

            yield return null; // 한 프레임 대기
        }

        // 끝난 후 크기 확실하게 고정 (오차 방지)
        transform.localScale = endSize;
    }

    // -------------------------------------------------------------
    // 아래는 기존 로직과 동일 (Trigger Enter/Exit 처리)
    // -------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ISlowable enemy = collision.GetComponent<ISlowable>();

        if (enemy != null)
        {
            if (!affectedEnemies.Contains(enemy))
            {
                affectedEnemies.Add(enemy);
                enemy.ApplySlow(slowFactor); // 적에게 "너 느려져!" 명령
            }

            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DamageRoutine());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        ISlowable enemy = collision.GetComponent<ISlowable>();

        // 적이 직접 나가거나, *장판이 작아져서 적과 분리될 때* 자동으로 호출됨!
        if (enemy != null && affectedEnemies.Contains(enemy))
        {
            RemoveEnemy(enemy); // 적에게 "너 이제 나가도 돼" 명령
        }
    }

    private IEnumerator DamageRoutine()
    {
        while (affectedEnemies.Count > 0)
        {
            yield return new WaitForSeconds(damageInterval);

            for (int i = affectedEnemies.Count - 1; i >= 0; i--)
            {
                ISlowable target = affectedEnemies[i];

                if (target == null || (target as MonoBehaviour) == null) 
                {
                    affectedEnemies.RemoveAt(i);
                    continue;
                }
                target.TakeDamage(damage);
            }
        }
        damageCoroutine = null;
    }

    private void RemoveEnemy(ISlowable enemy)
    {
        enemy.RemoveSlow();
        affectedEnemies.Remove(enemy);
    }

    private void OnDisable()
    {
        foreach (ISlowable enemy in affectedEnemies)
        {
            if (enemy != null && (enemy as MonoBehaviour) != null)
            {
                enemy.RemoveSlow();
            }
        }
        affectedEnemies.Clear();
        damageCoroutine = null;
    }
}