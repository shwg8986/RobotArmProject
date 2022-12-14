/****************************************************************
  ESP32 sample code for Mektron pressure sensor with
  10 x 10 pressure and 8 thermal sensing elements, 3 units for 3 fingers.
  2020 Mar 27 Hiroyuki Kajimoto (kajimoto@kaji-lab.jp)
  PC -> ESP32
  0xFE: request data
  ESP32 -> pc
  all 12bit data (2 points are packed to 3 bytes) + terminating character(0xFF)
  Currently, all data is sent with 8bit mode. For more precise measurement, 12bit mode can be made.
*****************************************************************/
#include <SPI.h>
#include "freertos/task.h"
#include "soc/timer_group_struct.h"
#include "soc/timer_group_reg.h"

//ADS7953 with 16 analog inputs.
#define ADS7953_CS1 33
#define ADS7953_CS2 25
#define ADS7953_CS3 13
#define SCLK 18
#define MOSI 23
#define MISO 19
#define PC_ESP32_MEASURE_REQUEST 0xFE
#define ESP32_PC_MEASURE_RESULT 0xFF
//variables for sensor
const int COL_NUM = 10;
const int ROW_NUM = 10;
const int THERMAL_NUM = 2;
const int FINGER_NUM = 3;
//int PressureRange = 3; //1 to 4. Defines sensing PressureRange.
short Conductance[FINGER_NUM][COL_NUM][ROW_NUM] = { 0 };
short Thermal_Conductance[FINGER_NUM][THERMAL_NUM] = { 0 };
int ColumnBus[COL_NUM] = {17, 2, 32, 4, 5, 12, 26, 14, 15, 16}; //B Bus

//variables for AD converter. Refer schematic of the board. The first two are repeated due to AD chip feature.
#define ADS7953_MODE 1
#define ADS7953_PROG 0
#define ADS7953_RANGE 0
//int ADChannel[ROW_NUM + 2] = {0, 1, 2, 3, 4, 0, 1};
int ADChannel[ROW_NUM + 2] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1};
//int ThermalADChannel[THERMAL_NUM + 2] = { 5, 6, 7, 8, 9, 10, 11, 12, 5, 6 };
int ThermalADChannel[THERMAL_NUM + 2] = { 10,11,10,11};
unsigned char snd[(COL_NUM*ROW_NUM+THERMAL_NUM)*FINGER_NUM*3/2+2];
long prevTime1=0,prevTime2=0;
int DataLength = 0;
//hardware timer
hw_timer_t * timer = NULL;


void ColumnClear()
{
  int i;
  for (i = 0; i < COL_NUM; i++) {
    digitalWrite(ColumnBus[i], LOW);
  }
}

