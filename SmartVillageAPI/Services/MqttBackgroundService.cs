using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using SmartVillageAPI.Hubs;
using SmartVillageAPI.Model;
using System.Text;

namespace SmartVillageAPI.Services
{
    // بنورث من BackgroundService عشان يفضل شغال في الخلفية طول ما الـ API شغال
    public class MqttBackgroundService : BackgroundService
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttOptions;

        // استخدمنا IServiceScopeFactory عشان نقدر نوصل للـ AppDbContext جوه الـ Background Service
        private readonly IServiceScopeFactory _scopeFactory;

        public MqttBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            // إعدادات الـ Broker (ممكن تستخدم Mosquitto محلي أو HiveMQ مجاني على النت)
            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com", 1883) // غيّر ده للـ Broker بتاعك
                .WithClientId($"SmartVillage_Backend_{Guid.NewGuid()}")
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1. لما رسالة تيجي من الـ ESP32
            _mqttClient.ApplicationMessageReceivedAsync += HandleIncomingMessage;

            // 2. لو النت فصل، يحاول يعمل Reconnect
            _mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine("[MQTT] Disconnected. Reconnecting...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                try { await _mqttClient.ConnectAsync(_mqttOptions, stoppingToken); } catch { }
            };

            // 3. الاتصال بالـ Broker وعمل Subscribe
            try
            {
                await _mqttClient.ConnectAsync(_mqttOptions, stoppingToken);
                Console.WriteLine("[MQTT] Connected successfully to Broker!");

                // بنسمع على أي موضوع بيبدأ بـ village/device/
                // الـ (+) معناها أي رقم (أي Device ID)
                await _mqttClient.SubscribeAsync("FadiSmartVillage2026/device/+/status");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] Connection Failed: {ex.Message}");
            }

            // يفضل شغال ميفصلش
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task HandleIncomingMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic; // مثال: village/device/5/status
            string payload = e.ApplicationMessage.ConvertPayloadToString();
            Console.WriteLine($"[MQTT] Received: Topic = {topic}, Payload = {payload}");

            try
            {
                // استخراج الـ DeviceId من الـ Topic
                var topicParts = topic.Split('/');
                if (topicParts.Length >= 3 && int.TryParse(topicParts[2], out int deviceId))
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<SmartVillageHub>>();

                        var device = await context.Devices.FindAsync(deviceId);
                        if (device != null)
                        {
                            // تحديث حالة الجهاز في الداتا بيز
                            device.CurrentState = payload;
                            await context.SaveChangesAsync();

                            // إرسال التحديث لايف للموبايل عن طريق SignalR
                            await hubContext.Clients.Group($"Zone_{device.ZoneId}").SendAsync("DeviceStateChanged", new
                            {
                                DeviceId = deviceId,
                                NewState = payload
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] Error processing message: {ex.Message}");
            }
        }
    }
}