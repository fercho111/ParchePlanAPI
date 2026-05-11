using ParchePlanAPI.Models.DTOs;

namespace ParchePlanAPI.Interfaces;

public interface IParcheService
{
    Task<List<ParcheResponseDTO>> GetParchesForUser(string userId);
    Task<(ParcheResponseDTO? Parche, string? Error)> CreateParche(string userId, CreateParcheDTO dto);
    Task<(ParcheResponseDTO? Parche, string? Error)> JoinParche(string userId, JoinParcheDTO dto);
    Task<(bool Success, string? Error)> UpdateMemberRole(string callerId, Guid parcheId, string targetUserId, UpdateMemberRoleDTO dto);
    Task<(ParcheResponseDTO? Parche, string? Error)> UpdateParche(string callerId, Guid parcheId, UpdateParcheDTO dto);
    Task<(List<MemberRankingDTO>? Rankings, string? Error)> GetRankings(string userId, Guid parcheId);
}
