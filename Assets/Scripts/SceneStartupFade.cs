using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class SceneStartupFade : MonoBehaviour
{
    [Tooltip("Seconds to reveal the scene from the starting color.")]
    [Min(0.01f)] public float fadeDuration = 3f;
    [Tooltip("Briefly hold the starting color while the scene settles.")]
    [Min(0f)] public float startDelay = 0.2f;
    public Color fadeColor = Color.black;

    private GameObject overlay;
    private CanvasGroup visibility;

    private void OnEnable()
    {
        // Build before the first rendered frame, above the game's other UI.
        overlay = new GameObject("Scene Startup Fade Overlay", typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        visibility = overlay.GetComponent<CanvasGroup>();
        visibility.alpha = 1f;
        visibility.interactable = false;
        visibility.blocksRaycasts = false;

        GameObject fill = new GameObject("Full Screen Fade", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(overlay.transform, false);
        RectTransform rect = fill.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = fill.GetComponent<Image>();
        Color opaqueColor = fadeColor;
        opaqueColor.a = 1f;
        image.color = opaqueColor;
        image.raycastTarget = false;

        StartCoroutine(RevealScene());
    }

    private IEnumerator RevealScene()
    {
        // Unscaled time also works if the game starts paused.
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, startDelay));
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            visibility.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
        }
        RemoveOverlay();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RemoveOverlay();
    }

    private void RemoveOverlay()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
            Destroy(overlay);
            overlay = null;
            visibility = null;
        }
    }
}
