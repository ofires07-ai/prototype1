using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // 처음에는 완전 투명 (화면 안 가림)
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 화면을 서서히 검게 만든다. (0 -> 1)
    /// </summary>
    public IEnumerator FadeInCoroutine(float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            yield break;
        }

        canvasGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            float dt = Time.unscaledDeltaTime;
            dt = Mathf.Min(dt, 0.05f); // 한 프레임에 최대 0.05초만 진행
            t += dt;
            float normalized = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, normalized);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 화면을 서서히 밝게 만든다. (1 -> 0)
    /// </summary>
    public IEnumerator FadeOutCoroutine(float duration)
{
    // 🔴 여기서 강제로 화면을 완전 검게 맞추고 시작
    canvasGroup.alpha = 1f;

    if (duration <= 0f)
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        yield break;
    }

    float t = 0f;
    while (t < duration)
    {
        float dt = Time.unscaledDeltaTime;
        dt = Mathf.Min(dt, 0.05f); // 한 프레임에 최대 0.05초만 진행
        t += dt;
        float normalized = Mathf.Clamp01(t / duration);
        float a = Mathf.Lerp(1f, 0f, normalized); // 1 -> 0

        canvasGroup.alpha = a;
        yield return null;
    }

    canvasGroup.alpha = 0f;
    canvasGroup.blocksRaycasts = false;
}

}
