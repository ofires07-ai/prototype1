using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingTipUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TMP_Text loadingText;  // "Loading..." 텍스트
    public TMP_Text tipText;      // Tip 내용을 표시할 텍스트
    public Button tipButton;      // Tip 패널 클릭용 버튼

    private GameFlowManager gameFlow;
    private Canvas canvas;        // ← 추가

    void Awake()
    {
        gameFlow = GameFlowManager.Instance;
        canvas  = GetComponent<Canvas>();   // ← 자기 Canvas 캐시

        if (tipButton != null)
            tipButton.onClick.AddListener(OnClickTip);

        if (loadingText != null)
            loadingText.text = "Loading...";

        gameObject.SetActive(false);
    }

    void ShowNextTip()
    {
        if (gameFlow == null)
            gameFlow = GameFlowManager.Instance;

        if (gameFlow == null)
        {
            if (tipText != null) tipText.text = "";
            return;
        }

        string tip = gameFlow.GetNextTipMessage();
        if (tipText != null)
            tipText.text = tip;
    }

    public IEnumerator ShowForSeconds(float duration)
    {
        // 🔝 항상 최상단으로 정렬
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder   = 1000;   // 다른 캔버스보다 확실히 큰 값
        }

        gameObject.SetActive(true);
        ShowNextTip();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void OnClickTip()
    {
        ShowNextTip();
    }
}
