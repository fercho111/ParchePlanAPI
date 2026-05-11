using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParchePlanAPI.Interfaces;
using ParchePlanAPI.Models.DTOs;

namespace ParchePlanAPI.Controllers;

/// <summary>
/// Controller for all Parche-related endpoints.
/// All endpoints require authentication (the user must be logged in).
/// </summary>
[ApiController]
[Route("api/parches")]
[Authorize]
public class ParcheController : Controller
{
    private readonly IParcheService _parcheService;
    private readonly IPlanService _planService;

    public ParcheController(IParcheService parcheService, IPlanService planService)
    {
        _parcheService = parcheService;
        _planService = planService;
    }

    /// <summary>
    /// GET /api/parches
    /// Returns all parches where the current user is a member.
    /// Each parche includes its full member list.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyParches()
    {
        // Get the current user's ID from the JWT token claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        // Call the service to get all parches for this user
        var parches = await _parcheService.GetParchesForUser(userId);

        return Ok(parches);
    }

    /// <summary>
    /// POST /api/parches
    /// Creates a new parche. Validates input via data annotations on the DTO.
    /// Auto-generates an invite code and adds the caller as Owner.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateParche([FromBody] CreateParcheDTO dto)
    {
        // ModelState handles validation from data annotations on CreateParcheDTO
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get the current user's ID from the JWT token claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        // Call the service to create the parche
        var (parche, error) = await _parcheService.CreateParche(userId, dto);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Created($"/api/parches/{parche!.IdParche}", parche);
    }

    /// <summary>
    /// POST /api/parches/join
    /// Joins a parche using an invite code.
    /// Verifies the user isn't already a member before adding them.
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> JoinParche([FromBody] JoinParcheDTO dto)
    {
        // ModelState handles validation from data annotations on JoinParcheDTO
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get the current user's ID from the JWT token claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        // Call the service to join the parche
        var (parche, error) = await _parcheService.JoinParche(userId, dto);

        if (error != null)
        {
            // If the error is about an invalid code, return 404
            if (error.Contains("No parche found"))
            {
                return NotFound(new { message = error });
            }

            // If the user is already a member, return 409 Conflict
            return Conflict(new { message = error });
        }

        return Ok(parche);
    }

    /// <summary>
    /// PATCH /api/parches/{parcheId}/members/{userId}/role
    /// Updates a member's role in a parche.
    /// Only the Owner can do this. Cannot change an Owner's role.
    /// </summary>
    [HttpPatch("{parcheId}/members/{userId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid parcheId, string userId, [FromBody] UpdateMemberRoleDTO dto)
    {
        // ModelState handles validation from data annotations on UpdateMemberRoleDTO
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get the caller's ID from the JWT token claims
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (callerId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        // Call the service to update the role
        var (success, error) = await _parcheService.UpdateMemberRole(callerId, parcheId, userId, dto);

        if (!success)
        {
            // If caller isn't Owner, return 403 Forbidden
            if (error!.Contains("Only the Owner"))
            {
                return StatusCode(403, new { message = error });
            }

            // If target not found, return 404
            if (error.Contains("not a member"))
            {
                return NotFound(new { message = error });
            }

            // Any other error is a bad request
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Role updated successfully." });
    }

    /// <summary>
    /// PATCH /api/parches/{parcheId}
    /// Updates a parche's name, description, and cover image.
    /// Only the Owner of the parche can do this.
    /// </summary>
    [HttpPatch("{parcheId}")]
    public async Task<IActionResult> UpdateParche(Guid parcheId, [FromBody] UpdateParcheDTO dto)
    {
        // ModelState handles validation from data annotations on UpdateParcheDTO
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get the caller's ID from the JWT token claims
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (callerId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        // Call the service to update the parche
        var (parche, error) = await _parcheService.UpdateParche(callerId, parcheId, dto);

        if (error != null)
        {
            // If parche doesn't exist, return 404
            if (error.Contains("not found"))
            {
                return NotFound(new { message = error });
            }

            // If caller isn't Owner, return 403 Forbidden
            if (error.Contains("Only the Owner"))
            {
                return StatusCode(403, new { message = error });
            }

            return BadRequest(new { message = error });
        }

        return Ok(parche);
    }

    /// <summary>
    /// GET /api/parches/{parcheId}/plans
    /// Returns all plans for a parche. Caller must be a member.
    /// </summary>
    [HttpGet("{parcheId}/plans")]
    public async Task<IActionResult> GetPlans(Guid parcheId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        var (plans, error) = await _planService.GetPlansForParche(userId, parcheId);

        if (error != null)
        {
            return StatusCode(403, new { message = error });
        }

        return Ok(plans);
    }
}
