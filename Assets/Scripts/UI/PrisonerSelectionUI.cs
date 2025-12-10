using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public class PrisonerSelectionUI : MonoBehaviour
{
    [Header("참조")]
    public CrimerManager crimerManager;    // 🔸 인스펙터에서 할당(없으면 자동으로 FindObjectOfType)

    [Header("슬롯 UI (5칸)")]
    public TMP_Text[] slotTexts;           // 각 슬롯에 표시할 이름 텍스트 (5개)

    [Header("새로고침 UI")]
    public TMP_Text refreshCountText;      // 남은 reroll 기회 표시용

    // 콜백: 선택이 끝났을 때 StageSceneController 등에 알려주기 위한 델리게이트
    private Action<int> _onSelected;

    private void Awake()
    {
        // CrimerManager 자동 할당
        if (crimerManager == null)
        {
            crimerManager = FindObjectOfType<CrimerManager>();
            if (crimerManager == null)
            {
                Debug.LogError("[PrisonerSelectionUI] CrimerManager를 찾을 수 없습니다!");
            }
        }

        // 기본 유효성 체크
        if (slotTexts == null || slotTexts.Length == 0)
        {
            Debug.LogWarning("[PrisonerSelectionUI] slotTexts가 설정되지 않았습니다.");
        }

        if (refreshCountText == null)
        {
            Debug.LogWarning("[PrisonerSelectionUI] refreshCountText가 설정되지 않았습니다.");
        }
    }

    /// <summary>
    /// StageSceneController에서 호출:
    /// prisonerSelectionUI.Open(OnPrisonerSelected);
    /// </summary>
    public void Open(Action<int> onSelected)
    {
        Debug.Log($"[PrisonerSelectionUI] Open on {gameObject.name}, scene={gameObject.scene.name}", gameObject);

        _onSelected = onSelected;

        // UI 활성화
        gameObject.SetActive(true);

        if (crimerManager == null)
        {
            Debug.LogError("[PrisonerSelectionUI] CrimerManager가 없습니다. Open을 진행할 수 없습니다.");
            return;
        }

        // currentDraft 초기화 후 첫 조합 생성
        if (crimerManager.currentDraft == null)
            crimerManager.currentDraft = new List<PickUnit>();
        crimerManager.currentDraft.Clear();

        // 최초 1회 조합 뽑기 (이 안에서 rerollChances가 1 줄어듦)
        crimerManager.SelectRandomCrimers();

        // GameManager의 최대 리롤 횟수 기록 (처음 한 번만)
        if (GameManager.Instance != null && GameManager.Instance.maxRerollChances <= 0)
        {
            // SelectRandomCrimers를 한 번 호출하면서 rerollChances가 이미 1 감소했으므로
            // "최초 최대 리롤 횟수" = 현재 rerollChances + 1
            GameManager.Instance.maxRerollChances = crimerManager.rerollChances + 1;
        }

        // 슬롯 및 새로고침 텍스트 갱신
        UpdateSlotsFromCurrentDraft();
        UpdateRefreshText();
    }

    /// <summary>
    /// 현재 Draft 목록을 바탕으로 슬롯 텍스트를 갱신
    /// </summary>
    private void UpdateSlotsFromCurrentDraft()
    {
        if (crimerManager == null)
            return;

        List<PickUnit> draft = crimerManager.currentDraft;
        if (draft == null)
            return;

        int slotCount = (slotTexts != null) ? slotTexts.Length : 0;
        int pickCount = Mathf.Min(slotCount, draft.Count);

        for (int i = 0; i < slotCount; i++)
        {
            TMP_Text textComp = slotTexts[i];
            if (textComp == null)
                continue;

            Transform slotRoot = textComp.transform.parent;

            if (i < pickCount)
            {
                PickUnit unit = draft[i];

                if (unit != null)
                {
                    // ✅ 여기서 프리팹 이름 대신 능력 이름을 사용
                    CrimerAbility ability = unit.GetComponent<CrimerAbility>();

                    if (ability != null)
                    {
                        // 능력이 있는 죄수: AbilityName 사용
                        textComp.text = ability.AbilityName;
                    }
                    else
                    {
                        // 능력이 없는 죄수: 예비 이름 (프리팹 이름 또는 기본 텍스트)
                        textComp.text = "일반 죄수";   // 또는 unit.name
                    }
                }
                else
                {
                    textComp.text = "None";
                }

                if (slotRoot != null)
                    slotRoot.gameObject.SetActive(true);
            }
            else
            {
                // 뽑힌 죄수가 없는 슬롯은 비활성화
                if (slotRoot != null)
                    slotRoot.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 새로고침 횟수 텍스트 갱신
    /// </summary>
    private void UpdateRefreshText()
    {
        if (refreshCountText == null || crimerManager == null)
            return;

        int current = crimerManager.rerollChances;
        int maxMinusOne = current;

        if (GameManager.Instance != null && GameManager.Instance.maxRerollChances > 0)
        {
            // maxRerollChances는 "처음 조합 생성 + 리롤 가능 횟수" 총량
            // 실제 UI에는 "리롤 버튼을 누를 수 있는 최대 횟수"를 보여주고 싶으니 -1
            maxMinusOne = GameManager.Instance.maxRerollChances - 1;

            // 혹시 계산상 잘못 되어 0 이하가 나오면 방어
            if (maxMinusOne <= 0)
                maxMinusOne = current;
        }

        // 예: "2/3"
        refreshCountText.text = $"{current}/{maxMinusOne}";
    }

    // ---------------- 버튼 이벤트 ----------------

    /// <summary>
    /// 새로고침 버튼 OnClick
    /// </summary>
    public void OnClickRefresh()
    {
        if (crimerManager == null)
        {
            Debug.LogError("[PrisonerSelectionUI] OnClickRefresh: CrimerManager가 없습니다.");
            return;
        }

        if (crimerManager.rerollChances <= 0)
        {
            Debug.Log("[PrisonerSelectionUI] 더 이상 리롤할 수 없습니다.");
            return;
        }

        if (crimerManager.currentDraft == null)
            crimerManager.currentDraft = new List<PickUnit>();
        crimerManager.currentDraft.Clear();

        // 새 조합 뽑기 (이 안에서 rerollChances 감소)
        crimerManager.SelectRandomCrimers();

        // UI 갱신
        UpdateSlotsFromCurrentDraft();
        UpdateRefreshText();
    }

    /// <summary>
    /// "확인" 버튼 OnClick
    /// </summary>
    public void OnClickConfirm()
    {
        // 현재 crimerManager.currentDraft에 떠 있는 애들을
        // "이 스테이지에서 사용할 유닛들"로 스테이지 쪽에서 사용하면 됩니다.
        // 아직 prisonerId를 쓰지 않는다면 0만 넘겨도 OK
        _onSelected?.Invoke(0);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// "취소" 버튼 OnClick (디버그용이거나, 사용 안 할 수도 있음)
    /// </summary>
    public void Cancel()
    {
        // 필요하다면 여기서 _onSelected?.Invoke(-1); 같은 처리도 가능
        gameObject.SetActive(false);
    }
}
