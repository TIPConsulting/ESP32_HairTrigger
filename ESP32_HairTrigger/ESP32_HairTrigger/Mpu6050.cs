using System;
using System.Device.I2c;

namespace ESP32_HairTrigger
{
    public class Mpu6050
    {
        //code adapted from https://www.hackster.io/oztl/near-perfect-gyroscope-e5e10e

        /// <summary>
        /// Create new MPU 6050 object and initiate device
        /// </summary>
        /// <param name="busID">Serial bus ID on ESP32 device (1 or 2)</param>
        /// <returns></returns>
        public static Mpu6050 StartNew(int busID)
        {
            var tmp = new Mpu6050(busID);
            tmp.Start();
            return tmp;
        }

        private readonly I2cDevice _i2c;
        private readonly byte[] _writeBuffer = new byte[1] { 0x3b };
        private readonly byte[] _readBuffer = new byte[14];
        //NO READONLY STRUCTS

        public Mpu6050(int busID)
        {
            _i2c = I2cDevice.Create(new I2cConnectionSettings(busID, 0x68, I2cBusSpeed.FastMode));
        }

        public void Start()
        {
            _ = _i2c.Write(new SpanByte(new byte[] { 0x6b, 0 }));
            _ = _i2c.WriteRead(new SpanByte(new byte[] { 0x6a }), new SpanByte(new byte[1]));
        }

        public MpuValue GetData()
        {
            _ = _i2c.WriteRead(_writeBuffer, _readBuffer);
            return new MpuValue(_readBuffer);
        }

    }

    public struct MpuValue
    {
        public short AccelX { get; }
        public short AccelY { get; }
        public short AccelZ { get; }

        public short GyroX { get; }
        public short GyroY { get; }
        public short GyroZ { get; }

        public MpuValue(SpanByte arr)
        {
            AccelX = (short)((arr[0] << 8) | arr[1]);
            AccelY = (short)((arr[2] << 8) | arr[3]);
            AccelZ = (short)((arr[4] << 8) | arr[5]);

            //skip 6, 7

            GyroX = (short)((arr[8] << 8) | arr[9]);
            GyroY = (short)((arr[10] << 8) | arr[11]);
            GyroZ = (short)((arr[12] << 8) | arr[13]);
        }
    }
}
