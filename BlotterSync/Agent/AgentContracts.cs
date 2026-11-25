using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BlotterSync.Agent;

public sealed class AgentRequest
{
    [Required, RegularExpression("^(incident_summary|intake_draft)$")]
    public string Workflow { get; set; } = "incident_summary";
    public int? RecordId { get; set; }
    [StringLength(6000)] public string Notes { get; set; } = "";
}

public sealed record AgentResult(string Title, string Summary, string SuggestedCategory,
    string Location, string[] MissingInformation, string[] NextSteps, int[] SourceRecordIds);
public sealed record AgentStep(string Tool, string Detail, DateTime At);
public sealed record AgentView(string Id, string Workflow, int? RecordId, string Status,
    string Mode, DateTime CreatedAt, DateTime? ReviewedAt, string? Error,
    AgentResult? Result, IReadOnlyList<AgentStep> Steps, string? SourceNotes);
public sealed class AgentReview
{
    [Required, RegularExpression("^(accepted|rejected)$")]
    public string Decision { get; set; } = "";
}
public sealed class AgentFailure(string message) : Exception(message);
public static class AgentJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
