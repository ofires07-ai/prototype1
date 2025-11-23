using UnityEngine;

// (미래 대비용) 하이라이트 모양 정의
public enum HighlightShape
{
    Rectangle, // 사각형 (기본)
    Circle     // 원형 (나중에 필요하면 사용)
}

[System.Serializable] // 이 줄이 있어야 인스펙터에 보입니다!
public class TutorialStep
{
    [Header("단계 설정")]
    [TextArea(3, 5)] // 텍스트 입력창을 넓게 보여줍니다.
    [Tooltip("설명창에 표시될 텍스트입니다.")]
    public string instructionText;

    [Header("타겟 설정")]
    [Tooltip("하이라이트 할 실제 게임 UI 요소의 RectTransform을 연결하세요. (없으면 비워두세요)")]
    public RectTransform targetUI; // 목표 대상

    [Tooltip("나중에 마스킹 방식 도입 시 사용할 모양 정보 (지금은 무시해도 됩니다)")]
    public HighlightShape shape = HighlightShape.Rectangle;
}