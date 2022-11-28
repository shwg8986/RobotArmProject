/*****************************************************************************
  HV513 128ch MultiTask, MultiCore example.
  Multitask was used to avoid unexpected stall during stimulation.
  2021 Dec. 7
  Hiroyuki Kajimoto
******************************************************************************/

#include <SPI.h>
#define SCLK 18
#define MOSI 23
#define MISO 19
#define DA_CS 22
#define AD_CS 5
#define LEDPIN 32
#define HV513_DIN 13 //digital in
#define HV513_BL 25
#define HV513_POL 27
#define HV513_CLK 14
#define HV513_LE 26

// --- vibration motor
#define VIB_MOTOR_1 33
#define VIB_MOTOR_2 12
// ---

#define ELECTRODE_NUM 128

#define PC_ESP32_STIM_PATTERN 0xFF
#define PC_ESP32_MEASURE_REQUEST 0xFE
#define PC_ESP32_POLARITY_CHANGE 0xFD
// ushiyama created for an experiment
#define PC_ESP32_VIBRATION 0xFC

#define ESP32_PC_RECEIVE_FINISHED 0xFE
#define ESP32_PC_MEASURE_RESULT 0xFF
#define CURRENT_FOR_MEASUREMENT 1024 //2.5mA 4096=10mA
#define HV513_PIN_NUM 8
#define HV513_NUM 16

unsigned char stim_pattern[ELECTRODE_NUM] = {0};
unsigned char voltage[ELECTRODE_NUM] = { 0 };

// for change stimulation intensity
unsigned char stim_amp_index[ELECTRODE_NUM] = {0}; // for amp_table
// 4段階の刺激強度
unsigned int amp_table[4] = {13, 25, 50, 100};
//unsigned int amp_table[8] = {9, 13, 18, 25, 35, 50, 71, 100};
// 8段階で各電極の確率を調節. 1.4倍ごと ( (by nakayamakun)

// 振動子駆動PWM duty比の最大値 (bit with)
int VIBRATION_BIT_WIDTH = 10;
int MAX_VIBRATION_DUTY = 1024; // 2^(VIBRATION_BIT_WIDTH)


TaskHandle_t th[1];

bool DA_TEST = true;
int Polarity = 1; //1:positive, -1:negative
short PulseHeight = 0;
int PulseWidth = 0;

/******************************************************************************/
/*  hv513FastScan                                                             */
/*  MAKE SURE THAT YOU INITIALIZE THIS FUNCTION BY FIRST CALLING              */
/*  hv513FastScan(0);                                                         */
/*  See manual for detail of this function.                                   */
/******************************************************************************/
void hv513FastScan(int usWhichPin) {
  int ii, pin;
  static int pos;

  //Load S/R
  digitalWrite(HV513_LE, LOW);
  if (usWhichPin == 0) {
    digitalWrite(HV513_DIN, HIGH);
    digitalWrite(HV513_CLK, HIGH);
    digitalWrite(HV513_CLK, LOW);
    pos = 0;
  } else {
    digitalWrite(HV513_DIN, LOW);
    pin = usWhichPin - pos;
    // ここで差分だけクロックを進めるのは、一点の刺激を走査しているため
    // 1フレームごとに順番に1点を刺激して、次の点を刺激して... を繰り返す
    for (ii = 0; ii < pin; ii++) {
      digitalWrite(HV513_CLK, HIGH);
      digitalWrite(HV513_CLK, LOW);
    }
    pos = usWhichPin;
  }
  digitalWrite(HV513_LE, HIGH); //added
}

/******************************************************************************/
/*  HV513 Clear                                                                */
/******************************************************************************/
void hv513Clear(int hv513_num) {
  hv513FastScan(HV513_PIN_NUM * hv513_num);
}

/******************************************************************************/
/*  HV513 init                                                                */
/******************************************************************************/
void hv513Init(int hv513_num) {
  int pin;
  int hv513_total = HV513_PIN_NUM * hv513_num;

  digitalWrite(HV513_POL, HIGH);
  digitalWrite(HV513_LE, LOW);
  digitalWrite(HV513_CLK, LOW);

  digitalWrite(HV513_BL, HIGH);
  digitalWrite(HV513_DIN, LOW);
  for (pin = 0; pin < hv513_total; pin++) {
    digitalWrite(HV513_CLK, HIGH);
    digitalWrite(HV513_CLK, LOW);
  }
}


