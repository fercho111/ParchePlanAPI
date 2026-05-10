using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParchePlanAPI.Models;

public class Plan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid IdPlan { get; set; }

    [Required]
    public Guid ParcheId { get; set; }
    [ForeignKey("ParcheId")]
    public Parche? Parche { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
    [ForeignKey("CreatedBy")]
    public User? Creator { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
    public PlanStateEnum State { get; set; } = PlanStateEnum.Draft;
    public DateTime VotingDeadline { get; set; }
    public DateTime CheckInStart { get; set; }
    public DateTime CheckInEnd { get; set; }

    public Guid? WinningOptionId { get; set; }
    [ForeignKey("WinningOptionId")]
    public PlanOption? WinningOption { get; set; }
}
