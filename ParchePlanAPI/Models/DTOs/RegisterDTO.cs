namespace ParchePlanAPI.Models.DTOs
{
    public class RegisterDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Major { get; set; }
        public string? AvatarUrl { get; set; }
    }
}