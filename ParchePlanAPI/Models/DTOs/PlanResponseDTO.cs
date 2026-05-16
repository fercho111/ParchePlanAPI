using ParchePlanAPI.Models.DTOs;

public class PlanResponseDTO
{
    public Guid IdPlan { get; set; }
    public Guid ParcheId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime VotingDeadline { get; set; }
    public DateTime CheckInStart { get; set; }
    public DateTime CheckInEnd { get; set; }

    public Guid? WinningOptionId { get; set; }

    public List<PlanOptionResponseDTO> Options { get; set; } = new();
    public List<PlanAttendanceResponseDTO> Attendance { get; set; } = new();

    public Guid? CurrentUserVoteOptionId { get; set; }
    public string? CurrentUserAttendanceStatus { get; set; }
    public bool CurrentUserCheckedIn { get; set; }
}