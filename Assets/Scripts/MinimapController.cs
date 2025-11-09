using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MinimapController : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    [Header("References")]
    public Camera mainCamera;            // 메인 카메라
    public Camera minimapCamera;         // 미니맵 전용 카메라(맵 전체를 봄)
    public RectTransform minimapRect;    // RawImage의 RectTransform
    public RawImage minimapImage;        // 미니맵 텍스처 표시용
    public RectTransform cameraFrame;    // 카메라 위치를 보여줄 흰 사각형(테두리만)
    public GameObject enemyDotPrefab;    // 빨간 점(UI Image가 달린 작은 프리팹)
    public Transform enemiesRoot;        // (선택) 적들을 담는 부모가 있으면 지정
    public BoxCollider2D worldBounds;    // 맵 전체 경계(권장)

    [Header("Tuning")]
    public float refreshInterval = 0.1f; // 적 점/프레임 갱신 주기(초)
    public string enemyTag = "Enemy";    // 적 태그(없으면 HY_Enemy 컴포넌트로 탐색)

    // 내부 상태
    private Bounds mapBounds;
    private float timer;
    private readonly Dictionary<Transform, RectTransform> dotMap = new();

    void Awake()
    {
        if (minimapCamera != null && worldBounds != null)
        {
            mapBounds = worldBounds.bounds;

            // 미니맵 카메라가 맵 전체를 보게 설정
            minimapCamera.orthographic = true;
            minimapCamera.transform.position =
                new Vector3(mapBounds.center.x, mapBounds.center.y, minimapCamera.transform.position.z);
            minimapCamera.orthographicSize = mapBounds.extents.y; // 높이 절반을 사이즈로
        }
        else
        {
            Debug.LogWarning("[Minimap] worldBounds 또는 minimapCamera가 비어 있습니다. 수동 크기/위치를 사용하세요.");
        }
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            RefreshEnemies();
            RefreshCameraFrame();
        }
    }

    // --- 적 점 갱신 ---
    void RefreshEnemies()
    {
        List<Transform> enemies = new List<Transform>();
        if (enemiesRoot != null)
        {
            foreach (Transform child in enemiesRoot)
            {
                if (child == null) continue;
                if (child.CompareTag(enemyTag) || child.GetComponent<HY_Enemy>() != null)
                    enemies.Add(child);
            }
        }
        else
        {
            // 1) 태그로 찾기(스크립트명이 달라져도 작동)
            var tagged = GameObject.FindGameObjectsWithTag(enemyTag);
            if (tagged != null && tagged.Length > 0)
            {
                foreach (var go in tagged) enemies.Add(go.transform);
            }
            else
            {
                // 2) 백업: HY_Enemy가 있으면 그것도 포함
                var all = GameObject.FindObjectsOfType<HY_Enemy>();
                foreach (var e in all) enemies.Add(e.transform);
            }
        }

        // 없는 점 정리
        var toRemove = new List<Transform>();
        foreach (var kv in dotMap)
        {
            if (kv.Key == null) toRemove.Add(kv.Key);
        }
        foreach (var dead in toRemove)
        {
            Destroy(dotMap[dead].gameObject);
            dotMap.Remove(dead);
        }

        // 점 생성/위치 갱신
        foreach (var tr in enemies)
        {
            if (tr == null) continue;

            if (!dotMap.TryGetValue(tr, out RectTransform dot))
            {
                var go = Instantiate(enemyDotPrefab, minimapRect);
                dot = go.GetComponent<RectTransform>();
                dotMap[tr] = dot;
            }

            Vector2 anchored = WorldToMinimapAnchored(tr.position);
            dot.anchoredPosition = anchored;
        }
    }

    // --- 카메라 프레임 갱신 ---
    void RefreshCameraFrame()
    {
        if (mainCamera == null || cameraFrame == null) return;
        if (minimapRect == null) return;

        // 메인 카메라의 월드 뷰포트 크기
        float halfH = mainCamera.orthographicSize;
        float halfW = halfH * mainCamera.aspect;

        // 프레임 크기를 미니맵 좌표계에서 계산
        Vector2 a = WorldToMinimapAnchored(new Vector2(mainCamera.transform.position.x - halfW, mainCamera.transform.position.y - halfH));
        Vector2 b = WorldToMinimapAnchored(new Vector2(mainCamera.transform.position.x + halfW, mainCamera.transform.position.y + halfH));

        Vector2 size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        Vector2 center = (a + b) * 0.5f;

        cameraFrame.sizeDelta = size;
        cameraFrame.anchoredPosition = center;
    }

    // --- 미니맵 클릭/드래그로 이동 ---
    public void OnPointerClick(PointerEventData eventData) => PanToPointer(eventData);
    public void OnDrag(PointerEventData eventData) => PanToPointer(eventData);

    void PanToPointer(PointerEventData eventData)
    {
        if (mainCamera == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        // local(anchored) -> 월드
        Vector3 target = MinimapAnchoredToWorld(local);

        // 카메라 중심 이동 + 경계 클램프
        if (worldBounds != null)
        {
            float halfH = mainCamera.orthographicSize;
            float halfW = halfH * mainCamera.aspect;

            float minX = worldBounds.bounds.min.x + halfW;
            float maxX = worldBounds.bounds.max.x - halfW;
            float minY = worldBounds.bounds.min.y + halfH;
            float maxY = worldBounds.bounds.max.y - halfH;

            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.y = Mathf.Clamp(target.y, minY, maxY);
        }

        mainCamera.transform.position = new Vector3(target.x, target.y, mainCamera.transform.position.z);
        RefreshCameraFrame();
    }

    // --- 좌표 변환 ---
    Vector2 WorldToMinimapAnchored(Vector2 worldPos)
    {
        if (worldBounds == null)
        {
            // worldBounds가 없으면 minimapCamera 기준으로 추정
            var size = minimapCamera.orthographicSize;
            float minX = minimapCamera.transform.position.x - size * minimapCamera.aspect;
            float maxX = minimapCamera.transform.position.x + size * minimapCamera.aspect;
            float minY = minimapCamera.transform.position.y - size;
            float maxY = minimapCamera.transform.position.y + size;
            return WorldToAnchored(worldPos, minX, maxX, minY, maxY);
        }
        else
        {
            var b = worldBounds.bounds;
            return WorldToAnchored(worldPos, b.min.x, b.max.x, b.min.y, b.max.y);
        }
    }

    Vector2 WorldToAnchored(Vector2 worldPos, float minX, float maxX, float minY, float maxY)
    {
        float nx = Mathf.InverseLerp(minX, maxX, worldPos.x);
        float ny = Mathf.InverseLerp(minY, maxY, worldPos.y);
        // minimapRect는 보통 pivot이 (1,1) 또는 (0.5,0.5)일 수 있음 → anchored 기준으로 계산
        Vector2 size = minimapRect.rect.size;
        // pivot을 고려한 centered좌표로 변환
        Vector2 anchored = new Vector2((nx - 0.5f) * size.x, (ny - 0.5f) * size.y);
        return anchored;
    }

    Vector3 MinimapAnchoredToWorld(Vector2 anchored)
    {
        Vector2 size = minimapRect.rect.size;
        // anchored -> 0~1 정규화
        float nx = (anchored.x / size.x) + 0.5f;
        float ny = (anchored.y / size.y) + 0.5f;

        float minX, maxX, minY, maxY;
        if (worldBounds != null)
        {
            var b = worldBounds.bounds;
            minX = b.min.x; maxX = b.max.x; minY = b.min.y; maxY = b.max.y;
        }
        else
        {
            float s = minimapCamera.orthographicSize;
            minX = minimapCamera.transform.position.x - s * minimapCamera.aspect;
            maxX = minimapCamera.transform.position.x + s * minimapCamera.aspect;
            minY = minimapCamera.transform.position.y - s;
            maxY = minimapCamera.transform.position.y + s;
        }

        float wx = Mathf.Lerp(minX, maxX, nx);
        float wy = Mathf.Lerp(minY, maxY, ny);
        return new Vector3(wx, wy, 0f);
    }
}