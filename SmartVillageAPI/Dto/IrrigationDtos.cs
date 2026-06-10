using SmartVillageAPI.Model;

namespace SmartVillageAPI.Dto
{
    // بيرجع للموبايل بيانات الزون
    public class IrrigationZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PlantType { get; set; }
        public double MoistureThreshold { get; set; }
        public double CurrentSoilMoisture { get; set; }
        public string ValveStatus { get; set; }
        public bool IsAutoMode { get; set; }
        public DateTime? LastIrrigatedAt { get; set; }
    }

    // الموبايل بيبعت ده لما يحفظ الإعدادات
    public class UpdateIrrigationSettingsDto
    {
        public PlantType PlantType { get; set; }
        public double MoistureThreshold { get; set; }
        public bool IsAutoMode { get; set; }
    }

    // الموبايل بيبعت ده لما يضغط Start/Stop Irrigation
    public class ControlIrrigationDto
    {
        // "START" أو "STOP"
        public string Action { get; set; }
    }

    // بيرجع للموبايل بيانات الـ Log
    public class IrrigationLogDto
    {
        public int Id { get; set; }
        public string ZoneName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public double? WaterUsedLiters { get; set; }
        public double SoilMoistureBeforeIrrigation { get; set; }
        public double? SoilMoistureAfterIrrigation { get; set; }
        public bool IsAutomatic { get; set; }
        public string Duration { get; set; } // "5 min 30 sec"
    }

    // الـ AI Recommendation Response
    public class AiRecommendationDto
    {
        public string Recommendation { get; set; }
        public string BestTimeToIrrigate { get; set; }
        public double EcoScore { get; set; }       // نسبة توفير المياه %
        public double WaterSavingsPercent { get; set; }
    }

    // Dashboard summary
    public class IrrigationDashboardDto
    {
        public double AverageSoilMoisture { get; set; }
        public AiRecommendationDto AiRecommendation { get; set; }
        public List<IrrigationZoneDto> Zones { get; set; }
    }
}
