using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParchePlanAPI.Models;

public class ParcheMember
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid IdParcheMember { get; set; }

    [Required]
    public Guid ParcheId { get; set; }
    [ForeignKey("ParcheId")]
    public Parche? Parche { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public ParcheRoleEnum Role { get; set; }
}
