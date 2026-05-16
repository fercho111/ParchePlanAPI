namespace ParchePlanAPI.Models.DTOs
{
    public class PlanOptionResponseDTO
    {
        public Guid IdOption { get; set; }
        public string Place { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int VoteCount { get; set; }
    }
}