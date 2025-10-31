using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// 이 클래스는 BuildSystem.cs 파일 밖으로 분리되어 모든 시스템이 공유합니다.
// UpgradeSystem은 이 BuildButtonUI 구조를 사용하여 버튼과 통신합니다.


public class UpgradeSystem : MonoBehaviour
{
    // BuildSystem의 UnitData와 동일하게 Upgrade 데이터를 정의합니다.
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

    // 업그레이드 데이터 목록
    [Header("구매 가능 업그레이드 목록")]
    public UpgradeData[] availableUpgrades; 

    // Hierarchy의 버튼들과 연결할 UI 목록
    [Header("업그레이드 버튼 UI 연결")]
    public BuildButtonUI[] upgradeButtons; // BuildSystem과 동일한 구조 사용

    // 현재 선택된 업그레이드의 비용
    private int[] _upgradeCosts = new int[5]; 
    private string _selectedUpgradeID;

    void Start()
    {
        // 게임 시작 시 모든 업그레이드 버튼의 UI를 초기화합니다.
        RefreshAllButtonUI();
    }
    
    // --- UI 업데이트 ---
    private void RefreshAllButtonUI()
    {
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            upgradeButtons[i].unitIndex = i; // 인덱스 저장
            
            if (i < availableUpgrades.Length)
            {
                UpgradeData data = availableUpgrades[i];

                // 1. 업그레이드 아이콘 설정
                upgradeButtons[i].unitImage.sprite = data.upgradeIcon;
                
                // 2. 자원 비용 설정 (최대 2개 자원 조합 표시)
                
                List<int> requiredTiers = new List<int>();
                for(int tier = 0; tier < data.costs.Length; tier++)
                {
                    if (data.costs[tier] > 0)
                    {
                        requiredTiers.Add(tier);
                    }
                }
                
                // --- 비용 표시 로직 (BuildSystem과 동일) ---

                // 자원 1 (requiredTiers[0]) 설정
                bool cost1Active = requiredTiers.Count >= 1;
                if (cost1Active)
                {
                    int tier1Index = requiredTiers[0];
                    upgradeButtons[i].cost1Icon.sprite = data.GetIconForTier(tier1Index);
                    upgradeButtons[i].cost1Text.text = data.costs[tier1Index].ToString();
                }
                upgradeButtons[i].cost1Icon.gameObject.SetActive(cost1Active);
                upgradeButtons[i].cost1Text.gameObject.SetActive(cost1Active);

                // 자원 2 (requiredTiers[1]) 설정
                bool cost2Active = requiredTiers.Count >= 2;
                if (cost2Active) 
                {
                    int tier2Index = requiredTiers[1];
                    upgradeButtons[i].cost2Icon.sprite = data.GetIconForTier(tier2Index);
                    upgradeButtons[i].cost2Text.text = data.costs[tier2Index].ToString();
                }
                upgradeButtons[i].cost2Icon.gameObject.SetActive(cost2Active);
                upgradeButtons[i].cost2Text.gameObject.SetActive(cost2Active);
                // --- 끝 ---
            }
            // 유닛 데이터가 부족하면 버튼 비활성화 (선택 사항)
            else if (upgradeButtons[i].unitImage.transform.parent.TryGetComponent<Button>(out Button btn))
            {
                btn.interactable = false;
            }
        }
    }

    // --- UI 버튼 클릭 이벤트에 연결될 함수 ---
    public void SelectUpgrade(int upgradeIndex)
    {
        if (upgradeIndex >= 0 && upgradeIndex < availableUpgrades.Length)
        {
            UpgradeData data = availableUpgrades[upgradeIndex];
            
            // 비용 배열 저장 (깊은 복사)
            _upgradeCosts = new int[data.costs.Length];
            System.Array.Copy(data.costs, _upgradeCosts, data.costs.Length);

            _selectedUpgradeID = data.upgradeID;
            
            // GameManager를 통해 자원 소모 시도
            if (GameManager.Instance.TrySpendMultipleResources(_upgradeCosts))
            {
                ApplyUpgradeEffect(data);
                Debug.Log($"Upgrade '{data.upgradeID}' purchased and applied.");
                
                // 구매 후 버튼 비활성화 로직 등 추가 가능
            }
        }
    }
    
    // 업그레이드 효과를 실제로 적용하는 더미 함수 (여기서는 콘솔 출력만)
    private void ApplyUpgradeEffect(UpgradeData data)
    {
        // TODO: 여기서 실제 게임 로직(데미지 증가, 생산 속도 증가 등)을 구현해야 합니다.
        Debug.Log($"Applying upgrade effect for ID: {data.upgradeID}");
    }
    
}