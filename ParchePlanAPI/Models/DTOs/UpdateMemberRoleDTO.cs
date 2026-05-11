using System.ComponentModel.DataAnnotations;

namespace ParchePlanAPI.Models.DTOs
{
    public class UpdateMemberRoleDTO
    {
        [Required]
        public string Role { get; set; }
    }
}
