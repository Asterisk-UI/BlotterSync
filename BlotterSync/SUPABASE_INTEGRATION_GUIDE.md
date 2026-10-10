# BlotterSync - Supabase PostgreSQL Integration Guide

BlotterSync now supports **Supabase PostgreSQL** as its primary cloud database provider while maintaining backwards compatibility with local SQL Server.

---

## 1. Prerequisites: Create a Supabase Project

1. Log in to [Supabase](https://supabase.com/) and create a new project.
2. Choose a project name and a secure database password (save this password—you will need it for the connection string).
3. Wait for the database provisioning to complete (typically 1–2 minutes).

---

## 2. Obtain Your Supabase PostgreSQL Connection String

1. In your Supabase project dashboard, navigate to **Project Settings** (gear icon) -> **Database**.
2. Scroll to the **Connection string** section.
3. You can use either the **URI** format or the **Connection Parameters** (ADO.NET / EF Core) format:

### Option A: Connection Pooler (Recommended for cloud apps / serverless)
* **Port**: `6543` (Transaction Mode) or `5432` (Session Mode)
* **Format**:
  ```text
  Host=aws-0-[REGION].pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[YOUR-PROJECT-REF];Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;
  ```
  *Or URI format:*
  ```text
  postgresql://postgres.[YOUR-PROJECT-REF]:[YOUR-PASSWORD]@aws-0-[REGION].pooler.supabase.com:6543/postgres
  ```

### Option B: Direct Connection
* **Port**: `5432`
* **Format**:
  ```text
  Host=db.[YOUR-PROJECT-REF].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;
  ```
  *Or URI format:*
  ```text
  postgresql://postgres:[YOUR-PASSWORD]@db.[YOUR-PROJECT-REF].supabase.co:5432/postgres
  ```

> [!NOTE]
> BlotterSync automatically detects and parses both standard ADO.NET connection strings and `postgresql://` URI strings. It also automatically applies `SSL Mode=Require` and disables statement multiplexing when connecting to port `6543` to ensure compatibility with Supabase's transaction pooler.

---

## 3. Configure BlotterSync

Open [`appsettings.json`](file:///c:/Users/Ree/Documents/GitHub/BlotterSync/BlotterSync/appsettings.json) and set your Supabase credentials:

```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-[REGION].pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[YOUR-PROJECT-REF];Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;",
    "SupabaseConnection": "Host=aws-0-[REGION].pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[YOUR-PROJECT-REF];Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;",
    "SqlServerConnection": "Server=.\\SQLEXPRESS;Database=BlotterSyncDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Database": {
    "EnsureCreated": true
  },
  "Supabase": {
    "Url": "https://[YOUR-PROJECT-REF].supabase.co",
    "AnonKey": "[YOUR-SUPABASE-ANON-KEY]"
  }
}
```

### Environment Variable Alternative (Docker / Cloud Hosts)
You can also supply the connection string via the standard `DATABASE_URL` environment variable:
```bash
DATABASE_URL="Host=aws-0-[REGION].pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[YOUR-PROJECT-REF];Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;"
```

---

## 4. Initialize Database Tables and Seed Data

You have two easy ways to set up tables in your Supabase database:

### Method 1: Automatic Table Creation (Zero SQL Required)
Keep `"Database:EnsureCreated": true` in `appsettings.json`. When you run the BlotterSync API, EF Core will automatically create all tables in your Supabase database on startup if they don't already exist.

### Method 2: Supabase SQL Editor (Recommended)
1. In your Supabase dashboard, open the **SQL Editor** tab.
2. Click **New query**.
3. Open [`supabase_schema.sql`](file:///c:/Users/Ree/Documents/GitHub/BlotterSync/BlotterSync/supabase_schema.sql) from this repository, copy its contents, paste them into the query editor, and click **Run**.
4. This script will:
   - Create all 7 tables (`Categories`, `Officers`, `Residents`, `Citizens`, `BlotterRecords`, `Involvements`, `Announcements`)
   - Configure primary keys, foreign key constraints, and performance indexes
   - Seed standard incident categories (Theft, Assault, Harassment, etc.)
   - Seed an initial default Admin officer account:
     - **Username**: `admin`
     - **Password**: `changeme`
     - **Role**: `Admin`

---

## 5. Verify the Connection

1. Run the application:
   ```bash
   dotnet run
   ```
2. Open Swagger UI at `https://localhost:7052/swagger` (or `http://localhost:5180/swagger`).
3. Test logging in via `POST /api/Auth/Login`:
   ```json
   {
     "username": "admin",
     "password": "changeme"
   }
   ```
   On first login, BlotterSync's authentication system will automatically hash and upgrade the default password to a PBKDF2 ASP.NET Core Identity password hash directly in Supabase Postgres.
4. Retrieve the JWT bearer token from the response and authorize requests in Swagger or [`BlotterSync.http`](file:///c:/Users/Ree/Documents/GitHub/BlotterSync/BlotterSync/BlotterSync.http).

---

## 6. Offline Fail-Safe (Internet Outages)

When the PostgreSQL/Supabase provider is selected, BlotterSync doesn't depend on the internet to serve requests:

* **The API always uses a local SQLite database** on the server (`App_Data/blottersync-local.db`, set by `ConnectionStrings:LocalConnection`). Logins, new blotter records, residents and announcements all keep working when the internet is down.
* **Every change is queued** in a local outbox table. A background worker (`Sync/CloudSyncService.cs`) pushes the queue to Supabase every `Sync:IntervalSeconds` (default 10). When the connection drops, the queue grows. When it comes back, the queued changes upload on their own, in order.
* **First start:** if the local database is new, BlotterSync first downloads all existing Supabase data into it. If Supabase can't be reached on that first start, the server still starts. Ids created offline start at 1,000,000 so they can't collide with existing cloud ids, and the Supabase data is merged in once the connection returns.
* **Status:** `GET /api/Sync/Status` reports `cloudOnline`, `pendingChanges` and `failedChanges`. Each page shows a banner (`sync-status.js`) when the server is offline or still uploading.
* **Rejected rows:** if Supabase refuses a row (for example a duplicate badge number), that row is retried up to 10 times and then set aside so the rest can sync. Admins can list these with `GET /api/Sync/Failed`, fix the data, and requeue them with `POST /api/Sync/RetryFailed`.

> [!IMPORTANT]
> This server's local database is the source of truth, and Supabase is a cloud copy of it. Edits made directly in the Supabase dashboard are not pulled back down and can be overwritten by later changes from the server. Back up `App_Data/` along with the rest of the server. To rebuild the local copy from Supabase, stop the app, check that `pendingChanges` was 0, then delete `App_Data/blottersync-local.db*` and start the app again.
