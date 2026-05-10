using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParchePlanAPI.Models;

public class Attendance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid IdAttendance { get; set; }

    [Required]
    public Guid PlanId { get; set; }
    [ForeignKey("PlanId")]
    public Plan? Plan { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public AttendanceStatusEnum Status { get; set; }
    public bool CheckedIn { get; set; }
}
