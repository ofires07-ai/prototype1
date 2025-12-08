using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class NameTagUI : MonoBehaviour
{
    private Canvas canvas;
    private SpriteRenderer[] childRenderers; // 내 자식에 있는 스프라이트들(아이콘 등)
    
    private Vector3 originalScale;
    private Vector3 originalPos;
    private Transform parentTransform;

    // 기준 오더 (몸통보다 무조건 위에 뜨게 높게 설정)
    private const int BASE_SORTING_ORDER = 50; 

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        
        // 1. 내 자식들 중에 숨어있는 SpriteRenderer들을 모두 찾습니다. (SourceIcon 등)
        childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        parentTransform = transform.parent;
        originalScale = transform.localScale;
        originalPos = transform.localPosition;

        if (canvas != null)
        {
            // 2. 캔버스 정렬 권한 가져오기
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "NameTag";
        }
        
        // Z축 살짝 당기기 (깜빡임 방지)
        transform.localPosition = new Vector3(originalPos.x, originalPos.y, -0.01f);
        originalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (parentTransform == null) return;

        // --- A. 좌우 반전 고정 ---
        if (parentTransform.lossyScale.x < 0)
        {
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
            transform.localPosition = new Vector3(-originalPos.x, originalPos.y, originalPos.z);
        }
        else
        {
            transform.localScale = originalScale;
            transform.localPosition = originalPos;
        }

        // --- B. Y축 정렬 (여기가 핵심!) ---
        
        // 🚨 중요: "Y가 높은 게 앞"이라고 하셨으므로 (+)를 씁니다.
        // 만약 반대로 작동하면 이 더하기(+)를 빼기(-)로 바꾸세요!
        // 예: BASE - (int)(transform.position.y * 100);
        int finalOrder = BASE_SORTING_ORDER - (int)(transform.position.y * 100);

        // 1. 캔버스(텍스트) 순서 적용
        if (canvas != null)
        {
            canvas.sortingOrder = finalOrder;
        }

        // 2. 자식 스프라이트(아이콘) 순서 적용
        if (childRenderers != null)
        {
            foreach (var sr in childRenderers)
            {
                // 얘네도 똑같이 NameTag 레이어로 맞춤
                sr.sortingLayerName = "NameTag";
                
                // 텍스트랑 겹치면 아이콘이 뒤로 가도록 -1 해줌 (취향껏 +1 해도 됨)
                sr.sortingOrder = finalOrder - 1; 
            }
        }
    }
}