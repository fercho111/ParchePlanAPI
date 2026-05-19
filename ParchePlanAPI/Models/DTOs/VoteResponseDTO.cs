namespace ParchePlanAPI.Models.DTOs
{
    public class VoteResponseDTO
    {
        public Guid IdVote { get; set; }
        public string UserId { get; set; }
        public Guid OptionId { get; set; }
    }
}