/******************************************************************************/
/* DA output by AD5452 and AD input by AD7276(SPI)                            */
/******************************************************************************/

short DAAD(short DA) {
  short AD;
  digitalWrite(DA_CS, LOW);     //enable clock
  digitalWrite(AD_CS, LOW);     //enable clock
  AD = SPI.transfer16(DA << 2);
  digitalWrite(DA_CS, HIGH);    //disable clock and load data
  digitalWrite(AD_CS, HIGH);
  return AD >> 2;               //bottom 2bits are unnecessary
}

void DAC_out(short DA)
{
  short DACvalue;

  // Prepare the SPI bus for DAC transfer, and do the transfer...
  SPI.beginTransaction(SPISettings(20000000, MSBFIRST, SPI_MODE3));  // Start DAC Transaction - using SPI Mode 3 for DAC
  digitalWrite(DA_CS, LOW);        // Enable DAC chip
  DACvalue = (DA << 2) & 0x3FFC;   // Shift bit alignment as required by AD5452, and mask off top & bottom 2 bits - especially the AD5452 "Control" bits.
  SPI.transfer16(DACvalue);
  digitalWrite(DA_CS, HIGH);       // Disable DAC chip
  SPI.endTransaction();            // End DAC Transaction
}

short ADC_in(void)
{
  short AD;

  // Prepare the SPI bus for ADC transfer, and do the transfer...
  SPI.beginTransaction(SPISettings(20000000, MSBFIRST, SPI_MODE0));  // Start ADC Transaction - using SPI Mode 0 for ADC chip
  digitalWrite(AD_CS, LOW);     // Enable ADC chip
  AD = SPI.transfer16(0);       // Read the ADC. Note that output to the DAC is Disabled here, so the 0 value has no effect on the DAC.
  digitalWrite(AD_CS, HIGH);    // Disable ADC chip
  SPI.endTransaction();         // End ADC Transaction

  return AD;  // Return the full 12-bit value from the ADC.
}


//Achieve 60Hs Stimluation. It can be 80Hz or higher for smoother sensation, or it can be 40Hz or lower for larger number of electrodes.
void stimulate(void *pvParameters)
{
  int ch, t, Step, StimNum, DelayMsForStableLoop;
  short AD;

  while (1) {
    StimNum = 0;
    hv513FastScan(0);
    for (ch = 0; ch < ELECTRODE_NUM; ch++) {
      if (stim_pattern[ch] != 0) {
        // control intensity using probabitlity (adjust pulse num during 1 sec)
        if (!amp_table[stim_amp_index[ch]] > random(100)) continue;
        StimNum++;
        if (ch != 0) {
          hv513FastScan(ch);
        }
        if (Polarity == -1) { //Cathodic Stimulation
          digitalWrite(HV513_POL, LOW);
        } else { //Anodic Stimulation
          digitalWrite(HV513_POL, HIGH);
        }
        noInterrupts(); //avoid interrupts during stimulation pulse. Might not necessary.
        DAAD(PulseHeight);
        //        DAC_out(PulseHeight);
        if (PulseWidth < 12) {
          Step = 0;
        } else {
          Step = (int)((float)PulseWidth * 0.1244 - 1.1678);
        }
        for (t = 0; t < Step; t++) {
          AD = DAAD(PulseHeight);
          //          DAC_out(PulseHeight);
        }
        //        DAC_out(0);
        AD = DAAD(0);
        voltage[ch] = AD >> 4; //record voltage. To calculate impedance, divide this by current.
        digitalWrite(HV513_POL, HIGH); //added (POL=1 & BL=0 means ALL GND)
        digitalWrite(HV513_BL, LOW);
        if (PulseWidth < 200) {
          Step = (int)((float)(200 - PulseWidth) * 0.1244 - 1.1678);
        }
        for (t = 0; t < Step; t++) {
          AD = DAAD(0);
          //          DAC_out(0);
        }
        interrupts();
        digitalWrite(HV513_BL, HIGH); //added
      }
    }
    hv513Clear(HV513_NUM);    //cleaning

    //    DelayMsForStableLoop = (int)(13.3 - (float)StimNum * 0.2); //13.3ms is for 60Hz loop. StimNum*0.2 is stimulation time.
    double span = 1000.0 / 200.0; // 100 Hz
    DelayMsForStableLoop = (int)(span - (float)StimNum * PulseWidth / 1000.0);
    if (DelayMsForStableLoop > 1) {
      vTaskDelay(DelayMsForStableLoop); //Delay time for freeRTOS
    } else {
      vTaskDelay(1); //At least 1ms delay is necessary for freeRTOS (to avoid watchdog bark)
    }
  }
}


