using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TIPC.Core.Tools.Threading;

namespace ArdNet.Server.Tests.CLI.Core
{
    public class MouseMover : IDisposable
    {
        private readonly IArdNetSystem _ardSys;
        private readonly CancelThread<object> _mouseWorkerThread;
        private Point _mouseVector = new Point();
        private volatile int _mouseVectorLifetime = 0;


        public MouseMover(IArdNetSystem ArdSys)
        {
            if (ArdSys is null)
            {
                throw new ArgumentNullException(nameof(ArdSys));
            }

            _ardSys = ArdSys;
            _ardSys.TcpCommandTable.Register("MPU", MpuHandler);
            _mouseWorkerThread = new CancelThread<object>(MouseWorkerHandler);
        }

        public void Start()
        {
            _mouseWorkerThread.Start();
        }


        [DllImport("user32", CharSet = CharSet.Unicode)]
        private static extern int mouse_event(uint dwFlags, int dx, int dy, uint dwData, nint dwExtraInfo);
        private void MpuHandler(IArdNetSystem sender, RequestResponderStateObject e)
        {
            float accel = 1.0f;
            //gyro data
            //x, y, z
            var xMove = int.Parse(e.RequestArgs[0]);
            var yMove = int.Parse(e.RequestArgs[2]);
            //Console.WriteLine($"[{xMove}, {yMove}]");

            if (Math.Abs(xMove) < 1000)
            {
                xMove = 0;
            }
            else
            {
                xMove = (int)(xMove / 750.0);
            }
            if (Math.Abs(yMove) < 1000)
            {
                yMove = 0;
            }
            else
            {
                yMove = (int)(yMove / 750.0);
            }

            _mouseVector = new Point(xMove, yMove);
            _mouseVectorLifetime = 0;
        }


        private void MouseWorkerHandler(object State, CancellationToken Token)
        {
            uint flags = 1; //mouse move
            //CsWin32 func signature is broken.  Must use manual pinvoke
            //PInvoke.mouse_event(flags, xMove, yMove, 0, 0);

            while (!Token.IsCancellationRequested)
            {
                //prevent vector from being used for too many cycles if no new data is available yet
                var newLife = Interlocked.Increment(ref _mouseVectorLifetime);
                if (newLife > 10)
                {
                    _ = Token.WaitHandle.WaitOne(1);
                    continue;
                }
                //move mouse
                var v = _mouseVector;
                _ = mouse_event(flags, v.X, v.Y, 0, 0);
                _ = Token.WaitHandle.WaitOne(10);
            }
        }


        public void Dispose()
        {
            _mouseWorkerThread.Dispose();
        }
    }
}
