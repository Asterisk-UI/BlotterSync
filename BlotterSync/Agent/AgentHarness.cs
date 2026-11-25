using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace BlotterSync.Agent;

public sealed class AgentHarness(AgentStore store, AgentTools tools, OpenAiAgent model, ILogger<AgentHarness> logger)
{
    private static readonly SemaphoreSlim Slots = new(2, 2);

    public async Task<AgentView> RunAsync(AgentRequest request, int officerId, bool isAdmin, CancellationToken ct)
    {
        if (request.Workflow == "incident_summary" && request.RecordId is null)
            throw new AgentFailure("Select an incident to summarize.");
        if (request.Workflow == "intake_draft" && (string.IsNullOrWhiteSpace(request.Notes) || request.RecordId is not null))
            throw new AgentFailure("Enter intake notes without an existing record reference.");
        var run = new AgentRun { OfficerId = officerId, Workflow = request.Workflow, RecordId = request.RecordId, SourceNotes = request.Workflow == "intake_draft" ? request.Notes : null, Mode = model.Enabled ? "openai" : "local" };
        store.Runs.Add(run);
        await store.SaveChangesAsync(ct);
        var steps = new List<AgentStep>();
        var entered = false;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        try
        {
            entered = await Slots.WaitAsync(TimeSpan.FromSeconds(2), timeout.Token);
            if (!entered) throw new AgentFailure("The assistant is busy. Try again in a moment.");
            IncidentSource? source = null;
            if (request.RecordId is int recordId)
            {
                source = await tools.ReadIncidentAsync(recordId, officerId, isAdmin, timeout.Token)
                    ?? throw new AgentFailure("The selected incident is not available to you.");
                if (source.Narrative.Length > 12000) throw new AgentFailure("This narrative exceeds the assistant context limit. Use a shorter intake excerpt.");
                steps.Add(new("read_incident", $"Read {source.TrackingNumber} within your record access.", DateTime.UtcNow));
            }
            var categories = await tools.CategoriesAsync(timeout.Token);
            steps.Add(new("list_categories", $"Loaded {categories.Length} incident categories.", DateTime.UtcNow));
            var result = model.Enabled
                ? await model.RunAsync(request, source, categories, tools, officerId, isAdmin, steps, timeout.Token)
                : LocalDraft(request, source, steps);
            ValidateResult(result, request.RecordId, categories, source?.Location ?? request.Notes);
            steps.Add(new("validate_draft", "Checked required fields, category and source references. Ready for officer review.", DateTime.UtcNow));
            run.ResultJson = JsonSerializer.Serialize(result, AgentJson.Options);
            run.Status = "needs_review";
        }
        catch (OperationCanceledException)
        {
            run.Status = "cancelled";
            run.Error = ct.IsCancellationRequested ? "The request was cancelled." : "The assistant timed out after 60 seconds. Retry with shorter notes.";
        }
        catch (AgentFailure ex) { run.Status = "failed"; run.Error = ex.Message; }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or HttpRequestException)
        {
            run.Status = "failed";
            run.Error = "The assistant could not produce a valid draft. Check the connection or review the incident manually.";
            logger.LogWarning("Agent run {RunId} failed with {ErrorType}.", run.Id, ex.GetType().Name);
        }
        finally
        {
            if (entered) Slots.Release();
            run.StepsJson = JsonSerializer.Serialize(steps, AgentJson.Options);
            // Preserve the audit entry even if the browser has disconnected.
            await store.SaveChangesAsync(CancellationToken.None);
        }
        return View(run);
    }

    public static void ValidateResult(AgentResult result, int? recordId, string[] categories, string locationEvidence)
    {
        if (string.IsNullOrWhiteSpace(result.Title) || result.Title.Length > 200 ||
            string.IsNullOrWhiteSpace(result.Summary) || result.Summary.Length > 8000 ||
            result.Location is null || result.Location.Length > 255 || result.SuggestedCategory is null ||
            result.MissingInformation is null || result.NextSteps is null || result.SourceRecordIds is null ||
            result.MissingInformation.Length > 12 || result.NextSteps.Length > 12 ||
            result.MissingInformation.Concat(result.NextSteps).Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 1000))
            throw new AgentFailure("The draft did not pass structure validation.");
        if (result.SuggestedCategory != "" && !categories.Contains(result.SuggestedCategory, StringComparer.Ordinal))
            throw new AgentFailure("The draft suggested an unknown incident category.");
        if (result.Location != "" && !locationEvidence.Contains(result.Location, StringComparison.OrdinalIgnoreCase))
            throw new AgentFailure("The draft introduced an unsupported location.");
        if (recordId is int id ? result.SourceRecordIds.Length != 1 || result.SourceRecordIds[0] != id : result.SourceRecordIds.Length != 0)
            throw new AgentFailure("The draft contained invalid source references.");
    }

    private static AgentResult LocalDraft(AgentRequest request, IncidentSource? source, List<AgentStep> steps)
    {
        steps.Add(new("local_extract", "Extracted supplied text without a model. No facts were inferred.", DateTime.UtcNow));
        return new(source is null ? "Intake notes for review" : $"Incident brief · {source.TrackingNumber}",
            source?.Narrative ?? request.Notes.Trim(), source?.Category ?? "", source?.Location ?? "",
            source is null ? ["Confirm the incident date and time.", "Confirm the exact location.", "Verify the involved parties and any witnesses."]
                : ["Verify the narrative with the reporting party.", "Confirm whether supporting evidence or witness statements are available."],
            ["Check the draft against the original account.", "Record only details verified by the desk officer."],
            source is null ? [] : [source.RecordId]);
    }

    public static AgentView View(AgentRun r) => new(r.Id, r.Workflow, r.RecordId, r.Status, r.Mode, r.CreatedAt,
        r.ReviewedAt, r.Error,
        r.ResultJson is null ? null : JsonSerializer.Deserialize<AgentResult>(r.ResultJson, AgentJson.Options),
        JsonSerializer.Deserialize<List<AgentStep>>(r.StepsJson, AgentJson.Options) ?? [], r.SourceNotes);
}
