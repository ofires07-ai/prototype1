using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class EndingUIController : MonoBehaviour
{
    [Header("UI 참조")]
    public Image endingImage;             
    public TMP_Text narrationText;        
    public TMP_Text centerEndingTitle;    

    [Header("점수 기준")]
    public int HiddenEndingSpecialThreshold = 9;
    public int goodEndingScoreThreshold = 200;
    public int normalEndingScoreThreshold = 100;

    [Header("슬라이드 자동 진행 설정")]
    public float slideDuration = 5f;          
    public float endingTitleDuration = 3f;    

    [Header("엔딩별 슬라이드 데이터")]
    public Sprite[] badEndingImages;
    [TextArea(2, 4)] public string[] badEndingLines;

    public Sprite[] normalEndingImages;
    [TextArea(2, 4)] public string[] normalEndingLines;

    public Sprite[] goodEndingImages;
    [TextArea(2, 4)] public string[] goodEndingLines;

    public Sprite[] hiddenEndingImages;       
    [TextArea(2, 4)] public string[] hiddenEndingLines;

    [Header("엔딩별 BGM")]
    public AudioSource bgmSource;
    public AudioClip badBgm;
    public AudioClip normalBgm;
    public AudioClip goodBgm;
    public AudioClip hiddenBgm; // <- 히든은 안 쓸 거지만 데이터는 유지해도 됨

    [Header("타이핑 SFX (히든 엔딩 전용)")]
    public AudioSource typingSource;   // 타이핑 소리 전용 AudioSource
    public AudioClip typingClip;       // 타이핑 소리 클립
    [Range(0f, 1f)] public float typingVolume = 0.8f;

    private Sprite[] currentImages;
    private string[] currentLines;
    private int currentSlideIndex = 0;

    private enum EndingType { Bad, Normal, Good, Hidden }
    private EndingType currentEnding;
    private string currentEndingTitleText = "ENDING";

    private Coroutine sequenceRoutine;

    [Header("타이핑 설정")]
    public float charInterval = 0.03f;   

    private bool isTyping = false;
    private Coroutine typingRoutine;

    void Start()
    {
        if (centerEndingTitle != null)
        {
            centerEndingTitle.gameObject.SetActive(false);
        }

        SetupEnding();      
        ShowSlide(0);       

        sequenceRoutine = StartCoroutine(PlayEndingSequence());
    }

    void SetupEnding()
    {
        int baseScore = 0;
        int totalSpecial = 0;

        if (GameFlowManager.Instance != null)
        {
            baseScore = GameFlowManager.Instance.TotalScore;
            totalSpecial = GameFlowManager.Instance.TotalSpecialLeft;
        }

        int finalScore = baseScore + totalSpecial * 100;

        bool isHidden = (totalSpecial >= HiddenEndingSpecialThreshold) && (finalScore >= normalEndingScoreThreshold);

        if (isHidden)
        {
            currentEnding = EndingType.Hidden;
            currentEndingTitleText = "HIDDEN ENDING";
            currentImages = hiddenEndingImages;
            currentLines  = hiddenEndingLines;

            // ✅ 히든 엔딩은 BGM을 사용하지 않음
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
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
        // ✅ 이전 타이핑 중이면 중지 + 타이핑 사운드도 정리
        StopTypingSfx();

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

        // ✅ 히든 엔딩일 때만 타이핑 사운드 재생 시작
        if (currentEnding == EndingType.Hidden)
        {
            PlayTypingSfx();
        }

        if (narrationText != null)
        {
            narrationText.text = line;
            narrationText.ForceMeshUpdate();

            int totalChars = narrationText.textInfo.characterCount;
            narrationText.maxVisibleCharacters = 0;

            for (int i = 0; i < totalChars; i++)
            {
                narrationText.maxVisibleCharacters = i + 1;
                yield return new WaitForSeconds(charInterval);
            }
        }

        isTyping = false;

        // ✅ 타이핑 종료 시 사운드도 종료
        StopTypingSfx();
    }

    void PlayTypingSfx()
    {
        if (typingSource == null || typingClip == null) return;

        typingSource.clip = typingClip;
        typingSource.volume = typingVolume;
        typingSource.loop = true;

        if (!typingSource.isPlaying)
            typingSource.Play();
    }

    void StopTypingSfx()
    {
        if (typingSource == null) return;

        if (typingSource.isPlaying)
            typingSource.Stop();

        typingSource.clip = null;
    }

    IEnumerator PlayEndingSequence()
    {
        while (true)
        {
            int imgLen = currentImages != null ? currentImages.Length : 0;
            int txtLen = currentLines   != null ? currentLines.Length  : 0;
            int maxLen = Mathf.Max(imgLen, txtLen);

            if (maxLen == 0) break;
            if (currentSlideIndex >= maxLen - 1) break;

            while (isTyping)
                yield return null;

            if (slideDuration > 0f)
                yield return new WaitForSeconds(slideDuration);

            ShowSlide(currentSlideIndex + 1);
        }

        while (isTyping)
            yield return null;

        if (slideDuration > 0f)
            yield return new WaitForSeconds(slideDuration);

        ShowCenterEndingTitle();

        if (endingTitleDuration > 0f)
            yield return new WaitForSeconds(endingTitleDuration);

        GoToResult();
    }

    void ShowCenterEndingTitle()
    {
        // ✅ 타이핑 중 정리 + 타이핑 SFX도 정리
        StopTypingSfx();

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
        isTyping = false;

        if (endingImage != null)
        {
            endingImage.enabled = false;
        }

        if (narrationText != null)
        {
            narrationText.text = string.Empty;
        }

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

    public void OnClickSkip()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        // ✅ 스킵 시에도 타이핑 SFX 정리
        StopTypingSfx();

        ShowCenterEndingTitle();
        GoToResult();
    }
}