//Initialize ADS7953
void Sensor_Init()
{
  digitalWrite(ADS7953_CS1, HIGH);
  digitalWrite(ADS7953_CS2, HIGH);
  digitalWrite(ADS7953_CS3, HIGH);

  digitalWrite(ADS7953_CS1, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | (0 & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(ADS7953_CS1, HIGH);

  digitalWrite(ADS7953_CS2, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | (0 & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(ADS7953_CS2, HIGH);

  digitalWrite(ADS7953_CS3, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | (0 & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(ADS7953_CS3, HIGH);
  ColumnClear();
}

//Data aquisition by manual mode.
//Automatic mode might be another option for faster transfer. See AD7953 catalog.
void Sensor_Get_Row(int CS_port, short data[ROW_NUM])
{
  digitalWrite(CS_port, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | ((ADChannel[0]) & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(CS_port, HIGH);

  digitalWrite(CS_port, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | ((ADChannel[1]) & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(CS_port, HIGH);
  
  for (int row = 0; row < ROW_NUM; row++) {
    digitalWrite(CS_port, LOW);
    data[row] = (0x0FFF & SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | (ADChannel[row + 2] & 0x0F) << 7 | (ADS7953_RANGE << 6)));
    digitalWrite(CS_port, HIGH);
  }
}

//Data aquisition by manual mode.
//Automatic mode might be another option for faster transfer. See AD7953 catalog.
void Sensor_Get_Row_Thermal(int CS_port, short data[THERMAL_NUM])
{
  digitalWrite(CS_port, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | ((ThermalADChannel[0]) & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(CS_port, HIGH);
  digitalWrite(CS_port, LOW);
  SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | ((ThermalADChannel[1]) & 0x0F) << 7 | (ADS7953_RANGE << 6));
  digitalWrite(CS_port, HIGH);
  for (int row = 0; row < THERMAL_NUM; row++) {
    digitalWrite(CS_port, LOW);
    data[row] = (0x0FFF & SPI.transfer16((ADS7953_MODE << 12) | (ADS7953_PROG << 11) | (ThermalADChannel[row + 2] & 0x0F) << 7 | (ADS7953_RANGE << 6)));
    digitalWrite(CS_port, HIGH);
  }
}

void loopMicroseconds1(int us)
{
  long t;
  t = micros();
  if(t - prevTime1 < us){
    delayMicroseconds(us - (t - prevTime1)+2); //2 is a magic number...
  }
  prevTime1 = micros();
}

void loopMicroseconds2(int us)
{
  long t;
  t = micros();
  if(t - prevTime2 < us){
    delayMicroseconds(us - (t - prevTime2)+2); //2 is a magic number...
  }
  prevTime2 = micros();
}

void feedTheDog(){
  // feed dog 0
  TIMERG0.wdt_wprotect=TIMG_WDT_WKEY_VALUE; // write enable
  TIMERG0.wdt_feed=1;                       // feed dog
  TIMERG0.wdt_wprotect=0;                   // write protect
  // feed dog 1
  TIMERG1.wdt_wprotect=TIMG_WDT_WKEY_VALUE; // write enable
  TIMERG1.wdt_feed=1;                       // feed dog
  TIMERG1.wdt_wprotect=0;                   // write protect
}

/******************************************/
/* Dual Task MAIN                         */
/******************************************/
void task0(void* param)
{
  char rcv;

  while(1) {
    if (Serial.available() > 0) {
      rcv = Serial.read();
      if (rcv == PC_ESP32_MEASURE_REQUEST) {
        Serial.write(snd, (COL_NUM*ROW_NUM+THERMAL_NUM)*FINGER_NUM*3/2 +2); //send 8bit time in milliseconds
      }else if (rcv > 0 && rcv < 5) { //change range of the sensor
        //PressureRange = rcv; //do not use this. We send 12 bits!
      }
    }
    feedTheDog();
    //loopMicroseconds2(2000);
  }
}

void setup() {
  int i;
  pinMode(ADS7953_CS1, OUTPUT);
  pinMode(ADS7953_CS2, OUTPUT);
  pinMode(ADS7953_CS3, OUTPUT);
  for (i = 0; i < COL_NUM; i++) {
    pinMode(ColumnBus[i], OUTPUT);
  }
  //Serial.setRxBufferSize(1024);
  Serial.begin(921600);
  SPI.begin(SCLK, MISO, MOSI);
  SPI.setFrequency(20000000);
  SPI.setDataMode(SPI_MODE0);
//  SPI.setHwCs(true);
  Sensor_Init();
  //task0 at core 0 for serial communication
  xTaskCreatePinnedToCore(task0, "Task0", 4096, NULL, 1, NULL, 0);

}

// Main loop. Always measure.
void loop() {
  int val, t, x1, x2, y1, y2;
  int ii, packing;

  for (int col = 0; col < COL_NUM; col++) {
    //ColumnSelect = 1 << col;
    digitalWrite(ColumnBus[col], HIGH);
    //delayMicroseconds(100);
    //wait_us(100); //Check if this wait is necessary.
    Sensor_Get_Row(ADS7953_CS1, Conductance[0][col]); //first finger
    Sensor_Get_Row(ADS7953_CS2, Conductance[1][col]); //second finger
    Sensor_Get_Row(ADS7953_CS3, Conductance[2][col]); //third finger
    digitalWrite(ColumnBus[col], LOW);
  }
  //For thermal sensing.
  Sensor_Get_Row_Thermal(ADS7953_CS1, Thermal_Conductance[0]);//first finger
  Sensor_Get_Row_Thermal(ADS7953_CS2, Thermal_Conductance[1]);//second finger
  Sensor_Get_Row_Thermal(ADS7953_CS3, Thermal_Conductance[2]);//third finger

  ii = 0;
  //Pack data. (12bit x 2 -> 8bit x 3)
  // Original Data: AAAAAAAAAAAA BBBBBBBBBBBB
  // Send Data:     AAAAAAAA AAAABBBB BBBBBBBB
  for (int finger = 0; finger < FINGER_NUM; finger++) {
    packing = 0;
    for (int col = 0; col < COL_NUM; col++) {
      for (int row = 0; row < ROW_NUM; row++) {
        if(packing == 0){
          val = Conductance[finger][col][row] >> 4;//AAAAAAAA
          snd[ii] = val;
          ii++;
          val = Conductance[finger][col][row] & 0x0F;//AAAA
          snd[ii] = val<<4; //pack to upper 4 bits
          packing = 1;
        }else if(packing == 1){
          val = Conductance[finger][col][row] >> 8;//BBBB
          snd[ii] |= val; //pack to lower 4 bits
          ii++;
          val = Conductance[finger][col][row] & 0xFF;//BBBBBBBB
          snd[ii] = val; //pack to 8 bits
          ii++;
          packing = 0;
        }
      }
    }
    packing = 0;
    for (int row = 0; row < THERMAL_NUM; row++) {
      if(packing == 0){
        val = Thermal_Conductance[finger][row] >> 4;//AAAAAAAA
        snd[ii] = val;
        ii++;
        val = Thermal_Conductance[finger][row] & 0x0F;//AAAA
        snd[ii] = val<<4; //pack to upper 4 bits
        packing = 1;
      }else if(packing == 1){
        val = Thermal_Conductance[finger][row] >> 8;//BBBB
        snd[ii] |= val; //pack to lower 4 bits
        ii++;
        val = Thermal_Conductance[finger][row] & 0xFF;//BBBBBBBB
        snd[ii] = val; //pack to 8 bits
        ii++;
        packing = 0;
      }
    }
  }
  t = micros()/100;
  snd[ii] = t & 0xFF;
  ii++;
  snd[ii] = ESP32_PC_MEASURE_RESULT;
  DataLength = ii;
  //Set the loop exactly at 500Hz
  //loopMicroseconds1(2000);
}
