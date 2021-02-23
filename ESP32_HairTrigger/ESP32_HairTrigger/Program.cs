using System;
using System.Diagnostics;
using System.Threading;
using System.Device.I2c;
using nanoFramework.Hardware.Esp32;

namespace ESP32_HairTrigger
{
    public class Program
    {
        static Mpu6050 _mpu;

        public static void Main()
        {
            Configuration.SetPinFunction(23, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            _mpu = Mpu6050.StartNew(1);

            Debug.WriteLine("Loop");

            while (true)
            {
                var data = _mpu.GetData();
                Debug.WriteLine($"Accel: [{data.AccelX}, {data.AccelY}, {data.AccelZ}] | Gyro: [{data.GyroX}, {data.GyroY}, {data.GyroZ}]");
                Thread.Sleep(50);
            }

        }

    }
}
