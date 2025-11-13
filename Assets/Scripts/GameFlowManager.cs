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

        [Header("Prisoner Selection")]
    public List<GameObject> selectedPrisonerPrefabs = new List<GameObject>();

    public void SaveSelectedPrisoners(List<GameObject> prisoners)
    {
        selectedPrisonerPrefabs.Clear();
        if (prisoners == null) return;
        selectedPrisonerPrefabs.AddRange(prisoners);
    }


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

    public bool HasNextStage => CurrentStageIndex + 1 < stageSceneNames.Length;

    // Title 씬의 Start 버튼에서 호출됨
    public void StartGame()
    {
        if (CurrentStageIndex >= 0) return; // 이미 진행 중

        // 새 게임 시작이므로 점수 리셋
        ResetScores();

        StartCoroutine(LoadStageRoutine(0));
    }

    public void GoToNextStage()
    {
        if (HasNextStage)
        {
            StartCoroutine(LoadStageRoutine(CurrentStageIndex + 1));
        }
        else
        {
            StartCoroutine(LoadResultRoutine());
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
        CurrentStageIndex = stageIndex;

        if (screenFader != null)
            yield return screenFader.FadeInCoroutine(fadeDuration);   // 화면 검게

        yield return SceneManager.LoadSceneAsync(stageSceneNames[stageIndex]); // 스테이지 로딩

        yield return null; // 한 프레임 대기

        if (screenFader != null)
            yield return screenFader.FadeOutCoroutine(fadeDuration);  // 게임 화면 보이기
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

}
