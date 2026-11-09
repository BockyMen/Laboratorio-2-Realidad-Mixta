using UnityEngine;

[RequireComponent(typeof(SunController))]
public class SunTimeInput : MonoBehaviour
{
    private const float SpeedUpFactor = 2f;
    private const float SlowDownFactor = 0.5f;

    [SerializeField] private KeyCode pauseKey = KeyCode.Space;
    [SerializeField] private KeyCode speedUpKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode slowDownKey = KeyCode.LeftArrow;

    private SunController sunController;

    private void Awake()
    {
        sunController = GetComponent<SunController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            sunController.TogglePause();
        }
        if (Input.GetKeyDown(speedUpKey))
        {
            sunController.ChangeTimeSpeed(SpeedUpFactor);
        }
        if (Input.GetKeyDown(slowDownKey))
        {
            sunController.ChangeTimeSpeed(SlowDownFactor);
        }
    }
}
