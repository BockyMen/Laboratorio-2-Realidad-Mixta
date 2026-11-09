using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
public class PanelTiltController : MonoBehaviour
{
    [SerializeField] private float servoMinAngle = SerialProtocol.MinAngle;
    [SerializeField] private float servoMaxAngle = SerialProtocol.MaxAngle;
    [SerializeField] private float panelMinTilt = -60f;
    [SerializeField] private float panelMaxTilt = 60f;
    [SerializeField, Min(0f)] private float springForce = 300f;
    [SerializeField, Min(0f)] private float damper = 30f;

    private HingeJoint hinge;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        ConfigureHinge();
    }

    public void ApplyServoAngle(float servoAngle)
    {
        float clampedAngle = Mathf.Clamp(servoAngle, servoMinAngle, servoMaxAngle);
        float servoProgress = Mathf.InverseLerp(servoMinAngle, servoMaxAngle, clampedAngle);

        JointSpring spring = hinge.spring;
        spring.targetPosition = Mathf.Lerp(panelMinTilt, panelMaxTilt, servoProgress);
        hinge.spring = spring;
    }

    private void ConfigureHinge()
    {
        hinge.useMotor = false;
        hinge.useSpring = true;
        hinge.spring = new JointSpring { spring = springForce, damper = damper, targetPosition = 0f };
        hinge.useLimits = true;
        hinge.limits = new JointLimits
        {
            min = Mathf.Min(panelMinTilt, panelMaxTilt),
            max = Mathf.Max(panelMinTilt, panelMaxTilt)
        };
    }
}
