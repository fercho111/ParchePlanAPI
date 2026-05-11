using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class CastVoteDTO
    {
        [Required]
        public Guid OptionId { get; set; }
    }
}
