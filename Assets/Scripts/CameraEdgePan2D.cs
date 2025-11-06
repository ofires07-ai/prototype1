using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 새 입력 시스템을 쓰면 마우스 위치용
#endif

[RequireComponent(typeof(Camera))]
public class CameraEdgePan2D : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 12f;           // 카메라 이동 속도(유닛/초)
    [Tooltip("화면 가장자리에서 몇 픽셀 안으로 들어오면 이동을 시작할지")]
    public int edgeThickness = 16;          // px

    [Header("맵 경계")]
    [Tooltip("경계를 BoxCollider2D로 지정하면 그 Bounds를 자동 사용")]
    public BoxCollider2D worldBounds;       // 선택: 맵의 바운더리 콜라이더
    [Tooltip("Collider를 안 쓸 때 수동 크기 지정 (월드 단위)")]
    public Vector2 manualMapSize = new Vector2(100, 100); // 가로x세로(월드)

    Camera cam;
    Bounds mapBounds;
    bool hasBounds = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogWarning("[CameraEdgePan2D] 2D용이므로 Camera는 Orthographic 권장");
        }

        if (worldBounds != null)
        {
            mapBounds = worldBounds.bounds;
            hasBounds = true;
        }
        else
        {
            // manualMapSize 기준으로, 현재 transform.position을 중심으로 하는 Bounds 구성
            Vector3 center = transform.position;
            Vector3 size = new Vector3(Mathf.Max(0.01f, manualMapSize.x), Mathf.Max(0.01f, manualMapSize.y), 10f);
            mapBounds = new Bounds(center, size);
            hasBounds = true;
        }
    }

    void Update()
    {
        Vector2 dir = GetEdgeDirection();           // 가장자리 접촉 방향(대각선 포함)
        if (dir.sqrMagnitude > 0f)
        {
            Vector3 delta = (Vector3)(dir.normalized * moveSpeed * Time.deltaTime);
            transform.position += delta;
            if (hasBounds) ClampToBounds();
        }
    }

    Vector2 GetEdgeDirection()
    {
        // 마우스 위치
        Vector2 mpos;
#if ENABLE_INPUT_SYSTEM
        mpos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        mpos = Input.mousePosition;
#endif
        float w = Screen.width;
        float h = Screen.height;

        float dx = 0f, dy = 0f;

        if (mpos.x <= edgeThickness) dx = -1f;
        else if (mpos.x >= w - edgeThickness) dx = 1f;

        if (mpos.y <= edgeThickness) dy = -1f;
        else if (mpos.y >= h - edgeThickness) dy = 1f;

        return new Vector2(dx, dy); // 모서리면 (±1, ±1) → 대각선 이동
    }

    void ClampToBounds()
    {
        // 카메라 반폭/반높이 계산(orthographic)
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // 카메라 중심이 갈 수 있는 최소/최대
        float minX = mapBounds.min.x + halfW;
        float maxX = mapBounds.max.x - halfW;
        float minY = mapBounds.min.y + halfH;
        float maxY = mapBounds.max.y - halfH;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 에디터에서 경계 시각화
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Bounds b;
        if (worldBounds != null) b = worldBounds.bounds;
        else
        {
            Vector3 center = Application.isPlaying ? mapBounds.center : transform.position;
            Vector3 size = new Vector3(
                Mathf.Max(0.01f, manualMapSize.x),
                Mathf.Max(0.01f, manualMapSize.y),
                1f
            );
            b = new Bounds(center, size);
        }
        Gizmos.DrawCube(b.center, b.size);
    }
#endif
}