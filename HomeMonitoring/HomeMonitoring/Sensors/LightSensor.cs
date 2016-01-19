using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
namespace HomeMonitoring.Sensors
{
    public class LightSensor
    {
        const int minValue = 0;
        const int maxValue = 100;
        SecretLabs.NETMF.Hardware.AnalogInput lightSensor { set; get; }
        public LightSensor(Cpu.Pin pin)
        {
            // Open the analog port
            lightSensor = new SecretLabs.NETMF.Hardware.AnalogInput(pin);

            // Set the value range between 0 and 100
            lightSensor.SetRange(minValue, maxValue);
        }
        public int ReadLightSensor()
        {
            // Read the value and invert it: 0 = dark, 100 = bright
            var lightSensorValue = lightSensor.Read();
            Debug.Print("light sensor :" + lightSensorValue);
            // Do something with the value
            return lightSensorValue;

        }
    }
}
