using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class CameraEdgePan2D : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 12f;
    public int edgeThickness = 16; // px

    [Header("줌")]
    public float zoomStep = 2f;      // 휠 한 칸 당 사이즈 변화량
    public float minOrtho = 4f;      // 최소 orthographicSize
    public float maxOrtho = 30f;     // 절대 상한(보호용)
    public bool zoomToCursor = true; // 커서 기준 줌

    [Tooltip("ON: 양쪽(위+아래 또는 좌+우)이 동시에 닿을 때에만 줌아웃 멈춤\nOFF: 한쪽이라도 닿으면 멈춤(기존 방식)")]
    public bool limitWhenBothSidesTouch = true;

    [Header("맵 경계")]
    public BoxCollider2D worldBounds;
    public Vector2 manualMapSize = new Vector2(100, 100); // worldBounds 없을 때 수동 지정

    private Camera cam;
    private Bounds mapBounds;
    private bool hasBounds;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic) cam.orthographic = true;

        if (worldBounds != null)
        {
            mapBounds = worldBounds.bounds;
            hasBounds = true;
        }
        else
        {
            Vector3 center = transform.position;
            Vector3 size = new Vector3(Mathf.Max(0.01f, manualMapSize.x), Mathf.Max(0.01f, manualMapSize.y), 10f);
            mapBounds = new Bounds(center, size);
            hasBounds = true;
        }
    }

    void Update()
    {
        // 1) 에지 팬
        Vector2 dir = GetEdgeDirection();
        if (dir.sqrMagnitude > 0f)
        {
            transform.position += (Vector3)(dir.normalized * moveSpeed * Time.deltaTime);
            if (hasBounds)
            {
                ClampToBounds();

                // 이동 후 현재 위치에서 허용되는 최대치보다 크면 보정 (둘 다 닿음/한쪽 닿음 모드에 따라 계산)
                float dynMax = GetAllowedMaxOrtho();
                if (cam.orthographicSize > dynMax)
                    cam.orthographicSize = dynMax;
            }
        }

        // 2) 휠 줌
        float scroll = ReadScroll();

        if (Mathf.Abs(scroll) > 0.001f)
        {
            Zoom(scroll);
        }
    }

    Vector2 GetEdgeDirection()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 mpos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        Vector2 mpos = Input.mousePosition;
#endif
        float w = Screen.width;
        float h = Screen.height;
        float dx = 0f, dy = 0f;

        if (mpos.x <= edgeThickness) dx = -1f;
        else if (mpos.x >= w - edgeThickness) dx = 1f;

        if (mpos.y <= edgeThickness) dy = -1f;
        else if (mpos.y >= h - edgeThickness) dy = 1f;

        return new Vector2(dx, dy);
    }

    float ReadScroll()
    {
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        // 새 Input System 먼저
        if (Mouse.current != null)
        {
            // 보통 -120, 120 단위로 들어옴
            float newSysScroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(newSysScroll) > 0.01f)
            {
                scroll = newSysScroll / 120f;
            }
        }
#endif

        // 새 Input System에서 못 읽었으면, 옛 Input Manager도 한 번 더 체크
        if (Mathf.Abs(scroll) < 0.0001f)
        {
            scroll = Input.mouseScrollDelta.y; // 에디터/빌드 둘 다에서 휠 지원
        }

        return scroll;
    }

    void Zoom(float scroll)
    {
        // 줌 전 커서 월드좌표
        Vector3 before = Vector3.zero;
        if (zoomToCursor)
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 mpos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
            Vector2 mpos = Input.mousePosition;
#endif
            before = cam.ScreenToWorldPoint(new Vector3(mpos.x, mpos.y, -cam.transform.position.z));
        }

        // 사용자 목표
        float desired = cam.orthographicSize - scroll * zoomStep;

        // 허용 최대(모드에 따라 계산)
        float allowedMax = GetAllowedMaxOrtho();

        // 최종 반영
        float clamped = Mathf.Clamp(desired, minOrtho, Mathf.Min(maxOrtho, allowedMax));
        cam.orthographicSize = clamped;

        // 커서 기준 줌 보정
        if (zoomToCursor)
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 mpos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
            Vector2 mpos = Input.mousePosition;
#endif
            Vector3 after = cam.ScreenToWorldPoint(new Vector3(mpos.x, mpos.y, -cam.transform.position.z));
            Vector3 delta = before - after;
            transform.position += new Vector3(delta.x, delta.y, 0f);
        }

        if (hasBounds) ClampToBounds();

        // 보정 후에도 초과가 있으면 한번 더 제한
        if (hasBounds)
        {
            float allowedMax2 = GetAllowedMaxOrtho();
            cam.orthographicSize = Mathf.Min(cam.orthographicSize, allowedMax2);
            ClampToBounds();
        }
    }

    /// <summary>
    /// 현재 설정에서 허용되는 최대 orthographicSize 계산
    /// - limitWhenBothSidesTouch=true: 맵 전체 크기 기준(글로벌) 제한 → 양쪽이 동시에 닿을 때 멈춤
    /// - false: 현 위치 기준(로컬) 제한 → 한쪽이라도 닿으면 멈춤(이전 방식)
    /// </summary>
    float GetAllowedMaxOrtho()
    {
        if (!hasBounds) return maxOrtho;

        if (limitWhenBothSidesTouch)
        {
            // 전역 제한: 맵 전체를 화각에 넣을 때까지 허용 (양쪽 동시 접촉 시 멈춤)
            float fullHalfH = mapBounds.extents.y;          // 지도 세로 절반
            float fullHalfW = mapBounds.extents.x;          // 지도 가로 절반
            float byHeight  = fullHalfH;
            float byWidth   = fullHalfW / cam.aspect;
            float globalMax = Mathf.Min(byHeight, byWidth);

            // 너무 딱 붙는 경계 떨림 방지용 epsilon
            const float eps = 0.0001f;
            return Mathf.Clamp(globalMax - eps, minOrtho, maxOrtho);
        }
        else
        {
            // 위치 기반(이전 방식): 한쪽이라도 닿으면 멈춤
            float camX = transform.position.x;
            float camY = transform.position.y;

            float minX = mapBounds.min.x;
            float maxX = mapBounds.max.x;
            float minY = mapBounds.min.y;
            float maxY = mapBounds.max.y;

            float roomLeft   = camX - minX;
            float roomRight  = maxX - camX;
            float roomDown   = camY - minY;
            float roomUp     = maxY - camY;

            float maxHalfH = Mathf.Min(roomDown, roomUp);
            float maxHalfW = Mathf.Min(roomLeft, roomRight);

            float byHeight = maxHalfH;
            float byWidth  = maxHalfW / cam.aspect;

            const float eps = 0.0001f;
            float dynMax = Mathf.Min(byHeight, byWidth) - eps;
            return Mathf.Clamp(dynMax, minOrtho, maxOrtho);
        }
    }

    void ClampToBounds()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

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
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Bounds b;
        if (worldBounds != null) b = worldBounds.bounds;
        else
        {
            Vector3 center = Application.isPlaying ? mapBounds.center : transform.position;
            Vector3 size = new Vector3(Mathf.Max(0.01f, manualMapSize.x), Mathf.Max(0.01f, manualMapSize.y), 1f);
            b = new Bounds(center, size);
        }
        Gizmos.DrawCube(b.center, b.size);
    }
#endif
}
