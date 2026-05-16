using Microsoft.EntityFrameworkCore;
using ParchePlanAPI.Interfaces;
using ParchePlanAPI.Models;
using ParchePlanAPI.Models.DTOs;
using ParchePlanAPI.Persistence;

namespace ParchePlanAPI.Services;

// Service that handles all Parche-related business logic.
// This is where validations, queries, and data manipulation happen.
public class ParcheService : IParcheService
{
    private readonly ApplicationDbContext _context;

    public ParcheService(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET all parches where the current user is a member.
    // For each parche, we also include the full member list with user info.
    public async Task<List<ParcheResponseDTO>> GetParchesForUser(string userId)
    {
        // Step 1: Find all parche IDs where this user is a member
        var parcheIds = await _context.ParcheMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ParcheId)
            .ToListAsync();

        // Step 2: Load those parches
        var parches = await _context.Parches
            .Where(p => parcheIds.Contains(p.IdParche))
            .ToListAsync();

        // Step 3: Load all members for those parches, including user info
        var allMembers = await _context.ParcheMembers
            .Where(pm => parcheIds.Contains(pm.ParcheId))
            .Include(pm => pm.User)
            .ToListAsync();

        // Step 4: Build the response DTOs
        var result = new List<ParcheResponseDTO>();

        foreach (var parche in parches)
        {
            var membersForThisParche = allMembers
                .Where(pm => pm.ParcheId == parche.IdParche)
                .Select(MapMemberToResponseDTO)
                .ToList();

            result.Add(new ParcheResponseDTO
            {
                IdParche = parche.IdParche,
                Name = parche.Name,
                Description = parche.Description,
                CoverImageUrl = parche.CoverImageUrl,
                InviteCode = parche.InviteCode,
                Members = membersForThisParche
            });
        }

        return result;
    }

    // POST - Create a new Parche.
    // Validates input, generates a unique invite code, saves the parche,
    // and adds the caller as the Owner.
    public async Task<(ParcheResponseDTO? Parche, string? Error)> CreateParche(
        string userId,
        CreateParcheDTO dto
    )
    {
        var parche = new Parche
        {
            Name = dto.Name,
            Description = dto.Description,
            CoverImageUrl = dto.CoverImageUrl,
            InviteCode = GenerateInviteCode()
        };

        _context.Parches.Add(parche);
        await _context.SaveChangesAsync();

        var ownerMember = new ParcheMember
        {
            ParcheId = parche.IdParche,
            UserId = userId,
            Role = ParcheRoleEnum.Owner
        };

        _context.ParcheMembers.Add(ownerMember);
        await _context.SaveChangesAsync();

        var memberWithUser = await _context.ParcheMembers
            .Where(pm => pm.ParcheId == parche.IdParche && pm.UserId == userId)
            .Include(pm => pm.User)
            .FirstOrDefaultAsync();

        var response = new ParcheResponseDTO
        {
            IdParche = parche.IdParche,
            Name = parche.Name,
            Description = parche.Description,
            CoverImageUrl = parche.CoverImageUrl,
            InviteCode = parche.InviteCode,
            Members = memberWithUser != null
                ? new List<ParcheMemberResponseDTO>
                {
                    MapMemberToResponseDTO(memberWithUser)
                }
                : new List<ParcheMemberResponseDTO>
                {
                    new ParcheMemberResponseDTO
                    {
                        UserId = userId,
                        FullName = string.Empty,
                        Email = string.Empty,
                        AvatarUrl = null,
                        Role = ParcheRoleEnum.Owner.ToString()
                    }
                }
        };

        return (response, null);
    }

    // POST - Join a parche using an invite code.
    // Finds the parche by code, checks the user isn't already a member,
    // and adds them with the Member role.
    public async Task<(ParcheResponseDTO? Parche, string? Error)> JoinParche(
        string userId,
        JoinParcheDTO dto
    )
    {
        var inviteCode = dto.InviteCode.Trim();

        var parche = await _context.Parches
            .FirstOrDefaultAsync(p => p.InviteCode.ToLower() == inviteCode.ToLower());

        if (parche == null)
        {
            return (null, "Invalid invite code. No parche found.");
        }

        var existingMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == parche.IdParche && pm.UserId == userId);

        if (existingMember != null)
        {
            return (null, "You are already a member of this parche.");
        }

        var newMember = new ParcheMember
        {
            ParcheId = parche.IdParche,
            UserId = userId,
            Role = ParcheRoleEnum.Member
        };

        _context.ParcheMembers.Add(newMember);
        await _context.SaveChangesAsync();

        var allMembers = await _context.ParcheMembers
            .Where(pm => pm.ParcheId == parche.IdParche)
            .Include(pm => pm.User)
            .ToListAsync();

        var response = new ParcheResponseDTO
        {
            IdParche = parche.IdParche,
            Name = parche.Name,
            Description = parche.Description,
            CoverImageUrl = parche.CoverImageUrl,
            InviteCode = parche.InviteCode,
            Members = allMembers.Select(MapMemberToResponseDTO).ToList()
        };

