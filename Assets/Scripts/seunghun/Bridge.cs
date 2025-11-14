using UnityEngine;

public class Bridge : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("구매에 필요한 자원 비용 (T1, T2, T3, T4, Special 순서)")]
    public int[] costs = new int[5]; // ⬅️ Inspector에서 비용 설정 (예: T1에 500)

    [Header("참조")]
    [Tooltip("이 다리가 구매되면 사라질 '경로 차단기' 오브젝트")]
    public GameObject pathBlocker; 

    private bool isPurchased = false; // 중복 구매 방지

    /// <summary>
    /// CrimerManager가 이 함수를 호출하여 구매를 시도합니다.
    /// </summary>
    public void TryPurchaseBridge()
    {
        // 1. 이미 구매했는지 확인
        if (isPurchased)
        {
            Debug.Log("이미 구매한 다리입니다.");
            return;
        }

        // 2. [핵심] GameManager에게 자원 지불을 시도합니다.
        if (GameManager.Instance != null && GameManager.Instance.TrySpendMultipleResources(costs))
        {
            // 3. [구매 성공]
            Debug.Log("다리 구매 성공! 경로를 엽니다.");
            isPurchased = true;

            // 4. 경로 차단기(Collider)를 비활성화합니다.
            if (pathBlocker != null)
            {
                pathBlocker.SetActive(false);

                // 5. [매우 중요] A* 맵을 실시간으로 갱신합니다!
                // "Blocker가 사라졌으니, 이 영역을 '갈 수 있는 길'로 다시 스캔해!"
                Collider2D blockerCollider = pathBlocker.GetComponent<Collider2D>();
                if (blockerCollider != null)
                {
                    AstarPath.active.UpdateGraphs(blockerCollider.bounds);
                }
            }
        }
        else
        {
            // 7. [구매 실패]
            Debug.Log("자원이 부족하여 다리를 구매할 수 없습니다.");
            // (필요하다면 유닛에게 "자원 부족" 음성 재생)
        }
    }
}
