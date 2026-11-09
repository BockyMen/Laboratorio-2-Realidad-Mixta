using TMPro;
using UnityEngine;

[RequireComponent(typeof(SunlightSensor))]
[RequireComponent(typeof(HingeJoint))]
public class SolarPanelDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text panelAngleText;

    private SunlightSensor sunlightSensor;
    private HingeJoint panelHinge;

    private void Awake()
    {
        sunlightSensor = GetComponent<SunlightSensor>();
        panelHinge = GetComponent<HingeJoint>();
    }

    private void Start()
    {
        if (powerText == null || panelAngleText == null)
        {
            Debug.LogError("SolarPanelDisplay: faltan los textos de potencia o de ángulo del panel.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        powerText.text = $"Potencia: {sunlightSensor.PowerWatts:F0} W";
        panelAngleText.text = $"Ángulo del panel: {panelHinge.angle:F1}°";
    }
}
