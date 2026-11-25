using System.Text.Json;
using BlotterSync.Models;
using Microsoft.EntityFrameworkCore;

namespace BlotterSync.Agent;

public sealed record IncidentSource(int RecordId, string TrackingNumber, string Category,
    DateTime IncidentDate, string Location, string Narrative, string Status);

public sealed class AgentTools(BlotterSyncContext db)
{
    public async Task<IncidentSource?> ReadIncidentAsync(int id, int officerId, bool isAdmin, CancellationToken ct)
        => await db.BlotterRecords.AsNoTracking()
            .Where(r => r.RecordId == id && (isAdmin || r.DeskOfficerId == officerId))
            .Select(r => new IncidentSource(r.RecordId, r.TrackingNumber, r.Category.Name,
                r.IncidentDate, r.Location, r.Narrative, r.Status)).SingleOrDefaultAsync(ct);

    public async Task<string[]> CategoriesAsync(CancellationToken ct)
        => await db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => c.Name).Take(100).ToArrayAsync(ct);

    public async Task<string> ExecuteAsync(string name, string arguments, int officerId, bool isAdmin,
        int? allowedRecordId, List<AgentStep> steps, CancellationToken ct)
    {
        using var args = JsonDocument.Parse(arguments);
        if (name == "list_categories")
        {
            if (args.RootElement.ValueKind != JsonValueKind.Object || args.RootElement.EnumerateObject().Any())
                throw new AgentFailure("Invalid category tool arguments.");
            steps.Add(new(name, "Read the incident category directory.", DateTime.UtcNow));
            return JsonSerializer.Serialize(await CategoriesAsync(ct), AgentJson.Options);
        }
        if (name == "read_incident")
        {
            if (!args.RootElement.TryGetProperty("recordId", out var property) || !property.TryGetInt32(out var id)
                || args.RootElement.EnumerateObject().Count() != 1 || id != allowedRecordId)
                throw new AgentFailure("The agent requested a record outside this task.");
            var record = await ReadIncidentAsync(id, officerId, isAdmin, ct)
                ?? throw new AgentFailure("The requested record is not available.");
            if (record.Narrative.Length > 12000)
                throw new AgentFailure("This narrative exceeds the agent context limit. Use a shorter intake excerpt.");
            steps.Add(new(name, $"Read {record.TrackingNumber} within your record access.", DateTime.UtcNow));
            return JsonSerializer.Serialize(record, AgentJson.Options);
        }
        throw new AgentFailure("The agent requested an unsupported tool.");
    }
}
