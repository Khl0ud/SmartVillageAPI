namespace SmartVillageAPI.Services
{
    public interface IMqttService
    {
        Task PublishAsync(string topic, string payload);
    }
}