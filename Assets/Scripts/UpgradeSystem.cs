using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 이 클래스는 BuildSystem.cs 파일 밖으로 분리되어 모든 시스템이 공유합니다.
// UpgradeSystem은 이 BuildButtonUI 구조를 사용하여 버튼과 통신합니다.

public class UpgradeSystem : MonoBehaviour
{
    // ===== 1. 개별 업그레이드 레벨 데이터 =====
    [System.Serializable]
    public class UpgradeData
    {
        // 이 업그레이드가 어떤 효과를 주는지 정의하는 필드 (예: 공격력 증가량 등)
        public string upgradeID;

        [Tooltip("Resource Costs: [T1, T2, T3, T4, T5]")]
        public int[] costs = new int[5]; // 5가지 자원 비용

        // 버튼에 표시될 업그레이드 이미지
        public Sprite upgradeIcon;

        // 버튼에 표시될 자원 아이콘들
        public Sprite resource1Icon;
        public Sprite resource2Icon;
        public Sprite resource3Icon;
        public Sprite resource4Icon;
        public Sprite resource5Icon;

        // 편의를 위한 자원 아이콘 반환 함수
        public Sprite GetIconForTier(int tier)
        {
            switch (tier)
            {
                case 0: return resource1Icon; // T1
                case 1: return resource2Icon; // T2
                case 2: return resource3Icon; // T3
                case 3: return resource4Icon; // T4
                case 4: return resource5Icon; // T5
                default: return null;
            }
        }
    }

    // ===== 2. 한 버튼에서 여러 레벨을 가지는 업그레이드 체인 =====
    [System.Serializable]
    public class UpgradeChain
    {
        [Header("체인 정보")]
        public string chainID;           // 예: "SCV_Upgrade"
        public UpgradeData[] levels;     // 레벨 1~3 데이터

        [HideInInspector]
        public int currentLevel = 0;     // 현재까지 완료한 레벨 수 (0 = 아직 안 찍음)

        public bool IsMaxed
        {
            get
            {
                return levels == null || levels.Length == 0 || currentLevel >= levels.Length;
            }
        }

        public UpgradeData GetCurrentLevelData()
        {
            if (IsMaxed) return null;
            return levels[currentLevel];
        }
    }

    // ===== 3. 인스펙터에서 설정할 것들 =====

    [Header("업그레이드 체인 목록 (버튼당 1개)")]
    public UpgradeChain[] upgradeChains;

    [Header("업그레이드 버튼 UI 연결")]
    public BuildButtonUI[] upgradeButtons; // BuildSystem과 동일한 구조 사용 (버튼 1개만 쓰면 Size=1)

    // 현재 선택된 업그레이드의 비용
    private int[] _upgradeCosts = new int[5];
    private string _selectedUpgradeID;

    void Start()
    {
        // 게임 시작 시 모든 업그레이드 버튼의 UI를 초기화합니다.
        RefreshAllButtonUI();
    }

