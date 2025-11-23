using System.Collections.Generic;
using UnityEngine;
// 혹시 UI 관련 다른 스크립트의 타입(예: PrisonerCardUI)을 써야 하면 네임스페이스 추가 필요

public class Stage1TutorialController : MonoBehaviour
{
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
    [Tooltip("게임 시작 시 자동으로 실행될 '죄수 선택' 시퀀스입니다.")]
    public List<TutorialStep> seq1_Selection = new List<TutorialStep>();

    [Tooltip("인게임 진입 후 실행될 '초반 안내' 시퀀스입니다.")]
    public List<TutorialStep> seq2_InGameStart = new List<TutorialStep>();

    // (필요한 시퀀스 리스트를 계속 추가...)
    // public List<TutorialStep> seq3_TurretBuild = new List<TutorialStep>();


    void Start()
    {
        // 튜토리얼이 꺼져있거나, 이미 클리어했다면 실행 안 함 (PlayerPrefs 활용 예시)
        // if (!enableTutorial || PlayerPrefs.GetInt("TutorialCleared_Stage1", 0) == 1) return;

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

    // 죄수 선택 시퀀스가 끝났을 때 호출됨
    void OnSelectionSequenceFinished()
    {
        Debug.Log("감독: '죄수 선택' 설명 끝. 플레이어의 실제 선택을 기다림.");
        // 여기서는 바로 다음 튜토리얼을 실행하지 않음.
        // 플레이어가 실제로 '선택' 버튼을 눌러서 인게임으로 넘어가는 이벤트를 기다려야 함.

        // 예시: 게임 매니저의 이벤트 구독 (실제 코드에 맞게 수정 필요)
        // GameManager.Instance.OnGameStarted += StartInGameSequence;
    }

    // 2. 인게임 초반 시퀀스 시작 (외부 이벤트에 의해 호출된다고 가정)
    public void StartInGameSequence()
    {
        // 이벤트 구독 해제 (중복 실행 방지)
        // GameManager.Instance.OnGameStarted -= StartInGameSequence;

        Debug.Log("감독: '인게임 초반' 시퀀스 시작.");
        TutorialManager.Instance.StartSequence(seq2_InGameStart, OnInGameStartFinished);
    }

    void OnInGameStartFinished()
    {
         Debug.Log("감독: '인게임 초반' 안내 끝. 다음 상황(예: 포탑 설치) 대기.");
         // 다음 튜토리얼 조건을 체크하거나 이벤트 대기...
    }
}