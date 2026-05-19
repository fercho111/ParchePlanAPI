namespace ParchePlanAPI.Models.DTOs
{
    public class AttendanceResponseDTO
    {
        public Guid IdAttendance { get; set; }
        public Guid PlanId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public bool CheckedIn { get; set; }
    }
}
