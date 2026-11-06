using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SerialManager : MonoBehaviour
{
    
    public int baudios = 9600;
    public bool autoConectar = false;
    public string puertoPorDefecto = "COM3";
    public hingedpanel panel;
    public Slider sliderDigital;
    public TMP_Dropdown dropdownPuertos;
    public Button botonConectar;
    public Button botonActualizar;
    public TMP_Text textoBotonConectar;
    public TMP_Text textoEstadoConexion;
    public TMP_Text textoAnguloServo;
    public TMP_Text textoFuente;
    public Color colorPot = new Color(0.30f, 0.85f, 0.40f);
    public Color colorDig = new Color(1.00f, 0.60f, 0.20f);
    public float intervaloEnvio = 0.05f;
    public float bloqueoSliderTrasEnvio = 0.35f;
    public bool conectado;
    public int anguloServo;
    public string fuente = "-";
 
    SerialPort puerto;
    Thread hilo;
    volatile bool leyendo;
    volatile string errorHilo;
    readonly ConcurrentQueue<string> cola = new ConcurrentQueue<string>();
 
    bool modoPruebaPrevio;
    bool actualizandoDesdeDispositivo;
    bool envioPendiente;
    int valorPendiente;
    float ultimoEnvio = -10f;
    float sliderBloqueadoHasta;
 
    const string SIN_PUERTOS = "(sin puertos)";
 
    // ------------------------------------------------------------------
    void Awake()
    {
        if (sliderDigital != null)
        {
            sliderDigital.minValue = 0f;
            sliderDigital.maxValue = 180f;
            sliderDigital.wholeNumbers = true;
            sliderDigital.SetValueWithoutNotify(90f);
            sliderDigital.onValueChanged.AddListener(AlCambiarSlider);
        }
        if (botonConectar != null) botonConectar.onClick.AddListener(AlternarConexion);
        if (botonActualizar != null) botonActualizar.onClick.AddListener(ActualizarPuertos);
    }
 
    void Start()
    {
        ActualizarPuertos();
        ActualizarUIConexion();
        ActualizarUIEstado();
        if (autoConectar) Conectar(PuertoSeleccionado());
    }
 
    void Update()
    {
        if (errorHilo != null)
        {
            string msg = errorHilo;
            errorHilo = null;
            Desconectar();
            Estado("Conexion perdida: " + msg);
        }

        string linea;
        while (cola.TryDequeue(out linea)) ProcesarLinea(linea);
 
        if (envioPendiente && Time.unscaledTime - ultimoEnvio >= intervaloEnvio)
            EnviarAngulo(valorPendiente);
    }
 
    public void ActualizarPuertos()
    {
        if (dropdownPuertos == null) return;
 
        string previo = PuertoSeleccionado();
        string[] nombres = SerialPort.GetPortNames();
        Array.Sort(nombres);
 
        dropdownPuertos.ClearOptions();
        if (nombres.Length == 0)
        {
            dropdownPuertos.AddOptions(new List<string> { SIN_PUERTOS });
        }
        else
        {
            dropdownPuertos.AddOptions(new List<string>(nombres));
            int idx = Array.IndexOf(nombres, previo);
            if (idx < 0) idx = Array.IndexOf(nombres, puertoPorDefecto);
            if (idx >= 0) dropdownPuertos.value = idx;
        }
        dropdownPuertos.RefreshShownValue();
    }
 
    string PuertoSeleccionado()
    {
        if (dropdownPuertos == null) return puertoPorDefecto;
        if (dropdownPuertos.options.Count == 0) return null;
        string t = dropdownPuertos.options[dropdownPuertos.value].text;
        return t == SIN_PUERTOS ? null : t;
    }
 
    public void AlternarConexion()
    {
        if (conectado) Desconectar();
        else Conectar(PuertoSeleccionado());
    }
 
    public void Conectar(string nombre)
    {
        if (conectado) return;
        if (string.IsNullOrEmpty(nombre))
        {
            Estado("Selecciona un puerto");
            return;
        }
 
        try
        {
            puerto = new SerialPort(nombre, baudios);
            puerto.NewLine = "\n";
            puerto.ReadTimeout = 100;
            puerto.WriteTimeout = 100;
            puerto.DtrEnable = true;
            puerto.Open();
            puerto.DiscardInBuffer();
        }
        catch (Exception e)
        {
            Estado("Error: " + e.Message);
            if (puerto != null) { puerto.Dispose(); puerto = null; }
            return;
        }
 
        errorHilo = null;
        leyendo = true;
        hilo = new Thread(BucleLectura) { IsBackground = true };
        hilo.Start();
 
        conectado = true;
        if (panel != null)
        {
            modoPruebaPrevio = panel.modoPrueba;
            panel.modoPrueba = false;   
        }
 
        Estado("Conectado a " + nombre + " (el Arduino tarda ~2 s en reiniciar)");
        ActualizarUIConexion();
    }
 
    public void Desconectar()
    {
        if (!conectado && puerto == null) return;
        bool eraConectado = conectado;
 
        leyendo = false;
        if (hilo != null) { hilo.Join(300); hilo = null; }
 
        if (puerto != null)
        {
            try { if (puerto.IsOpen) puerto.Close(); } catch (Exception) { }
            puerto.Dispose();
            puerto = null;
        }
        conectado = false;
 
        string basura;
        while (cola.TryDequeue(out basura)) { }
 
        if (eraConectado && panel != null) panel.modoPrueba = modoPruebaPrevio;
 
        Estado("Desconectado");
        ActualizarUIConexion();
    }
 
    void OnDestroy() { Desconectar(); }
    void OnApplicationQuit() { Desconectar(); }
 

    void BucleLectura()
    {
        StringBuilder sb = new StringBuilder();
        byte[] buf = new byte[256];
 
        while (leyendo)
        {
            try
            {
                int disponibles = puerto.BytesToRead;
                if (disponibles <= 0) { Thread.Sleep(2); continue; }
 
                int leidos = puerto.Read(buf, 0, Math.Min(disponibles, buf.Length));
                for (int i = 0; i < leidos; i++)
                {
                    char c = (char)buf[i];
                    if (c == '\n')
                    {
                        if (sb.Length > 0) cola.Enqueue(sb.ToString());
                        sb.Length = 0;
                    }
                    else if (c != '\r')
                    {
                        if (sb.Length < 64) sb.Append(c);
                    }
                }
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                errorHilo = e.Message;
                break;
            }
        }
    }
 
 
    void ProcesarLinea(string linea)
    {
        linea = linea.Trim();
        if (!linea.StartsWith("S:")) return;
 
        string[] partes = linea.Substring(2).Split(',');
        if (partes.Length != 2) return;
 
        int ang;
        if (!int.TryParse(partes[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ang)) return;
        string src = partes[1].Trim();
        if (src != "POT" && src != "DIG") return;
        if (ang < 0 || ang > 180) return;
 
        anguloServo = ang;
        fuente = src;

        if (panel != null) panel.AplicarAnguloServo(ang);
        if (sliderDigital != null && src == "POT" && Time.unscaledTime >= sliderBloqueadoHasta)
        {
            actualizandoDesdeDispositivo = true;
            sliderDigital.SetValueWithoutNotify(ang);
            actualizandoDesdeDispositivo = false;
        }
 
        ActualizarUIEstado();
    }
 
 
    void AlCambiarSlider(float v)
    {
        if (actualizandoDesdeDispositivo) return;   
 
        valorPendiente = Mathf.RoundToInt(v);
        envioPendiente = true;
        sliderBloqueadoHasta = Time.unscaledTime + bloqueoSliderTrasEnvio;
 
        if (Time.unscaledTime - ultimoEnvio >= intervaloEnvio)
            EnviarAngulo(valorPendiente);
    }
 
    void EnviarAngulo(int ang)
    {
        envioPendiente = false;
        ultimoEnvio = Time.unscaledTime;
        if (!conectado || puerto == null || !puerto.IsOpen) return;
 
        try
        {
            puerto.Write("A:" + ang.ToString(CultureInfo.InvariantCulture) + "\n");
        }
        catch (Exception e)
        {
            Estado("Error al enviar: " + e.Message);
        }
    }
 
    void Estado(string msg)
    {
        if (textoEstadoConexion != null) textoEstadoConexion.text = msg;
        Debug.Log("[SerialManager] " + msg);
    }
 
    void ActualizarUIConexion()
    {
        if (textoBotonConectar != null) textoBotonConectar.text = conectado ? "Desconectar" : "Conectar";
        if (dropdownPuertos != null) dropdownPuertos.interactable = !conectado;
        if (botonActualizar != null) botonActualizar.interactable = !conectado;
    }
 
    void ActualizarUIEstado()
    {
        if (textoAnguloServo != null)
            textoAnguloServo.text = "Servo: " + anguloServo + "°";
 
        if (textoFuente != null)
        {
            bool pot = fuente == "POT";
            textoFuente.text = fuente == "-" ? "Mando: -" : (pot ? "Mando: físico (POT)" : "Mando: digital (DIG)");
            textoFuente.color = pot ? colorPot : colorDig;
        }
    }
}