using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour
{
    // ----------------- 데이터 정의 -----------------

    [Serializable]
    public enum TowerCategory
    {
        Defense,    // HY_Tower
        Production  // ProductionTower
    }

    [Serializable]
    public enum StatType
    {
        AttackPower,        // HY_Tower.bulletForce
        AttackSpeed,        // HY_Tower.fireCooldown (작아질수록 빠름)

        MaxUnits,           // ProductionTower.maxUnits
        ProductionSpeed     // ProductionTower.productionTime (작아질수록 빠름)
    }

    [Serializable]
    public class UpgradeCardData
    {
        [Header("기본 정보")]
        public string id;              // 예: "DEF_0_AttackPower"
        public TowerCategory category; // Defense / Production
        [Tooltip("타워 인덱스 (수비/생산 각각 자신의 배열 인덱스와 일치)")]
        public int towerIndex;
        public StatType statType;

        [Header("표시용")]
        public string title;           // 카드 제목
        [TextArea]
        public string description;     // 카드 설명
        public Sprite icon;            // 카드 아이콘 (선택)

        [Header("수치 설정")]
        [Tooltip("배율: 공격력 +20% = 1.2, 공격속도 20% 빨라짐 = 0.8 (쿨타임 감소)")]
        public float valueMultiplier = 1.2f;
    }

    [Serializable]
    public class CardSlotUI
    {
        public Button button;
        public TMP_Text titleText;
        public TMP_Text descriptionText;
        public Image iconImage;
    }

    // ----------------- 인스펙터 설정 -----------------

    [Header("카드 풀 (8개)")]
    [Tooltip("수비 타워 2개 × 2 옵션 + 생산 타워 2개 × 2 옵션 = 8장")]
    public List<UpgradeCardData> allCards = new List<UpgradeCardData>();

    [Header("UI 슬롯 (3장)")]
    public CardSlotUI[] slots = new CardSlotUI[3];

    [Header("업그레이드 대상 타워 프리팹")]
    [Tooltip("수비 타워 프리팹들 (HY_Tower가 붙어 있는 프리팹)")]
    public GameObject[] defenseTowerPrefabs;      // 예: 2개 (0,1)

    [Tooltip("생산 타워 프리팹들 (ProductionTower가 붙어 있는 프리팹)")]
    public GameObject[] productionTowerPrefabs;   // 예: 2개 (0,1)

    // ----------------- 내부 상태 -----------------

    private readonly List<UpgradeCardData> _currentCards = new List<UpgradeCardData>(3);
    private Action<int> _onSelected;
    private System.Random _rand = new System.Random();

    // StageSceneController에서 호출
    public void Open(Action<int> onSelected)
    {
        _onSelected = onSelected;
        gameObject.SetActive(true);

        ShowRandomCards();
    }

    void OnDisable()
    {
        // 버튼 리스너 중복 방지
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot != null && slot.button != null)
                slot.button.onClick.RemoveAllListeners();
        }
    }

    // ----------------- 랜덤 카드 표시 -----------------

    void ShowRandomCards()
    {
        _currentCards.Clear();

        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogWarning("[UpgradeCardUI] allCards가 비어 있습니다. 인스펙터에서 8개 카드를 설정하세요.");
            return;
        }

        // 인덱스 셔플
        List<int> indices = new List<int>();
        for (int i = 0; i < allCards.Count; i++)
            indices.Add(i);

        for (int i = 0; i < indices.Count; i++)
        {
            int j = _rand.Next(i, indices.Count);
            int tmp = indices[i];
            indices[i] = indices[j];
            indices[j] = tmp;
        }

        int cardCountToShow = Mathf.Min(3, indices.Count);

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < cardCountToShow)
            {
                var cardData = allCards[indices[i]];
                _currentCards.Add(cardData);
                BindSlot(slots[i], cardData, i);
            }
            else
            {
                if (slots[i] != null && slots[i].button != null)
                    slots[i].button.gameObject.SetActive(false);
            }
        }
    }

    void BindSlot(CardSlotUI slot, UpgradeCardData data, int slotIndex)
    {
        if (slot == null || slot.button == null) return;

        slot.button.gameObject.SetActive(true);

        if (slot.titleText != null)
            slot.titleText.text = data.title;

        if (slot.descriptionText != null)
            slot.descriptionText.text = data.description;

        if (slot.iconImage != null)
        {
            if (data.icon != null)
            {
                slot.iconImage.sprite = data.icon;
                slot.iconImage.enabled = true;
            }
            else
            {
                slot.iconImage.enabled = false;
            }
        }

        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(() => SelectCard(slotIndex));
    }

    // ----------------- 선택 처리 -----------------

    public void SelectCard(int index)
    {
        if (index < 0 || index >= _currentCards.Count)
        {
            Debug.LogError($"[UpgradeCardUI] 잘못된 카드 인덱스: {index}");
            return;
        }

        var chosenCard = _currentCards[index];

        // 1) 실제 업그레이드 적용
        ApplyUpgrade(chosenCard);

        // 2) StageSceneController에 인덱스 전달 → 다음 스테이지 이동
        _onSelected?.Invoke(index);

        // 3) UI 닫기
        gameObject.SetActive(false);
    }

    // ----------------- 업그레이드 실제 적용 -----------------

    void ApplyUpgrade(UpgradeCardData card)
    {
        GameObject targetPrefab = null;

        switch (card.category)
        {
            case TowerCategory.Defense:
                if (card.towerIndex >= 0 && card.towerIndex < defenseTowerPrefabs.Length)
                    targetPrefab = defenseTowerPrefabs[card.towerIndex];
                break;

            case TowerCategory.Production:
                if (card.towerIndex >= 0 && card.towerIndex < productionTowerPrefabs.Length)
                    targetPrefab = productionTowerPrefabs[card.towerIndex];
                break;
        }

        if (targetPrefab == null)
        {
            Debug.LogWarning($"[UpgradeCardUI] 대상 타워 프리팹이 없습니다. category={card.category}, index={card.towerIndex}");
            return;
        }

        // ---------------- 방어 타워 업그레이드 ----------------
        if (card.category == TowerCategory.Defense)
        {
            // 1) 탱크형 타워 (HY_TankController)
            if (targetPrefab.TryGetComponent<HY_TankController>(out var tank))
            {
                switch (card.statType)
                {
                    case StatType.AttackPower:
                        tank.bulletForce *= card.valueMultiplier;
                        Debug.Log($"[UpgradeCardUI] TANK {targetPrefab.name} bulletForce x{card.valueMultiplier} → {tank.bulletForce}");
                        break;

                    case StatType.AttackSpeed:
                        // fireCooldown은 작을수록 빠름
                        tank.fireCooldown *= card.valueMultiplier;
                        Debug.Log($"[UpgradeCardUI] TANK {targetPrefab.name} fireCooldown x{card.valueMultiplier} → {tank.fireCooldown}");
                        break;

                    default:
                        Debug.LogWarning($"[UpgradeCardUI] Defense에서 사용할 수 없는 StatType: {card.statType}");
                        break;
                }

                return; // 탱크에 적용했으면 여기서 끝
            }

            // 2) RPG / 미사일 타워 (TurretController + ExplosionDamage)
            if (targetPrefab.TryGetComponent<TurretController>(out var turret))
            {
                switch (card.statType)
                {
                    case StatType.AttackPower:
                        // 공격력은 ExplosionDamage.damage 기준으로 업그레이드
                        var explosion = targetPrefab.GetComponent<ExplosionDamage>();
                        if (explosion != null)
                        {
                            explosion.damage = Mathf.RoundToInt(explosion.damage * card.valueMultiplier);
                            Debug.Log($"[UpgradeCardUI] RPG {targetPrefab.name} explosion damage x{card.valueMultiplier} → {explosion.damage}");
                        }
                        else
                        {
                            Debug.LogWarning($"[UpgradeCardUI] {targetPrefab.name}에 ExplosionDamage 컴포넌트가 없습니다.");
                        }
                        break;

                    case StatType.AttackSpeed:
                        turret.fireCooldown *= card.valueMultiplier;
                        Debug.Log($"[UpgradeCardUI] RPG {targetPrefab.name} fireCooldown x{card.valueMultiplier} → {turret.fireCooldown}");
                        break;

                    default:
                        Debug.LogWarning($"[UpgradeCardUI] Defense에서 사용할 수 없는 StatType: {card.statType}");
                        break;
                }

                return; // RPG 타워에 적용했으면 여기서 끝
            }

            // 둘 다 아니라면 경고
            Debug.LogWarning($"[UpgradeCardUI] 지원하지 않는 방어 타워 타입입니다. prefab={targetPrefab.name}");
        }
        // ---------------- 생산 타워 업그레이드 (기존 그대로) ----------------
        else if (card.category == TowerCategory.Production)
        {
            ProductionTower prod = targetPrefab.GetComponent<ProductionTower>();
            if (prod == null)
            {
                Debug.LogWarning($"[UpgradeCardUI] ProductionTower 컴포넌트를 찾을 수 없습니다. prefab={targetPrefab.name}");
                return;
            }

            switch (card.statType)
            {
                case StatType.MaxUnits:
                    prod.maxUnits = Mathf.RoundToInt(prod.maxUnits * card.valueMultiplier);
                    Debug.Log($"[UpgradeCardUI] {targetPrefab.name} maxUnits x{card.valueMultiplier} → {prod.maxUnits}");
                    break;

                case StatType.ProductionSpeed:
                    prod.productionTime *= card.valueMultiplier;
                    Debug.Log($"[UpgradeCardUI] {targetPrefab.name} productionTime x{card.valueMultiplier} → {prod.productionTime}");
                    break;

                default:
                    Debug.LogWarning($"[UpgradeCardUI] Production에서 사용할 수 없는 StatType: {card.statType}");
                    break;
            }
        }
    }


}
