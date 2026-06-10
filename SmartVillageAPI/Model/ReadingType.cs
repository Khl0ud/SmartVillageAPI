namespace SmartVillageAPI.Model
{
    public enum ReadingType
    {
        Temperature,  // حرارة (DHT11)
        Humidity,     // رطوبة الجو (DHT11)
        SoilMoisture, // رطوبة التربة
        GasLevel,     // مستوى الغاز
        WaterLevel,   // منسوب المياه في الخزان
        LightLevel,   // مستوى الإضاءة (LDR)
        RainLevel,    // مستوى المطر
        Distance      // المسافة (Ultrasonic للزبالة)
    }
}
