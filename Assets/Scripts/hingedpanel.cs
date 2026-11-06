using UnityEngine;

public class hingedpanel : MonoBehaviour
{

    public float servoMin = 0f;
    public float servoMax = 180f;
    public float panelMin = -60f;
    public float panelMax = 60f;
    public float resorte = 300f;
    public float amortiguacion = 30f;
    public bool modoPrueba = true;
    public float anguloServoPrueba = 90f;
    public float panelObjetivo;
 
    HingeJoint hinge;
 
    void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        ConfigurarBisagra();
    }
 
    void ConfigurarBisagra()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
        }
 
        hinge.useMotor = false;
 
        hinge.useSpring = true;
        JointSpring s = hinge.spring;
        s.spring = resorte;
        s.damper = amortiguacion;
        s.targetPosition = 0f;
        hinge.spring = s;
 
        hinge.useLimits = true;
        JointLimits l = hinge.limits;
        l.min = Mathf.Min(panelMin, panelMax);
        l.max = Mathf.Max(panelMin, panelMax);
        hinge.limits = l;
    }
 
    void Update()
    {
        if (modoPrueba) AplicarAnguloServo(anguloServoPrueba);
    }

    public void AplicarAnguloServo(float anguloServo)
    {
        float a = Mathf.Clamp(anguloServo, servoMin, servoMax);
        float t = Mathf.InverseLerp(servoMin, servoMax, a);
        panelObjetivo = Mathf.Lerp(panelMin, panelMax, t);
 
        JointSpring s = hinge.spring;   
        s.targetPosition = panelObjetivo;
        hinge.spring = s;
    }
}