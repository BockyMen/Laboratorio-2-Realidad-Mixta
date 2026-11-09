using UnityEngine;

[RequireComponent(typeof(ArduinoSerialConnection))]
[RequireComponent(typeof(DigitalCommandSender))]
public class DeviceStateReceiver : MonoBehaviour
{
    [SerializeField] private PanelTiltController panelTilt;

    private ArduinoSerialConnection connection;
    private DigitalCommandSender commandSender;

    private void Awake()
    {
        connection = GetComponent<ArduinoSerialConnection>();
        commandSender = GetComponent<DigitalCommandSender>();
    }

    private void Start()
    {
        if (panelTilt == null)
        {
            Debug.LogError("DeviceStateReceiver: falta asignar el controlador de inclinación del panel.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        while (connection.TryReadMessage(out string message))
        {
            ApplyMessage(message);
        }
    }

    private void ApplyMessage(string message)
    {
        if (!SerialProtocol.TryParseState(message, out int angle, out bool fromPotentiometer))
        {
            return;
        }

        panelTilt.ApplyServoAngle(angle);
        if (fromPotentiometer)
        {
            commandSender.ShowDeviceAngle(angle);
        }
    }
}
