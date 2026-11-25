# Agent API

Every endpoint requires an officer JWT in `Authorization: Bearer …`. All paths use the same server origin as the workspace.

| Method | Path | Behavior |
| --- | --- | --- |
| GET | `/api/agent/status` | Local or OpenAI mode, model name, turn and timeout limits. Never returns credentials. |
| POST | `/api/agent/runs` | Execute a bounded run and return the persisted run view. Six requests per officer per minute; at most two active process-wide. |
| GET | `/api/agent/runs` | Most recent 50 runs belonging to the signed-in officer. |
| GET | `/api/agent/runs/{id}` | Personal run, its result and tool activity. Another officer receives 404. |
| POST | `/api/agent/runs/{id}/review` | Accept or reject a needs_review run. Atomic transition; later review attempts receive 409. |

Summary request:

```json
{"workflow":"incident_summary","recordId":2}
```

Intake request (up to 6,000 characters):

```json
{"workflow":"intake_draft","notes":"The resident reported a missing bag. Exact time unverified."}
```

Review request:

```json
{"decision":"accepted"}
```

Run statuses: `running`, `needs_review`, `accepted`, `rejected`, `failed`, `cancelled`. A completed HTTP request can return a failed run (200 with `status: failed`); validation failures use 400, authentication failures 401, access failures 404 and rate limits 429. The workspace handles run errors separately from transport errors. Disconnecting a request cancels model work; cancellation is persisted even if the browser has gone away.

Results contain `title`, `summary`, `suggestedCategory`, `location`, `missingInformation`, `nextSteps`, and `sourceRecordIds`. For summaries, source references must match exactly the selected record; intakes cannot reference saved records. Category names must exist in the category directory. Locations must occur in the input evidence. These checks validate structure and references, not the factual correctness of generated prose.

The harness forces a scoped source read before summary generation. Tool calls are dispatched through `AgentTools`, which repeats authorization and task-scope checks. OpenAI receives a strict JSON schema and tools `read_incident(recordId)` and `list_categories()`. A run permits at most four model turns, six requested tool calls, 1,800 output tokens per turn, a 256 KB provider response and 60 seconds overall. Unknown tools, malformed output, unsupported references, refusals and incomplete model responses fail visibly. No automatic provider retries are made.

Audit entries retain only final structured results, run metadata, concise tool activity and review decisions. Raw model responses and hidden reasoning are not stored. Original intake notes are saved as `sourceNotes` on the run for source comparison after reloading. Older runs have null sourceNotes and the UI identifies that missing snapshot explicitly. Run state resides outside the cloud-synced record schema.
