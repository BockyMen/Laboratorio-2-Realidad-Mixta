#include <Servo.h>

const uint8_t PIN_SERVO = 6;
const uint8_t PIN_POT = A0;

const int ANG_MIN = 0;
const int ANG_MAX = 180;

const unsigned long T_POT = 10;
const unsigned long T_MOVER = 15;
const unsigned long T_REPORTE = 50;

const float VEL_MAX = 60.0;
const float PASO = VEL_MAX * T_MOVER / 1000.0;

const int RUIDO_POT = 4;
const int UMBRAL_TOMAR_MANDO = 18;

Servo servo;
float potFiltrado;
int potAplicado;
int potReferencia;
bool mandoDigital = false;
int objetivo;
float actual;
int ultimoEscrito = -1;

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

  if (ahora - tPot >= T_POT) { tPot = ahora; leerPot(); }
  if (ahora - tMover >= T_MOVER) { tMover = ahora; moverServo(); }
  if (ahora - tReporte >= T_REPORTE) { tReporte = ahora; reportar(); }
}

void leerPot() {
  int lectura = analogRead(PIN_POT);
  potFiltrado += 0.2 * (lectura - potFiltrado);
  int pot = (int)(potFiltrado + 0.5);

  if (mandoDigital) {
    if (abs(pot - potReferencia) > UMBRAL_TOMAR_MANDO) {
      mandoDigital = false;
      potAplicado = pot;
      objetivo = potAAngulo(pot);
    }
  } else {
    if (abs(pot - potAplicado) >= RUIDO_POT) {
      potAplicado = pot;
      objetivo = potAAngulo(pot);
    }
  }
}

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
      else desbordado = true;
    }
  }
}

void procesarLinea() {
  if (n < 3 || n > 5) return;
  if (buf[0] != 'A' || buf[1] != ':') return;

  int valor = 0;
  for (uint8_t i = 2; i < n; i++) {
    if (buf[i] < '0' || buf[i] > '9') return;
    valor = valor * 10 + (buf[i] - '0');
  }
  if (valor < ANG_MIN || valor > ANG_MAX) return;

  objetivo = valor;
  mandoDigital = true;
  potReferencia = (int)(potFiltrado + 0.5);
}

void moverServo() {
  float dif = objetivo - actual;
  if (fabs(dif) <= PASO) actual = objetivo;
  else actual += (dif > 0) ? PASO : -PASO;

  int ang = (int)(actual + 0.5);
  if (ang != ultimoEscrito) {
    servo.write(ang);
    ultimoEscrito = ang;
  }
}

void reportar() {
  Serial.print("S:");
  Serial.print((int)(actual + 0.5));
  Serial.print(',');
  Serial.print(mandoDigital ? "DIG" : "POT");
  Serial.print('\n');
}