        return (response, null);
    }

    // PATCH - Update a member's role in a parche.
    // Validates: caller is Owner, target member exists, target isn't Owner,
    // and the new role is valid (Moderator or Member).
    public async Task<(bool Success, string? Error)> UpdateMemberRole(
        string callerId,
        Guid parcheId,
        string targetUserId,
        UpdateMemberRoleDTO dto
    )
    {
        var callerMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == parcheId && pm.UserId == callerId);

        if (callerMember == null)
        {
            return (false, "You are not a member of this parche.");
        }

        if (callerMember.Role != ParcheRoleEnum.Owner)
        {
            return (false, "Only the Owner can change member roles.");
        }

        var targetMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == parcheId && pm.UserId == targetUserId);

        if (targetMember == null)
        {
            return (false, "Target user is not a member of this parche.");
        }

        if (targetMember.Role == ParcheRoleEnum.Owner)
        {
            return (false, "Cannot change the role of an Owner.");
        }

        if (!Enum.TryParse<ParcheRoleEnum>(dto.Role, ignoreCase: true, out var newRole))
        {
            return (false, "Invalid role. Valid roles are: Moderator, Member.");
        }

        if (newRole == ParcheRoleEnum.Owner)
        {
            return (false, "Cannot assign the Owner role.");
        }

        targetMember.Role = newRole;
        await _context.SaveChangesAsync();

        return (true, null);
    }

    // PATCH - Update a parche's info.
    // Only the Owner can edit the parche.
    // Updates name, description, and cover image URL.
    public async Task<(ParcheResponseDTO? Parche, string? Error)> UpdateParche(
        string callerId,
        Guid parcheId,
        UpdateParcheDTO dto
    )
    {
        var parche = await _context.Parches.FindAsync(parcheId);

        if (parche == null)
        {
            return (null, "Parche not found.");
        }

        var callerMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == parcheId && pm.UserId == callerId);

        if (callerMember == null)
        {
            return (null, "You are not a member of this parche.");
        }

        if (callerMember.Role != ParcheRoleEnum.Owner)
        {
            return (null, "Only the Owner can edit the parche.");
        }

        parche.Name = dto.Name;
        parche.Description = dto.Description;
        parche.CoverImageUrl = dto.CoverImageUrl;

        await _context.SaveChangesAsync();

        var allMembers = await _context.ParcheMembers
            .Where(pm => pm.ParcheId == parcheId)
            .Include(pm => pm.User)
            .ToListAsync();

        var response = new ParcheResponseDTO
        {
            IdParche = parche.IdParche,
            Name = parche.Name,
            Description = parche.Description,
            CoverImageUrl = parche.CoverImageUrl,
            InviteCode = parche.InviteCode,
            Members = allMembers.Select(MapMemberToResponseDTO).ToList()
        };

        return (response, null);
    }

    public async Task<(List<MemberRankingDTO>? Rankings, string? Error)> GetRankings(
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

        var members = await _context.ParcheMembers
            .Where(pm => pm.ParcheId == parcheId)
            .Include(pm => pm.User)
            .ToListAsync();

        var scheduledPlans = await _context.Plans
            .Where(p => p.ParcheId == parcheId && p.State == PlanStateEnum.Scheduled)
            .ToListAsync();

        var scheduledPlanIds = scheduledPlans
            .Select(p => p.IdPlan)
            .ToList();

        var ghostAttendances = await _context.Attendances
            .Where(a =>
                scheduledPlanIds.Contains(a.PlanId) &&
                a.Status == AttendanceStatusEnum.Yes &&
                !a.CheckedIn
            )
            .ToListAsync();

        var rankings = new List<MemberRankingDTO>();

        foreach (var member in members)
        {
            var organizerScore = scheduledPlans
                .Count(p => p.CreatedBy == member.UserId);

            var ghostScore = ghostAttendances
                .Count(a => a.UserId == member.UserId);

            rankings.Add(new MemberRankingDTO
            {
                UserId = member.UserId,
                FullName = member.User != null ? member.User.FullName : string.Empty,
                Email = member.User != null ? member.User.Email : string.Empty,
                OrganizerScore = organizerScore,
                GhostScore = ghostScore
            });
        }

        return (rankings, null);
    }

    // Generates a random 8-character alphanumeric invite code.
    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[8];

        for (var i = 0; i < code.Length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }

    private static ParcheMemberResponseDTO MapMemberToResponseDTO(ParcheMember member)
    {
        return new ParcheMemberResponseDTO
        {
            UserId = member.UserId,
            FullName = member.User != null ? member.User.FullName : string.Empty,
            Email = member.User != null ? member.User.Email : string.Empty,
            AvatarUrl = member.User != null ? member.User.AvatarUrl : null,
            Role = member.Role.ToString()
        };
    }
}