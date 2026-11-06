using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-2000)]
[DisallowMultipleComponent]
public class BlackScreenStartup : MonoBehaviour
{
    public CanvasGroup overlay;
    [Min(0.01f)] public float fadeDuration = 3f;
    [Min(0f)] public float holdSeconds = 0.2f;

    private void OnEnable()
    {
        if (overlay == null)
        {
            Debug.LogWarning("Assign the saved black CanvasGroup to BlackScreenStartup.", this);
            enabled = false;
            return;
        }
        overlay.gameObject.SetActive(true);
        overlay.alpha = 1f;
        StartCoroutine(FadeFromBlack());
    }

    private IEnumerator FadeFromBlack()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, holdSeconds));
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);
        while (elapsed < duration && overlay != null)
        {
            yield return null;
            if (overlay == null)
                yield break;
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
        }
        HideOverlay();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        HideOverlay();
    }

    private void HideOverlay()
    {
        if (overlay == null)
            return;
        overlay.alpha = 0f;
        overlay.gameObject.SetActive(false);
    }
}
