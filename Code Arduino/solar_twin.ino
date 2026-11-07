
#include <Servo.h>

// ---------- Pines ----------
const uint8_t PIN_SERVO = 6;
const uint8_t PIN_POT   = A0;

// ---------- Rango del servo (grados) ----------
const int ANG_MIN = 0;
const int ANG_MAX = 180;

// ---------- Periodos (ms) ----------
const unsigned long T_POT     = 10;   // lectura y filtrado del potenciometro
const unsigned long T_MOVER   = 15;   // paso del servo
const unsigned long T_REPORTE = 50;   // reporte S: (20 Hz)

// ---------- Movimiento progresivo ----------
const float VEL_MAX = 60.0;                        // grados por segundo (configurable)
const float PASO    = VEL_MAX * T_MOVER / 1000.0;  // grados por paso

// ---------- Arbitraje (cuentas del ADC, 0..1023; ~5.7 cuentas por grado) ----------
const int RUIDO_POT          = 4;    // cambio minimo para actualizar el objetivo en modo POT (~0.7 grados)
const int UMBRAL_TOMAR_MANDO = 18;   // cuanto debe moverse el pot para quitar el mando al digital (~3 grados)

// ---------- Estado ----------
Servo servo;
float potFiltrado;        // lectura del pot con filtro exponencial
int   potAplicado;        // ultima lectura del pot que movio el objetivo
int   potReferencia;      // lectura del pot cuando el mando digital tomo el control
bool  mandoDigital = false;
int   objetivo;           // angulo ordenado
float actual;             // angulo intermedio que se envia al servo
int   ultimoEscrito = -1; // ultimo valor enviado al servo

// ---------- Buffer serial ----------
char buf[8];
uint8_t n = 0;
bool desbordado = false;

unsigned long tPot = 0, tMover = 0, tReporte = 0;

int potAAngulo(int lectura) {
  return map(lectura, 0, 1023, ANG_MIN, ANG_MAX);
}

void setup() {
  Serial.begin(9600);
  potFiltrado = analogRead(PIN_POT);
  potAplicado = (int)potFiltrado;
  objetivo = potAAngulo(potAplicado);
  actual = objetivo;
  servo.attach(PIN_SERVO);
  servo.write(objetivo);
  ultimoEscrito = objetivo;
}

void loop() {
  unsigned long ahora = millis();

  leerSerial();

  if (ahora - tPot >= T_POT)         { tPot = ahora;     leerPot(); }
  if (ahora - tMover >= T_MOVER)     { tMover = ahora;   moverServo(); }
  if (ahora - tReporte >= T_REPORTE) { tReporte = ahora; reportar(); }
}

// ---------------- Potenciometro + arbitraje ----------------
void leerPot() {
  int lectura = analogRead(PIN_POT);
  potFiltrado += 0.2 * (lectura - potFiltrado);
  int pot = (int)(potFiltrado + 0.5);

  if (mandoDigital) {
    // El pot recupera el mando solo si se movio de verdad
    if (abs(pot - potReferencia) > UMBRAL_TOMAR_MANDO) {
      mandoDigital = false;
      potAplicado = pot;
      objetivo = potAAngulo(pot);
    }
  } else {
    // Modo POT: se ignora el ruido pequeno
    if (abs(pot - potAplicado) >= RUIDO_POT) {
      potAplicado = pot;
      objetivo = potAAngulo(pot);
    }
  }
}

// ---------------- Serial (no bloqueante) ----------------
void leerSerial() {
  while (Serial.available() > 0) {
    char c = Serial.read();
    if (c == '\n') {
      if (!desbordado) {
        buf[n] = '\0';
        procesarLinea();
      }
      n = 0;
      desbordado = false;
    } else if (c != '\r') {
      if (n < sizeof(buf) - 1) buf[n++] = c;
      else desbordado = true;      // linea demasiado larga: se descarta
    }
  }
}

void procesarLinea() {
  // Formato valido: "A:" + 1 a 3 digitos
  if (n < 3 || n > 5) return;
  if (buf[0] != 'A' || buf[1] != ':') return;

  int valor = 0;
  for (uint8_t i = 2; i < n; i++) {
    if (buf[i] < '0' || buf[i] > '9') return;   // mal formado
    valor = valor * 10 + (buf[i] - '0');
  }
  if (valor < ANG_MIN || valor > ANG_MAX) return;  // fuera de rango

  objetivo = valor;
  mandoDigital = true;
  potReferencia = (int)(potFiltrado + 0.5);
}

// ---------------- Servo ----------------
void moverServo() {
  float dif = objetivo - actual;
  if (fabs(dif) <= PASO) actual = objetivo;
  else actual += (dif > 0) ? PASO : -PASO;

  int ang = (int)(actual + 0.5);
  if (ang != ultimoEscrito) {       // solo se escribe si el valor cambio
    servo.write(ang);
    ultimoEscrito = ang;
  }
}

// ---------------- Reporte ----------------
void reportar() {
  Serial.print("S:");
  Serial.print((int)(actual + 0.5));
  Serial.print(',');
  Serial.print(mandoDigital ? "DIG" : "POT");
  Serial.print('\n');
}
