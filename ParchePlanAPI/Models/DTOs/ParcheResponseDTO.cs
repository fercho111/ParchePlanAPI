namespace ParchePlanAPI.Models.DTOs
{
    public class ParcheResponseDTO
    {
        public Guid IdParche { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;

        public List<ParcheMemberResponseDTO> Members { get; set; } = new();
    }
}