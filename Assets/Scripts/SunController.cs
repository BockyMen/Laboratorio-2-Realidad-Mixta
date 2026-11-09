using UnityEngine;

public class SunController : MonoBehaviour
{
    private const float DefaultOrbitRadius = 20f;
    private const float MinimumInitialRadius = 1f;
    private const float MinTimeSpeed = 0.25f;
    private const float MaxTimeSpeed = 16f;

    [SerializeField] private Transform orbitCenter;
    [SerializeField, Min(0f)] private float orbitRadius;
    [SerializeField] private Vector3 eastWestAxis = Vector3.forward;
    [SerializeField] private float sunriseAngle = -90f;
    [SerializeField] private float sunsetAngle = 90f;
    [SerializeField] private float arcTiltDegrees;
    [SerializeField, Min(0.01f)] private float dayDurationSeconds = 60f;
    [SerializeField, Range(0f, 1f)] private float timeOfDay = 0.25f;
    [SerializeField, Min(0f)] private float timeSpeed = 1f;
    [SerializeField] private bool isPaused;

    public bool IsDaytime { get; private set; }

    private void Start()
    {
        if (orbitCenter == null || eastWestAxis == Vector3.zero)
        {
            Debug.LogError("SunController: falta asignar el centro de órbita o el eje este-oeste es cero.", this);
            enabled = false;
            return;
        }

        if (orbitRadius <= 0f)
        {
            float initialDistance = Vector3.Distance(transform.position, orbitCenter.position);
            orbitRadius = initialDistance >= MinimumInitialRadius ? initialDistance : DefaultOrbitRadius;
        }
    }

    private void Update()
    {
        if (!isPaused)
        {
            AdvanceTime();
        }
        UpdatePosition();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    public void ChangeTimeSpeed(float factor)
    {
        timeSpeed = Mathf.Clamp(timeSpeed * factor, MinTimeSpeed, MaxTimeSpeed);
    }

    private void AdvanceTime()
    {
        timeOfDay = (timeOfDay + Time.deltaTime * timeSpeed / dayDurationSeconds) % 1f;
    }

    private void UpdatePosition()
    {
        Vector3 directionFromCenter = CalculateArcDirection();
        transform.position = orbitCenter.position + directionFromCenter * orbitRadius;
        transform.LookAt(orbitCenter);
        IsDaytime = directionFromCenter.y > 0f;
    }

    private Vector3 CalculateArcDirection()
    {
        float angleRadians = Mathf.Lerp(sunriseAngle, sunsetAngle, timeOfDay) * Mathf.Deg2Rad;
        Vector3 axis = eastWestAxis.normalized;
        Vector3 direction = axis * Mathf.Sin(angleRadians) + Vector3.up * Mathf.Cos(angleRadians);
        return Quaternion.AngleAxis(arcTiltDegrees, axis) * direction;
    }
}
