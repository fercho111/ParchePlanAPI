namespace ParchePlanAPI.Models.DTOs
{
    public class ParcheMemberResponseDTO
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
