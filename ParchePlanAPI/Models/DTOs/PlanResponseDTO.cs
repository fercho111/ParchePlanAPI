namespace ParchePlanAPI.Models.DTOs
{
    public class PlanResponseDTO
    {
        public Guid IdPlan { get; set; }
        public Guid ParcheId { get; set; }
        public string CreatedBy { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public string State { get; set; }
        public DateTime VotingDeadline { get; set; }
        public DateTime CheckInStart { get; set; }
        public DateTime CheckInEnd { get; set; }
    }
}
