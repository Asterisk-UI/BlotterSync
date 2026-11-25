using System.Security.Claims;
using System.Text;
using AutoMapper;
using BlotterSync.DTOs;
using BlotterSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BlotterSync.DTOs.BlotterRecordDTOs;

namespace BlotterSync.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BlotterRecordsController : ControllerBase
    {
        private readonly BlotterSyncContext _context;
        private readonly IMapper _mapper;

        public BlotterRecordsController(BlotterSyncContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlotterRecordDTO>>> GetBlotterRecords(
            [FromQuery] string? searchStatus,
            [FromQuery] string? searchLocation)
        {
            var query = _context.BlotterRecords
                .Include(b => b.Category)
                .Include(b => b.DeskOfficer)
                .Include(b => b.Complainant)
                .Include(b => b.Respondent)
                .AsQueryable();

            if (!IsAdmin())
            {
                var userId = GetCurrentOfficerId();
                query = query.Where(b => b.DeskOfficerId == userId);
            }

            if (!string.IsNullOrWhiteSpace(searchStatus))
            {
                query = query.Where(b => b.Status.Contains(searchStatus));
            }

            if (!string.IsNullOrWhiteSpace(searchLocation))
            {
                query = query.Where(b => b.Location.Contains(searchLocation));
            }

            var records = await query.OrderByDescending(b => b.IncidentDate).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<BlotterRecordDTO>>(records));
        }

        [AllowAnonymous]
        [HttpGet("Public")]
        public async Task<ActionResult<IEnumerable<object>>> GetPublicBlotterData()
        {
            var publicRecords = await _context.BlotterRecords
                .Select(b => new
                {
                    b.IncidentDate,
                    b.Location,
                    b.Status
                })
                .ToListAsync();

            return Ok(publicRecords);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BlotterRecordDTO>> GetBlotterRecord(int id)
        {
            var record = await _context.BlotterRecords
                .Include(b => b.Category)
                .Include(b => b.DeskOfficer)
                .Include(b => b.Complainant)
                .Include(b => b.Respondent)
                .FirstOrDefaultAsync(b => b.RecordId == id);

            if (record == null || !CanAccess(record))
            {
                return NotFound();
            }

            return Ok(_mapper.Map<BlotterRecordDTO>(record));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Export")]
        public async Task<IActionResult> ExportBlotterRecordsToCSV()
        {
            var records = await _context.BlotterRecords
                .Include(b => b.Category)
                .Include(b => b.DeskOfficer)
                .Include(b => b.Complainant)
                .Include(b => b.Respondent)
                .OrderByDescending(b => b.IncidentDate)
                .ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Tracking Number,Incident Date,Location,Status,Narrative");

            foreach (var record in records)
            {
                builder.AppendLine(string.Join(",",
                    CsvField(record.TrackingNumber),
                    CsvField(record.IncidentDate.ToString("yyyy-MM-dd HH:mm")),
                    CsvField(record.Location),
                    CsvField(record.Status),
                    CsvField(record.Narrative)));
            }

            var csvBytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
                .ToArray();

            return File(csvBytes, "text/csv", $"Malanday_BlotterReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpPost]
        public async Task<ActionResult<BlotterRecordDTO>> CreateBlotterRecord([FromBody] CreateBlotterRecordDTO createDto)
        {
            var referenceError = await ValidateReferencesAsync(createDto);
            if (referenceError != null)
            {
                return BadRequest(referenceError);
            }

            var blotterRecord = _mapper.Map<BlotterRecord>(createDto);

            blotterRecord.DeskOfficerId = GetCurrentOfficerId();
            blotterRecord.TrackingNumber = $"BS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}";
            blotterRecord.ReportedDate = DateTime.UtcNow;
            blotterRecord.Status = "Pending";

            _context.BlotterRecords.Add(blotterRecord);
            await _context.SaveChangesAsync();

            var returnDto = _mapper.Map<BlotterRecordDTO>(blotterRecord);

            return CreatedAtAction(nameof(GetBlotterRecord), new { id = blotterRecord.RecordId }, returnDto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBlotterRecord(int id, [FromBody] UpdateBlotterRecordDTO updateDto)
        {
            var existingRecord = await _context.BlotterRecords.FindAsync(id);
            if (existingRecord == null || !CanAccess(existingRecord))
            {
                return NotFound();
            }

            _mapper.Map(updateDto, existingRecord);

            if (existingRecord.Status == "Resolved" && existingRecord.ResolutionDate == null)
            {
                existingRecord.ResolutionDate = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BlotterRecordExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBlotterRecord(int id)
        {
            var record = await _context.BlotterRecords.FindAsync(id);
            if (record == null || !CanAccess(record))
            {
                return NotFound();
            }

            _context.BlotterRecords.Remove(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BlotterRecordExists(int id)
        {
            return _context.BlotterRecords.Any(e => e.RecordId == id);
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        private int GetCurrentOfficerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool CanAccess(BlotterRecord record) => IsAdmin() || record.DeskOfficerId == GetCurrentOfficerId();

        private async Task<string?> ValidateReferencesAsync(CreateBlotterRecordDTO dto)
        {
            if (!await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            {
                return "Unknown incident category.";
            }

            if (dto.ComplainantId != null && !await _context.Residents.AnyAsync(r => r.ResidentId == dto.ComplainantId))
            {
                return "Complainant not found.";
            }

            if (dto.RespondentId != null && !await _context.Residents.AnyAsync(r => r.ResidentId == dto.RespondentId))
            {
                return "Respondent not found.";
            }

            return null;
        }

        private static string CsvField(string? value)
        {
            value ??= string.Empty;

            if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
            {
                value = "'" + value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}