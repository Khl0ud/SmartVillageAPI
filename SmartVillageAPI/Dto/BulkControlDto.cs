namespace SmartVillageAPI.Dto
{
    public class BulkControlDto
    {
        public List<int> DeviceIds { get; set; } // ليستة بأرقام الأجهزة اللي عايزين نتحكم فيها
        public string NewState { get; set; }     // الحالة الجديدة (مثلاً "ON", "OFF", "Locked")
    }
}
