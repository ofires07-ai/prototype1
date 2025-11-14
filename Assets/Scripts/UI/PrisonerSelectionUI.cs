using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public class PrisonerSelectionUI : MonoBehaviour
{
    [Header("참조")]
    public CrimerManager crimerManager;    // 🔸 인스펙터에서 할당

    [Header("슬롯 UI (5칸)")]
    public TMP_Text[] slotTexts;           // 각 슬롯에 표시할 이름 텍스트 (5개)

    [Header("새로고침 UI")]
    public TMP_Text refreshCountText;      // 남은 reroll 기회 표시용

    private Action<int> _onSelected;       // StageSceneController에서 넘겨주는 콜백

    void Awake()
    {
        if (crimerManager == null)
        {
            crimerManager = FindObjectOfType<CrimerManager>();
            if (crimerManager == null)
            {
                Debug.LogError("[PrisonerSelectionUI] CrimerManager를 찾을 수 없습니다.");
            }
        }

        if (slotTexts == null || slotTexts.Length == 0)
        {
            Debug.LogWarning("PrisonerSelectionUI: slotTexts가 설정되지 않았습니다.");
        }
    }

    // StageSceneController에서 호출: prisonerSelectionUI.Open(OnPrisonerSelected);
    public void Open(Action<int> onSelected)
    {
        Debug.Log($"[PrisonerSelectionUI] Open on {gameObject.name}, scene={gameObject.scene.name}", gameObject);

        _onSelected = onSelected;
        gameObject.SetActive(true);

        RollNewCombination();
        UpdateRefreshText();
    }

    // ---------------- CrimerManager를 이용해 랜덤 조합 뽑기 ----------------
    void RollNewCombination()
    {
        Debug.Log("[PrisonerSelectionUI] RollNewCombination");

        if (crimerManager == null)
        {
            Debug.LogError("[PrisonerSelectionUI] crimerManager == null");
            return;
        }

        // currentDraft가 null이면 초기화
        if (crimerManager.currentDraft == null)
        {
            crimerManager.currentDraft = new List<PickUnit>();
        }

        // 항상 새로 뽑기 느낌을 주기 위해 먼저 비우고 다시 뽑기
        crimerManager.currentDraft.Clear();
        crimerManager.SelectRandomCrimers();   // 내부에서 rerollChances를 1 줄임

        UpdateSlotsFromCurrentDraft();
    }

    void UpdateSlotsFromCurrentDraft()
    {
        if (crimerManager == null)
            return;

        List<PickUnit> draft = crimerManager.currentDraft;
        if (draft == null)
            return;

        int slotCount = slotTexts.Length;
        int pickCount = Mathf.Min(slotCount, draft.Count);

        for (int i = 0; i < slotCount; i++)
        {
            if (slotTexts[i] == null)
                continue;

            if (i < pickCount)
            {
                PickUnit unit = draft[i];
                slotTexts[i].text = unit != null ? unit.name : "None";
                slotTexts[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                slotTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    void UpdateRefreshText()
    {
        if (refreshCountText == null)
            return;

        if (crimerManager == null)
        {
            refreshCountText.text = "-";
            return;
        }

        refreshCountText.text = crimerManager.rerollChances.ToString();
    }

    // ---------------- 버튼 이벤트 ----------------

    // 새로고침 버튼 OnClick에 연결
    public void OnClickRefresh()
    {
        if (crimerManager == null)
        {
            Debug.LogError("[PrisonerSelectionUI] OnClickRefresh: crimerManager == null");
            return;
        }

        if (crimerManager.rerollChances <= 0)
        {
            Debug.Log("[PrisonerSelectionUI] rerollChances가 0이라 새로고침 불가");
            return;
        }

        RollNewCombination();
        UpdateRefreshText();
    }

    // 선택 버튼 OnClick에 연결
    public void OnClickConfirm()
    {
        // 현재 crimerManager.currentDraft에 떠 있는 애들을
        // "이 스테이지에서 사용할 유닛들"로 스테이지 쪽에서 사용하면 됩니다.
        _onSelected?.Invoke(0); // 아직 prisonerId 안 쓰면 0만 넘김

        gameObject.SetActive(false);
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
    }
}
