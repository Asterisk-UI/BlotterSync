# AI workspace implementation plan

## Outcome
An officer opens a record, generates a cited incident brief, reviews missing information and accepts or rejects the draft. For intake, the officer provides notes, generates a structured draft and explicitly files a new Pending record after checking the fields. Existing records are never changed by the agent.

## Architecture
- Retain ASP.NET Core 8, EF Core, JWT and the local/Supabase synchronization path.
- Serve the new responsive workspace from the same origin at `/workspace.html`; retain legacy routes.
- Add a request-bounded agent harness with an OpenAI Responses API adapter. The only model tools are `read_incident` and `list_categories`. The tool dispatcher enforces the officer's scope independently of the model.
- Force a source read for record summaries, limit model turns and tool calls, cap input/output, apply a total timeout and accept only schema-valid outputs with validated citations.
- Store runs and officer review decisions in a separate local SQLite audit database. Keep this database outside the existing cloud synchronization schema. Never persist API keys or hidden model reasoning.
- Provide deterministic local extraction when no model key is configured. Clearly label it Local assistance; it is not a simulated AI response. Provider errors fail visibly rather than silently changing modes.

## Product surfaces
- Overview with a real record queue, status counts and recent activity.
- Searchable/filterable incident register and record detail panel.
- Incident summary and intake workflows, visible tool activity, citations, questions and review actions.
- Persistent personal run history and a connection panel for model and sync status.
- Keyboard-accessible forms, inline errors, loading and empty states, mobile layout.

## Implementation sequence
1. Make SQLite-only startup and an isolated opt-in development demo work.
2. Implement the agent contract, scoped tools, model adapter, validation and audit store.
3. Add authenticated run/review endpoints and the workspace interface.
4. Verify actual API behavior and harness failure boundaries, then inspect desktop/mobile.
5. Document setup, key configuration, architecture and operational limits.

## Acceptance checks
- Anonymous requests cannot run or read the agent. Another officer cannot read a record or run, including through model tools.
- Missing provider credentials produce explicitly local output; malformed provider responses, unsupported calls and invalid citations fail closed.
- Accepted/rejected reviews survive restart and cannot be rewritten by another request.
- Intake requires explicit validated officer submission. Accepting a brief changes no incident.
- Demo mode is development-only and uses a separate SQLite store; live Supabase access is not needed for verification.

## Deployment boundary
The retained ASP.NET application requires a .NET-capable host and a persistent volume for SQLite. The Sites Worker runtime cannot execute this backend. This repository deliverable runs locally; production hosting is an open deployment decision.

## Delivered verification
The implementation passed 15 harness tests and 26 live development-demo API checks. Desktop (1440px) and mobile (390px) overview and intake screens were inspected. A GitHub Actions workflow repeats the automated checks. OpenAI response/tool behavior was tested with fake HTTP responses, including invalid output, forbidden tools, cross-record calls and endless tool loops. A live model call and live Supabase synchronization were not exercised because production credentials are not part of this deliverable.

API implementation references: [OpenAI function calling](https://developers.openai.com/api/docs/guides/function-calling) and [structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs).

The default model is configurable and uses [GPT-5.4 mini](https://developers.openai.com/api/docs/models/gpt-5.4-mini), whose documented default is no reasoning effort and which supports Responses, function tools and structured output. Opaque reasoning context, if returned by another configured model, is carried only in memory for the bounded run and never written to the audit log.
