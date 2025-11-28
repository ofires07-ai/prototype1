using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class EndingUIController : MonoBehaviour
{
    [Header("UI 참조")]
    public Image endingImage;             // 슬라이드 이미지
    public TMP_Text narrationText;        // 하단 나레이션 텍스트
    public TMP_Text centerEndingTitle;    // 마지막에 한가운데 크게 띄울 엔딩 이름

    [Header("점수 기준")]
    public int goodEndingScoreThreshold = 200;
    public int normalEndingScoreThreshold = 100;

    [Header("슬라이드 자동 진행 설정")]
    public float slideDuration = 5f;          // 각 슬라이드 유지 시간
    public float endingTitleDuration = 3f;    // 마지막 엔딩 타이틀 유지 시간

    [Header("엔딩별 슬라이드 데이터")]
    public Sprite[] badEndingImages;
    [TextArea(2, 4)] public string[] badEndingLines;

    public Sprite[] normalEndingImages;
    [TextArea(2, 4)] public string[] normalEndingLines;

    public Sprite[] goodEndingImages;
    [TextArea(2, 4)] public string[] goodEndingLines;

    public Sprite[] hiddenEndingImages;       // 혁명 엔딩
    [TextArea(2, 4)] public string[] hiddenEndingLines;

    [Header("엔딩별 BGM")]
    public AudioSource bgmSource;
    public AudioClip badBgm;
    public AudioClip normalBgm;
    public AudioClip goodBgm;
    public AudioClip hiddenBgm;

    private Sprite[] currentImages;
    private string[] currentLines;
    private int currentSlideIndex = 0;

    private enum EndingType { Bad, Normal, Good, Hidden }
    private EndingType currentEnding;
    private string currentEndingTitleText = "ENDING";

    private Coroutine sequenceRoutine;

    [Header("타이핑 설정")]
    public float charInterval = 0.03f;   // 글자 하나 찍히는 간격 (초)

    private bool isTyping = false;
    private Coroutine typingRoutine;

    void Start()
    {
        // 중앙 엔딩 타이틀은 처음에는 숨김
        if (centerEndingTitle != null)
        {
            centerEndingTitle.gameObject.SetActive(false);
        }

        SetupEnding();      // 점수 기반으로 어떤 엔딩인지 결정 + BGM 설정
        ShowSlide(0);       // 첫 나레이션 슬라이드 표시

        sequenceRoutine = StartCoroutine(PlayEndingSequence());
    }

    /// <summary>
    /// GameFlowManager에서 점수/특수 자원 정보를 가져와 엔딩 종류를 결정
    /// </summary>
    void SetupEnding()
    {
        int baseScore = 0;
        int totalSpecial = 0;

        if (GameFlowManager.Instance != null)
        {
            baseScore = GameFlowManager.Instance.TotalScore;
            totalSpecial = GameFlowManager.Instance.TotalSpecialLeft;
        }

        // 최종 점수 = 기본 자원 점수 + (남은 특수 자원 수 * 100)
        int finalScore = baseScore + totalSpecial * 100;

        // 히든(혁명) 엔딩 조건
        bool isHidden = (totalSpecial >= 9) && (finalScore >= normalEndingScoreThreshold);

        if (isHidden)
        {
            currentEnding = EndingType.Hidden;
            currentEndingTitleText = "혁명";
            currentImages = hiddenEndingImages;
            currentLines  = hiddenEndingLines;
            PlayBgm(hiddenBgm);
        }
        else if (finalScore >= goodEndingScoreThreshold)
        {
            currentEnding = EndingType.Good;
            currentEndingTitleText = "GOOD ENDING";
            currentImages = goodEndingImages;
            currentLines  = goodEndingLines;
            PlayBgm(goodBgm);
        }
        else if (finalScore >= normalEndingScoreThreshold)
        {
            currentEnding = EndingType.Normal;
            currentEndingTitleText = "NORMAL ENDING";
            currentImages = normalEndingImages;
            currentLines  = normalEndingLines;
            PlayBgm(normalBgm);
        }
        else
        {
            currentEnding = EndingType.Bad;
            currentEndingTitleText = "BAD ENDING";
            currentImages = badEndingImages;
            currentLines  = badEndingLines;
            PlayBgm(badBgm);
        }
    }

    void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    void ShowSlide(int index)
    {
        currentSlideIndex = index;

        int imgLen = currentImages != null ? currentImages.Length : 0;
        int txtLen = currentLines   != null ? currentLines.Length  : 0;

        if (endingImage != null && imgLen > 0 && index >= 0 && index < imgLen)
        {
            endingImage.sprite = currentImages[index];
            endingImage.enabled = true;
        }

        if (narrationText != null && txtLen > 0 && index >= 0 && index < txtLen)
        {
            StartTyping(currentLines[index]);
        }
    }

    void StartTyping(string line)
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        typingRoutine = StartCoroutine(TypeLineCoroutine(line));
    }

    IEnumerator TypeLineCoroutine(string line)
    {
        isTyping = true;

        if (narrationText != null)
        {
            // 1) 완성본 텍스트를 한 번에 넣고
            narrationText.text = line;

            // 2) 메쉬 업데이트(문자 개수 계산 위해)
            narrationText.ForceMeshUpdate();

            int totalChars = narrationText.textInfo.characterCount;

            // 3) 처음에는 아무 글자도 안 보이게
            narrationText.maxVisibleCharacters = 0;

            // 4) 한 글자씩 maxVisibleCharacters만 증가
            for (int i = 0; i < totalChars; i++)
            {
                narrationText.maxVisibleCharacters = i + 1;
                yield return new WaitForSeconds(charInterval);
            }
        }

        isTyping = false;
    }


    IEnumerator PlayEndingSequence()
    {
        // 1) 나레이션 + 이미지 슬라이드 자동 진행
        while (true)
        {
            int imgLen = currentImages != null ? currentImages.Length : 0;
            int txtLen = currentLines   != null ? currentLines.Length  : 0;
            int maxLen = Mathf.Max(imgLen, txtLen);

            if (maxLen == 0)
                break;

            // 마지막 슬라이드까지 도달했으면 루프 탈출
            if (currentSlideIndex >= maxLen - 1)
                break;

            // 🔹 먼저 타이핑이 끝날 때까지 기다렸다가
            while (isTyping)
            {
                yield return null;
            }

            // 🔹 그 다음에 슬라이드 유지 시간만큼 보여줌
            if (slideDuration > 0f)
            {
                yield return new WaitForSeconds(slideDuration);
            }

            // 다음 슬라이드로 이동
            ShowSlide(currentSlideIndex + 1);
        }

        // 마지막 슬라이드도 타이핑 끝까지 기다린 뒤,
        while (isTyping)
        {
            yield return null;
        }

        // 마지막 슬라이드를 추가로 조금 더 보여주고 싶으면
        if (slideDuration > 0f)
        {
            yield return new WaitForSeconds(slideDuration);
        }

        // 2) 중앙에 엔딩 타이틀 크게 보여주기
        ShowCenterEndingTitle();

        // 3) 엔딩 타이틀을 일정 시간 보여준 뒤 Result로 이동
        if (endingTitleDuration > 0f)
        {
            yield return new WaitForSeconds(endingTitleDuration);
        }

        GoToResult();
    }


    void ShowCenterEndingTitle()
    {
        // 🔹 타이핑 중이면 정리
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
        isTyping = false;

        // 1) 이미지 숨기기
        if (endingImage != null)
        {
            endingImage.enabled = false;
        }

        // 2) 나레이션 텍스트 비우기
        if (narrationText != null)
        {
            narrationText.text = string.Empty;
            // narrationText.gameObject.SetActive(false); // 완전 숨기고 싶으면
        }

        // 3) 중앙 엔딩 타이틀 표시
        if (centerEndingTitle != null)
        {
            centerEndingTitle.text = currentEndingTitleText;
            centerEndingTitle.gameObject.SetActive(true);
        }
    }


    void GoToResult()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GoToResult();
        }
    }

    // 스킵 버튼을 둘 경우를 대비한 함수 (선택사항)
    public void OnClickSkip()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
        ShowCenterEndingTitle();
        GoToResult();
    }
}
