using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class UpdateParcheDTO
    {
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; }

        [Required]
        [StringLength(250, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 250 characters.")]
        public string Description { get; set; }

        [Required]
        [Url(ErrorMessage = "CoverImageUrl must be a valid URL.")]
        public string CoverImageUrl { get; set; }
    }
}
