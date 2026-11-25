# BlotterSync

An offline-capable barangay incident workspace built on ASP.NET Core 8. The AI extension adds reviewed incident briefs, intake drafts, source references, tool activity and personal agent history. The original dashboard and community kiosk remain available.

## Run the development demo

Requires the .NET 8 SDK. From the repository root:

```sh
dotnet restore BlotterSync/BlotterSync.csproj
dotnet run --project BlotterSync/BlotterSync.csproj --launch-profile http
```

Open [the officer workspace](http://localhost:5180/workspace.html) and choose **Explore the demo workspace**. Development demo mode seeds eight synthetic incidents in `BlotterSync/App_Data/blottersync-demo.db`. The demo login endpoint is unavailable outside Development or when `Demo:Enabled` is false. The development JWT signing key is generated per process unless configured, so sign in again after restarting.

The runtime frontend is in `BlotterSync/wwwroot`; repository-root HTML files are retained upstream snapshots, not the runtime entrypoints. The application serves the workspace at `/`. `/index.html` retains officer login/registration, `/dashboard.html` retains the legacy dashboard, and `/kiosk.html` retains the community kiosk. The `.slnx` came from the upstream repository; the project commands above work directly with .NET 8.

## Enable AI

Without a key, the app uses **Local assistance**: verbatim extraction and an administrative verification checklist. This mode performs no model calls and does not claim to summarize or infer facts.

Configure the server with `OPENAI_API_KEY` (or .NET user secrets `AI:ApiKey`) and restart. For example, use the normal secret manager or environment settings of your host. Never put keys in HTML, browser storage or tracked configuration. `AI:Model` defaults to `gpt-5.4-mini`; override it with `AI__Model` for another Responses API model that supports function calling and strict structured output.

The AI path sends the selected narrative or intake notes and category names to OpenAI. It uses `store: false`, a strict output schema and two allowlisted read-only tools. There are no shell, SQL, web or write tools. Provider errors remain visible failures rather than silently switching to local extraction. No live provider call is part of the test suite; provider behavior is exercised with fake HTTP responses.

## How review works

1. Select an incident and prepare a brief, or enter an initial account in Intake assistant.
2. Compare the draft with the source, verify missing details, and accept or reject it.
3. An accepted brief records a review decision without changing the incident.
4. An accepted intake fills the filing form. You can restore it later from Agent history with **Use in filing form**, and compare it with its saved original notes. Check category, Philippine date/time, location and narrative; explicitly submit **File incident** to create a Pending record.

Agent runs have independent per-officer access checks. Administrators can summarize records they can already access; run histories remain personal. The server validates categories, location evidence and record references; officers must still check semantic accuracy. Model text is escaped before display.

## Storage and deployment

The default provider is SQLite. The existing Supabase sync remains available: set `DatabaseProvider=PostgreSQL`, configure `ConnectionStrings__DefaultConnection` or `DATABASE_URL`, and disable development demo mode. The API continues to use the local SQLite store and existing outbox sync service. Existing SQL Server configuration is also supported with `DatabaseProvider=SqlServer`.

For non-demo Development use, set `Demo__Enabled=false` and set `ConnectionStrings__LocalConnection` to your intended local database; the development configuration otherwise selects the demo file. Production uses the main configuration and has no demo login.

Production requires `Jwt__Key` (at least 32 bytes), plus `Jwt__Issuer` and `Jwt__Audience` if changing the defaults. Use a .NET-capable host, HTTPS, and a persistent private volume for `App_Data`. For a production process without a launch profile:

```sh
dotnet run --project BlotterSync/BlotterSync.csproj --no-launch-profile --environment Production
```

Runs are stored separately in `agent-demo.db` or `agent-runs.db` in `App_Data`; they are not synchronized to Supabase. Both the incident and audit databases must be backed up and access-restricted. Audit outputs and saved original intake notes contain incident text. This first version retains runs until an operator applies a retention policy; it is designed for a single server process. Restart recovery marks unfinished runs as cancelled. A production rollout should define data retention and officer provisioning; the upstream self-registration route remains enabled. The retained AutoMapper dependency also has its own production licensing requirements.

The retained backend cannot run in the Sites Cloudflare Worker runtime. This deliverable runs locally; production hosting must be configured separately.

## Verification

```sh
dotnet test BlotterSync.Tests/BlotterSync.Tests.csproj
node --check BlotterSync/wwwroot/workspace.js
# With the Development demo server running:
python3 scripts/smoke-test.py
```

The HTTP smoke script first verifies demo mode, creates synthetic test accounts/records in that development database, and checks authentication, isolation, immutable reviews, drafts and explicit filing. Tests never require a cloud database or model key.

See [the implementation plan](docs/AI_IMPLEMENTATION_PLAN.md) for architecture and acceptance criteria, and [the agent API](docs/AGENT_API.md) for endpoint contracts.
