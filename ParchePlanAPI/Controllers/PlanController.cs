using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParchePlanAPI.Interfaces;
using ParchePlanAPI.Models.DTOs;

namespace ParchePlanAPI.Controllers;


[ApiController]
[Route("api/plans")]
[Authorize]
public class PlanController : Controller
{
    private readonly IPlanService _planService;

    public PlanController(IPlanService planService)
    {
        _planService = planService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        var (plan, error) = await _planService.CreatePlan(userId, dto);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Created($"/api/plans/{plan!.IdPlan}", plan);
    }
    
    [HttpPatch("{planId}/state")]
    public async Task<IActionResult> AdvanceState(Guid planId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        var (plan, error) = await _planService.AdvanceState(userId, planId);

        if (error != null)
        {
            // Plan not found
            if (error.Contains("not found"))
            {
                return NotFound(new { message = error });
            }

            // Not authorized (not Owner/Moderator)
            if (error.Contains("Only Owner or Moderator"))
            {
                return StatusCode(403, new { message = error });
            }

            // Already final state or other
            return BadRequest(new { message = error });
        }

        return Ok(plan);
    }

    [HttpPost("{planId}/votes")]
    public async Task<IActionResult> CastVote(Guid planId, [FromBody] CastVoteDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        var (success, error) = await _planService.CastVote(userId, planId, dto);

        if (!success)
        {
            if (error!.Contains("not found"))
                return NotFound(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Vote registered." });
    }

    [HttpPut("{planId}/attendance")]
    public async Task<IActionResult> UpsertAttendance(Guid planId, [FromBody] UpsertAttendanceDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        var (success, error) = await _planService.UpsertAttendance(userId, planId, dto);

        if (!success)
        {
            if (error!.Contains("not found"))
                return NotFound(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Attendance updated." });
    }

    [HttpPost("{planId}/check-in")]
    public async Task<IActionResult> CheckIn(Guid planId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found in token." });
        }

        var (success, error) = await _planService.CheckIn(userId, planId);

        if (!success)
        {
            if (error!.Contains("not found"))
                return NotFound(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Checked in successfully." });
    }
}
