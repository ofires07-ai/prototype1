using UnityEngine;

/// <summary>
/// 스타크래프트 스타일의 Fog of War 시스템
/// 유닛 주변만 밝게 보이고 나머지는 어둡게 표현
/// </summary>
public class FogOfWar : MonoBehaviour
{
    [Header("Fog of War 설정")]
    [Tooltip("어둠(Fog) 레이어 (SpriteRenderer 또는 UI Image)")]
    [SerializeField] private SpriteRenderer fogRenderer;
    
    [Tooltip("Fog 텍스처 크기 (해상도)")]
    [SerializeField] private int textureSize = 512;
    
    [Tooltip("Fog 색상 (기본: 검정색)")]
    [SerializeField] private Color fogColor = new Color(0, 0, 0, 0.8f);
    
    [Header("맵 설정")]
    [Tooltip("맵의 월드 크기 (width, height)")]
    [SerializeField] private Vector2 mapSize = new Vector2(20f, 20f);
    
    [Tooltip("맵의 중심 위치")]
    [SerializeField] private Vector2 mapCenter = Vector2.zero;

    private Texture2D fogTexture;
    private Color[] fogPixels;
    private bool needsUpdate = true;

    void Start()
    {
        InitializeFog();
    }

    void Update()
    {
        if (needsUpdate)
        {
            UpdateFogTexture();
            needsUpdate = false;
        }
    }

    /// <summary>
    /// Fog of War 초기화
    /// </summary>
    void InitializeFog()
    {
        // Fog 텍스처 생성
        fogTexture = new Texture2D(textureSize, textureSize);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        // 모든 픽셀을 어둡게 초기화
        fogPixels = new Color[textureSize * textureSize];
        for (int i = 0; i < fogPixels.Length; i++)
        {
            fogPixels[i] = fogColor;
        }

        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();

        // SpriteRenderer 설정
        if (fogRenderer == null)
        {
            // 자동으로 SpriteRenderer 생성
            GameObject fogObj = new GameObject("FogOfWar");
            fogObj.transform.SetParent(transform);
            fogRenderer = fogObj.AddComponent<SpriteRenderer>();
            fogRenderer.sortingOrder = 1000; // 최상위 레이어
        }

        // 텍스처를 스프라이트로 변환
        Sprite fogSprite = Sprite.Create(
            fogTexture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize / mapSize.x
        );

        fogRenderer.sprite = fogSprite;
        fogRenderer.transform.position = new Vector3(mapCenter.x, mapCenter.y, 0);
        fogRenderer.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1);

        Debug.Log("[FogOfWar] 초기화 완료!");
    }

    /// <summary>
    /// 유닛 위치를 밝게 만들기
    /// </summary>
    /// <param name="worldPosition">유닛의 월드 좌표</param>
    /// <param name="revealRadius">밝아지는 범위 (월드 단위)</param>
    public void RevealArea(Vector3 worldPosition, float revealRadius)
    {
        // 월드 좌표를 텍스처 좌표로 변환
        Vector2 normalizedPos = WorldToNormalized(worldPosition);
        int centerX = Mathf.RoundToInt(normalizedPos.x * textureSize);
        int centerY = Mathf.RoundToInt(normalizedPos.y * textureSize);

        // 반경을 픽셀 단위로 변환
        int radiusInPixels = Mathf.RoundToInt((revealRadius / mapSize.x) * textureSize);

        // 원 범위 내의 픽셀을 투명하게
        for (int y = -radiusInPixels; y <= radiusInPixels; y++)
        {
            for (int x = -radiusInPixels; x <= radiusInPixels; x++)
            {
                // 원 안에 있는지 확인
                if (x * x + y * y <= radiusInPixels * radiusInPixels)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    // 텍스처 범위 내에 있는지 확인
                    if (pixelX >= 0 && pixelX < textureSize && pixelY >= 0 && pixelY < textureSize)
                    {
                        int index = pixelY * textureSize + pixelX;
                        
                        // 부드러운 페이드 효과를 위한 값 계산
                        float distance = Mathf.Sqrt(x * x + y * y);
                        float fadeAmount = 1f - (distance / radiusInPixels);
                        fadeAmount = Mathf.Clamp01(fadeAmount);

                        // 알파 값을 부드럽게 조절 (중심은 투명, 가장자리는 원래 fog 색상)
                        float currentAlpha = fogPixels[index].a;
                        float targetAlpha = fogColor.a * (1f - fadeAmount);
                        fogPixels[index].a = Mathf.Min(currentAlpha, targetAlpha);
                    }
                }
            }
        }

        needsUpdate = true;
    }

    /// <summary>
    /// Fog 텍스처 업데이트
    /// </summary>
    void UpdateFogTexture()
    {
        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();
    }

    /// <summary>
    /// 월드 좌표를 0~1 범위의 정규화된 좌표로 변환
    /// </summary>
    Vector2 WorldToNormalized(Vector3 worldPos)
    {
        float normalizedX = (worldPos.x - (mapCenter.x - mapSize.x / 2)) / mapSize.x;
        float normalizedY = (worldPos.y - (mapCenter.y - mapSize.y / 2)) / mapSize.y;
        return new Vector2(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY));
    }

    /// <summary>
    /// Fog을 완전히 리셋 (모두 어둡게)
    /// </summary>
    public void ResetFog()
    {
        for (int i = 0; i < fogPixels.Length; i++)
        {
            fogPixels[i] = fogColor;
        }
        needsUpdate = true;
        Debug.Log("[FogOfWar] Fog 리셋 완료");
    }

    /// <summary>
    /// 특정 영역을 다시 어둡게 만들기 (선택적)
    /// </summary>
    public void ConcealArea(Vector3 worldPosition, float concealRadius)
    {
        Vector2 normalizedPos = WorldToNormalized(worldPosition);
        int centerX = Mathf.RoundToInt(normalizedPos.x * textureSize);
        int centerY = Mathf.RoundToInt(normalizedPos.y * textureSize);

        int radiusInPixels = Mathf.RoundToInt((concealRadius / mapSize.x) * textureSize);

        for (int y = -radiusInPixels; y <= radiusInPixels; y++)
        {
            for (int x = -radiusInPixels; x <= radiusInPixels; x++)
            {
                if (x * x + y * y <= radiusInPixels * radiusInPixels)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    if (pixelX >= 0 && pixelX < textureSize && pixelY >= 0 && pixelY < textureSize)
                    {
                        int index = pixelY * textureSize + pixelX;
                        fogPixels[index] = fogColor;
                    }
                }
            }
        }

        needsUpdate = true;
    }

    // 디버그용: Scene 뷰에서 맵 범위 표시
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(mapCenter.x, mapCenter.y, 0), new Vector3(mapSize.x, mapSize.y, 0));
    }
}