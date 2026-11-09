using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;
using UnityEngine;

public class ArduinoSerialConnection : MonoBehaviour
{
    private const int ReadTimeoutMs = 100;
    private const int WriteTimeoutMs = 100;
    private const int IdleSleepMs = 2;
    private const int ThreadJoinTimeoutMs = 300;
    private const int ReadBufferSize = 256;
    private const int MaxMessageLength = 64;
    private const char CarriageReturn = '\r';

    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = SerialProtocol.BaudRate;
    [SerializeField] private bool connectOnStart = true;

    private readonly ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();
    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool isReading;
    private volatile string readError;

    public bool IsConnected => serialPort != null && serialPort.IsOpen;

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    private void Update()
    {
        if (readError == null)
        {
            return;
        }

        Debug.LogError("Conexión serial perdida: " + readError, this);
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect()
    {
        if (IsConnected)
        {
            return;
        }

        try
        {
            serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = ReadTimeoutMs,
                WriteTimeout = WriteTimeoutMs,
                DtrEnable = true
            };
            serialPort.Open();
            serialPort.DiscardInBuffer();
        }
        catch (Exception exception)
        {
            Debug.LogError($"No se pudo abrir {portName}: {exception.Message}", this);
            CloseSerialPort();
            return;
        }

        StartReading();
    }

    public void Disconnect()
    {
        isReading = false;
        if (readThread != null)
        {
            readThread.Join(ThreadJoinTimeoutMs);
            readThread = null;
        }

        CloseSerialPort();
        readError = null;
        ClearMessages();
    }

    public bool TryReadMessage(out string message)
    {
        return receivedMessages.TryDequeue(out message);
    }

    public void Write(string text)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            serialPort.Write(text);
        }
        catch (Exception exception)
        {
            Debug.LogError("Error al enviar por el puerto serial: " + exception.Message, this);
        }
    }

    private void StartReading()
    {
        readError = null;
        isReading = true;
        readThread = new Thread(ReadLoop) { IsBackground = true };
        readThread.Start();
    }

    private void ReadLoop()
    {
        StringBuilder currentMessage = new StringBuilder();
        byte[] buffer = new byte[ReadBufferSize];

        while (isReading)
        {
            try
            {
                int availableBytes = serialPort.BytesToRead;
                if (availableBytes <= 0)
                {
                    Thread.Sleep(IdleSleepMs);
                    continue;
                }

                int bytesRead = serialPort.Read(buffer, 0, Math.Min(availableBytes, buffer.Length));
                ProcessBytes(buffer, bytesRead, currentMessage);
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception exception)
            {
                readError = exception.Message;
                return;
            }
        }
    }

    private void ProcessBytes(byte[] buffer, int count, StringBuilder currentMessage)
    {
        for (int i = 0; i < count; i++)
        {
            char character = (char)buffer[i];
            if (character == SerialProtocol.LineTerminator)
            {
                EnqueueMessage(currentMessage);
            }
            else if (character != CarriageReturn && currentMessage.Length < MaxMessageLength)
            {
                currentMessage.Append(character);
            }
        }
    }

    private void EnqueueMessage(StringBuilder message)
    {
        if (message.Length == 0)
        {
            return;
        }

        receivedMessages.Enqueue(message.ToString());
        message.Clear();
    }

    private void CloseSerialPort()
    {
        if (serialPort == null)
        {
            return;
        }

        try
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Error al cerrar el puerto serial: " + exception.Message, this);
        }

        serialPort.Dispose();
        serialPort = null;
    }

    private void ClearMessages()
    {
        while (receivedMessages.TryDequeue(out _))
        {
        }
    }
}
