using System.Security.Claims;
using BlotterSync.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BlotterSync.Controllers;

[ApiController, Authorize, Route("api/agent")]
public sealed class AgentController(AgentHarness harness, AgentStore store, OpenAiAgent model) : ControllerBase
{
    private int OfficerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("status")]
    public IActionResult Status() => Ok(new { mode = model.Enabled ? "openai" : "local", model = model.Enabled ? model.Model : null,
        description = model.Enabled ? "AI connected. Drafts require your review." : "Local assistance. Connect a server API key to enable AI summaries.",
        maxTurns = 4, timeoutSeconds = 60 });

    [HttpPost("runs"), EnableRateLimiting("agent")]
    public async Task<IActionResult> Run(AgentRequest request, CancellationToken ct)
    {
        try { return Ok(await harness.RunAsync(request, OfficerId, User.IsInRole("Admin"), ct)); }
        catch (AgentFailure ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("runs")]
    public async Task<IActionResult> Runs(CancellationToken ct)
        => Ok((await store.Runs.AsNoTracking().Where(r => r.OfficerId == OfficerId)
            .OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync(ct)).Select(AgentHarness.View));

    [HttpGet("runs/{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var run = await store.Runs.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.OfficerId == OfficerId, ct);
        return run is null ? NotFound() : Ok(AgentHarness.View(run));
    }

    [HttpPost("runs/{id}/review")]
    public async Task<IActionResult> Review(string id, AgentReview review, CancellationToken ct)
    {
        var updated = await store.Runs.Where(r => r.Id == id && r.OfficerId == OfficerId && r.Status == "needs_review")
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, review.Decision).SetProperty(r => r.ReviewedAt, DateTime.UtcNow), ct);
        if (updated == 0)
        {
            var exists = await store.Runs.AnyAsync(r => r.Id == id && r.OfficerId == OfficerId, ct);
            return exists ? Conflict(new { error = "This draft has already been reviewed or is not ready for review." }) : NotFound();
        }
        return await Get(id, ct);
    }
}
