using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ArduinoSerialConnection))]
public class DigitalCommandSender : MonoBehaviour
{
    [SerializeField] private Slider angleSlider;
    [SerializeField, Range(SerialProtocol.MinAngle, SerialProtocol.MaxAngle)] private int initialAngle = 90;
    [SerializeField, Min(0f)] private float minSendInterval = 0.05f;
    [SerializeField, Min(0f)] private float followLockDuration = 0.35f;

    private ArduinoSerialConnection connection;
    private bool hasPendingCommand;
    private int pendingAngle;
    private float lastSendTime = float.NegativeInfinity;
    private float followLockedUntil;

    private void Awake()
    {
        connection = GetComponent<ArduinoSerialConnection>();
        if (angleSlider == null)
        {
            Debug.LogError("DigitalCommandSender: falta asignar el slider.", this);
            enabled = false;
            return;
        }

        angleSlider.minValue = SerialProtocol.MinAngle;
        angleSlider.maxValue = SerialProtocol.MaxAngle;
        angleSlider.wholeNumbers = true;
        angleSlider.SetValueWithoutNotify(initialAngle);
        angleSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void Update()
    {
        if (hasPendingCommand && Time.unscaledTime - lastSendTime >= minSendInterval)
        {
            SendPendingCommand();
        }
    }

    private void OnDestroy()
    {
        if (angleSlider != null)
        {
            angleSlider.onValueChanged.RemoveListener(OnSliderChanged);
        }
    }

    public void ShowDeviceAngle(int angle)
    {
        if (!enabled || Time.unscaledTime < followLockedUntil)
        {
            return;
        }

        angleSlider.SetValueWithoutNotify(angle);
    }

    private void OnSliderChanged(float value)
    {
        pendingAngle = Mathf.RoundToInt(value);
        hasPendingCommand = true;
        followLockedUntil = Time.unscaledTime + followLockDuration;
    }

    private void SendPendingCommand()
    {
        hasPendingCommand = false;
        lastSendTime = Time.unscaledTime;
        connection.Write(SerialProtocol.BuildAngleCommand(pendingAngle));
    }
}
