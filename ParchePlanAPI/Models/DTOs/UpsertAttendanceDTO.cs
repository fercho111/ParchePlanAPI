using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class UpsertAttendanceDTO
    {
        [Required]
        public string Status { get; set; }
    }
}
