using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 혹시 UI 관련 다른 스크립트의 타입(예: PrisonerCardUI)을 써야 하면 네임스페이스 추가 필요

public class Stage1TutorialController : MonoBehaviour
{
    // ✅ [추가] 우주선 스크립트 참조 (스폰된 죄수 명단을 얻기 위해 필요)
    public SpaceShip spaceShipScript;
    [Header("--- [설정] 튜토리얼 활성화 여부 ---")]
    public bool enableTutorial = true;

    [Header("--- [타겟 연결] 실제 게임 UI 버튼들을 연결하세요 ---")]
    [Header("[시퀀스 1: 죄수 선택]")]
    public RectTransform crimerCards;
    public RectTransform rerollButtonTarget;

    [Header("[시퀀스 2: 인게임 초반]")]
    // (필요한 타겟들을 계속 추가...)
    // public RectTransform mineralTarget_A; 
    // public RectTransform buildMenuButtonTarget;


    [Header("--- [데이터] 시퀀스별 단계 정의 ---")]
    // 인스펙터에서 직접 데이터를 입력할 리스트들입니다.
    [Tooltip("게임 시작 시 자동으로 실행될 '죄수 능력 및 선택' 시퀀스입니다.")]
    public List<TutorialStep> seq1_Selection = new List<TutorialStep>();

    [Tooltip("인게임 진입 후 실행될 '죄수 유닛 조작' 시퀀스입니다.")]
    public List<TutorialStep> seq2_CrimerControll = new List<TutorialStep>();

    [Tooltip("'광물 및 상부UI' 시퀀스입니다.")]
    public List<TutorialStep> seq3_Source = new List<TutorialStep>();
    
    [Tooltip("'하부 UI 및 포탑 설치' 시퀀스입니다.")]
    public List<TutorialStep> seq4_Build = new List<TutorialStep>();

    [Tooltip("'웨이브 및 보스' 시퀀스입니다.")]
    public List<TutorialStep> seq5_Wave = new List<TutorialStep>();

    void Start()
    {
        Debug.Log("[Stage1TutorialController] Start 호출됨");

        // (안전장치) 혹시 인스펙터에서 우주선 연결을 까먹었을 경우 자동 검색
        if (spaceShipScript == null) spaceShipScript = FindObjectOfType<SpaceShip>();

        Debug.Log($"[Stage1TutorialController] enableTutorial: {enableTutorial}, spaceShipScript: {(spaceShipScript != null ? spaceShipScript.name : "null")}");

        if (!enableTutorial)
        {
            Debug.Log("[Stage1TutorialController] 튜토리얼이 비활성화되어 있습니다.");
            return;
        }

        // 튜토리얼이 꺼져있거나, 이미 클리어했다면 실행 안 함 (PlayerPrefs 활용 예시)
        // if (!enableTutorial || PlayerPrefs.GetInt("TutorialCleared_Stage1", 0) == 1) return;

        // ✅ [수정] 이벤트를 미리 구독해두고 시작
        SpaceShip.OnAllPrisonersSpawned += OnPrisonersSpawnedEarly;

        // 게임 시작 후 약간의 딜레이를 두고 첫 번째 시퀀스 시작
        Invoke(nameof(StartSelectionSequence), 0.5f);
    }

    // --- [시퀀스 실행 함수들] ---

    // 1. 죄수 선택 시퀀스 시작
    void StartSelectionSequence()
    {
        // 조교에게 시퀀스 데이터와, 끝났을 때 호출할 콜백 함수를 넘김
        TutorialManager.Instance.StartSequence(seq1_Selection, OnSelectionSequenceFinished);
    }

    // ✅ [신규 함수] 이벤트를 미리 받아두는 함수
    private bool prisonersSpawned = false;
    private bool selectionSequenceFinished = false;

