using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SunlightSensor))]
[RequireComponent(typeof(LineRenderer))]
public class SunlightVisualFeedback : MonoBehaviour
{
    private const string RayShaderName = "Sprites/Default";
    private const int RayPointCount = 2;

    [SerializeField] private Color rayColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField, Min(0f)] private float maxRayWidth = 0.15f;
    [SerializeField, Range(0f, 1f)] private float minRayAlpha = 0.15f;
    [SerializeField, Range(0f, 1f)] private float minRayWidthFactor = 0.3f;
    [SerializeField] private Light modulatedLight;
    [SerializeField, Range(0f, 1f)] private float minimumIntensityFactor = 0.05f;

    private SunlightSensor sunlightSensor;
    private LineRenderer rayRenderer;
    private float baseLightIntensity;

    private void Awake()
    {
        sunlightSensor = GetComponent<SunlightSensor>();
        rayRenderer = GetComponent<LineRenderer>();
        ConfigureRayRenderer();
    }

    private void Start()
    {
        if (modulatedLight == null)
        {
            Debug.LogError("SunlightVisualFeedback: falta asignar la luz a modular.", this);
            enabled = false;
            return;
        }
        baseLightIntensity = modulatedLight.intensity;
    }

    private void LateUpdate()
    {
        UpdateRay();
        UpdateLightIntensity();
    }

    private void ConfigureRayRenderer()
    {
        rayRenderer.positionCount = RayPointCount;
        rayRenderer.useWorldSpace = true;
        rayRenderer.material = new Material(Shader.Find(RayShaderName));
        rayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        rayRenderer.receiveShadows = false;
    }

    private void UpdateRay()
    {
        rayRenderer.enabled = sunlightSensor.enabled && sunlightSensor.IsSunUp;
        if (!rayRenderer.enabled)
        {
            return;
        }

        rayRenderer.SetPosition(0, sunlightSensor.SunPosition);
        rayRenderer.SetPosition(1, sunlightSensor.RayEnd);
        ApplyRayAppearance(sunlightSensor.IncidenceFactor);
    }

    private void ApplyRayAppearance(float incidenceFactor)
    {
        Color color = rayColor;
        color.a = Mathf.Lerp(minRayAlpha, 1f, incidenceFactor);
        rayRenderer.startColor = color;
        rayRenderer.endColor = color;

        float width = maxRayWidth * Mathf.Lerp(minRayWidthFactor, 1f, incidenceFactor);
        rayRenderer.startWidth = width;
        rayRenderer.endWidth = width;
    }

    private void UpdateLightIntensity()
    {
        float intensityFactor = Mathf.Lerp(minimumIntensityFactor, 1f, sunlightSensor.IncidenceFactor);
        modulatedLight.intensity = baseLightIntensity * intensityFactor;
    }
}
