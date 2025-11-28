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
            transform.rotation = mainCamera.transform.rotation;
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