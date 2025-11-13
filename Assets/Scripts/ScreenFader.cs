using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // 처음엔 완전 투명(안 보이게)
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public IEnumerator FadeInCoroutine(float duration)
    {
        // 화면을 검게 (0 -> 1)
        canvasGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeOutCoroutine(float duration)
    {
        // 검은 화면에서 게임 화면으로 (1 -> 0)
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}

