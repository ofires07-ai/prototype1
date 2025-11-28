using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameOverUIController : MonoBehaviour
{
    [Header("UI 참조")]
    public Image deathImage;              // 우주선 파괴 일러스트 1장
    public TMP_Text deathText;            // 하단 나레이션 텍스트
    public TMP_Text centerGameOverTitle;  // 중앙 큰 "GAME OVER" 텍스트
    public Button backToTitleButton;      // 타이틀로 돌아가기 버튼

    [Header("나레이션 설정")]
    [TextArea(2, 4)]
    public string deathNarration =
        "당신의 우주선은 파괴되고 점령은 실패하였습니다.\n" +
        "당신은 아무도 기억하지 못하는 곳에서 최후를 맞이하였습니다.";

    [Tooltip("글자 하나가 나타나는 간격(초, 실제 시간 기준)")]
    public float charInterval = 0.03f;

    [Tooltip("나레이션 타이핑이 끝난 뒤, 화면에 그대로 유지할 시간(초)")]
    public float deathTextStayDuration = 3f;

    [Header("BGM 설정")]
    public AudioSource bgmSource;     // GameOver용 BGM AudioSource
    public AudioClip gameOverBgm;     // 나중에 Inspector에서 할당

    private Coroutine typingRoutine;
    private bool isTyping = false;

    void Start()
    {
        // 초기 상태: 이미지 + 나레이션만, GAME OVER / 버튼은 숨김
        if (centerGameOverTitle != null)
            centerGameOverTitle.gameObject.SetActive(false);

        if (backToTitleButton != null)
            backToTitleButton.gameObject.SetActive(false);

        if (deathText != null)
        {
            // 완성 텍스트 먼저 넣고, maxVisibleCharacters로 타이핑 연출
            deathText.text = deathNarration;
            deathText.maxVisibleCharacters = 0;
        }

        // BGM 세팅 (클립만 나중에 넣어도 동작하도록 방어)
        if (bgmSource != null && gameOverBgm != null)
        {
            bgmSource.clip = gameOverBgm;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // 게임 오버 시퀀스 시작
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        // 1) 나레이션 타이핑
        if (deathText != null)
        {
            typingRoutine = StartCoroutine(TypeNarration());
            yield return typingRoutine;
        }

        // 2) 타이핑이 끝난 후, 일정 시간 동안 그대로 유지
        if (deathTextStayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(deathTextStayDuration);
        }

        // 3) 이미지/나레이션 숨기고 GAME OVER + 버튼 표시
        ShowGameOverUI();
    }

    IEnumerator TypeNarration()
    {
        isTyping = true;

        // 텍스트 정보 갱신
        deathText.ForceMeshUpdate();
        int totalChars = deathText.textInfo.characterCount;
        deathText.maxVisibleCharacters = 0;

        for (int i = 0; i < totalChars; i++)
        {
            deathText.maxVisibleCharacters = i + 1;
            // Time.timeScale = 0 상태에서도 돌아가게 Realtime 사용
            yield return new WaitForSecondsRealtime(charInterval);
        }

        isTyping = false;
    }

    void ShowGameOverUI()
    {
        // 1) 기존 이미지 / 텍스트 숨기기
        if (deathImage != null)
            deathImage.enabled = false;

        if (deathText != null)
        {
            deathText.text = string.Empty;
            // 완전히 안 보이게 하고 싶으면 아래도 가능
            // deathText.gameObject.SetActive(false);
        }

        // 2) 중앙 GAME OVER 텍스트 표시
        if (centerGameOverTitle != null)
        {
            centerGameOverTitle.text = "GAME OVER";
            centerGameOverTitle.gameObject.SetActive(true);
        }

        // 3) Back to Title 버튼 활성화
        if (backToTitleButton != null)
            backToTitleButton.gameObject.SetActive(true);
    }

    // 버튼 OnClick
    public void OnClickBackToTitle()
    {
        // 게임 속도 복구 (StageSceneController에서 0으로 만들었기 때문)
        Time.timeScale = 1f;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.BackToTitle();
        }
        else
        {
            Debug.LogError("[GameOverUIController] GameFlowManager.Instance가 없습니다!");
        }
    }
}
