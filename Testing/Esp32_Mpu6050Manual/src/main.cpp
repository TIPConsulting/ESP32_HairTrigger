#include <Arduino.h>
#include <Wire.h>

#define MPU_addr 0x68 //0x means HEX (because I don't really understand these things yet)
/**
        _
   _  _ \_\  _
  |_|  /_\_\ | |_
          \_\
  Version 1.4.2 (2/ now 4 filters done)
**/
//much of the original code was done by JonChi of the arduino community!
//vars for stuff
double AccelX, AccelY, AccelZ, GyroX, GyroY, GyroZ, AngleX, AngleY, AngleZ;
int tOne = 0, tTwo = 0;
int16_t AccelX_out, AccelY_out, AccelZ_out, Tmp, GyroX_out, GyroY_out, GyroZ_out;
double xR = 0, yR = 0, zR = 0, OldXr = 0, xA = 0, yA = 0, zA = 0;
double DegX, DegSX;
double dFudgeGyroX = 0;

//this loop just gets everything up and going
void setup()
{

  Serial.begin(9600);
  Wire.begin();
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B); // PWR_MGMT_1 register
  Wire.write(0);    // set to zero (wakes up the MPU-6050)
  Wire.endTransmission(true);

  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6A);
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_addr, 1, true);
  Wire.endTransmission(true);
}

void loop()
{
  //communication with sensor
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x3B); // starting with register 0x3B (ACCEL_XOUT_H)
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_addr, 14, true);        // request a total of 14 registers
  AccelX_out = Wire.read() << 8 | Wire.read(); // 0x3B (ACCEL_XOUT_H) & 0x3C (ACCEL_XOUT_L)
  AccelY_out = Wire.read() << 8 | Wire.read(); // 0x3D (ACCEL_YOUT_H) & 0x3E (ACCEL_YOUT_L)
  AccelZ_out = Wire.read() << 8 | Wire.read(); // 0x3F (ACCEL_ZOUT_H) & 0x40 (ACCEL_ZOUT_L)
  Tmp = Wire.read() << 8 | Wire.read();        // 0x41 (TEMP_OUT_H) & 0x42 (TEMP_OUT_L)
  GyroX_out = Wire.read() << 8 | Wire.read();  // 0x43 (GYRO_XOUT_H) & 0x44 (GYRO_XOUT_L)
  GyroY_out = Wire.read() << 8 | Wire.read();  // 0x45 (GYRO_YOUT_H) & 0x46 (GYRO_YOUT_L)
  GyroZ_out = Wire.read() << 8 | Wire.read();  // 0x47 (GYRO_ZOUT_H) & 0x48 (GYRO_ZOUT_L)

  Serial.printf("Accel: [%d, %d, %d] | Gyro: [%d, %d, %d]\n", AccelX_out, AccelY_out, AccelZ_out, GyroX_out, GyroY_out, GyroZ_out);

  return;
  int debugPin = digitalRead(13);
  if (debugPin == 0)
  {
    delay(1000);
    xR = 0;
    yR = 0;
    zR = 0;
    Serial.println("Variables reset!");
  }

  //the stablization numbers
  AccelX = atan2(AccelY_out, AccelZ_out) * 57.2957795131;
  AccelY = atan2(AccelX_out, AccelZ_out) * 57.2957795131;
  //AccelZ = atan2(AccelX, AccelY): //due to math problems, the z axis isn't avaible as yet. however, two axis (plural) should be fine
  //TAccelX = SAccelX * ((double(tTwo - tOne)) / 1000.0);

  if (GyroX_out >= 0)
  {
    GyroX = (double(GyroX_out)) * 250 / 32767.0;
  }
  else if (GyroX_out < 0)
  {
    GyroX = (double(GyroX_out)) * 250 / 32768.0;
  }

  if (GyroY_out >= 0)
  {
    GyroY = (double(GyroY_out)) * 250 / 32767.0;
  }
  else if (GyroX_out < 0)
  {
    GyroY = (double(GyroY_out)) * 250 / 32768.0;
  }
  //GyroX = nGyroX;
  tTwo = millis();
  //SGyroX = GyroX * ((double(tTwo - tOne)) / 1000.0);
  GyroX = GyroX * ((double(tTwo - tOne)) / 1000.0);
  yR += GyroY;
  xR += GyroX;
  yR = (0.50 * yR) + (0.50 * AccelX);
  xR = (0.50 * xR) + (0.50 * AccelX);
  //if (nAccelX + nAccelY + nAccelZ < 32767) {
  AngleX = (0.97 * ((AngleX + xR) / 2) + 0.03 * (AccelX / 2));
  AngleY = (0.97 * (AngleY + GyroY) + 0.03 * (AccelY / 2));
  AngleZ = (0.97 * (AngleZ + GyroZ) + 0.03 * (1 /*compass*/));
  //}
  /*
    if (AngleX >= 0) {
    xR = (double(AngleX)) * 250 / 32767.0;
    } else if (AngleX < 0) {
    xR = (double(AngleX)) * 250 / 32768.0;
    }
    //AngleX = 0;
    //AngleX = xR * ((double(tTwo - tOne)) / 1000.0);
  */
  //Serial.println(tTwo - tOne);
  tOne = tTwo;

  //GyroY = GyroY * 250 / 32767.5;
  //GyroZ = GyroZ * 250 / 32767.5;
  Serial.print(AngleX);
  //Serial.print(" ");
  //Serial.print(AngleY);
  //Serial.print(GyroX_out);
  //Serial.print(" ");
  //Serial.print(AccelX);
  //Serial.print(" ");
  //Serial.print(nGyroX);
  //Serial.print(" ");
  //Serial.println(AccelZ);
  //Serial.print(SGyroX);
  Serial.println();

  OldXr = xR;
  delay(10);
}