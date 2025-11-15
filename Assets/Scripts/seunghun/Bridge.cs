using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bridge : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("구매에 필요한 자원 비용 (T1, T2, T3, T4, Special 순서)")]
    public int[] costs = new int[5]; // ⬅️ Inspector에서 비용 설정 (예: T1에 500)

    [Header("참조")]
    [Tooltip("이 다리가 구매되면 사라질 '경로 차단기' 오브젝트")]
    public GameObject pathBlocker; 

    [Header("UI 참조")]
    public GameObject uiRoot;      // BridgePanel 같은 최상위 UI 오브젝트
    public Image cost1Icon;
    public TMP_Text cost1Text;
    public Image cost2Icon;
    public TMP_Text cost2Text;

    [Tooltip("각 티어별 아이콘 (T1~T5 순서)")]
    public Sprite[] tierIcons = new Sprite[5];

    private bool isPurchased = false; // 중복 구매 방지

    void Awake()
    {
        if (pathBlocker == null)
            pathBlocker = gameObject;
    }

    void Start()
    {
        SetupCostUI();
    }

    void SetupCostUI()
    {
        if (uiRoot == null) return;

        // 어떤 티어가 쓰였는지 모으기
        var usedTiers = new System.Collections.Generic.List<int>();
        for (int tier = 0; tier < costs.Length; tier++)
        {
            if (costs[tier] > 0)
                usedTiers.Add(tier);
        }

        // 첫 번째 자원
        if (usedTiers.Count >= 1)
        {
            int t = usedTiers[0];
            if (cost1Icon != null)
            {
                cost1Icon.gameObject.SetActive(true);
                if (tierIcons != null && t < tierIcons.Length)
                    cost1Icon.sprite = tierIcons[t];
            }
            if (cost1Text != null)
            {
                cost1Text.gameObject.SetActive(true);
                cost1Text.text = costs[t].ToString();
            }
        }
        else
        {
            if (cost1Icon != null) cost1Icon.gameObject.SetActive(false);
            if (cost1Text != null) cost1Text.gameObject.SetActive(false);
        }

        // 두 번째 자원
        if (usedTiers.Count >= 2)
        {
            int t = usedTiers[1];
            if (cost2Icon != null)
            {
                cost2Icon.gameObject.SetActive(true);
                if (tierIcons != null && t < tierIcons.Length)
                    cost2Icon.sprite = tierIcons[t];
            }
            if (cost2Text != null)
            {
                cost2Text.gameObject.SetActive(true);
                cost2Text.text = costs[t].ToString();
            }
        }
        else
        {
            if (cost2Icon != null) cost2Icon.gameObject.SetActive(false);
            if (cost2Text != null) cost2Text.gameObject.SetActive(false);
        }
    }

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

            if (uiRoot != null)
                uiRoot.SetActive(false);
        }
        else
        {
            // 7. [구매 실패]
            Debug.Log("자원이 부족하여 다리를 구매할 수 없습니다.");
            // (필요하다면 유닛에게 "자원 부족" 음성 재생)
        }
    }
}
