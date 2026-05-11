using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class CreatePlanDTO
    {
        [Required]
        public Guid ParcheId { get; set; }

        [Required]
        [MinLength(4, ErrorMessage = "Title must be at least 4 characters.")]
        public string Title { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters.")]
        public string Description { get; set; }

        [Required]
        public DateTime DateStart { get; set; }

        [Required]
        public DateTime DateEnd { get; set; }

        [Required]
        public DateTime VotingDeadline { get; set; }

        [Required]
        [MinLength(3, ErrorMessage = "At least 3 options are required.")]
        public List<PlanOptionDTO> Options { get; set; }
    }
}
