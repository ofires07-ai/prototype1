using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public class PrisonerSelectionUI : MonoBehaviour
{
    [Header("후보 죄수 프리팹 (최대 10개)")]
    public GameObject[] prisonerPrefabs;   // 에디터에서 10개까지 넣기

    [Header("슬롯 UI (5칸)")]
    public TMP_Text[] slotTexts;           // 각 슬롯에 표시할 이름 텍스트 (5개)

    [Header("새로고침 UI")]
    public TMP_Text refreshCountText;      // "2/2" 같은 텍스트
    public int maxRefreshCount = 2;        // 최대 새로고침 횟수 (기본 2)

    private int remainingRefreshCount;
    private int[] currentIndices;          // 현재 슬롯에 들어간 프리팹 인덱스
    private Action<int> _onSelected;       // StageSceneController에서 넘겨주는 콜백

    void Awake()
    {
        if (slotTexts == null || slotTexts.Length == 0)
        {
            Debug.LogWarning("PrisonerSelectionUI: slotTexts가 설정되지 않았습니다.");
        }

        currentIndices = new int[slotTexts.Length];
        gameObject.SetActive(false);       // 시작 시에는 안 보이게
    }

    // StageSceneController에서 호출: prisonerSelectionUI.Open(OnPrisonerSelected);
    public void Open(Action<int> onSelected)
    {
        _onSelected = onSelected;
        remainingRefreshCount = maxRefreshCount;
        gameObject.SetActive(true);

        RollNewCombination();   // 첫 조합 뽑기
        UpdateRefreshText();
    }

    // ---------------- 랜덤 조합 뽑기 ----------------
    void RollNewCombination()
    {
        if (prisonerPrefabs == null || prisonerPrefabs.Length == 0)
        {
            Debug.LogWarning("PrisonerSelectionUI: prisonerPrefabs가 비어 있습니다.");
            return;
        }

        int slotCount = slotTexts.Length;
        int availableCount = prisonerPrefabs.Length;
        int pickCount = Mathf.Min(slotCount, availableCount);

        // 0..availableCount-1 인덱스 리스트 생성
        List<int> indices = new List<int>();
        for (int i = 0; i < availableCount; i++)
            indices.Add(i);

        // 앞에서부터 pickCount개만 필요하므로 부분 셔플
        for (int i = 0; i < pickCount; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, indices.Count);
            int temp = indices[i];
            indices[i] = indices[swapIndex];
            indices[swapIndex] = temp;
        }

        // 슬롯 채우기
        for (int i = 0; i < slotCount; i++)
        {
            if (i < pickCount)
            {
                int prefabIndex = indices[i];
                currentIndices[i] = prefabIndex;

                GameObject prefab = prisonerPrefabs[prefabIndex];

                if (slotTexts[i] != null)
                    slotTexts[i].text = prefab != null ? prefab.name : "None";

                // 슬롯 GameObject를 켬 (slotTexts의 부모를 슬롯으로 가정)
                if (slotTexts[i] != null)
                    slotTexts[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                currentIndices[i] = -1;
                if (slotTexts[i] != null)
                    slotTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    void UpdateRefreshText()
    {
        if (refreshCountText != null)
            refreshCountText.text = $"{remainingRefreshCount}/{maxRefreshCount}";
    }

    // ---------------- 버튼 이벤트 ----------------

    // 새로고침 버튼 OnClick에 연결
    public void OnClickRefresh()
    {
        if (remainingRefreshCount <= 0)
        {
            // 더 이상 새로고침 불가
            return;
        }

        remainingRefreshCount--;
        RollNewCombination();
        UpdateRefreshText();
    }

    // 선택 버튼 OnClick에 연결
    public void OnClickConfirm()
    {
        // 현재 슬롯에 떠 있는 프리팹들 수집
        List<GameObject> selected = new List<GameObject>();
        for (int i = 0; i < currentIndices.Length; i++)
        {
            int idx = currentIndices[i];
            if (idx >= 0 && idx < prisonerPrefabs.Length)
            {
                selected.Add(prisonerPrefabs[idx]);
            }
        }

        // GameFlowManager에 저장
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SaveSelectedPrisoners(selected);
        }

        // StageSceneController에게 "선택 완료" 알림
        _onSelected?.Invoke(0); // 지금은 prisonerId 안 쓰므로 0만 보냄

        gameObject.SetActive(false);
    }

    // 필요하면 닫기용
    public void Cancel()
    {
        gameObject.SetActive(false);
    }
}
