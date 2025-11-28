using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("씬 이름 설정")]
    public string titleSceneName = "Title";      // 타이틀 씬 이름
    public string[] stageSceneNames =           // 스테이지 씬 목록
    {
        "Stage1",
        "Stage2"
    };
    public string resultSceneName = "Result";   // 결과 씬 이름

    [Header("페이드 설정")]
    public ScreenFader screenFader;
    public float fadeDuration = 1.0f;           // 1~2초 정도

    [Header("점수")]
    [Tooltip("각 스테이지별 점수 (Stage1 = 0, Stage2 = 1, ...)")]
    public int[] stageScores;
    public int TotalScore { get; private set; }

    [Tooltip("모든 스테이지에서 남긴 특수 자원(T5)의 총합")]
    public int TotalSpecialLeft { get; private set; }

    [Header("로딩 Tip 설정")]
    public LoadingTipUI loadingTipUI;
    [Tooltip("스테이지 사이에서 Tip을 보여줄 최소 시간(초)")]
    public float tipDisplayTime = 5f;

    [Tooltip("로딩 화면에서 순차적으로 보여줄 Tip 메시지들")]
    [TextArea(2, 4)]
    public string[] tipMessages;

    [Header("스토리 / 엔딩 씬 설정")]
    [Tooltip("Title 이후, Stage1에 들어가기 전에 보여줄 스토리 씬 이름")]
    public string storySceneNameBeforeStage1 = "Story_BeforeStage1";

    [Tooltip("마지막 스테이지 클리어 후, Result 전에 보여줄 엔딩 씬 이름")]
    public string endingSceneName = "EndingScene";

    [Header("게임 오버 씬 설정")]
    public string gameOverSceneName = "GameOver";



    private int _currentTipIndex = 0;   // 지금까지 몇 번의 Tip을 보여줬는지 기록


    public int CurrentStageIndex { get; private set; } = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureStageScoreArray();

        if (screenFader == null)
            screenFader = GetComponentInChildren<ScreenFader>();

        // 게임 시작 시에는 검은 화면에서 서서히 밝아지게
        //if (screenFader != null)
        //    StartCoroutine(screenFader.FadeOutCoroutine(fadeDuration));
    }

    void EnsureStageScoreArray()
    {
        if (stageSceneNames == null) return;

        if (stageScores == null || stageScores.Length != stageSceneNames.Length)
        {
            stageScores = new int[stageSceneNames.Length];
        }
    }

    public void ResetScores()
    {
        EnsureStageScoreArray();
        TotalScore = 0;
        TotalSpecialLeft = 0;

        if (stageScores != null)
        {
            for (int i = 0; i < stageScores.Length; i++)
            {
                stageScores[i] = 0;
            }
        }
    }

    /// <summary>
    /// 현재 스테이지의 클리어 점수를 등록하고, TotalScore를 다시 계산합니다.
    /// </summary>
    public void AddStageScore(int score)
    {
        EnsureStageScoreArray();

        if (CurrentStageIndex >= 0 &&
            stageScores != null &&
            CurrentStageIndex < stageScores.Length)
        {
            stageScores[CurrentStageIndex] = score;
        }

        // 총합 재계산
        TotalScore = 0;
        if (stageScores != null)
        {
            for (int i = 0; i < stageScores.Length; i++)
            {
                TotalScore += stageScores[i];
            }
        }
    }

    public void AddStageScore(int score, int specialLeft)
    {
        AddStageScore(score);        // 기존 로직 재사용
        TotalSpecialLeft += specialLeft;  // 🔹남은 특수 자원 누적
    }

    public bool HasNextStage => CurrentStageIndex + 1 < stageSceneNames.Length;

    // Title 씬의 Start 버튼에서 호출됨
    public void StartGame()
    {
        if (CurrentStageIndex >= 0) return; // 이미 진행 중

        Debug.Log("[GF] StartGame 호출됨");
        // 새 게임 시작이므로 점수 리셋
        ResetScores();

        StartCoroutine(LoadStageRoutine(0));
    }
    public void GoToNextStage()
    {
        int len = (stageSceneNames != null) ? stageSceneNames.Length : 0;
        int nextIndex = CurrentStageIndex + 1;

        Debug.Log($"[GF] GoToNextStage 호출: CurrentStageIndex={CurrentStageIndex}, " +
                $"nextIndex={nextIndex}, stageSceneNames.Length={len}");

        if (nextIndex < len)
        {
            // 아직 다음 스테이지가 남아있으면 → Tip + 다음 스테이지
            Debug.Log($"[GF] 다음 스테이지 로드: index={nextIndex} ({stageSceneNames[nextIndex]})");
            StartCoroutine(LoadStageRoutine(nextIndex));
        }
        else
        {
            // 🔚 모든 스테이지를 다 깬 경우 → 엔딩 씬으로
            Debug.Log("[GF] 마지막 스테이지 클리어! 엔딩 씬으로 이동");

            if (!string.IsNullOrEmpty(endingSceneName))
            {
                StartCoroutine(LoadEndingRoutine());
            }
            else
            {
                // endingSceneName을 비워두면 기존처럼 바로 Result로
                Debug.LogWarning("[GF] endingSceneName이 설정되지 않아 바로 Result로 이동합니다.");
                StartCoroutine(LoadResultRoutine());
            }
        }
    }


    public void BackToTitle()
    {
        CurrentStageIndex = -1;
        ResetScores();
        StartCoroutine(LoadTitleRoutine());
    }

    IEnumerator LoadStageRoutine(int stageIndex)
    {
        Debug.Log($"[GF] LoadStageRoutine 시작. stageIndex={stageIndex}");

        // 🔹 이전 스테이지 인덱스를 기억해둔다.
        int prevStageIndex = CurrentStageIndex;

        // 🔹 이제부터 로드될 스테이지 인덱스를 현재 인덱스로 설정
        CurrentStageIndex = stageIndex;

        // 1) 먼저 화면을 검게 페이드 인
        if (screenFader != null)
        {
            Debug.Log("[GF] FadeInCoroutine 호출");
            yield return screenFader.FadeInCoroutine(fadeDuration);
        }

        // 2) "이전 스테이지가 존재할 때만" 로딩 Tip 노출
        //    - 타이틀 → Stage1: prevStageIndex == -1 이므로 Tip X
        //    - Stage1 → Stage2: prevStageIndex == 0 → Tip O
        if (prevStageIndex >= 0 && loadingTipUI != null && tipDisplayTime > 0f)
        {
            Debug.Log("[GF] LoadingTipUI.ShowForSeconds 호출");
            yield return loadingTipUI.ShowForSeconds(tipDisplayTime);
        }

        // 3) 실제 스테이지 씬 로딩
        yield return SceneManager.LoadSceneAsync(stageSceneNames[stageIndex]);
        Debug.Log("[GF] Scene 로드 완료");
        yield return null; // 한 프레임 대기

        // 4) 화면을 다시 밝게 페이드 아웃
        if (screenFader != null)
        {
            Debug.Log("[GF] FadeOutCoroutine 호출");
            yield return screenFader.FadeOutCoroutine(fadeDuration);
        }
    }


    IEnumerator LoadResultRoutine()
    {
        if (screenFader != null)
            yield return screenFader.FadeInCoroutine(fadeDuration);

        yield return SceneManager.LoadSceneAsync(resultSceneName);

        yield return null;

        if (screenFader != null)
            yield return screenFader.FadeOutCoroutine(fadeDuration);
    }

    IEnumerator LoadTitleRoutine()
    {
        if (screenFader != null)
            yield return screenFader.FadeInCoroutine(fadeDuration);

        yield return SceneManager.LoadSceneAsync(titleSceneName);

        yield return null;

        if (screenFader != null)
            yield return screenFader.FadeOutCoroutine(fadeDuration);
    }

        public void ExitGame()
    {
#if UNITY_EDITOR
        // 에디터에서 테스트할 때는 Play 모드를 끔
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드(.exe, .app 등)에서는 게임 종료
        Application.Quit();
#endif
    }

    /// <summary>
    /// tipMessages 배열에서 다음 Tip을 꺼내고, 인덱스를 앞으로 진행시킵니다.
    /// 끝까지 간 경우 다시 0으로 돌아갑니다.
    /// </summary>
    public string GetNextTipMessage()
    {
        if (tipMessages == null || tipMessages.Length == 0)
            return "";

        // 현재 인덱스의 Tip
        string tip = tipMessages[_currentTipIndex];

        // 다음 호출을 위해 인덱스 증가 (배열 끝이면 0으로)
        _currentTipIndex = (_currentTipIndex + 1) % tipMessages.Length;

        return tip;
    }

    public void GoToStoryBeforeStage1()
    {
        // 이미 게임이 진행 중이면(스테이지 안이라면) 다시 타이틀-스토리로 가지 않도록
        if (CurrentStageIndex >= 0) return;

        if (string.IsNullOrEmpty(storySceneNameBeforeStage1))
        {
            Debug.LogWarning("[GF] storySceneNameBeforeStage1가 설정되지 않아 바로 Stage1로 진행합니다.");
            StartGame(); // 기존 로직으로 바로 Stage1
            return;
        }

        StartCoroutine(LoadStoryRoutine(storySceneNameBeforeStage1));
    }

    IEnumerator LoadStoryRoutine(string sceneName)
    {
        if (screenFader != null)
            yield return screenFader.FadeInCoroutine(fadeDuration);

        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        yield return null;

        if (screenFader != null)
            yield return screenFader.FadeOutCoroutine(fadeDuration);
    }

    IEnumerator LoadEndingRoutine()
    {
        if (screenFader != null)
            yield return screenFader.FadeInCoroutine(fadeDuration);

        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(endingSceneName);
        yield return null;

        if (screenFader != null)
            yield return screenFader.FadeOutCoroutine(fadeDuration);
    }

    public void GoToResult()
    {
        StartCoroutine(LoadResultRoutine());
    }


    IEnumerator LoadGameOverRoutine()
    {
        if (screenFader != null)
            yield return screenFader.FadeInCoroutine(fadeDuration);

        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(gameOverSceneName);
        yield return null;

        if (screenFader != null)
            yield return screenFader.FadeOutCoroutine(fadeDuration);
    }

    public void GoToGameOver()
    {
        StartCoroutine(LoadGameOverRoutine());
    }

}
