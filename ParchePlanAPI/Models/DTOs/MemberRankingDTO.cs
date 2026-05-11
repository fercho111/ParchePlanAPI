namespace ParchePlanAPI.Models.DTOs
{
    public class MemberRankingDTO
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int OrganizerScore { get; set; }
        public int GhostScore { get; set; }
    }
}
