using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingTipUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TMP_Text loadingText;  // "Loading..." 텍스트
    public TMP_Text tipText;      // Tip 내용을 표시할 텍스트
    public Button tipButton;      // Tip 영역(패널)에 붙일 버튼

    private GameFlowManager gameFlow;    

    void Awake()
    {
        gameFlow = GameFlowManager.Instance;
        gameObject.SetActive(false);

        if (tipButton != null)
        {
            tipButton.onClick.AddListener(OnClickTip);
        }

        if (loadingText != null)
        {
            loadingText.text = "Loading...";
        }
    }

    /// <summary>
    /// GameFlowManager에서 넘어오는 다음 Tip을 UI에 세팅
    /// </summary>
    void ShowNextTip()
    {
        if (gameFlow == null)
        {
            tipText.text = "";
            return;
        }

        string tip = gameFlow.GetNextTipMessage();
        tipText.text = tip;
    }

    /// <summary>
    /// 지정된 시간 동안 Tip UI를 보여주는 코루틴
    /// </summary>
    public IEnumerator ShowForSeconds(float duration)
    {
        // UI 켜기 + 첫 Tip 표시
        gameObject.SetActive(true);
        ShowNextTip();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;   // 타임스케일과 무관하게
            yield return null;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Tip 패널을 클릭했을 때 호출 (Inspector에서 Button OnClick으로 연결해도 됨)
    /// </summary>
    public void OnClickTip()
    {
        ShowNextTip();
    }
}

