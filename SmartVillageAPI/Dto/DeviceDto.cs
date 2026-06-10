namespace SmartVillageAPI.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Sensor or Actuator
        public string CurrentState { get; set; } // ON, OFF, 25°C, etc.
        public bool IsActive { get; set; }
    }
}