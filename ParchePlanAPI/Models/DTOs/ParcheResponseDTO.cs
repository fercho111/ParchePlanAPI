namespace ParchePlanAPI.Models.DTOs
{
    public class ParcheResponseDTO
    {
        public Guid IdParche { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public string InviteCode { get; set; }

        // List of members belonging to this Parche
        public List<ParcheMemberResponseDTO> Members { get; set; }
    }
}
