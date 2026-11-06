using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class PanelSolar : MonoBehaviour
{
    
    public SunController sol;
    public Light luzModulada;
    public HingeJoint bisagra;
    public Vector3 normalLocal = Vector3.up;
    public float potenciaMax = 400f; // W
    public float intensidadMinima = 0.05f;
    public float suavizado = 6f;
    public LayerMask capasRaycast = ~0;
    public bool mostrarRayo = true;
    public Color colorRayo = new Color(1f, 0.9f, 0.3f, 1f);
    public float anchoRayo = 0.15f;

    
    public TMP_Text textoPotencia;
    public TMP_Text textoIncidencia;
    public TMP_Text textoAnguloPanel;
    public TMP_Text textoEstado;
    public Image barraPotencia;
    public float anguloIncidencia;
    public float factorIncidencia;
    public float potenciaW;
    public float anguloPanel;
    public bool iluminado;

    Collider colPanel;
    LineRenderer rayo;
    float intensidadBase = 1f;

    void Awake()
    {
        if (bisagra == null) bisagra = GetComponent<HingeJoint>();
        CrearColliderSiHaceFalta();
        CrearRayo();
    }

    void Start()
    {
        if (luzModulada != null) intensidadBase = luzModulada.intensity;
        if (sol == null) Debug.LogWarning("PanelSolar: falta asignar el Sol (SolController).");
    }

    void CrearColliderSiHaceFalta()
    {
        colPanel = GetComponent<Collider>();
        if (colPanel != null) return;

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        bool hay = false;
        Bounds b = new Bounds();

        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            Vector3 c = mb.center, e = mb.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = transform.InverseTransformPoint(mf.transform.TransformPoint(esquina));
                if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; }
                else b.Encapsulate(p);
            }
        }

        if (hay) { box.center = b.center; box.size = b.size; }
        box.isTrigger = true;
        colPanel = box;
    }

    void CrearRayo()
    {
        rayo = GetComponent<LineRenderer>();
        if (rayo == null) rayo = gameObject.AddComponent<LineRenderer>();
        rayo.positionCount = 2;
        rayo.useWorldSpace = true;
        rayo.material = new Material(Shader.Find("Sprites/Default"));
        rayo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rayo.receiveShadows = false;
    }

    void LateUpdate()
    {
        if (sol == null || colPanel == null) return;

    
        Vector3 origen = sol.transform.position;
        Vector3 centro = colPanel.bounds.center;
        Vector3 dir = centro - origen;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;
        dir /= dist;

        Vector3 finRayo = centro;
        iluminado = false;

        if (sol.esDeDia &&
            Physics.Raycast(origen, dir, out RaycastHit hit, dist + 1f, capasRaycast, QueryTriggerInteraction.Collide))
        {
            finRayo = hit.point;
            iluminado = (hit.collider == colPanel);  
        }

        Vector3 normal = transform.TransformDirection(normalLocal).normalized;
        float cos = Vector3.Dot(normal, -dir);
        anguloIncidencia = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;

        float objetivo = (iluminado && cos > 0f) ? cos : 0f;  
        factorIncidencia = Mathf.Lerp(factorIncidencia, objetivo, Time.deltaTime * suavizado);
        potenciaW = factorIncidencia * potenciaMax;
        anguloPanel = bisagra != null ? bisagra.angle : 0f;

        if (luzModulada != null)
            luzModulada.intensity = intensidadBase * Mathf.Lerp(intensidadMinima, 1f, factorIncidencia);

        ActualizarRayo(origen, finRayo);
        ActualizarUI();
    }

    void ActualizarRayo(Vector3 origen, Vector3 fin)
    {
        rayo.enabled = mostrarRayo && sol.esDeDia;
        if (!rayo.enabled) return;

        rayo.SetPosition(0, origen);
        rayo.SetPosition(1, fin);

        Color c = colorRayo;
        c.a = Mathf.Lerp(0.15f, 1f, factorIncidencia);
        rayo.startColor = rayo.endColor = c;
        rayo.startWidth = rayo.endWidth = anchoRayo * Mathf.Lerp(0.3f, 1f, factorIncidencia);
    }

    void ActualizarUI()
    {
        if (textoPotencia != null) textoPotencia.text = potenciaW.ToString("F0") + " W";
        if (textoIncidencia != null) textoIncidencia.text = "Incidencia: " + anguloIncidencia.ToString("F1") + "°";
        if (textoAnguloPanel != null) textoAnguloPanel.text = "Panel: " + anguloPanel.ToString("F1") + "°";
        if (barraPotencia != null) barraPotencia.fillAmount = potenciaMax > 0f ? potenciaW / potenciaMax : 0f;
        if (textoEstado != null)
            textoEstado.text = !sol.esDeDia ? "Noche" : (iluminado ? "Iluminado" : "Sombra / sin incidencia");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 p = colPanel != null ? colPanel.bounds.center : transform.position;
        Gizmos.DrawLine(p, p + transform.TransformDirection(normalLocal).normalized * 5f); // normal del panel
    }

    void OnValidate()
{
    if (bisagra == null) bisagra = GetComponent<HingeJoint>();
}
}