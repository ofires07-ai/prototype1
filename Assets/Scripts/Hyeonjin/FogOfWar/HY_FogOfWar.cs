using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI Image를 쓸 때만 사용됩니다.

/*
 * HY_FogOfWar
 * - 스타크래프트 스타일 Fog of War
 * - 유닛 시야 원형으로 안개 알파를 낮춰서 밝힘(영구 공개)
 * - 자원 프리팹 주변만 초기 알파를 '더 진하게' 올려서 종류는 안 보이고 위치만 어렴풋이 보이게
 * - SpriteRenderer / UI Image 둘 다 지원 (fogImage가 지정되어 있으면 UI 모드)
 */
[DisallowMultipleComponent]
public class HY_FogOfWar : MonoBehaviour
{
    public static HY_FogOfWar Instance { get; private set; }

    [Header("Fog 표시 타겟 (둘 중 하나만 사용)")]
    [Tooltip("SpriteRenderer 기반으로 맵 위를 덮을 때 사용")]
    [SerializeField] private SpriteRenderer fogRenderer; // 비워두면 자동 생성
    [Tooltip("UI Canvas 위의 Image로 사용할 때 지정 (Raycast Target 해제 권장)")]
    [SerializeField] private Image fogImage;

    [Header("Fog 텍스처/비주얼")]
    [Tooltip("Fog 텍스처 해상도(정사각). 높을수록 선명하지만 비용 증가")]
    [SerializeField] private int textureSize = 512;
    [Tooltip("기본 안개 색상(알파가 전체 투명도)")]
    [SerializeField] private Color fogColor = new Color(0f, 0f, 0f, 0.6f);
    [Tooltip("시야 가장자리 부드러움(0=하드엣지, 1=부드러움)")]
    [Range(0f, 1f)] public float softEdge = 0.6f;

    [Header("맵 설정(월드 단위)")]
    [Tooltip("맵의 월드 크기(가로, 세로)")]
    [SerializeField] private Vector2 mapSize = new Vector2(20f, 20f);
    [Tooltip("맵의 중심 좌표")]
    [SerializeField] private Vector2 mapCenter = Vector2.zero;

    [Header("업데이트")]
    [Tooltip("Reveal 요청을 모아 이 주기마다 텍스처 Apply")]
    [SerializeField] private float applyInterval = 0.05f;

    [Header("Resource Hint (선택)")]
    [Tooltip("자원 주변만 초기 알파를 더 진하게 올려 종류 노출을 막음(위치는 어렴풋이 보임)")]
    [SerializeField] private bool useResourceBoost = true;
    [Tooltip("자원 프리팹 태그")]
    [SerializeField] private string resourceTag = "Resource";
    [Tooltip("자원 주변을 더 짙게 덮을 반경(월드 단위)")]
    [SerializeField] private float resourceBoostRadius = 0.6f;
    [Tooltip("자원 주변 영역의 목표 알파(기본 안개보다 크게 설정)")]
    [SerializeField, Range(0f, 1f)] private float resourceBoostAlpha = 0.85f;
    
    [Header("Ability: Special Resource Reveal")]
    [Tooltip("어빌리티: 특수 자원을 식별할 태그")]
    [SerializeField] private string specialResourceTag = "SpecialSource"; // ⬅️ 이 태그를 유니티에서 새로 만드셔야 합니다.
    [Tooltip("어빌리티: 특수 자원을 밝힐 반경(월드 단위)")]
    [SerializeField] public float specialResourceRevealRadius = 0.3f; // ⬅️ 밝힐 반경 (조절 가능)

    [Header("초기 항상 보이는 영역(옵션)")]
    [Tooltip("초기화 시, 특정 월드 영역(rect)을 항상 보이도록(알파=0) 만듭니다.")]
    [SerializeField] private bool useAlwaysVisibleAreas = false;

    // world-space 좌표 기준의 사각형들 (x,y = 중심, width,height = 크기)
    [SerializeField] private List<Rect> alwaysVisibleRects = new List<Rect>();

    // 내부 상태
    private Texture2D fogTexture;
    private Color[] fogPixels;
    private bool dirty;
    private float applyTimer;

    private struct CircleReveal { public Vector3 pos; public float radius; }
    private readonly List<CircleReveal> pendingReveals = new List<CircleReveal>();

