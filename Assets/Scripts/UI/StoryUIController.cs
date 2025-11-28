using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class StoryUIController : MonoBehaviour
{
    [Header("UI 참조")]
    public TMP_Text nameText;        // 화자 이름 (예: "독재자 방송")
    public TMP_Text dialogueText;    // 실제 대사 텍스트
    public GameObject nextIcon;      // "▶" 등 다음 표시 아이콘

    [Header("대사 내용")]
    [TextArea(2, 4)]
    public string[] lines;           // Inspector에서 대사 여러 줄 입력

    [Header("타이핑 설정")]
    public float charInterval = 0.03f;   // 글자 하나씩 보이는 간격 (초)

    [Header("사운드 설정")]
    public AudioSource typingSource;     // 5초짜리 타이핑 사운드 AudioSource (Loop = true)

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingRoutine;

    void Start()
    {
        currentIndex = 0;

        if (nameText != null)
        {
            nameText.text = "독재자 방송"; // 원하시는 이름으로 변경 가능
        }

        ShowLine(currentIndex);
    }

    // 버튼 / 패널 클릭 시 호출
    public void OnClickNext()
    {
        // 1) 아직 타이핑 중이면 → 타이핑 한 번에 완성
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        // 2) 다음 줄로 진행
        currentIndex++;

        // 3) 마지막 줄을 넘었다 → Stage1 시작
        if (currentIndex >= lines.Length)
        {
            StopTypingSound();  // 혹시 몰라 한 번 더 정리

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartGame();  // Stage1 로드
            }
            else
            {
                SceneManager.LoadScene("Stage1");
            }
            return;
        }

        // 4) 다음 줄 타이핑 시작
        ShowLine(currentIndex);
    }

    void ShowLine(int index)
    {
        if (dialogueText == null || lines == null || lines.Length == 0)
            return;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        typingRoutine = StartCoroutine(TypeLine(lines[index]));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if (nextIcon != null) nextIcon.SetActive(false);

        // 🔸 타이핑 사운드 시작 (Loop 켜진 AudioSource)
        StartTypingSound();

        if (dialogueText != null)
        {
            // 1) 완성본 텍스트 먼저 집어넣고
            dialogueText.text = line;

            // 2) 레이아웃/캐릭터 수 갱신
            dialogueText.ForceMeshUpdate();

            int totalChars = dialogueText.textInfo.characterCount;

            // 3) 처음에는 모든 글자를 숨김
            dialogueText.maxVisibleCharacters = 0;

            // 4) 한 글자씩 보이게
            for (int i = 0; i < totalChars; i++)
            {
                dialogueText.maxVisibleCharacters = i + 1;
                yield return new WaitForSeconds(charInterval);
            }
        }

        // 🔸 타이핑 종료 후 사운드 정지
        StopTypingSound();

        isTyping = false;
        if (nextIcon != null) nextIcon.SetActive(true);
    }

    void CompleteTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (dialogueText != null)
        {
            // 모든 글자를 한 번에 보이게 (충분히 큰 값)
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        StopTypingSound();

        isTyping = false;
        if (nextIcon != null) nextIcon.SetActive(true);
    }

    // --- 사운드 제어 ---

    void StartTypingSound()
    {
        if (typingSource != null && !typingSource.isPlaying)
        {
            typingSource.Play();   // Loop = true 이므로 계속 재생
        }
    }

    void StopTypingSound()
    {
        if (typingSource != null && typingSource.isPlaying)
        {
            typingSource.Stop();
        }
    }
}
