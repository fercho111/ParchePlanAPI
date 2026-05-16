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

    public async Task<(List<PlanResponseDTO>? Plans, string? Error)> GetPlansForParche(
        string userId,
        Guid parcheId
    )
    {
        var isMember = await _context.ParcheMembers
            .AnyAsync(pm => pm.ParcheId == parcheId && pm.UserId == userId);

        if (!isMember)
        {
            return (null, "You are not a member of this parche.");
        }

        var plans = await _context.Plans
            .Where(p => p.ParcheId == parcheId)
            .OrderByDescending(p => p.DateStart)
            .ToListAsync();

        var response = new List<PlanResponseDTO>();

        foreach (var plan in plans)
        {
            response.Add(await MapPlanToResponseDTO(plan, userId));
        }

        return (response, null);
    }

    public async Task<(PlanResponseDTO? Plan, string? Error)> CreatePlan(
        string userId,
        CreatePlanDTO dto
    )
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

        if (dto.VotingDeadline >= dto.DateStart)
        {
            return (null, "VotingDeadline must be before DateStart.");
        }

        if (dto.Options == null || dto.Options.Count < 3)
        {
            return (null, "At least 3 options are required.");
        }

        foreach (var option in dto.Options)
        {
            if (
                string.IsNullOrWhiteSpace(option.Place) ||
                string.IsNullOrWhiteSpace(option.Time)
            )
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

        var createdOptions = dto.Options.Select(optionDto => new PlanOption
        {
            PlanId = plan.IdPlan,
            Place = optionDto.Place,
            Time = optionDto.Time
        }).ToList();

        _context.PlanOptions.AddRange(createdOptions);
        await _context.SaveChangesAsync();

        var response = await MapPlanToResponseDTO(plan, userId);

        return (response, null);
    }

    public async Task<(PlanResponseDTO? Plan, string? Error)> AdvanceState(
        string userId,
        Guid planId
    )
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (null, "Plan not found.");
        }

        var callerMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm =>
                pm.ParcheId == plan.ParcheId &&
                pm.UserId == userId
            );

        if (callerMember == null)
        {
            return (null, "You are not a member of this parche.");
        }

        if (
            callerMember.Role != ParcheRoleEnum.Owner &&
            callerMember.Role != ParcheRoleEnum.Moderator
        )
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

        var response = await MapPlanToResponseDTO(plan, userId);

        return (response, null);
    }

    private async Task<PlanResponseDTO> MapPlanToResponseDTO(
        Plan plan,
        string userId
    )
    {
        var options = await _context.PlanOptions
            .Where(option => option.PlanId == plan.IdPlan)
            .ToListAsync();

        var orderedOptions = options
            .OrderBy(option => ParseOptionTime(option.Time))
            .ThenBy(option => option.IdPlanOption)
            .ToList();

        var attendanceList = await _context.Attendances
            .Where(attendance => attendance.PlanId == plan.IdPlan)
            .Include(attendance => attendance.User)
            .ToListAsync();

        var optionIds = options
            .Select(option => option.IdPlanOption)
            .ToList();

        var voteCounts = await _context.Votes
            .Where(vote => optionIds.Contains(vote.OptionId))
            .GroupBy(vote => vote.OptionId)
            .Select(group => new
            {
                OptionId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.OptionId,
                item => item.Count
            );

        var currentUserVoteOptionId = await _context.Votes
            .Where(vote =>
                vote.UserId == userId &&
                optionIds.Contains(vote.OptionId)
            )
            .Select(vote => (Guid?)vote.OptionId)
            .FirstOrDefaultAsync();

        var currentUserAttendance = await _context.Attendances
            .FirstOrDefaultAsync(attendance =>
                attendance.PlanId == plan.IdPlan &&
                attendance.UserId == userId
            );

        var winningOptionId =
            plan.State == PlanStateEnum.VotingClosed ||
            plan.State == PlanStateEnum.Scheduled
                ? GetWinningOptionId(orderedOptions, voteCounts)
                : null;

        return new PlanResponseDTO
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
            WinningOptionId = winningOptionId,

            Options = orderedOptions.Select(option => new PlanOptionResponseDTO
            {
                IdOption = option.IdPlanOption,
                Place = option.Place,
                Time = option.Time,
                VoteCount = voteCounts.TryGetValue(option.IdPlanOption, out var count)
                    ? count
                    : 0
            }).ToList(),

            CurrentUserVoteOptionId = currentUserVoteOptionId,
            CurrentUserAttendanceStatus = currentUserAttendance?.Status.ToString(),
            CurrentUserCheckedIn = currentUserAttendance?.CheckedIn ?? false,

            Attendance = attendanceList.Select(attendance => new PlanAttendanceResponseDTO
            {
                UserId = attendance.UserId,
                FullName = attendance.User != null ? attendance.User.FullName : string.Empty,
                Email = attendance.User != null ? attendance.User.Email : string.Empty,
                AvatarUrl = attendance.User != null ? attendance.User.AvatarUrl : null,
                Status = attendance.Status.ToString(),
                CheckedIn = attendance.CheckedIn
            }).ToList()
        };
    }

    private static Guid? GetWinningOptionId(
        List<PlanOption> orderedOptions,
        Dictionary<Guid, int> voteCounts
    )
    {
        if (orderedOptions.Count == 0)
        {
            return null;
        }

        Guid? winningOptionId = null;
        var maxVotes = -1;

        foreach (var option in orderedOptions)
        {
            var voteCount = voteCounts.TryGetValue(option.IdPlanOption, out var count)
                ? count
                : 0;

            if (voteCount > maxVotes)
            {
                maxVotes = voteCount;
                winningOptionId = option.IdPlanOption;
            }
        }

        return winningOptionId;
    }

    private static TimeSpan ParseOptionTime(string value)
    {
        if (TimeSpan.TryParse(value, out var time))
        {
            return time;
        }

        return TimeSpan.MaxValue;
    }

    public async Task<(bool Success, string? Error)> CastVote(
        string userId,
        Guid planId,
        CastVoteDTO dto
    )
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
            .AnyAsync(option =>
                option.IdPlanOption == dto.OptionId &&
                option.PlanId == planId
            );

        if (!optionExists)
        {
            return (false, "Option does not exist for this plan.");
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(item =>
                item.PlanId == planId &&
                item.UserId == userId
            );

        if (attendance == null || attendance.Status == AttendanceStatusEnum.No)
        {
            return (false, "You must have attendance status of Yes or Maybe to vote.");
        }

        var planOptionIds = await _context.PlanOptions
            .Where(option => option.PlanId == planId)
            .Select(option => option.IdPlanOption)
            .ToListAsync();

        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(vote =>
                vote.UserId == userId &&
                planOptionIds.Contains(vote.OptionId)
            );

        if (existingVote != null)
        {
            existingVote.OptionId = dto.OptionId;
        }
        else
        {
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

    public async Task<(bool Success, string? Error)> UpsertAttendance(
        string userId,
        Guid planId,
        UpsertAttendanceDTO dto
    )
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (false, "Plan not found.");
        }

        if (
            !Enum.TryParse<AttendanceStatusEnum>(
                dto.Status,
                ignoreCase: true,
                out var status
            )
        )
        {
            return (false, "Invalid status. Valid values are: Yes, No, Maybe.");
        }

        var existing = await _context.Attendances
            .FirstOrDefaultAsync(attendance =>
                attendance.PlanId == planId &&
                attendance.UserId == userId
            );

        if (existing != null)
        {
            if (
                existing.Status == AttendanceStatusEnum.Yes &&
                status != AttendanceStatusEnum.Yes
            )
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

    public async Task<(bool Success, string? Error)> CheckIn(
        string userId,
        Guid planId
    )
    {
        var plan = await _context.Plans.FindAsync(planId);

        if (plan == null)
        {
            return (false, "Plan not found.");
        }

        var now = DateTime.Now;

        if (now < plan.CheckInStart || now > plan.CheckInEnd)
        {
            return (false, "Check-in is not available at this time.");
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(item =>
                item.PlanId == planId &&
                item.UserId == userId
            );

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