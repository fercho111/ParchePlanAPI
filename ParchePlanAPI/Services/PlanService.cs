using Microsoft.EntityFrameworkCore;
using ParchePlanAPI.Interfaces;
using ParchePlanAPI.Models;
using ParchePlanAPI.Models.DTOs;
using ParchePlanAPI.Persistence;

namespace ParchePlanAPI.Services;

public class PlanService : IPlanService
{
    private readonly ApplicationDbContext _context;

    public PlanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<PlanResponseDTO>? Plans, string? Error)> GetPlansForParche(string userId, Guid parcheId)
    {
        var isMember = await _context.ParcheMembers
            .AnyAsync(pm => pm.ParcheId == parcheId && pm.UserId == userId);

        if (!isMember)
        {
            return (null, "You are not a member of this parche.");
        }

        var plans = await _context.Plans
            .Where(p => p.ParcheId == parcheId)
            .Select(p => new PlanResponseDTO
            {
                IdPlan = p.IdPlan,
                ParcheId = p.ParcheId,
                CreatedBy = p.CreatedBy,
                Title = p.Title,
                Description = p.Description,
                DateStart = p.DateStart,
                DateEnd = p.DateEnd,
                State = p.State.ToString(),
                VotingDeadline = p.VotingDeadline,
                CheckInStart = p.CheckInStart,
                CheckInEnd = p.CheckInEnd,
                Options = _context.PlanOptions
                    .Where(o => o.PlanId == p.IdPlan)
                    .Select(o => new PlanOptionResponseDTO
                    {
                        IdPlanOption = o.IdPlanOption,
                        Place = o.Place,
                        Time = o.Time
                    }).ToList()
            })
            .ToListAsync();

        return (plans, null);
    }

    public async Task<(PlanResponseDTO? Plan, string? Error)> CreatePlan(string userId, CreatePlanDTO dto)
    {
        var isMember = await _context.ParcheMembers
            .AnyAsync(pm => pm.ParcheId == dto.ParcheId && pm.UserId == userId);

        if (!isMember)
        {
            return (null, "You are not a member of this parche.");
        }

        if (dto.DateEnd < dto.DateStart)
        {
            return (null, "DateEnd must be greater than or equal to DateStart.");
        }

        if (dto.VotingDeadline < dto.DateStart || dto.VotingDeadline > dto.DateEnd)
        {
            return (null, "VotingDeadline must be between DateStart and DateEnd.");
        }

        if (dto.Options == null || dto.Options.Count < 3)
        {
            return (null, "At least 3 options are required.");
        }

        foreach (var option in dto.Options)
        {
            if (string.IsNullOrWhiteSpace(option.Place) || string.IsNullOrWhiteSpace(option.Time))
            {
                return (null, "Each option must have a place and time.");
            }
        }

        var checkInStart = dto.DateStart.Date.AddHours(18);
        var checkInEnd = dto.DateStart.Date.AddHours(23);

        var plan = new Plan
        {
            ParcheId = dto.ParcheId,
            CreatedBy = userId,
            Title = dto.Title,
            Description = dto.Description,
            DateStart = dto.DateStart,
            DateEnd = dto.DateEnd,
            State = PlanStateEnum.Draft,
            VotingDeadline = dto.VotingDeadline,
            CheckInStart = checkInStart,
            CheckInEnd = checkInEnd
        };

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        foreach (var optionDto in dto.Options)
        {
            var planOption = new PlanOption
            {
                PlanId = plan.IdPlan,
                Place = optionDto.Place,
                Time = optionDto.Time
            };
            _context.PlanOptions.Add(planOption);
        }

        await _context.SaveChangesAsync();

        var savedOptions = await _context.PlanOptions
            .Where(o => o.PlanId == plan.IdPlan)
            .Select(o => new PlanOptionResponseDTO
            {
                IdPlanOption = o.IdPlanOption,
                Place = o.Place,
                Time = o.Time
            }).ToListAsync();

        var response = new PlanResponseDTO
        {
            IdPlan = plan.IdPlan,
            ParcheId = plan.ParcheId,
            CreatedBy = plan.CreatedBy,
            Title = plan.Title,
            Description = plan.Description,
            DateStart = plan.DateStart,
            DateEnd = plan.DateEnd,
            State = plan.State.ToString(),
            VotingDeadline = plan.VotingDeadline,
            CheckInStart = plan.CheckInStart,
            CheckInEnd = plan.CheckInEnd,
            Options = savedOptions
        };

        return (response, null);
    }

    public async Task<(PlanResponseDTO? Plan, string? Error)> AdvanceState(string userId, Guid planId)
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (null, "Plan not found.");
        }

        var callerMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == plan.ParcheId && pm.UserId == userId);

        if (callerMember == null)
        {
            return (null, "You are not a member of this parche.");
        }

        if (callerMember.Role != ParcheRoleEnum.Owner && callerMember.Role != ParcheRoleEnum.Moderator)
        {
            return (null, "Only Owner or Moderator can advance the plan state.");
        }

        switch (plan.State)
        {
            case PlanStateEnum.Draft:
                plan.State = PlanStateEnum.VotingOpen;
                break;

            case PlanStateEnum.VotingOpen:
                plan.State = PlanStateEnum.VotingClosed;
                await TallyVotesAndSetWinner(plan);
                break;

            case PlanStateEnum.VotingClosed:
                plan.State = PlanStateEnum.Scheduled;
                break;

            case PlanStateEnum.Scheduled:
                return (null, "Plan is already in its final state (Scheduled).");

            default:
                return (null, "Invalid plan state.");
        }

        await _context.SaveChangesAsync();

        var advanceOptions = await _context.PlanOptions
            .Where(o => o.PlanId == plan.IdPlan)
            .Select(o => new PlanOptionResponseDTO
            {
                IdPlanOption = o.IdPlanOption,
                Place = o.Place,
                Time = o.Time
            }).ToListAsync();

        var response = new PlanResponseDTO
        {
            IdPlan = plan.IdPlan,
            ParcheId = plan.ParcheId,
            CreatedBy = plan.CreatedBy,
            Title = plan.Title,
            Description = plan.Description,
            DateStart = plan.DateStart,
            DateEnd = plan.DateEnd,
            State = plan.State.ToString(),
            VotingDeadline = plan.VotingDeadline,
            CheckInStart = plan.CheckInStart,
            CheckInEnd = plan.CheckInEnd,
            Options = advanceOptions
        };

        return (response, null);
    }
    
    private async Task TallyVotesAndSetWinner(Plan plan)
    {
        var options = await _context.PlanOptions
            .Where(o => o.PlanId == plan.IdPlan)
            .ToListAsync();

        if (options.Count == 0)
        {
            return;
        }

        var voteCounts = await _context.Votes
            .Where(v => options.Select(o => o.IdPlanOption).Contains(v.OptionId))
            .GroupBy(v => v.OptionId)
            .Select(g => new { OptionId = g.Key, Count = g.Count() })
            .ToListAsync();
        
        PlanOption? winner = null;
        int maxVotes = -1;

        foreach (var option in options.OrderBy(o => o.IdPlanOption))
        {
            var voteCount = voteCounts
                .FirstOrDefault(vc => vc.OptionId == option.IdPlanOption)?.Count ?? 0;

            if (voteCount > maxVotes)
            {
                maxVotes = voteCount;
                winner = option;
            }
        }
    }

    public async Task<(bool Success, string? Error)> CastVote(string userId, Guid planId, CastVoteDTO dto)
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (false, "Plan not found.");
        }

        if (plan.State != PlanStateEnum.VotingOpen)
        {
            return (false, "Voting is not open for this plan.");
        }

        var optionExists = await _context.PlanOptions
            .AnyAsync(o => o.IdPlanOption == dto.OptionId && o.PlanId == planId);

        if (!optionExists)
        {
            return (false, "Option does not exist for this plan.");
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.PlanId == planId && a.UserId == userId);

        if (attendance == null || attendance.Status == AttendanceStatusEnum.No)
        {
            return (false, "You must have attendance status of Yes or Maybe to vote.");
        }

        // Upsert: one vote per user per plan
        // Find any existing vote by this user on any option of this plan
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId &&
                _context.PlanOptions.Where(o => o.PlanId == planId).Select(o => o.IdPlanOption).Contains(v.OptionId));

        if (existingVote != null)
        {
            // Update existing vote to the new option
            existingVote.OptionId = dto.OptionId;
        }
        else
        {
            // Create new vote
            var vote = new Vote
            {
                UserId = userId,
                OptionId = dto.OptionId
            };
            _context.Votes.Add(vote);
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(List<VoteResponseDTO>? Votes, string? Error)> GetVotes(string userId, Guid planId)
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (null, "Plan not found.");
        }

        var isMember = await _context.ParcheMembers
            .AnyAsync(pm => pm.ParcheId == plan.ParcheId && pm.UserId == userId);

        if (!isMember)
        {
            return (null, "You are not a member of this parche.");
        }

        var optionIds = await _context.PlanOptions
            .Where(o => o.PlanId == planId)
            .Select(o => o.IdPlanOption)
            .ToListAsync();

        var votes = await _context.Votes
            .Where(v => optionIds.Contains(v.OptionId))
            .Select(v => new VoteResponseDTO
            {
                IdVote = v.IdVote,
                UserId = v.UserId,
                OptionId = v.OptionId
            })
            .ToListAsync();

        return (votes, null);
    }

    public async Task<(bool Success, string? Error)> UpsertAttendance(string userId, Guid planId, UpsertAttendanceDTO dto)
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (false, "Plan not found.");
        }

        if (!Enum.TryParse<AttendanceStatusEnum>(dto.Status, ignoreCase: true, out var status))
        {
            return (false, "Invalid status. Valid values are: Yes, No, Maybe.");
        }

        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a => a.PlanId == planId && a.UserId == userId);

        if (existing != null)
        {
            // If changing away from Yes, reset checkedIn
            if (existing.Status == AttendanceStatusEnum.Yes && status != AttendanceStatusEnum.Yes)
            {
                existing.CheckedIn = false;
            }
            existing.Status = status;
        }
        else
        {
            var attendance = new Attendance
            {
                PlanId = planId,
                UserId = userId,
                Status = status,
                CheckedIn = false
            };
            _context.Attendances.Add(attendance);
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(List<AttendanceResponseDTO>? Attendances, string? Error)> GetAllAttendances(string userId, Guid planId)
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (null, "Plan not found.");
        }

        var isMember = await _context.ParcheMembers
            .AnyAsync(pm => pm.ParcheId == plan.ParcheId && pm.UserId == userId);

        if (!isMember)
        {
            return (null, "You are not a member of this parche.");
        }

        var attendances = await _context.Attendances
            .Where(a => a.PlanId == planId)
            .Select(a => new AttendanceResponseDTO
            {
                IdAttendance = a.IdAttendance,
                PlanId = a.PlanId,
                UserId = a.UserId,
                Status = a.Status.ToString(),
                CheckedIn = a.CheckedIn
            })
            .ToListAsync();

        return (attendances, null);
    }

    public async Task<(bool Success, string? Error)> CheckIn(string userId, Guid planId)
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (false, "Plan not found.");
        }

        // Verify current time is within check-in window
        var now = DateTime.Now;
        if (now < plan.CheckInStart || now > plan.CheckInEnd)
        {
            return (false, "Check-in is not available at this time.");
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.PlanId == planId && a.UserId == userId);

        if (attendance == null)
        {
            return (false, "You have no attendance record for this plan.");
        }

        if (attendance.Status != AttendanceStatusEnum.Yes)
        {
            return (false, "Your attendance must be 'Yes' to check in.");
        }

        if (attendance.CheckedIn)
        {
            return (false, "You have already checked in.");
        }

        attendance.CheckedIn = true;
        await _context.SaveChangesAsync();
        return (true, null);
    }
}
