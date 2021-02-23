using System;
using System.Diagnostics;
using System.Threading;
using System.Device.I2c;
using nanoFramework.Hardware.Esp32;
using ArdNet.Nano.Client;
using ArdNet.Nano;
using System.Collections;

namespace ESP32_HairTrigger
{
    public class Program
    {
        private static PinController.LedBlinker _led = null;
        private static Mpu6050 _mpu;
        private static ArdNetClientManager _ardManager;

        public static void Main()
        {
            Configuration.SetPinFunction(23, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            _mpu = Mpu6050.StartNew(1);


            Debug.WriteLine("Init");
            _led = new PinController.LedBlinker(13);
            _led.On();
            Thread.Sleep(100);
            _led.Off();


            var AppID = "Esp32.HairTrigger";
            var serverUdpPort = 48597;
            var nanoConfig = new ArdNetClientConfig(AppID, null, serverUdpPort);
            nanoConfig.TCP.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(2000);
            nanoConfig.TCP.HeartbeatConfig.ForceStrictHeartbeat = true;
            nanoConfig.TCP.HeartbeatConfig.RespondToHeartbeats = false;

            _ardManager = new ArdNetClientManager(SystemConfig.WiFiCredentials, nanoConfig);
            _ardManager.StartWorkerThread();


            Debug.WriteLine("Loop");

            while (true)
            {
                var data = _mpu.GetData();
                var args = new ArrayList()
                {
                    data.AccelX.ToString(),
                    data.AccelY.ToString(),
                    data.AccelZ.ToString(),
                    data.GyroX.ToString(),
                    data.GyroY.ToString(),
                    data.GyroZ.ToString()
                };

                var request = TcpRequest.CreateOutbound("Device.Sensors.MPU", args);
                _ardManager.EnqueueTask(x =>
                {
                    _ = x.SendCommand(request, null);
                });
                Thread.Sleep(50);
            }

        }
    }
}
