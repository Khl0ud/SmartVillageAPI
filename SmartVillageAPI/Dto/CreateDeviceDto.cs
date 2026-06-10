using SmartVillageAPI.Model;

public class CreateDeviceDto
{
    public string Name { get; set; }
    public DeviceType Type { get; set; }
    public int ZoneId { get; set; }
}