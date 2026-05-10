using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParchePlanAPI.Models;

public class PlanOption
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid IdPlanOption { get; set; }

    [Required]
    public Guid PlanId { get; set; }
    [ForeignKey("PlanId")]
    public Plan? Plan { get; set; }

    public string Place { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}
