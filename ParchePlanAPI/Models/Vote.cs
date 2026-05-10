using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParchePlanAPI.Models;

public class Vote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid IdVote { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    public Guid OptionId { get; set; }
    [ForeignKey("OptionId")]
    public PlanOption? Option { get; set; }
}
