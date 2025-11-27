using UnityEngine;

public enum InstructionPos
{
    BottomMid, // 하단 중앙 (기본값)
    TopMid,    // 상단 중앙
    Center,     // 정중앙
    LeftMid, //좌측 중앙
    RightMid //우측 중앙
}

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
    
    // ✅ [신규 추가] 3D 월드 오브젝트(죄수 유닛 등) 타겟용 변수
    [Tooltip("게임 월드에 있는 3D 오브젝트를 타겟팅할 때 사용합니다.")]
    public Transform targetWorldObject;
    
    [Tooltip("이 단계에서 설명창이 표시될 위치를 선택하세요.")]
    public InstructionPos boxPosition = InstructionPos.BottomMid;
    
    [Header("설명창 크기 옵션")]
    [Tooltip("설명창의 너비를 설정합니다. (0으로 두면 기본 크기 유지)")]
    public float boxWidth = 0f; 
    [Tooltip("설명창의 높이를 설정합니다. (0으로 두면 기본 크기 유지)")]
    public float boxHeight = 0f;

    [Tooltip("나중에 마스킹 방식 도입 시 사용할 모양 정보 (지금은 무시해도 됩니다)")]
    public HighlightShape shape = HighlightShape.Rectangle;
}