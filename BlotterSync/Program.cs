using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using BlotterSync.Agent;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using BlotterSync.Models;
using BlotterSync.Profiles;
using BlotterSync.Sync;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var rawConnectionString = DatabaseConnection.ResolveCloudConnectionString(builder.Configuration);

var configuredProvider = builder.Configuration.GetValue<string>("DatabaseProvider");
bool isPostgreSql = DatabaseConnection.IsPostgreSql(configuredProvider, rawConnectionString);

var demoEnabled = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demoEnabled && !string.Equals(configuredProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Development demo requires the SQLite provider to keep sample records separate from cloud data.");

if (string.Equals(configuredProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
{
    var localConnection = DatabaseConnection.ResolveLocalConnectionString(builder.Configuration, builder.Environment.ContentRootPath);
    builder.Services.AddDbContext<BlotterSyncContext>(options => options.UseSqlite(localConnection));
    builder.Services.AddSingleton<SyncStatus>();
}
else if (isPostgreSql)
{
    // Offline fail-safe: the API always uses a local SQLite database on this server,
    // and CloudSyncService copies every change to Supabase whenever the internet is up.
    var localConnectionString = DatabaseConnection.ResolveLocalConnectionString(
        builder.Configuration, builder.Environment.ContentRootPath);
    var pgConnectionString = DatabaseConnection.FormatNpgsqlConnectionString(rawConnectionString!);

    builder.Services.AddDbContext<LocalBlotterSyncContext>(options => options.UseSqlite(localConnectionString));
    builder.Services.AddScoped<BlotterSyncContext>(sp => sp.GetRequiredService<LocalBlotterSyncContext>());
    builder.Services.AddDbContextFactory<CloudBlotterSyncContext>(options => options.UseNpgsql(pgConnectionString));

    builder.Services.AddSingleton<SyncStatus>();
    builder.Services.AddSingleton<CloudSyncService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<CloudSyncService>());
}
else
{
    builder.Services.AddDbContext<BlotterSyncContext>(options =>
        options.UseSqlServer(rawConnectionString));
    builder.Services.AddSingleton<SyncStatus>();
}

builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<BlotterProfile>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
        policyBuilder.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader());
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Jwt:Key"] = jwtKey;
}

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddScoped<IPasswordHasher<Officer>, PasswordHasher<Officer>>();

var agentPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", demoEnabled ? "agent-demo.db" : "agent-runs.db");
Directory.CreateDirectory(Path.GetDirectoryName(agentPath)!);
builder.Services.AddDbContext<AgentStore>(options => options.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = agentPath }.ConnectionString));
builder.Services.AddScoped<AgentTools>();
builder.Services.AddScoped<AgentHarness>();
builder.Services.AddHttpClient<OpenAiAgent>(client => client.Timeout = TimeSpan.FromSeconds(55));
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("agent", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 6, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BlotterSync API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token here",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (isPostgreSql)
{
    await app.Services.GetRequiredService<CloudSyncService>().InitializeLocalStoreAsync(CancellationToken.None);
}

if (app.Configuration.GetValue<bool>("Database:EnsureCreated", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BlotterSyncContext>();

    try
    {
        db.Database.EnsureCreated();

        var duplicatesToMerge = new (string CanonicalName, int CanonicalSeverity, string[] DuplicateNames)[]
        {
            ("Theft / Robbery", 3, new[] { "Theft", "Theft / Robbery" }),
            ("Physical Injury / Assault", 4, new[] { "Physical Assault", "Physical Injury / Assault" }),
            ("Vandalism / Property Damage", 2, new[] { "Vandalism & Property Damage", "Vandalism / Property Damage" }),
            ("Disturbance of Peace / Noise", 1, new[] { "Noise Complaint", "Disturbance of Peace / Noise" })
        };

        foreach (var (canonicalName, severity, dupNames) in duplicatesToMerge)
        {
            var matching = db.Categories.Where(c => dupNames.Contains(c.Name)).ToList();
            if (matching.Count > 1)
            {
                var keep = matching.First();
                keep.Name = canonicalName;
                keep.SeverityLevel = severity;
                var toRemove = matching.Skip(1).ToList();

                foreach (var rem in toRemove)
                {
                    var recordsToReassign = db.BlotterRecords.Where(r => r.CategoryId == rem.CategoryId).ToList();
                    foreach (var rec in recordsToReassign)
                    {
                        rec.CategoryId = keep.CategoryId;
                    }
                    db.Categories.Remove(rem);
                }

                db.SaveChanges();
            }
            else if (matching.Count == 1 && matching[0].Name != canonicalName)
            {
                matching[0].Name = canonicalName;
                matching[0].SeverityLevel = severity;
                db.SaveChanges();
            }
        }

        var standardCategories = new (string Name, int Severity)[]
        {
            ("Theft / Robbery", 3),
            ("Physical Injury / Assault", 4),
            ("Domestic Dispute", 3),
            ("Vandalism / Property Damage", 2),
            ("Disturbance of Peace / Noise", 1),
            ("Harassment", 2),
            ("Trespassing", 2),
            ("Fraud / Estafa", 2),
            ("Lost and Found", 1),
            ("Boundary Dispute", 1),
            ("Cyberbullying / Online Threats", 2),
            ("Other / Miscellaneous", 1)
        };

        var existingCategoryNames = db.Categories.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool addedAny = false;

        foreach (var cat in standardCategories)
        {
            if (!existingCategoryNames.Contains(cat.Name))
            {
                db.Categories.Add(new Category { Name = cat.Name, SeverityLevel = cat.Severity });
                addedAny = true;
            }
        }

        if (addedAny)
        {
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database seeding failed.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var audit = scope.ServiceProvider.GetRequiredService<AgentStore>();
    await audit.InitializeAsync();
    await audit.Runs.Where(r => r.Status == "running").ExecuteUpdateAsync(set => set
        .SetProperty(r => r.Status, "cancelled").SetProperty(r => r.Error, "The server restarted before this run finished. Please retry."));
}
if (demoEnabled) await DemoData.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
var defaults = new DefaultFilesOptions();
defaults.DefaultFileNames.Clear();
defaults.DefaultFileNames.Add("workspace.html");
app.UseDefaultFiles(defaults);
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();
