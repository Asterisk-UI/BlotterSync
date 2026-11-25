using System.Net;
using System.Text;
using System.Text.Json;
using BlotterSync.Agent;
using BlotterSync.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlotterSync.Tests;

public sealed class AgentHarnessTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection dataConnection = new("Data Source=:memory:");
        private readonly SqliteConnection auditConnection = new("Data Source=:memory:");
        public BlotterSyncContext Data { get; }
        public AgentStore Store { get; }
        public AgentTools Tools { get; }
        public Fixture()
        {
            dataConnection.Open(); auditConnection.Open();
            Data = new(new DbContextOptionsBuilder<BlotterSyncContext>().UseSqlite(dataConnection).Options);
            Store = new(new DbContextOptionsBuilder<AgentStore>().UseSqlite(auditConnection).Options);
            Data.Database.EnsureCreated(); Store.Database.EnsureCreated();
            Data.Categories.Add(new Category { CategoryId = 1, Name = "Lost and Found", SeverityLevel = 1 });
            Data.Officers.Add(new Officer { OfficerId = 1, Username = "one", BadgeNumber = "ONE", FirstName = "One", LastName = "Officer", PasswordHash = "hash", Role = "Officer" });
            Data.Officers.Add(new Officer { OfficerId = 2, Username = "two", BadgeNumber = "TWO", FirstName = "Two", LastName = "Officer", PasswordHash = "hash", Role = "Officer" });
            Data.BlotterRecords.Add(new BlotterRecord { RecordId = 1, DeskOfficerId = 1, CategoryId = 1, TrackingNumber = "TEST-1", Location = "Malanday hall", Narrative = "A bag was reported missing. Ownership is unverified.", Status = "Pending", IncidentDate = DateTime.UtcNow });
            Data.SaveChanges(); Tools = new(Data);
        }
        public AgentHarness Harness(HttpMessageHandler? handler = null)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(handler is null ? [] : new Dictionary<string, string?> { ["AI:ApiKey"] = "test-key" }).Build();
            return new(Store, Tools, new OpenAiAgent(new HttpClient(handler ?? new RejectNetwork()), config), NullLogger<AgentHarness>.Instance);
        }
        public async ValueTask DisposeAsync() { await Data.DisposeAsync(); await Store.DisposeAsync(); await dataConnection.DisposeAsync(); await auditConnection.DisposeAsync(); }
    }
    private sealed class RejectNetwork : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => throw new InvalidOperationException("Unexpected network call.");
    }
    private sealed class Responses(params string[] responses) : HttpMessageHandler
    {
        private int index;
        public List<string> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Assert.Equal("https://api.openai.com/v1/responses", r.RequestUri!.ToString());
            Requests.Add(await r.Content!.ReadAsStringAsync(ct));
            return new(HttpStatusCode.OK) { Content = new StringContent(responses[Math.Min(index++, responses.Length - 1)], Encoding.UTF8, "application/json") };
        }
    }
    private static AgentRequest Summary(int id = 1) => new() { Workflow = "incident_summary", RecordId = id };
    private static AgentResult ValidResult() => new("Incident brief", "The resident reported a missing bag. Ownership remains unverified.", "Lost and Found", "Malanday hall", ["Verify ownership."], ["Review the original account."], [1]);
    private static string Final(AgentResult? result = null) => JsonSerializer.Serialize(new { status = "completed", output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = JsonSerializer.Serialize(result ?? ValidResult(), AgentJson.Options) } } } } });
    private static string Call(string name, string args) => JsonSerializer.Serialize(new { status = "completed", output = new[] { new { type = "function_call", name, arguments = args, call_id = "call_1" } } });

    [Fact] public async Task Existing_audit_stores_upgrade_without_losing_runs()
    {
        await using var f = new Fixture();
        f.Store.Runs.Add(new AgentRun { OfficerId = 1, Workflow = "intake_draft", Status = "accepted" });
        await f.Store.SaveChangesAsync();
        await f.Store.Database.ExecuteSqlRawAsync("ALTER TABLE \"Runs\" DROP COLUMN \"SourceNotes\";");
        f.Store.ChangeTracker.Clear();
        await f.Store.InitializeAsync();
        var run = await f.Store.Runs.AsNoTracking().SingleAsync();
        Assert.Equal("accepted", run.Status); Assert.Null(run.SourceNotes);
        await f.Store.InitializeAsync(); // Repeated startup is idempotent.
    }
    [Fact] public void Explicit_sqlite_does_not_activate_cloud_sync_from_an_environment_url()
    {
        Assert.False(BlotterSync.Sync.DatabaseConnection.IsPostgreSql("SQLite", "postgres://example.invalid/database"));
    }
    [Fact] public async Task Local_mode_extracts_without_network_and_persists_sources()
    {
        await using var f = new Fixture();
        var run = await f.Harness().RunAsync(Summary(), 1, false, default);
        Assert.Equal("needs_review", run.Status); Assert.Equal("local", run.Mode);
        Assert.Equal((await f.Data.BlotterRecords.SingleAsync()).Narrative, run.Result!.Summary);
        Assert.Equal([1], run.Result.SourceRecordIds); Assert.Equal(4, run.Steps.Count);
        Assert.Single(await f.Store.Runs.ToListAsync());
    }
    [Fact] public async Task Other_officers_source_is_not_exposed_even_to_the_model()
    {
        await using var f = new Fixture(); var provider = new Responses(Final());
        var run = await f.Harness(provider).RunAsync(Summary(), 2, false, default);
        Assert.Equal("failed", run.Status); Assert.Null(run.Result); Assert.Empty(provider.Requests); Assert.Empty(run.Steps);
    }
    [Fact] public async Task Admin_can_summarize_an_officers_record()
    {
        await using var f = new Fixture();
        Assert.Equal("needs_review", (await f.Harness().RunAsync(Summary(), 2, true, default)).Status);
    }
    [Fact] public async Task Intake_uses_only_the_supplied_notes_and_no_record_references()
    {
        await using var f = new Fixture();
        var run = await f.Harness().RunAsync(new() { Workflow = "intake_draft", Notes = "  A bag is missing.  " }, 1, false, default);
        Assert.Equal("  A bag is missing.  ", run.SourceNotes);
        f.Store.ChangeTracker.Clear();
        Assert.Equal(run.SourceNotes, (await f.Store.Runs.AsNoTracking().SingleAsync()).SourceNotes);
        Assert.Equal("A bag is missing.", run.Result!.Summary); Assert.Empty(run.Result.SourceRecordIds);
        Assert.Equal("", run.Result.Location); Assert.Equal("", run.Result.SuggestedCategory);
        Assert.Equal(1, await f.Data.BlotterRecords.CountAsync());
    }
    [Fact] public async Task Tool_loop_executes_allowed_tool_and_sends_result_back()
    {
        await using var f = new Fixture(); var provider = new Responses(Call("read_incident", "{\"recordId\":1}"), Final());
        var run = await f.Harness(provider).RunAsync(Summary(), 1, false, default);
        Assert.Equal("needs_review", run.Status); Assert.Equal("openai", run.Mode); Assert.Equal(2, provider.Requests.Count);
        Assert.Contains("function_call_output", provider.Requests[1]);
        using var request = JsonDocument.Parse(provider.Requests[0]);
        Assert.False(request.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal("gpt-5.4-mini", request.RootElement.GetProperty("model").GetString());
        Assert.Equal("reasoning.encrypted_content", request.RootElement.GetProperty("include")[0].GetString());
        Assert.True(request.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean());
    }
    [Theory]
    [InlineData("delete_incident", "{\"recordId\":1}")]
    [InlineData("read_incident", "{\"recordId\":2}")]
    [InlineData("list_categories", "{\"sql\":\"SELECT * FROM Officers\"}")]
    public async Task Unapproved_tool_calls_fail_closed(string tool, string args)
    {
        await using var f = new Fixture(); var provider = new Responses(Call(tool, args));
        var run = await f.Harness(provider).RunAsync(Summary(), 1, false, default);
        Assert.Equal("failed", run.Status); Assert.Null(run.Result); Assert.Single(provider.Requests);
    }
    [Fact] public async Task Endless_tool_loop_stops_after_four_model_turns()
    {
        await using var f = new Fixture(); var provider = new Responses(Call("list_categories", "{}"));
        var run = await f.Harness(provider).RunAsync(Summary(), 1, false, default);
        Assert.Equal("failed", run.Status); Assert.Equal(4, provider.Requests.Count); Assert.Contains("turn limit", run.Error);
    }
    [Fact] public async Task Invalid_provider_response_fails_without_falling_back_to_local()
    {
        await using var f = new Fixture(); var run = await f.Harness(new Responses("not-json")).RunAsync(Summary(), 1, false, default);
        Assert.Equal("failed", run.Status); Assert.Equal("openai", run.Mode); Assert.Null(run.Result);
    }
    [Fact] public async Task Forged_citations_are_not_saved_as_a_draft()
    {
        await using var f = new Fixture(); var run = await f.Harness(new Responses(Final(ValidResult() with { SourceRecordIds = [2] }))).RunAsync(Summary(), 1, false, default);
        Assert.Equal("failed", run.Status); Assert.Null(run.Result);
    }
    [Fact] public void Unknown_categories_and_invented_locations_are_rejected()
    {
        Assert.Throws<AgentFailure>(() => AgentHarness.ValidateResult(ValidResult() with { SuggestedCategory = "Unknown" }, 1, ["Lost and Found"], "Malanday hall"));
        Assert.Throws<AgentFailure>(() => AgentHarness.ValidateResult(ValidResult() with { Location = "Elsewhere" }, 1, ["Lost and Found"], "Malanday hall"));
    }
    [Fact] public async Task A_cancelled_run_remains_in_the_audit_store()
    {
        await using var f = new Fixture(); var provider = new CancellingResponse();
        var run = await f.Harness(provider).RunAsync(Summary(), 1, false, default);
        Assert.Equal("cancelled", run.Status); Assert.Equal("cancelled", (await f.Store.Runs.SingleAsync()).Status);
    }
    private sealed class CancellingResponse : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => throw new OperationCanceledException();
    }
}
