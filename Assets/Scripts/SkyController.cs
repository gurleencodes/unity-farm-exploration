using UnityEngine;

[DisallowMultipleComponent]
public class SkyController : MonoBehaviour
{
    [Tooltip("Seconds to blend from blue sky to warm sky, then back again.")]
    [Min(1f)] public float transitionSeconds = 180f;
    public Color blueSky = new Color(0.5f, 0.65f, 0.85f);
    public Color warmSky = new Color(0.85f, 0.55f, 0.4f);
    [Tooltip("Used by skybox shaders that support rotation.")]
    public float rotationSpeed = 0.5f;

    private Material originalSky;
    private Material runtimeSky;
    private string tintProperty;
    private float elapsed;
    private float rotation;

    private void OnEnable()
    {
        originalSky = RenderSettings.skybox;
        if (originalSky == null)
        {
            Debug.LogWarning("SkyController needs a skybox in the scene's Lighting settings.", this);
            enabled = false;
            return;
        }

        // Change a runtime copy, never the shared skybox asset or scene lights.
        runtimeSky = new Material(originalSky) { name = originalSky.name + " (Sky Cycle)" };
        tintProperty = runtimeSky.HasProperty("_SkyTint") ? "_SkyTint"
            : runtimeSky.HasProperty("_Tint") ? "_Tint" : null;
        elapsed = 0f;
        rotation = runtimeSky.HasProperty("_Rotation") ? runtimeSky.GetFloat("_Rotation") : 0f;
        RenderSettings.skybox = runtimeSky;
    }

    private void Update()
    {
        if (runtimeSky == null)
            return;

        elapsed += Time.deltaTime;
        float blend = Mathf.SmoothStep(0f, 1f,
            Mathf.PingPong(elapsed / Mathf.Max(1f, transitionSeconds), 1f));
        if (tintProperty != null)
            runtimeSky.SetColor(tintProperty, Color.Lerp(blueSky, warmSky, blend));

        if (runtimeSky.HasProperty("_Rotation"))
        {
            rotation = Mathf.Repeat(rotation + rotationSpeed * Time.deltaTime, 360f);
            runtimeSky.SetFloat("_Rotation", rotation);
        }
    }

    private void OnDisable()
    {
        if (runtimeSky == null)
            return;
        if (RenderSettings.skybox == runtimeSky)
            RenderSettings.skybox = originalSky;
        Destroy(runtimeSky);
        runtimeSky = null;
    }
}