    // ===== 4. UI 업데이트 =====
    private void RefreshAllButtonUI()
    {
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            BuildButtonUI ui = upgradeButtons[i];
            ui.unitIndex = i; // 인덱스 저장

            UpgradeChain chain = (i < upgradeChains.Length) ? upgradeChains[i] : null;

            // 체인 데이터가 없으면 버튼 비활성화
            if (chain == null || chain.levels == null || chain.levels.Length == 0)
            {
                if (ui.unitImage.transform.parent.TryGetComponent<Button>(out Button btnNull))
                {
                    btnNull.interactable = false;
                }
                continue;
            }

            // 아직 만렙 아니면: 현재 레벨 데이터로 아이콘/비용 표시
            if (!chain.IsMaxed)
            {
                UpgradeData data = chain.GetCurrentLevelData();
                
                if (ui.unitImage != null)
                    ui.unitImage.gameObject.SetActive(true);
                // 1) 업그레이드 아이콘
                if (ui.unitImage != null)
                    ui.unitImage.sprite = data.upgradeIcon;

                // 2) 자원 비용 표시 (최대 2개 자원 조합)
                List<int> requiredTiers = new List<int>();
                for (int tier = 0; tier < data.costs.Length; tier++)
                {
                    if (data.costs[tier] > 0)
                        requiredTiers.Add(tier);
                }

                // --- 비용 1 ---
                bool cost1Active = requiredTiers.Count >= 1;
                if (cost1Active)
                {
                    int tier1Index = requiredTiers[0];
                    ui.cost1Icon.sprite = data.GetIconForTier(tier1Index);
                    ui.cost1Text.text = data.costs[tier1Index].ToString();
                }
                ui.cost1Icon.gameObject.SetActive(cost1Active);
                ui.cost1Text.gameObject.SetActive(cost1Active);

                // --- 비용 2 ---
                bool cost2Active = requiredTiers.Count >= 2;
                if (cost2Active)
                {
                    int tier2Index = requiredTiers[1];
                    ui.cost2Icon.sprite = data.GetIconForTier(tier2Index);
                    ui.cost2Text.text = data.costs[tier2Index].ToString();
                }
                ui.cost2Icon.gameObject.SetActive(cost2Active);
                ui.cost2Text.gameObject.SetActive(cost2Active);

                // Max 텍스트는 숨기기
                if (ui.maxText != null)
                    ui.maxText.gameObject.SetActive(false);

                // 버튼 활성화
                if (ui.unitImage.transform.parent.TryGetComponent<Button>(out Button btn))
                    btn.interactable = true;
            }
            // 이미 만렙이면: 비용 숨기고 "MAX UPGRADE" 표시 + 버튼 비활성화
            else
            {
                // 🔹 유닛 아이콘도 안 보이게
                if (ui.unitImage != null)
                    ui.unitImage.gameObject.SetActive(false);

                // 비용/아이콘 숨기기
                if (ui.cost1Icon != null) ui.cost1Icon.gameObject.SetActive(false);
                if (ui.cost1Text != null) ui.cost1Text.gameObject.SetActive(false);
                if (ui.cost2Icon != null) ui.cost2Icon.gameObject.SetActive(false);
                if (ui.cost2Text != null) ui.cost2Text.gameObject.SetActive(false);

                // Max 텍스트 켜기
                if (ui.maxText != null)
                {
                    ui.maxText.text = "MAX UPGRADE";
                    ui.maxText.gameObject.SetActive(true);
                }

                // 버튼 비활성화(원하면 그대로)
                if (ui.unitImage.transform.parent.TryGetComponent<Button>(out Button btn))
                    btn.interactable = false;                                           
            }
        }
    }

    // ===== 5. 버튼 클릭 시 호출될 함수 =====
    // Hierarchy Button의 OnClick()에 UpgradeSystem.SelectUpgrade(해당체인인덱스) 연결
    public void SelectUpgrade(int chainIndex)
    {
        if (chainIndex < 0 || chainIndex >= upgradeChains.Length)
            return;

        UpgradeChain chain = upgradeChains[chainIndex];

        // 이미 만렙이면 무시
        if (chain.IsMaxed)
        {
            Debug.Log($"[UpgradeSystem] {chain.chainID} 이미 최대 레벨입니다.");
            return;
        }

        UpgradeData data = chain.GetCurrentLevelData();

        // 비용 배열 저장 (깊은 복사)
        _upgradeCosts = new int[data.costs.Length];
        System.Array.Copy(data.costs, _upgradeCosts, data.costs.Length);

        _selectedUpgradeID = data.upgradeID;

        // GameManager를 통해 자원 소모 시도
        if (GameManager.Instance != null &&
            GameManager.Instance.TrySpendMultipleResources(_upgradeCosts))
        {
            // 실제 효과 적용
            ApplyUpgradeEffect(data);

            Debug.Log($"Upgrade '{data.upgradeID}' purchased and applied. " +
                      $"(chain={chain.chainID}, level={chain.currentLevel + 1}/{chain.levels.Length})");

            // 레벨 1 증가
            chain.currentLevel++;

            // UI 갱신 (다음 레벨 또는 MAX 표시)
            RefreshAllButtonUI();
        }
        else
        {
            Debug.Log($"[UpgradeSystem] 자원이 부족하여 업그레이드를 진행할 수 없습니다. ({data.upgradeID})");
        }
    }

    // ===== 6. 업그레이드 효과를 실제로 적용하는 함수 =====
    /// <summary>
    /// 업그레이드 효과를 실제로 적용하는 함수
    /// - 현재 씬에 존재하는 모든 일반 자원(Source)의 amountPerTick을 +1 합니다.
    /// - 스테이지가 바뀌면 새로 생성되는 Source들은 프리팹 기본값으로 다시 시작하므로,
    ///   업그레이드 효과는 스테이지 단위로 초기화됩니다.
    /// </summary>
    private void ApplyUpgradeEffect(UpgradeData data)
    {
        // 1. 현재 씬에 존재하는 모든 Source 찾기
        Source[] sources = FindObjectsOfType<Source>();

        foreach (Source src in sources)
        {
            // 🔹 여기서 Source는 일반 자원(T1~T4)을 의미한다고 가정합니다.
            //    (T5는 SpecialSource로 따로 관리되는 구조라면 이걸로 충분합니다.)
            //    만약 resourceType으로 세부 필터링하고 싶으면 아래와 같이 조건을 추가할 수 있습니다.
            //
            //    if (src.resourceType == ResourceType.T1 ||
            //        src.resourceType == ResourceType.T2 ||
            //        src.resourceType == ResourceType.T3 ||
            //        src.resourceType == ResourceType.T4)
            //    {
            //        src.amountPerTick += 1f;
            //    }

            src.amountPerTick += 1f;
        }

        Debug.Log($"[UpgradeSystem] Upgrade '{data.upgradeID}' applied: 모든 Source 채굴량(amountPerTick) +1");
    }
}
