namespace SmartVillageAPI.Model
{
    public enum DeviceType
    {
        Sensor,    // حساسات (حرارة، رطوبة، مسافة، غاز، مطر، إضاءة)
        Actuator,  // أجهزة تنفيذية (موتور، ريلاي، لمبة، سيرفو، بازر)
        Camera,    // كاميرات المراقبة
        RFID       // قارئ الكروت الذكية
    }
}
