using UnityEngine;

using UnityEngine;

[System.Serializable] // 인스펙터에서 리스트로 보기 위해 필수
public class TutorialStep : MonoBehaviour
{
    [Header("설명 텍스트")]
    [TextArea(3, 5)] // 텍스트 입력창 넓게 보기
    public string instruction;

    [Header("하이라이트 타겟")]
    [Tooltip("강조 표시할 UI 요소의 RectTransform을 연결하세요.")]
    public RectTransform targetRect;

    // (선택 사항) Next 버튼 대신 특정 행동을 기다려야 할 때 사용
    // public bool waitForSpecificAction = false; 
}
