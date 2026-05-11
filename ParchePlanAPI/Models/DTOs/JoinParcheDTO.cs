using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class JoinParcheDTO
    {
        [Required]
        public string InviteCode { get; set; }
    }
}
