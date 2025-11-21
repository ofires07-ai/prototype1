using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // 싱글톤 (어디서든 접근 쉽게)
    public static TutorialManager Instance;

    [Header("튜토리얼 UI 요소 연결")]
    public GameObject tutorialCanvasRoot;   // 튜토리얼 전체 부모 오브젝트
    public RectTransform focusHighlighter;  // 타겟을 따라다닐 하이라이트 프레임
    public TextMeshProUGUI instructionText; // 설명 텍스트
    public Button nextButton;               // 다음 버튼

    [Header("튜토리얼 단계 데이터")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    private int currentStepIndex = -1;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작 시 튜토리얼 UI 숨기기
        tutorialCanvasRoot.SetActive(false);

        // Next 버튼 리스너 연결
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    void Start()
    {
        StartTutorial();
    }

    // --- 공개 메서드 ---

    // 튜토리얼 시작
    public void StartTutorial()
    {
        if (steps.Count == 0) return;

        Debug.Log("튜토리얼 시작!");
        tutorialCanvasRoot.SetActive(true);
        
        // [핵심] 게임 일시 정지!
        Time.timeScale = 0f; 
        // (만약 Time.timeScale로 멈추지 않는 요소가 있다면, GameManager.Instance.PauseGame() 같은 별도 함수 필요)

        currentStepIndex = 0;
        ShowStep(currentStepIndex);
    }

    // 튜토리얼 종료
    public void EndTutorial()
    {
        Debug.Log("튜토리얼 종료!");
        tutorialCanvasRoot.SetActive(false);
        
        // [핵심] 게임 재개!
        Time.timeScale = 1f;
        
        // (선택) 튜토리얼 완료 정보 저장 (PlayerPrefs 등)
        PlayerPrefs.SetInt("TutorialCleared", 1);
    }

    // --- 내부 로직 ---

    // 특정 단계 보여주기
    private void ShowStep(int index)
    {
        TutorialStep step = steps[index];

        // 1. 텍스트 업데이트
        instructionText.text = step.instruction;

        // 2. 하이라이터 위치 및 크기 이동 (핵심 로직!)
        if (step.targetRect != null)
        {
            focusHighlighter.gameObject.SetActive(true);

            // 타겟의 월드 위치를 캔버스 기준 로컬 위치로 변환 (UI 좌표계 맞추기)
            // 하이라이터의 부모가 타겟과 같은 캔버스 공간에 있다고 가정
            focusHighlighter.position = step.targetRect.position;
            
            // 크기 맞추기 (타겟의 크기 + 약간의 여유 공간)
            focusHighlighter.sizeDelta = step.targetRect.sizeDelta + new Vector2(20f, 20f); 

            // (고급) 만약 타겟의 앵커(Anchor) 설정이 복잡하다면 
            // RectTransformUtility.ScreenPointToLocalPointInRectangle 등을 사용해야 할 수도 있음.
            // 일단은 간단하게 position과 sizeDelta 복사로 시작해보세요.
        }
        else
        {
            // 타겟이 없으면 하이라이터 숨김 (그냥 설명만 나오는 단계)
            focusHighlighter.gameObject.SetActive(false);
        }
    }

    // Next 버튼 클릭 시
    private void OnNextButtonClicked()
    {
        currentStepIndex++;

        if (currentStepIndex < steps.Count)
        {
            ShowStep(currentStepIndex);
        }
        else
        {
            EndTutorial();
        }
    }
}