    void OnPrisonersSpawnedEarly()
    {
        Debug.Log("[Stage1TutorialController] 죄수 스폰 완료! (미리 감지)");
        prisonersSpawned = true;
        CheckAndStartCrimerSequence();
    }

    // 죄수 선택 시퀀스가 끝났을 때 호출됨
    void OnSelectionSequenceFinished()
    {
        Debug.Log("감독: '죄수 선택' 설명 끝. 플레이어의 실제 선택을 기다림.");
        selectionSequenceFinished = true;
        CheckAndStartCrimerSequence();
    }

    // 두 조건이 모두 만족되면 다음 시퀀스 시작
    void CheckAndStartCrimerSequence()
    {
        if (prisonersSpawned && selectionSequenceFinished)
        {
            Debug.Log("[Stage1TutorialController] 두 조건 모두 만족! Crimer 시퀀스 시작");
            StartCrimerControllSequence();
        }
    }

    // 2. 인게임 초반 시퀀스 시작 (외부 이벤트에 의해 호출된다고 가정)
    public void StartCrimerControllSequence()
    {
        Debug.Log("[Stage1TutorialController] StartCrimerControllSequence 호출됨!");

        // ✅ [중요] 이벤트는 한 번만 발생하면 되므로, 실행 즉시 구독을 해제합니다.
        // (OnDestroy에 있는 것과는 별개로, 중복 실행 방지를 위해 여기서도 해주는 게 좋습니다.)
        SpaceShip.OnAllPrisonersSpawned -= StartCrimerControllSequence;
        AssignPrisonerTargets(); // 이때 타겟을 할당합니다.
        TutorialManager.Instance.StartSequence(seq2_CrimerControll, OnCrimerControllSequenceFinished);
    }

    void OnCrimerControllSequenceFinished()
    {
         Debug.Log("감독: '인게임 초반' 안내 끝. 다음 상황(예: 포탑 설치) 대기.");
         // 다음 튜토리얼 조건을 체크하거나 이벤트 대기...
    }
    
    // ✅ [수정된 핵심 함수] 죄수 대신 스폰 포인트를 타겟으로 할당
    private void AssignPrisonerTargets()
    {
        // 🚨 안전장치: 우주선 스크립트가 없거나 스폰 포인트 목록이 비어있으면 중단
        if (spaceShipScript == null || spaceShipScript.rallyPoints == null || spaceShipScript.rallyPoints.Count == 0)
        {
            Debug.LogWarning("감독: 우주선에서 스폰 포인트(rallyPoints)를 찾을 수 없습니다! 타겟 할당 실패.");
            return;
        }

        // 예시: 'seq2_CrimerControll'의 첫 번째 단계(Element 0)가
        // 첫 번째 죄수가 스폰된 위치를 가리켜야 한다고 가정합니다.
        if (seq2_CrimerControll.Count > 0)
        {
            // ✅ [변경점] 죄수 유닛 대신 우주선의 첫 번째 스폰 포인트를 가져옵니다.
            Transform firstSpawnPoint = spaceShipScript.rallyPoints[0];

            // TutorialStep의 월드 타겟에 할당합니다.
            seq2_CrimerControll[0].targetWorldObject = firstSpawnPoint;
            
            // (중요: UI 타겟은 비워둡니다.)
            seq2_CrimerControll[0].targetUI = null; 

            Debug.Log($"감독: 스폰 포인트 {firstSpawnPoint.name}을(를) 튜토리얼 타겟으로 할당했습니다.");
        }
        
        // 필요하다면 다른 단계에도 추가 할당 가능
        // if (seq2_CrimerControll.Count > 1 && spaceShipScript.rallyPoints.Count > 1) { ... }
    }
    
    void OnDestroy()
    {
        // 이 객체가 파괴될 때 구독을 해제합니다.
        SpaceShip.OnAllPrisonersSpawned -= OnPrisonersSpawnedEarly;
        SpaceShip.OnAllPrisonersSpawned -= StartCrimerControllSequence;
    }
}