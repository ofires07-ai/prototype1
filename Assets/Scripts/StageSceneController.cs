using UnityEngine;

public class StageSceneController : MonoBehaviour
{
    [Header("필수 참조")]
    public GameManager gameManager;
    public SpawnManager spawnManager;

    [Header("UI 참조")]
    public PrisonerSelectionUI prisonerSelectionUI;
    public UpgradeCardUI upgradeCardUI;

    [Header("설정")]
    [Tooltip("이 스테이지 클리어 후 업그레이드 카드를 먼저 보여줄지 여부")]
    public bool showUpgradeBeforeNextStage = false;

    void Awake()
    {
        // 스테이지 로드 후, 죄수 선택 전까지는 게임 로직 정지
        if (gameManager != null) gameManager.enabled = false;
        if (spawnManager != null) spawnManager.enabled = false;
    }

    void Start()
    {
        // 죄수 선택 UI를 띄운다.
        if (prisonerSelectionUI != null)
        {
            prisonerSelectionUI.Open(OnPrisonerSelected);
        }
        else
        {
            // 임시: 죄수 선택 UI가 아직 없으면 바로 게임 시작
            StartGameplay();
        }

        // 스테이지 클리어 이벤트 구독
        if (spawnManager != null)
        {
            spawnManager.OnAllWavesCompleted += HandleAllWavesCompleted;
        }
    }

    void OnDestroy()
    {
        if (spawnManager != null)
        {
            spawnManager.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }
    }

    // 죄수 선택 완료 콜백
    void OnPrisonerSelected(int prisonerId)
    {
        // TODO: prisonerId를 다른 시스템(플레이어 스탯, 능력 등)에 넘기고 싶으면 여기서 처리
        StartGameplay();
    }

    void StartGameplay()
    {
        if (gameManager != null && !gameManager.enabled)
            gameManager.enabled = true;

        if (spawnManager != null && !spawnManager.enabled)
            spawnManager.enabled = true;
    }

    // Boss Wave까지 포함해서 모든 웨이브를 다 깬 시점
    void HandleAllWavesCompleted()
    {
        // 1) 남은 자원 총합을 스테이지 점수로 계산
        int stageScore = CalculateStageScoreFromResources();

        // 2) GameFlowManager에 등록 (Stage1, Stage2 각각 들어감)
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AddStageScore(stageScore);
        }

        // 3) 이후 흐름: 업그레이드 → 다음 스테이지/결과
        if (showUpgradeBeforeNextStage && upgradeCardUI != null)
        {
            // 스테이지1: 업그레이드 카드 먼저
            upgradeCardUI.Open(OnUpgradeCardSelected);
        }
        else
        {
            // 스테이지2: 바로 다음 씬(결과 화면 or 다음 스테이지)
            GameFlowManager.Instance.GoToNextStage();
        }
    }

    void OnUpgradeCardSelected(int cardIndex)
    {
        // TODO: 선택된 카드 효과를 아군 포탑 강화에 적용하는 로직
        // (어떤 능력치/프리팹인지는 나중에 결정)
        GameFlowManager.Instance.GoToNextStage();
    }

    /// <summary>
    /// InventoryManager.resourceTotals에 남아 있는 자원 총합을 점수로 사용.
    /// </summary>
    int CalculateStageScoreFromResources()
    {
        var inv = FindObjectOfType<InventoryManager>();
        if (inv == null || inv.resourceTotals == null)
            return 0;

        int sum = 0;
        foreach (var kvp in inv.resourceTotals)
        {
            sum += kvp.Value;
        }

        return sum;
    }
}
