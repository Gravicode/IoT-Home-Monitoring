using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Configuration;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HomeMonitoring.Web
{
    [Serializable]
    public class Room
    {
        public bool Movement { set; get; }
        public int Sound { set; get; }
        public int Light { set; get; }
        public float Temperature { set; get; }
        public float Humidity { set; get; }
        public int Gas { set; get; }

    }
    [HubName("IOTHub")]
    public class IOTHub : Hub
    {
        public static MqttClient client { set; get; }
        public static string MQTT_BROKER_ADDRESS
        {
            get { return ConfigurationManager.AppSettings["MQTT_BROKER_ADDRESS"]; }
        }
        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { "/home/status" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

       
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string Pesan = Encoding.UTF8.GetString(e.Message);
            switch (e.Topic)
            {
                case "/home/status":
                    var MyRoom = JsonConvert.DeserializeObject<Room>(Pesan);
                    StringBuilder sb = new StringBuilder();
                    sb.Append("DHT sensor Read() ok, RH = " + MyRoom.Humidity.ToString("F1") + "%, Temp = " + MyRoom.Temperature.ToString("F1") + "°C " + (MyRoom.Temperature * 1.8 + 32).ToString("F1") + "°F <br/>");
                    sb.Append("Suara : " + MyRoom.Sound +"<br/>");
                    sb.Append("Gas Lpg : " + MyRoom.Gas + "<br/>");
                    sb.Append((MyRoom.Movement ? "Ada Gerakan" : "Ruangan Kosong") + "<br/>");
                    sb.Append("Cahaya :" + MyRoom.Light + "<br/>");
                    //you can put some logic to test threshold here...
                    WriteMessage(sb.ToString());
                    break;
            }
        }
        public IOTHub()
        {
            if (client == null)
            {
                // create client instance 
                client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

                string clientId = Guid.NewGuid().ToString();
                client.Connect(clientId, "guest", "guest");

                SubscribeMessage();
            }
        }
        [HubMethodName("SetAlarm")]
        public void SetAlarm(bool State)
        {
            string Pesan = State ? "ALARM:ON" : "ALARM:OFF";
            client.Publish("/home/control", Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }

        internal static void WriteMessage(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
            dynamic allClients = context.Clients.All.WriteData(message);
        }
    }
}