using UnityEngine;
using System.Collections;

public class FadeInFadeOut : MonoBehaviour
{
    public float fadeDuration = 2.0f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("FadeInFadeOut needs a SpriteRenderer assigned. "
                + "The component has been disabled until a fade display is provided.", this);
            enabled = false;
            return;
        }

        StartCoroutine(Fade());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Fade()
    {
        bool fadingIn = true;
        while (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Color endColor = startColor;
            endColor.a = fadingIn ? 1f : 0f;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);

            while (elapsed < duration)
            {
                // Always yield, including when time is paused or duration is tiny.
                yield return null;
                if (spriteRenderer == null)
                {
                    enabled = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(startColor, endColor,
                    Mathf.Clamp01(elapsed / duration));
            }

            spriteRenderer.color = endColor;
            fadingIn = !fadingIn;
        }
    }
}
