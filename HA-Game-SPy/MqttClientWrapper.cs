using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;

namespace HA_Game_Spy
{
    public class MqttClientWrapper
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttOptions;

        public MqttClientWrapper(string clientId, string mqttBroker, string username, string password)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(mqttBroker)
                .WithCredentials(username, password)
                .WithCleanSession()
                .Build();
        }

        public async Task ConnectAsync()
        {
            try
            {
                await _mqttClient.ConnectAsync(_mqttOptions);
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., connection failure)
            }
        }

        public async Task PublishAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
        }

        // Additional methods as needed
    }
}
