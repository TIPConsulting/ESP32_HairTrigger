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
        private static ArrayList _mpuArgList;
        private static ManualResetEvent _serverMsgHandle = new ManualResetEvent(initialState: false);

        public static void Main()
        {
            Configuration.SetPinFunction(23, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            _mpu = Mpu6050.StartNew(1);
            _mpuArgList = new ArrayList() { "", "", "" };


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
            _ardManager.TcpEndpointConnected += ArdManager_TcpEndpointConnected;
            _ardManager.TcpEndpointDisconnected += NanoClient_ServerDisconnected;
            _ardManager.StartWorkerThread();


            Debug.WriteLine("Loop");

            MpuValue data;
            while (true)
            {
                _ = _serverMsgHandle.WaitOne();

                data = _mpu.GetData();
                //gyro data
                //x, y, z
                _mpuArgList[0] = data.GyroX.ToString();
                _mpuArgList[1] = data.GyroY.ToString();
                _mpuArgList[2] = data.GyroZ.ToString();

                var request = TcpRequest.CreateOutbound("MPU", _mpuArgList);
                _ardManager.EnqueueTask(x =>
                {
                    _ = x.SendCommand(request);
                });
                Thread.Sleep(75);
            }

        }
        private static void ArdManager_TcpEndpointConnected(IArdNetSystem Sender, IConnectedSystemEndpoint e)
        {
            _ = _serverMsgHandle.Set();
            Debug.WriteLine("ArdNet Connected");
        }

        private static void NanoClient_ServerDisconnected(IArdNetSystem Sender, ISystemEndpoint e)
        {
            _ = _serverMsgHandle.Reset();
        }

    }
}