    // ------------------------------- Unity Life Cycle -------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        InitializeFog();
    }

    private void Update()
    {
        // 모인 시야 요청 처리
        if (pendingReveals.Count > 0)
        {
            for (int i = 0; i < pendingReveals.Count; i++)
            {
                var r = pendingReveals[i];
                RevealCircleImmediate(r.pos, r.radius);
            }
            pendingReveals.Clear();
            dirty = true;
        }

        // 배치 Apply
        applyTimer += Time.deltaTime;
        if (dirty && applyTimer >= applyInterval)
        {
            fogTexture.SetPixels(fogPixels);
            fogTexture.Apply();
            dirty = false;
            applyTimer = 0f;
        }
    }

    // ------------------------------- Public API -------------------------------
    /// <summary>
    /// 유닛에서 호출: 원형 시야(월드 반경)를 큐에 쌓음 → 배치 처리
    /// </summary>
    public void RevealCircle(Vector3 worldPosition, float revealRadius)
    {
        pendingReveals.Add(new CircleReveal { pos = worldPosition, radius = revealRadius });
    }

    /// <summary>
    /// 과거 코드 호환용: RevealArea == RevealCircle
    /// </summary>
    public void RevealArea(Vector3 worldPosition, float revealRadius)
    {
        RevealCircle(worldPosition, revealRadius);
    }

    /// <summary>
    /// 안개 전부 리셋(다시 어둡게) + 자원 힌트 재적용
    /// </summary>
    public void ResetFog()
    {
        if (fogPixels == null) return;

        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i] = fogColor;

        // 자원 주변만 더 진하게(초기 노출 억제)
        if (useResourceBoost)
            ApplyResourceBoostOnce();

        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();

        dirty = false;
        applyTimer = 0f;
        pendingReveals.Clear();
    }

    // ------------------------------- Core -------------------------------
    private void InitializeFog()
    {
        // 텍스처 생성/초기화
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        fogPixels = new Color[textureSize * textureSize];
        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i] = fogColor;

        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();

        // 텍스처 → 스프라이트
        float pixelsPerUnit = Mathf.Max(1f, textureSize / Mathf.Max(0.0001f, mapSize.x));
        var fogSprite = Sprite.Create(
            fogTexture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        // UI Image 모드 우선
        if (fogImage != null)
        {
            fogImage.sprite = fogSprite;
            fogImage.color  = Color.white;
            fogImage.preserveAspect = false; // Canvas 전체를 덮도록
        }
        else
        {
            // SpriteRenderer 모드: 없으면 자동 생성
            if (fogRenderer == null)
            {
                var fogObj = new GameObject("FogOfWar");
                fogObj.transform.SetParent(transform, false);
                fogRenderer = fogObj.AddComponent<SpriteRenderer>();
                fogRenderer.sortingOrder = 1000; // 필요 시 정렬 조정
            }

            fogRenderer.sprite = fogSprite;
            fogRenderer.transform.position = new Vector3(mapCenter.x, mapCenter.y, 0f);
            fogRenderer.transform.localScale = Vector3.one; // 픽셀-유닛 스케일은 ppu로 맞춤
        }

        // 초기 자원 힌트 적용(자원 주변만 더 진하게)
        if (useResourceBoost)
        {
            ApplyResourceBoostOnce();
        }

        // 🔹 항상 보이는 영역(강, 강 사이 등) 마스크 적용
        if (useAlwaysVisibleAreas && alwaysVisibleRects != null)
        {
            for (int i = 0; i < alwaysVisibleRects.Count; i++)
            {
                ClearRectArea(alwaysVisibleRects[i]);
            }
        }

        // 마지막으로 텍스처 반영
        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();

        Debug.Log("[HY_FogOfWar] 초기화 완료");
    }

    // 큐 없이 즉시 픽셀 편집
    private void RevealCircleImmediate(Vector3 worldPosition, float revealRadius)
    {
        Vector2 uv = WorldToUV(worldPosition);
        int cx = Mathf.RoundToInt(uv.x * textureSize);
        int cy = Mathf.RoundToInt(uv.y * textureSize);

        int rPx = Mathf.RoundToInt((revealRadius / mapSize.x) * textureSize);
        if (rPx <= 0) return;

        float softStart = Mathf.Clamp01(1f - softEdge); // t<=softStart까지는 완전 투명, 바깥은 페이드

        int x0 = Mathf.Max(0, cx - rPx);
        int x1 = Mathf.Min(textureSize - 1, cx + rPx);
        int y0 = Mathf.Max(0, cy - rPx);
        int y1 = Mathf.Min(textureSize - 1, cy + rPx);

        for (int y = y0; y <= y1; y++)
        {
            int dy = y - cy;
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx;
                float d = dx * dx + dy * dy;
                if (d > rPx * rPx) continue;

                float dist = Mathf.Sqrt(d);
                float t = dist / Mathf.Max(1, rPx); // 0~1
                float fade = (t <= softStart) ? 1f : Mathf.Clamp01(1f - (t - softStart) / (1f - softStart));
                float targetA = fogColor.a * (1f - fade); // 중심 0, 테두리 fogA

                int idx = y * textureSize + x;

                // 더 밝아지는 방향(알파 ↓)만 허용 → 영구 공개
                if (targetA < fogPixels[idx].a)
                {
                    var c = fogPixels[idx];
                    c.a = targetA;
                    fogPixels[idx] = c;
                }
            }
        }
    }

    // 자원 주변 원형 영역의 알파를 최소 targetAlpha 이상으로 '올림'
    private void BoostAreaAlpha(Vector3 worldPosition, float radius, float targetAlpha)
    {
        Vector2 uv = WorldToUV(worldPosition);
        int cx = Mathf.RoundToInt(uv.x * textureSize);
        int cy = Mathf.RoundToInt(uv.y * textureSize);

        int rPx = Mathf.RoundToInt((radius / mapSize.x) * textureSize);
        if (rPx <= 0) return;

        int x0 = Mathf.Max(0, cx - rPx);
        int x1 = Mathf.Min(textureSize - 1, cx + rPx);
        int y0 = Mathf.Max(0, cy - rPx);
        int y1 = Mathf.Min(textureSize - 1, cy + rPx);

        for (int y = y0; y <= y1; y++)
        {
            int dy = y - cy;
            int dy2 = dy * dy;
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy2 > rPx * rPx) continue;

                int idx = y * textureSize + x;
                if (fogPixels[idx].a < targetAlpha)
                {
                    var c = fogPixels[idx];
                    c.a = targetAlpha;
                    fogPixels[idx] = c;
                }
            }
        }
    }

    // 월드 좌표 기준 직사각형 영역을 "완전 투명"으로 깎는 함수
    private void ClearRectArea(Rect worldRect)
    {
        // Rect는 (x, y, width, height)이고, x,y는 "좌측 하단"이 아니라 "xMin,yMin"로 취급해야 하므로
        Vector2 min = new Vector2(worldRect.xMin, worldRect.yMin);
        Vector2 max = new Vector2(worldRect.xMax, worldRect.yMax);

        Vector2 uvMin = WorldToUV(new Vector3(min.x, min.y));
        Vector2 uvMax = WorldToUV(new Vector3(max.x, max.y));

        int x0 = Mathf.Clamp(Mathf.RoundToInt(uvMin.x * textureSize), 0, textureSize - 1);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(uvMax.x * textureSize), 0, textureSize - 1);
        int y0 = Mathf.Clamp(Mathf.RoundToInt(uvMin.y * textureSize), 0, textureSize - 1);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(uvMax.y * textureSize), 0, textureSize - 1);

        for (int y = y0; y <= y1; y++)
        {
            int row = y * textureSize;
            for (int x = x0; x <= x1; x++)
            {
                int idx = row + x;

                // 완전 투명으로 설정
                var c = fogPixels[idx];
                c.a = 0f;
                fogPixels[idx] = c;
            }
        }
    }


    // 초기화/리셋 시 1회 실행
    private void ApplyResourceBoostOnce()
    {
        var resources = GameObject.FindGameObjectsWithTag(resourceTag);
        for (int i = 0; i < resources.Length; i++)
            BoostAreaAlpha(resources[i].transform.position, resourceBoostRadius, resourceBoostAlpha);
    }

    // 월드 → UV(0~1)
    private Vector2 WorldToUV(Vector3 worldPos)
    {
        float x0 = mapCenter.x - mapSize.x * 0.5f;
        float y0 = mapCenter.y - mapSize.y * 0.5f;

        float u = (worldPos.x - x0) / mapSize.x;
        float v = (worldPos.y - y0) / mapSize.y;
        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    private void OnDrawGizmos()
    {
        // 맵 전체 영역 (기존 코드)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(mapCenter.x, mapCenter.y, 0f),
                            new Vector3(mapSize.x, mapSize.y, 0f));

        // 🔹 항상 보이는 영역 Rect 들도 그려주기
        if (useAlwaysVisibleAreas && alwaysVisibleRects != null)
        {
            // 테두리 색
            Gizmos.color = Color.cyan;

            foreach (var rect in alwaysVisibleRects)
            {
                // Rect는 (x, y, width, height) 구조이고, (x, y)는 좌측 하단이 아니라
                // Unity에서 center/size를 따로 가져올 수 있습니다.
                Vector3 center = new Vector3(rect.center.x, rect.center.y, 0f);
                Vector3 size   = new Vector3(rect.size.x,   rect.size.y,   0f);

                Gizmos.DrawWireCube(center, size);
            }
        }
    }


    /// <summary>
    /// [새 함수] 어빌리티가 호출할 함수: 씬의 모든 특수 자원 위치를 즉시 밝힙니다.
    /// </summary>
    public void RevealAllSpecialSources()
    {
        // 안개 텍스처(fogPixels)가 아직 준비되지 않았다면 (Start 함수가 아직 실행 안 됨)
        if (fogPixels == null)
        {
            Debug.LogError("[HY_FogOfWar] 안개가 초기화되기 전에 특수 자원 밝히기 어빌리티가 호출되었습니다!");
            return;
        }

        // 1. "SpecialSource" 태그를 가진 모든 오브젝트를 씬에서 찾습니다.
        var specialResources = GameObject.FindGameObjectsWithTag(specialResourceTag);
        
        if (specialResources.Length == 0)
        {
            Debug.LogWarning("[HY_FogOfWar] 'SpecialSource' 태그를 가진 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[HY_FogOfWar] 죄수 어빌리티: 특수 자원 {specialResources.Length}개의 위치를 밝힙니다.");

        // 2. 찾은 모든 자원 위치에 대해 '즉시 밝히기'를 실행합니다.
        for (int i = 0; i < specialResources.Length; i++)
        {
            RevealCircleImmediate(specialResources[i].transform.position, specialResourceRevealRadius);
        }
        
        // 3. 텍스처에 변경 사항을 적용하도록 'dirty' 플래그를 true로 설정합니다.
        dirty = true;
    }
}
