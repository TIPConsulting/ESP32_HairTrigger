using System.Device.Gpio;
using System.Threading;

namespace ESP32_HairTrigger
{
    public static class PinController
    {
        public static GpioController Default { get; } = new GpioController();

        public class LedBlinker
        {
            private readonly GpioPin _ledPin;
            private readonly Timer _onboardBlinkTimer;
            public LedBlinker(int PinNumber)
            {
                _ledPin = Default.OpenPin(PinNumber);
                _ledPin.SetPinMode(PinMode.Output);
                _onboardBlinkTimer = new Timer(ShutdownTimerCallback, _ledPin, Timeout.Infinite, Timeout.Infinite);
            }

            private void ShutdownTimerCallback(object e)
            {
                ((GpioPin)e).Write(PinValue.Low);
            }

            public void On() => _ledPin.Write(PinValue.High);

            public void Off() => _ledPin.Write(PinValue.Low);

            public void Blink(int millis)
            {
                _ledPin.Write(PinValue.High);
                _ = _onboardBlinkTimer.Change(millis, Timeout.Infinite);
            }

        }

    }
}
