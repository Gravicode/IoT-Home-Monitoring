using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using HomeMonitoring.Sensors;
using CW.NETMF;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using Json.NETMF;

namespace HomeMonitoring
{
    public class Room
    {
        public bool Movement { set; get; }
        public int Sound { set; get; }
        public int Light { set; get; }
        public float Temperature { set; get; }
        public float Humidity { set; get; }
        public int Gas { set; get; }

    }
    public class Program
    {
        const string MQTT_BROKER_ADDRESS = "192.168.1.102";
        public static MqttClient client { set; get; }
        public static Room MyRoom { set; get; }

        static OutputPort Buzzer = new OutputPort(Pins.GPIO_PIN_D4, false);

        public static void Main()
        {
            //Init
            MyRoom = new Room();

            var Beep = false;
            // write your code here
            var Led = new OutputPort(Pins.GPIO_PIN_D3, false);
            var lightSensor = new LightSensor(Pins.GPIO_PIN_A0);
            var TempSensor = new Dht11Sensor(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1, PullUpResistor.Internal);
            var SoundSensor = new SecretLabs.NETMF.Hardware.AnalogInput(Pins.GPIO_PIN_A1);
            //SoundSensor.SetRange(0, 100);
            var GasSensor = new SecretLabs.NETMF.Hardware.AnalogInput(Pins.GPIO_PIN_A2);
            //GasSensor.SetRange(0, 100);
            var PirSensor = new InputPort(Pins.GPIO_PIN_D2, false, Port.ResistorMode.PullUp);
            var PirState = false;
         

            //waiting till connect...
            if (!Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IsDhcpEnabled)
            {
                // using static IP
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) ; // wait for network connectivity
            }
            else
            {
                // using DHCP
                while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ; // wait for DHCP-allocated IP address
            }
            
            //Debug print our IP address
            Debug.Print("Device IP: " + Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);

            //setup mqtt client
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);
            SubscribeMessage();

            while (true)
            {
                Beep = !Beep;
                MyRoom.Light = lightSensor.ReadLightSensor();
                if (TempSensor.Read())
                {
                    MyRoom.Temperature = TempSensor.Temperature;
                    MyRoom.Humidity = TempSensor.Humidity;
                    Debug.Print("DHT sensor Read() ok, RH = " + MyRoom.Humidity.ToString("F1") + "%, Temp = " + MyRoom.Temperature.ToString("F1") + "°C " + (MyRoom.Temperature * 1.8 + 32).ToString("F1") + "°F");
                }
                MyRoom.Sound = SoundSensor.Read();
                Debug.Print("Suara : " + MyRoom.Sound);
                MyRoom.Gas = GasSensor.Read();
                Debug.Print("Gas Lpg : " + MyRoom.Gas);
                var Pir = PirSensor.Read();
                if (Pir)
                {
                    if (PirState == false)
                    {
                        Debug.Print("Ada gerakan.");
                        PirState = true;
                        MyRoom.Movement = true;
                    }
                }
                else
                {
                    if (PirState)
                    {
                        Debug.Print("Gerakan berhenti.");
                        PirState = false;
                        MyRoom.Movement = false;
                    }
                }
                Thread.Sleep(1000);
                //Buzzer.Write(Beep);
                Led.Write(Beep);
                //update status
                PublishMessage("/home/status", JsonSerializer.SerializeObject(MyRoom));
            }
        }
        static void PublishMessage(string Topic, string Pesan)
        {
            client.Publish(Topic, Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { "/home/control" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {

            string Message = new string(Encoding.UTF8.GetChars(e.Message));
            if (Message.IndexOf(":") < 1) return;
            // handle message received 
            Debug.Print("Message Received : " + Message);
            string[] splittedMessage = Message.Split(':');
            if (splittedMessage[0] == "ALARM" && splittedMessage[1]=="ON")
            {
                Buzzer.Write(true);
            }
            else
            {
                Buzzer.Write(false);
            }
        }
    }
}
