using UnityEngine;
using TMPro;

public class ResultUIController : MonoBehaviour
{
    [Header("UI 참조")]
    public TMP_Text clearText;
    public TMP_Text scoreText;

    void Start()
    {
        // 클리어 텍스트 기본값
        if (clearText != null)
            clearText.text = "GAME CLEAR";

        int totalScore = 0;
        if (GameFlowManager.Instance != null)
        {
            totalScore = GameFlowManager.Instance.TotalScore;
        }

        if (scoreText != null)
        {
            scoreText.text = $"Score : {totalScore}";
        }
    }

    // 버튼 OnClick에서 호출
    public void OnClickBackToTitle()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.BackToTitle();
        }
    }
}
