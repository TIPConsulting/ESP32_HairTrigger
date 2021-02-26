//#define platform_test
#define platform_beetle
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
        //LED
        private static PinController.LedBlinker _led = null;

        //ArdClient
        private static ArdNetClientManager _ardManager;
        private static ManualResetEvent _serverMsgHandle = new ManualResetEvent(initialState: false);

        //MPU
        private static Mpu6050 _mpu;
        private static ArrayList _mpuArgList;
        private static DateTime _mpuLastSend = DateTime.MinValue;

        //touchpads
        private static TouchPadController _touchController = new TouchPadController(new TouchPadSystemConfig());
        private static TouchPad _palmTouch;
        private static TouchPad _triggerTouch;
        private static bool _triggerIsDown = false;
        private static DateTime _triggerLastSend = DateTime.MinValue;
        private static TcpRequest _triggerTcpRequest_Down = TcpRequest.CreateOutbound("LClick", "down");
        private static TcpRequest _triggerTcpRequest_Up = TcpRequest.CreateOutbound("LClick", "up");

        public static void Main()
        {
#if platform_test && !platform_beetle
            Configuration.SetPinFunction(23, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            _mpu = Mpu6050.StartNew(1);
#elif !platform_test && platform_beetle
            _mpu = Mpu6050.StartNew(2);
#endif
            _mpuArgList = new ArrayList() { "", "", "" };
            _led = new PinController.LedBlinker(2);
            _touchController.Init();
            _palmTouch = _touchController.OpenPin(27, new TouchPadConfig() { PinSelectMode = TouchPinSelectMode.GpioIndex });
            _triggerTouch = _touchController.OpenPin(13, new TouchPadConfig() { PinSelectMode = TouchPinSelectMode.GpioIndex });


            Debug.WriteLine("Init");
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
            DateTime now;
            while (true)
            {
                _ = _serverMsgHandle.WaitOne();
                now = DateTime.UtcNow;

                if (!_palmTouch.IsTouched())
                {
                    Thread.Sleep(10);
                    continue;
                }

                if ((now - _mpuLastSend).TotalMilliseconds > 75)
                {
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
                    _mpuLastSend = now;
                }

                now = DateTime.UtcNow;
                if (!_triggerIsDown && (now - _triggerLastSend).TotalMilliseconds > 50)
                {
                    if (_triggerTouch.IsTouched())
                    {
                        _ardManager.EnqueueTask(x => x.SendCommand(_triggerTcpRequest_Down));
                        _triggerIsDown = true;
                        _triggerLastSend = now;
                    }
                }
                else if (_triggerIsDown && (now - _triggerLastSend).TotalMilliseconds > 10)
                {
                    if (!_triggerTouch.IsTouched())
                    {
                        _ardManager.EnqueueTask(x => x.SendCommand(_triggerTcpRequest_Up));
                        _triggerIsDown = false;
                        _triggerLastSend = now;
                    }
                }
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
