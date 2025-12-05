using UnityEngine;

public class HY_SmoothHealthBar : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("실제로 줄어들 빨간색 바(Bar)의 트랜스폼")]
    [SerializeField] private Transform barTransform;

    [Tooltip("체력이 줄어드는 속도 (높을수록 빠름, 5~10 추천)")]
    [SerializeField] private float smoothSpeed = 10f;

    [Header("빌보드 설정 (카메라 보기)")]
    [Tooltip("체력바가 항상 카메라를 바라보게 할지 여부")]
    [SerializeField] private bool lookAtCamera = true;

    // 내부 변수
    private float targetScaleX = 1f; // 목표 크기 (최종적으로 되어야 할 크기)
    private float currentScaleX = 1f; // 현재 크기 (눈에 보이는 크기)
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        
        // 시작할 때는 꽉 찬 상태
        targetScaleX = 1f;
        currentScaleX = 1f;
    }

    void Update()
    {
        // 1. 부드러운 움직임 계산 (Mathf.Lerp)
        // 현재값(current)을 목표값(target) 쪽으로 매 프레임 조금씩 이동시킵니다.
        // Time.deltaTime * smoothSpeed는 이동하는 보폭을 의미합니다.
        currentScaleX = Mathf.Lerp(currentScaleX, targetScaleX, Time.deltaTime * smoothSpeed);

        // 2. 실제로 크기 적용
        // Pivot이 왼쪽(0)에 있다면 오른쪽에서 왼쪽으로 스르륵 줄어듭니다.
        if (barTransform != null)
        {
            barTransform.localScale = new Vector3(currentScaleX, 1f, 1f);
        }
    }

    void LateUpdate()
    {
        // 3. 빌보드 처리 (체력바가 항상 카메라 정면을 보게 함)
        if (lookAtCamera && mainCamera != null)
        {
            // (1) 월드 공간에서 카메라의 회전을 직접 따라가는 방식 (기존 코드)
            // transform.rotation = mainCamera.transform.rotation;

            // 🌟 (2) 개선된 방식: 카메라를 향해 자신의 Z축을 돌리는 방식 (LookAt)
            // LookAt은 해당 오브젝트의 Z축(파란색 축)이 대상(카메라)을 바라보게 합니다.
            // 하지만 3D 게임에서 HP바는 X-Z 평면에 수평인 경우가 많으므로,
            // X-Z 평면 회전은 막고 Y축 회전만 사용하는 'Y축 고정 빌보드'를 더 많이 사용합니다.

            // 🌟 (3) 가장 안정적인 방식: Y축 회전만 복사하기
            // HP바는 보통 X-Z 평면에 수평하게 놓여 있어 위아래로 기울어질 필요 없이,
            // 좌우 회전(Y축 회전)만 카메라와 동일하게 맞추면 됩니다.
            // 이렇게 하면 HP바가 위아래로 기울어져서 이상하게 보이는 현상을 막을 수 있습니다.
            
            // 💡 팁: 3D 오브젝트인 HP바가 카메라와 '평행'하게 보이게 하려면 카메라 회전 자체를 사용합니다.
            // 기존 코드는 이미 이 방식이므로, 해당 코드가 다른 문제(예: 부모 오브젝트 회전)를 일으키지 않는다면, 
            // 현재 코드의 'transform.rotation = mainCamera.transform.rotation;' 방식도 올바릅니다.

            // 만약 캐릭터가 회전할 때 HP바가 캐릭터와 함께 회전하면서 문제가 발생한다면,
            // HP바의 부모는 캐릭터의 회전을 따르지 않는 '빈 오브젝트'로 두고,
            // HP바 오브젝트만 그 '빈 오브젝트'의 자식으로 두는 계층 구조를 사용해야 합니다.

            // ➡️ 최종적으로, 현재 코드는 올바른 빌보드 로직을 가지고 있으므로,
            // '왼쪽으로 이동 시 보이지 않음'은 **시야각 이탈**이나 **부모 오브젝트 회전**으로 인한 경우가 큽니다.
            // 하지만 만약의 경우를 대비하여 가장 강력한 Y축 고정 빌보드를 사용해 봅시다.
            
            Quaternion camRotation = mainCamera.transform.rotation;
            
            // X축과 Z축 회전은 무시하고, Y축 회전(좌우 회전)만 카메라와 맞춥니다.
            transform.rotation = Quaternion.Euler(0f, camRotation.eulerAngles.y, 0f);
        }
    }

    /// <summary>
    /// 외부(적 유닛)에서 데미지를 입었을 때 호출하는 함수
    /// 여기서는 '목표치'만 설정하고, 실제 움직임은 Update에서 처리합니다.
    /// </summary>
    public void SetHealth(float currentHp, float maxHp)
    {
        // 비율 계산 (0.0 ~ 1.0)
        float hpRatio = currentHp / maxHp;

        // 안전 장치: 0보다 작거나 1보다 커지지 않게 고정
        hpRatio = Mathf.Clamp01(hpRatio);

        // "목표"만 수정합니다. (실제 크기는 Update에서 Lerp가 따라감)
        targetScaleX = hpRatio;
    }
}