void setup() {
  pinMode(LEDPIN, OUTPUT);
  pinMode(DA_CS, OUTPUT);
  pinMode(AD_CS, OUTPUT);
  pinMode(HV513_DIN, OUTPUT);
  pinMode(HV513_BL, OUTPUT);
  pinMode(HV513_POL, OUTPUT);
  pinMode(HV513_CLK, OUTPUT);
  pinMode(HV513_LE, OUTPUT);

  SPI.begin(SCLK, MISO, MOSI);
  SPI.setFrequency(32000000);
  //  SPI.setDataMode(SPI_MODE1); //This may be the cause of instable stimulation?
  SPI.setDataMode(SPI_MODE0); //This may be the cause of instable stimulation?
  Serial.begin(921600);
  //  Serial.begin(1000000);
  hv513Init(HV513_NUM); //added
  xTaskCreatePinnedToCore(stimulate, "stimulate", 4096, NULL, 3, &th[0], 0); //Task for stimulation. Higher priority than main loop.
  // vibration motor
  int freq = 40000; // Hz
  ledcSetup(0, freq, VIBRATION_BIT_WIDTH);
  ledcAttachPin(VIB_MOTOR_1, 0);
  ledcSetup(1, freq, VIBRATION_BIT_WIDTH);
  ledcAttachPin(VIB_MOTOR_2, 1);
}

void vibration(int finger, float duty) {
  int thumb = 0;
  int index = 1;
  // 0.0 ~ 1.0 に制限
  duty = constrain(duty, 0, 1.0);
  // 5Vを使っていて定格 3.5Vぐらいなので、0.7に制限する
  duty = constrain(duty, 0, 0.7);

  if (finger == thumb) {
    ledcWrite(thumb, duty * MAX_VIBRATION_DUTY);
  } else if (finger == index) {
    ledcWrite(index, duty * MAX_VIBRATION_DUTY);
  }
  return;
}

float thumb_vib_duty = 0.0;
float index_vib_duty = 0.0;

void loop() {
  int t, pin, ch;
  char rcv;
  short AD;
  short vol_l, vol_h;
  //  ledcWrite(0, 500);
  //  ledcWrite(1, 500);
  if (Serial.available() >= ELECTRODE_NUM + 2) {
    rcv = Serial.read();
    //        digitalWrite(LEDPIN, HIGH);   // sets the LED on
    //        delay(3);
    //        digitalWrite(LEDPIN, LOW);   // sets the LED off
    if (rcv == PC_ESP32_STIM_PATTERN) {
      vol_l = Serial.read();
      vol_h = Serial.read() << 6;
      PulseHeight = vol_h + vol_l; // 12 bit 利用
      //      rcv = Serial.read();
      //      PulseHeight = rcv << 4;


      rcv = Serial.read();//pulse width (0 to 250)
      if (rcv > 250) {
        rcv = 0;
      }
      PulseWidth = rcv * 10; //if 200us, rcv is 20.
      for (ch = 0; ch < ELECTRODE_NUM; ch++) {
        byte sp = Serial.read();
        stim_pattern[ch] = sp >> 2;
        stim_amp_index[ch] = sp & 0x03;
      }
    } else if (rcv == PC_ESP32_VIBRATION) {
      rcv = Serial.read(); // vibration intensity of thumb
      thumb_vib_duty = rcv / (float)255.0;


      rcv = Serial.read(); // vibration intensity of index
      index_vib_duty = rcv / (float)255.0;

      byte _rcv = 0;
      for (ch = 0; ch < ELECTRODE_NUM; ch++) {
        _rcv = Serial.read();
        // dummy data due to Serial.available() > ELECTRODE_NUM + 2
      }

    } else  if (rcv == PC_ESP32_POLARITY_CHANGE) {
      Polarity = Polarity * -1;
    } else  if (rcv == PC_ESP32_MEASURE_REQUEST) {
      Serial.write(ESP32_PC_MEASURE_RESULT);//send 0xFF
      for (ch = 0; ch < ELECTRODE_NUM; ch++) {
        Serial.write(voltage[ch]);
      }
    }
  }
  vibration(0, thumb_vib_duty);
  vibration(1, index_vib_duty);
  //  vTaskDelay(1); //added for stability
  delay(1);
}
