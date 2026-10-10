using BlotterSync.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlotterSync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly SyncStatus _status;
        private readonly IServiceProvider _services;

        public SyncController(SyncStatus status, IServiceProvider services)
        {
            _status = status;
            _services = services;
        }

        [AllowAnonymous]
        [HttpGet("Status")]
        public async Task<IActionResult> GetStatus()
        {
            var pending = 0;
            var failed = 0;

            if (_status.Enabled)
            {
                var local = _services.GetRequiredService<LocalBlotterSyncContext>();
                pending = await local.SyncOutbox.CountAsync(e => e.Attempts < CloudSyncService.MaxAttempts);
                failed = await local.SyncOutbox.CountAsync(e => e.Attempts >= CloudSyncService.MaxAttempts);
            }

            return Ok(new
            {
                enabled = _status.Enabled,
                cloudOnline = !_status.Enabled || _status.CloudOnline,
                bootstrapped = _status.Bootstrapped,
                pendingChanges = pending,
                failedChanges = failed,
                lastSuccessAt = _status.LastSuccessAt,
                lastAttemptAt = _status.LastAttemptAt
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Failed")]
        public async Task<IActionResult> GetFailed()
        {
            if (!_status.Enabled)
            {
                return Ok(Array.Empty<SyncOutboxEntry>());
            }

            var local = _services.GetRequiredService<LocalBlotterSyncContext>();
            return Ok(await local.SyncOutbox
                .Where(e => e.Attempts >= CloudSyncService.MaxAttempts)
                .OrderBy(e => e.Id)
                .ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("RetryFailed")]
        public async Task<IActionResult> RetryFailed()
        {
            if (!_status.Enabled)
            {
                return Ok(new { retried = 0 });
            }

            var local = _services.GetRequiredService<LocalBlotterSyncContext>();
            var retried = await local.SyncOutbox
                .Where(e => e.Attempts >= CloudSyncService.MaxAttempts)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Attempts, 0));

            return Ok(new { retried });
        }
    }
}
