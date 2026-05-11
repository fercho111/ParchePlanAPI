using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class PlanOptionDTO
    {
        [Required]
        public string Place { get; set; }

        [Required]
        public string Time { get; set; }
    }
}
