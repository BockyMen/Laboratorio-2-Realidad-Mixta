using UnityEngine;

public class SunController : MonoBehaviour
{

    public Transform objetivo;
    public float radio = 0f;
    public Vector3 ejeEsteOeste = new Vector3(0f, 0f, 1f);
    public float anguloAmanecer = -90f;
    public float anguloAtardecer = 90f;
    public float duracionDia = 60f;
    public float inclinacionArco = 0f;

    
    public bool pausado = false;
    public float horaDelDia = 0.25f;
    public bool repetirDia = true;
    public float velocidadTiempo = 1f;
    public KeyCode teclaPausa = KeyCode.Space;
    public KeyCode teclaAcelerar = KeyCode.RightArrow;
    public KeyCode teclaFrenar = KeyCode.LeftArrow;

    public float elevacionGrados;
    public bool esDeDia;

    Vector3 Centro => objetivo != null ? objetivo.position : Vector3.zero;

    void Start()
    {
        if (objetivo == null)
            Debug.LogWarning("SolController: falta asignar el Objetivo (arrastra el Panel). El sol orbitara el origen del mundo.");

        if (radio <= 0f)
        {
            radio = Vector3.Distance(transform.position, Centro);
            if (radio < 1f) radio = 20f;
        }
    }

    void Update()
    {
        LeerTeclas();

        if (!pausado)
        {
            horaDelDia += (Time.deltaTime / duracionDia) * velocidadTiempo;
            if (horaDelDia > 1f)
                horaDelDia = repetirDia ? 0f : 1f;
        }

        ActualizarPosicion();
    }

    void LeerTeclas()
    {
        if (Input.GetKeyDown(teclaPausa)) pausado = !pausado;
        if (Input.GetKeyDown(teclaAcelerar)) velocidadTiempo = Mathf.Min(velocidadTiempo * 2f, 16f);
        if (Input.GetKeyDown(teclaFrenar)) velocidadTiempo = Mathf.Max(velocidadTiempo * 0.5f, 0.25f);
    }

    Vector3 DireccionEnArco(float t)
    {
        float ang = Mathf.Lerp(anguloAmanecer, anguloAtardecer, t) * Mathf.Deg2Rad;
        Vector3 eje = ejeEsteOeste.sqrMagnitude > 0.0001f ? ejeEsteOeste.normalized : Vector3.right;
        Vector3 dir = eje * Mathf.Sin(ang) + Vector3.up * Mathf.Cos(ang);
    
        return Quaternion.AngleAxis(inclinacionArco, eje) * dir;
    }

    void ActualizarPosicion()
    {
        Vector3 dir = DireccionEnArco(horaDelDia);

        
        transform.position = Centro + dir * radio;
        transform.LookAt(Centro); // ademas, apunta hacia el panel

        elevacionGrados = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        esDeDia = elevacionGrados > 0f;
    }


    public Vector3 DireccionHaciaSol()
    {
        return (transform.position - Centro).normalized;
    }

    public void FijarHora(float valor) { horaDelDia = Mathf.Clamp01(valor); }
    public void AlternarPausa() { pausado = !pausado; }

    void OnDrawGizmosSelected()
    {
        float r = radio > 0f ? radio : 20f;
        Vector3 c = Centro;
        Vector3 previo = Vector3.zero;
        for (int i = 0; i <= 40; i++)
        {
            Vector3 p = c + DireccionEnArco(i / 40f) * r;
            if (i > 0) Gizmos.DrawLine(previo, p);
            previo = p;
        }
    }
}