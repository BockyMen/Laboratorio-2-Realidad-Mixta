using UnityEngine;

public class SunlightSensor : MonoBehaviour
{
    private const float RaycastExtraDistance = 1f;
    private const float MinimumDistance = 0.0001f;

    [SerializeField] private SunController sun;
    [SerializeField] private Vector3 panelNormalLocal = Vector3.up;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField, Min(0f)] private float smoothingSpeed = 6f;
    [SerializeField, Min(0f)] private float maxPowerWatts = 400f;

    private Collider panelCollider;
    private bool isLit;

    public float IncidenceFactor { get; private set; }
    public float PowerWatts => IncidenceFactor * maxPowerWatts;
    public Vector3 RayEnd { get; private set; }
    public Vector3 SunPosition => sun.transform.position;
    public bool IsSunUp => sun.IsDaytime;

    private void Start()
    {
        panelCollider = GetComponent<Collider>();
        if (sun == null || panelCollider == null)
        {
            Debug.LogError("SunlightSensor: falta asignar el sol o el panel no tiene collider.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        Vector3 directionToPanel = panelCollider.bounds.center - sun.transform.position;
        float distance = directionToPanel.magnitude;
        if (distance < MinimumDistance)
        {
            return;
        }
        directionToPanel /= distance;

        UpdateIllumination(directionToPanel, distance);
        UpdateIncidenceFactor(directionToPanel);
    }

    private void UpdateIllumination(Vector3 directionToPanel, float distance)
    {
        RayEnd = panelCollider.bounds.center;
        isLit = false;
        if (!sun.IsDaytime)
        {
            return;
        }

        if (Physics.Raycast(sun.transform.position, directionToPanel, out RaycastHit hit,
                distance + RaycastExtraDistance, raycastLayers, QueryTriggerInteraction.Collide))
        {
            RayEnd = hit.point;
            isLit = hit.collider == panelCollider;
        }
    }

    private void UpdateIncidenceFactor(Vector3 directionToPanel)
    {
        Vector3 panelNormal = transform.TransformDirection(panelNormalLocal).normalized;
        float incidenceCosine = Vector3.Dot(panelNormal, -directionToPanel);
        float targetFactor = isLit ? Mathf.Max(0f, incidenceCosine) : 0f;
        IncidenceFactor = Mathf.Lerp(IncidenceFactor, targetFactor, Time.deltaTime * smoothingSpeed);
    }
}
