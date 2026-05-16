namespace ParchePlanAPI.Models.DTOs
{
    public class PlanAttendanceResponseDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool CheckedIn { get; set; }
    }
}