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

        // Step 2: For each parche, load the parche data and its members
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
            // Get the members that belong to this specific parche
            var membersForThisParche = allMembers
                .Where(pm => pm.ParcheId == parche.IdParche)
                .Select(pm => new ParcheMemberResponseDTO
                {
                    UserId = pm.UserId,
                    FullName = pm.User != null ? pm.User.FullName : "",
                    Email = pm.User != null ? pm.User.Email : "",
                    Role = pm.Role.ToString()
                })
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
    public async Task<(ParcheResponseDTO? Parche, string? Error)> CreateParche(string userId, CreateParcheDTO dto)
    {
        // Step 1: Create the Parche entity with an auto-generated invite code
        var parche = new Parche
        {
            Name = dto.Name,
            Description = dto.Description,
            CoverImageUrl = dto.CoverImageUrl,
            InviteCode = GenerateInviteCode()
        };

        // Step 2: Save the parche to the database
        _context.Parches.Add(parche);
        await _context.SaveChangesAsync();

        // Step 3: Add the caller as the Owner of this new parche
        var ownerMember = new ParcheMember
        {
            ParcheId = parche.IdParche,
            UserId = userId,
            Role = ParcheRoleEnum.Owner
        };

        _context.ParcheMembers.Add(ownerMember);
        await _context.SaveChangesAsync();

        // Step 4: Load the user info so we can return it in the response
        var user = await _context.Users.FindAsync(userId);

        // Step 5: Build and return the response DTO
        var response = new ParcheResponseDTO
        {
            IdParche = parche.IdParche,
            Name = parche.Name,
            Description = parche.Description,
            CoverImageUrl = parche.CoverImageUrl,
            InviteCode = parche.InviteCode,
            Members = new List<ParcheMemberResponseDTO>
            {
                new ParcheMemberResponseDTO
                {
                    UserId = userId,
                    FullName = user != null ? user.FullName : "",
                    Email = user != null ? user.Email : "",
                    Role = ParcheRoleEnum.Owner.ToString()
                }
            }
        };

        return (response, null);
    }

    // POST - Join a parche using an invite code.
    // Finds the parche by code, checks the user isn't already a member,
    // and adds them with the Member role.
    public async Task<(ParcheResponseDTO? Parche, string? Error)> JoinParche(string userId, JoinParcheDTO dto)
    {
        // Step 1: Find the parche by invite code
        var parche = await _context.Parches
            .FirstOrDefaultAsync(p => p.InviteCode == dto.InviteCode);

        if (parche == null)
        {
            return (null, "Invalid invite code. No parche found.");
        }

        // Step 2: Check if the user is already a member of this parche
        var existingMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == parche.IdParche && pm.UserId == userId);

        if (existingMember != null)
        {
            return (null, "You are already a member of this parche.");
        }

        // Step 3: Add the user as a Member
        var newMember = new ParcheMember
        {
            ParcheId = parche.IdParche,
            UserId = userId,
            Role = ParcheRoleEnum.Member
        };

        _context.ParcheMembers.Add(newMember);
        await _context.SaveChangesAsync();

        // Step 4: Load all members for the response (including the new one)
        var allMembers = await _context.ParcheMembers
            .Where(pm => pm.ParcheId == parche.IdParche)
            .Include(pm => pm.User)
            .ToListAsync();

        // Step 5: Build the response
        var response = new ParcheResponseDTO
        {
            IdParche = parche.IdParche,
            Name = parche.Name,
            Description = parche.Description,
            CoverImageUrl = parche.CoverImageUrl,
            InviteCode = parche.InviteCode,
            Members = allMembers.Select(pm => new ParcheMemberResponseDTO
            {
                UserId = pm.UserId,
                FullName = pm.User != null ? pm.User.FullName : "",
                Email = pm.User != null ? pm.User.Email : "",
                Role = pm.Role.ToString()
            }).ToList()
        };

        return (response, null);
    }
    
    // PATCH - Update a member's role in a parche.
    // Validates: caller is Owner, target member exists, target isn't Owner,
    // and the new role is valid (Moderator or Member).
    public async Task<(bool Success, string? Error)> UpdateMemberRole(string callerId, Guid parcheId, string targetUserId, UpdateMemberRoleDTO dto)
    {
        // Step 1: Verify the caller is an Owner of this parche
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

        // Step 2: Find the target member
        var targetMember = await _context.ParcheMembers
            .FirstOrDefaultAsync(pm => pm.ParcheId == parcheId && pm.UserId == targetUserId);

        if (targetMember == null)
        {
            return (false, "Target user is not a member of this parche.");
        }

        // Step 3: Verify the target isn't an Owner (can't change an Owner's role)
        if (targetMember.Role == ParcheRoleEnum.Owner)
        {
            return (false, "Cannot change the role of an Owner.");
        }

        // Step 4: Parse and validate the new role
        // Only "Moderator" and "Member" are valid assignments
        if (!Enum.TryParse<ParcheRoleEnum>(dto.Role, ignoreCase: true, out var newRole))
        {
            return (false, "Invalid role. Valid roles are: Moderator, Member.");
        }

        // Don't allow assigning Owner role through this endpoint
        if (newRole == ParcheRoleEnum.Owner)
        {
            return (false, "Cannot assign the Owner role.");
        }

        // Step 5: Update the role and save
        targetMember.Role = newRole;
        await _context.SaveChangesAsync();

        return (true, null);
    }

    // <summary>
    // PATCH - Update a parche's info.
    // Only the Owner can edit the parche.
    // Updates name, description, and cover image URL.
    public async Task<(ParcheResponseDTO? Parche, string? Error)> UpdateParche(string callerId, Guid parcheId, UpdateParcheDTO dto)
    {
        // Step 1: Check that the parche exists
        var parche = await _context.Parches.FindAsync(parcheId);

        if (parche == null)
        {
            return (null, "Parche not found.");
        }

        // Step 2: Verify the caller is the Owner of this parche
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

        // Step 3: Update the parche fields
        parche.Name = dto.Name;
        parche.Description = dto.Description;
        parche.CoverImageUrl = dto.CoverImageUrl;

        // Step 4: Save changes
        await _context.SaveChangesAsync();

        // Step 5: Load all members for the response
        var allMembers = await _context.ParcheMembers
            .Where(pm => pm.ParcheId == parcheId)
            .Include(pm => pm.User)
            .ToListAsync();

        // Step 6: Build and return the response
        var response = new ParcheResponseDTO
        {
            IdParche = parche.IdParche,
            Name = parche.Name,
            Description = parche.Description,
            CoverImageUrl = parche.CoverImageUrl,
            InviteCode = parche.InviteCode,
            Members = allMembers.Select(pm => new ParcheMemberResponseDTO
            {
                UserId = pm.UserId,
                FullName = pm.User != null ? pm.User.FullName : "",
                Email = pm.User != null ? pm.User.Email : "",
                Role = pm.Role.ToString()
            }).ToList()
        };

        return (response, null);
    }

    // Generates a random 8-character alphanumeric invite code.
    // Simple and readable for sharing.
    private string GenerateInviteCode()
    {
        // Use a mix of uppercase letters and digits for readability
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[8];

        for (int i = 0; i < 8; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }
}
