using ParchePlanAPI.Models.DTOs;

namespace ParchePlanAPI.Interfaces;

public interface IPlanService
{
    Task<(List<PlanResponseDTO>? Plans, string? Error)> GetPlansForParche(string userId, Guid parcheId);
    Task<(PlanResponseDTO? Plan, string? Error)> CreatePlan(string userId, CreatePlanDTO dto);
    Task<(PlanResponseDTO? Plan, string? Error)> AdvanceState(string userId, Guid planId);
    Task<(bool Success, string? Error)> CastVote(string userId, Guid planId, CastVoteDTO dto);
    Task<(bool Success, string? Error)> UpsertAttendance(string userId, Guid planId, UpsertAttendanceDTO dto);
    Task<(bool Success, string? Error)> CheckIn(string userId, Guid planId);
}
