
using SmartVillageAPI.Model;

namespace SmartVillageAPI.DTOs
{
    public class CreateSensorReadingDto
    {
        public int DeviceId { get; set; }
        public ReadingType Type { get; set; } // مثلاً 0 = Temp, 1 = Humidity
        public double Value { get; set; } // القيمة نفسها (مثلاً 25.5)
    }
}