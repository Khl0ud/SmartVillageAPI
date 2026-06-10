using MQTTnet;
using System.Text;

namespace SmartVillageAPI.Services
{
    public class MqttService : IMqttService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;

        public MqttService()
        {
            // ✅ التعديل هنا: استخدام MqttClientFactory عشان إنت شغال بـ MQTTnet v5
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            // إعدادات الاتصال بالسيرفر
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com", 1883)
                .WithClientId($"SmartVillageAPI_{Guid.NewGuid()}")
                .Build();
        }

        public async Task PublishAsync(string topic, string payload)
        {
            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ConnectAsync(_options);
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
        }
    }
}