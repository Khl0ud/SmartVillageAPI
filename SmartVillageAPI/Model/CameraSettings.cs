// Model/CameraSettings.cs
namespace SmartVillageAPI.Model
{
    public class CameraSettings
    {
        public string RecordingsPath { get; set; } =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "MediaRecords");
    }
}