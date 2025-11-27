using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System; // Action을 쓰기 위해 필요

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance; // 싱글톤 인스턴스

    [Header("UI 컴포넌트 연결")]
    [Tooltip("튜토리얼 UI 전체를 감싸는 최상위 부모 오브젝트")]
    public GameObject tutorialRootPanel;
    [Tooltip("실제 타겟을 따라다닐 하이라이트 테두리 이미지")]
    public RectTransform highlighterFrame;
    
    // 설명창 전체를 감싸는 패널의 RectTransform이 필요합니다!
    [Tooltip("InstructionText와 NextButton을 포함하는 설명창 패널")]
    public RectTransform instructionPanelRect;
    
    [Tooltip("설명을 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI instructionTextPro;
    [Tooltip("다음 단계로 넘어가는 버튼")]
    public Button nextButton;

    // 내부 상태 변수들
    private List<TutorialStep> currentSequence = new List<TutorialStep>();
    private int currentStepIndex = -1;
    private Action onSequenceEndCallback; // 시퀀스 종료 시 호출될 콜백 저장용
    // 기본 크기 저장용 변수
    private Vector2 defaultBoxSize;

    void Awake()
    {
        // 싱글톤 설정 (중복 방지 및 씬 전환 유지)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 살아남도록 설정 (선택 사항, 필요 없으면 주석 처리)
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 초기화
        tutorialRootPanel.SetActive(false); // 시작 땐 숨김
        nextButton.onClick.AddListener(OnNextButtonClicked); // 버튼에 기능 연결
        
        if (instructionPanelRect != null)
        {
            defaultBoxSize = instructionPanelRect.sizeDelta;
        }
    }

    // --- [외부 호출용 함수] 감독이 이 함수들을 부릅니다 ---

    /// <summary>
    /// 특정 튜토리얼 시퀀스를 시작합니다.
    /// </summary>
    /// <param name="steps">실행할 단계 리스트</param>
    /// <param name="onEnd">시퀀스가 끝났을 때 실행할 함수 (선택 사항)</param>
    public void StartSequence(List<TutorialStep> steps, Action onEnd = null)
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] 빈 시퀀스가 전달되었습니다.");
            onEnd?.Invoke(); // 할 게 없으니 바로 종료 콜백 실행
            return;
        }

        currentSequence = steps;
        currentStepIndex = 0;
        onSequenceEndCallback = onEnd;

        tutorialRootPanel.SetActive(true); // UI 켜기
        Time.timeScale = 0f; // 게임 일시 정지 (중요!)

        ShowStep(currentStepIndex); // 첫 번째 단계 보여주기
    }

    /// <summary>
    /// 튜토리얼을 강제로 종료합니다.
    /// </summary>
    public void EndTutorial()
    {
        tutorialRootPanel.SetActive(false); // UI 끄기
        Time.timeScale = 1f; // 게임 재개 (중요!)

        currentSequence.Clear();
        currentStepIndex = -1;

        // 저장해둔 종료 콜백이 있다면 실행
        onSequenceEndCallback?.Invoke();
        onSequenceEndCallback = null;

        Debug.Log("[TutorialManager] 시퀀스 종료.");
    }

    // --- [내부 로직 함수] 조교가 알아서 하는 일들 ---

    // Next 버튼 클릭 시 호출됨
    private void OnNextButtonClicked()
    {
        currentStepIndex++; // 다음 인덱스로

        if (currentStepIndex < currentSequence.Count)
        {
            // 아직 단계가 남았으면 보여준다
            ShowStep(currentStepIndex);
        }
        else
        {
            // 모든 단계가 끝났으면 종료한다
            EndTutorial();
        }
    }

    // 특정 인덱스의 단계를 화면에 표시함
    private void ShowStep(int index)
    {
        TutorialStep step = currentSequence[index];

        // 1. 설명 텍스트 업데이트
        instructionTextPro.text = step.instructionText;
        
        // ✅ [수정] 2. 설명창 위치 및 크기 업데이트!
        SetInstructionBoxTransform(step);

        // 2. 하이라이트 이동 (핵심 기능!)
        FocusOnTarget(step);
    }
    
   // ✅ [초강력 디버깅 Ver.] 좌표 변환 집중 추적
    private void FocusOnTarget(TutorialStep step)
    {
        RectTransform uiTarget = step.targetUI;
        Transform worldTarget = step.targetWorldObject;

        if (uiTarget == null && worldTarget == null)
        {
            highlighterFrame.gameObject.SetActive(false);
            return;
        }

        highlighterFrame.gameObject.SetActive(true);

        Vector3 finalScreenPos = Vector3.zero;
        Vector2 finalSize = Vector2.zero;
        Camera mainCam = Camera.main; // 카메라 캐싱

        // --- 경우의 수 1: 타겟이 UI일 때 ---
        if (uiTarget != null)
        {
            finalScreenPos = uiTarget.position;
            finalSize = uiTarget.sizeDelta;
        }
        // --- 경우의 수 2: 타겟이 월드 오브젝트(죄수)일 때 ---
        else if (worldTarget != null)
        {
            // 🚨 [체크] 카메라 존재 여부
            if (mainCam == null)
            {
                Debug.LogError("🚨 [치명적 오류] MainCamera를 찾을 수 없습니다! 태그를 확인하세요.");
                return;
            }

            // 1. 위치 계산 (월드 좌표 -> 스크린 좌표)
            Vector3 targetWorldPos = worldTarget.position;
            Vector3 convertedScreenPos = mainCam.WorldToScreenPoint(targetWorldPos);

            Debug.Log($"--- [좌표 변환 추적 시작] ---");
            Debug.Log($"1. 타겟 월드 좌표: {targetWorldPos}");
            Debug.Log($"2. 변환된 스크린 좌표(raw): {convertedScreenPos}");
            Debug.Log($"   (참고: 현재 화면 해상도: {Screen.width} x {Screen.height})");

            // 🚨 [중요 체크] 변환된 좌표가 화면 안에 있는가?
            bool isOnScreen = convertedScreenPos.x >= 0 && convertedScreenPos.x <= Screen.width &&
                              convertedScreenPos.y >= 0 && convertedScreenPos.y <= Screen.height;
            
            // Z값이 0보다 작으면 카메라 뒤에 있다는 뜻 (Orthographic에서도 확인 필요)
            bool isInFrontOfCamera = convertedScreenPos.z > 0;

            if (!isOnScreen || !isInFrontOfCamera)
            {
                 Debug.LogWarning($"⚠️ [경고] 타겟이 화면 밖이나 카메라 뒤에 있습니다! 하이라이터가 이상한 곳에 표시될 수 있습니다. (화면안: {isOnScreen}, 카메라앞: {isInFrontOfCamera})");
            }
            else
            {
                Debug.Log("✅ [정상] 타겟 좌표가 화면 내부에 정상적으로 변환되었습니다.");
            }

            finalScreenPos = convertedScreenPos;
            Debug.Log($"--- [좌표 변환 추적 종료] ---");


            // 2. 크기 계산 (기존 로직 유지)
            SpriteRenderer sr = worldTarget.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Bounds bounds = sr.bounds;
                Vector3 screenBL = mainCam.WorldToScreenPoint(bounds.min);
                Vector3 screenTR = mainCam.WorldToScreenPoint(bounds.max);
                finalSize = new Vector2(Mathf.Abs(screenTR.x - screenBL.x), Mathf.Abs(screenTR.y - screenBL.y));
            }
            else
            {
                // SpriteRenderer가 없을 경우 대비
                Collider2D col = worldTarget.GetComponent<Collider2D>();
                if (col != null) {
                     Bounds bounds = col.bounds;
                     Vector3 screenBL = mainCam.WorldToScreenPoint(bounds.min);
                     Vector3 screenTR = mainCam.WorldToScreenPoint(bounds.max);
                     finalSize = new Vector2(Mathf.Abs(screenTR.x - screenBL.x), Mathf.Abs(screenTR.y - screenBL.y));
                } else {
                    finalSize = new Vector2(100f, 100f); // 기본값
                }
            }
        }

        // 최종 적용 (Z축은 UI이므로 0으로 평탄화)
        finalScreenPos.z = 0f; 
        highlighterFrame.position = finalScreenPos;
        highlighterFrame.sizeDelta = finalSize + new Vector2(50f, 50f);
    }

    // // 타겟 위치로 하이라이트 프레임을 이동시킴
    // // (⭐ 나중에 마스킹 방식으로 바꿀 때 이 함수 내용만 교체하면 됨 ⭐)
    // private void FocusOnTarget(RectTransform target, HighlightShape shape)
    // {
    //     // 타겟이 없으면 하이라이터 숨김 (설명만 있는 단계)
    //     if (target == null)
    //     {
    //         highlighterFrame.gameObject.SetActive(false);
    //         return;
    //     }
    //
    //     highlighterFrame.gameObject.SetActive(true);
    //
    //     // --- [방식 1: 테두리 프레임 이동 로직] ---
    //     
    //     // 타겟의 월드 좌표를 하이라이터의 좌표로 복사 (둘 다 UI이므로 잘 작동함)
    //     highlighterFrame.position = target.position;
    //
    //     // 크기 맞추기 (타겟 크기 + 약간의 여유 공간 20픽셀)
    //     // shape 정보는 지금은 무시하지만, 나중에 원형/사각형 구분 시 사용 가능
    //     highlighterFrame.sizeDelta = target.sizeDelta + new Vector2(20f, 20f);
    //
    //     // (참고: 타겟의 피벗/앵커 설정이 복잡해서 위치가 어긋난다면
    //     // RectTransformUtility.ScreenPointToLocalPointInRectangle 등을 써야 할 수 있음.
    //     // 일단은 이 간단한 방식으로 시작해보는 것 추천)
    // }
    
    // ✅ [수정됨] 설명창의 위치와 크기를 설정합니다.
    private void SetInstructionBoxTransform(TutorialStep step)
    {
        // 2-1. 크기 설정 (기존 코드 유지)
        float newWidth = (step.boxWidth > 0f) ? step.boxWidth : defaultBoxSize.x;
        float newHeight = (step.boxHeight > 0f) ? step.boxHeight : defaultBoxSize.y;
        instructionPanelRect.sizeDelta = new Vector2(newWidth, newHeight);

        // 2-2. 위치 설정 (앵커 고정, 피벗 및 위치만 변경)
        
        // [중요] 앵커를 중앙으로 고정합니다. (화면 정중앙 기준)
        // 이렇게 하면 화면 해상도가 바뀌어도 항상 중앙을 기준으로 계산됩니다.
        instructionPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionPanelRect.anchorMax = new Vector2(0.5f, 0.5f);

        // 여백 값 정의 (취향에 따라 조절하세요)
        float paddingX = 100f; // 좌우 가장자리에서 떨어진 거리
        float paddingY = 100f; // 상하 가장자리에서 떨어진 거리

        switch (step.boxPosition)
        {
            case InstructionPos.BottomMid:
                // 피벗을 하단 중앙으로 설정
                instructionPanelRect.pivot = new Vector2(0.5f, 0f);
                // 화면 중앙 기준(0,0)에서 아래쪽으로 이동, 바닥에서 paddingY만큼 띄움
                // (화면 높이의 절반만큼 아래로 간 뒤 + 패딩)
                instructionPanelRect.anchoredPosition = new Vector2(0f, -Screen.height / 2f + paddingY);
                break;

            case InstructionPos.TopMid:
                // 피벗을 상단 중앙으로 설정
                instructionPanelRect.pivot = new Vector2(0.5f, 1f);
                // 화면 중앙 기준에서 위쪽으로 이동, 천장에서 paddingY만큼 띄움
                instructionPanelRect.anchoredPosition = new Vector2(0f, Screen.height / 2f - paddingY);
                break;

            case InstructionPos.LeftMid:
                // 피벗을 좌측 중앙으로 설정
                instructionPanelRect.pivot = new Vector2(0f, 0.5f);
                // 화면 중앙 기준에서 왼쪽으로 이동, 왼쪽 벽에서 paddingX만큼 띄움
                instructionPanelRect.anchoredPosition = new Vector2(-Screen.width / 2f + paddingX, 0f);
                break;

            case InstructionPos.RightMid:
                // 피벗을 우측 중앙으로 설정
                instructionPanelRect.pivot = new Vector2(1f, 0.5f);
                // 화면 중앙 기준에서 오른쪽으로 이동, 오른쪽 벽에서 paddingX만큼 띄움
                instructionPanelRect.anchoredPosition = new Vector2(Screen.width / 2f - paddingX, 0f);
                break;

            case InstructionPos.Center:
                // 피벗을 중앙으로 설정
                instructionPanelRect.pivot = new Vector2(0.5f, 0.5f);
                // 화면 정중앙에 배치
                instructionPanelRect.anchoredPosition = Vector2.zero;
                break;
        }
    }
}