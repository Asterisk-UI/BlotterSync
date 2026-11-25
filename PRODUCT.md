# BlotterSync

<!-- impeccable:product-schema 1 -->

## Platform
web

## Users
Barangay desk officers and administrators recording and reviewing incidents. The repository identifies Barangay Malanday.

## Product Purpose
Preserve the existing digital blotter and offline database sync while adding incident summaries and intake assistance. The user confirmed those as the first AI workflows.

## Capabilities and Constraints
.NET 8 API, JWT officer authentication, role-scoped records, categories, resident references, announcements, CSV reports, SQLite and optional Supabase sync. AI outputs are drafts requiring officer review; they must not decide guilt or resolve cases. Server-side model credentials only. Local assistance must be labeled distinctly from model-generated output. Demonstration records are synthetic.

## Brand Commitments
Extend the repository's BlotterSync name and slate/blue officer workspace. Keep existing login, dashboard and kiosk routes available. No rebrand requested.

## Evidence on Hand
Existing source in BlotterSync/, with legacy static frontend copies in the repository root. No production incident dataset provided for this work.

## Open Decisions
Production hosting and model account credentials have not been supplied. The implementation must be locally runnable without them.
