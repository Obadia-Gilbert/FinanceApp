using System.Text;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Application.Services;
using FinanceApp.Infrastructure.Email;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Repositories;
using FinanceApp.Infrastructure.Services;
using FinanceApp.Infrastructure.Subscription;
using FinanceApp.Localization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// The API has no physical wwwroot/ dir checked in (it serves no static files), so
// IWebHostEnvironment.WebRootPath is null here unlike FinanceApp.Web. Upload-path
// factories (SupportingDocumentService, AccountDeletionService) rely on WebRootPath
// being set; the directories themselves are created on demand when first written to.
if (string.IsNullOrEmpty(builder.Environment.WebRootPath))
    builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

// Shared billing + default LocalDB fallback — must load *first* so user secrets / env override ConnectionStrings.
var sharedSettings = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Shared", "appsettings.shared.json"));
var sharedSource = new JsonConfigurationSource
{
    Path = sharedSettings,
    Optional = true,
    ReloadOnChange = true
};
sharedSource.ResolveFileProvider();
builder.Configuration.Sources.Insert(0, sharedSource);

// DbContext (use SQLite when Testing for integration tests — enforces constraints so Identity works)
var isTesting = string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
if (isTesting)
{
    var sqlitePath = builder.Configuration["Testing:SqlitePath"]
        ?? Path.Combine(Path.GetTempPath(), "FinanceAppTest_" + Guid.NewGuid().ToString("N") + ".db");
    builder.Services.AddDbContext<FinanceDbContext>(options => options.UseSqlite("Data Source=" + sqlitePath));
}
else
    builder.Services.AddDbContext<FinanceDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Data Protection keys persisted in the DB (not local disk) so auth/reset tokens
// survive a redeploy or a second replica — see GOING_LIVE.md.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<FinanceDbContext>();

// Jobs:Enabled gates the hosted services that would duplicate side effects (creating
// recurring transactions, creating reminder notifications) if run in more than one
// process against the same database — e.g. Web and API both running against
// production. Defaults to true so local `dotnet run` keeps working unchanged; set
// explicitly per-process in production (see docker-compose.yml / GOING_LIVE.md).
var jobsEnabled = builder.Configuration.GetValue("Jobs:Enabled", true);

// Repositories & Application services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ICategoryBudgetService, CategoryBudgetService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IIncomeService, IncomeService>();
builder.Services.AddScoped<IRecurringTemplateService, RecurringTemplateService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
if (jobsEnabled)
    builder.Services.AddHostedService<FinanceApp.Infrastructure.Services.RecurringTransactionJob>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBudgetNotificationService, BudgetNotificationService>();
builder.Services.AddScoped<IDailyActivityReminderService, DailyActivityReminderService>();
builder.Services.AddScoped<IExpenseQueryService, FinanceApp.Infrastructure.Services.ExpenseQueryService>();
builder.Services.AddScoped<IMonthlyReportService, MonthlyReportService>();
builder.Services.AddSingleton<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.Configure<FinanceApp.Infrastructure.Services.ExchangeRateSettings>(
    builder.Configuration.GetSection("ExchangeRates:Provider"));
builder.Services.AddHttpClient(FinanceApp.Infrastructure.Services.ExchangeRateApiProvider.HttpClientName);
builder.Services.AddSingleton<IExchangeRateStore, FinanceApp.Infrastructure.Services.ExchangeRateStore>();
builder.Services.AddSingleton<IExchangeRateProvider, FinanceApp.Infrastructure.Services.ExchangeRateApiProvider>();
if (jobsEnabled)
    builder.Services.AddHostedService<FinanceApp.Infrastructure.Services.ExchangeRateRefreshJob>();
builder.Services.AddSingleton<SubscriptionProductMapper>();
builder.Services.AddScoped<IAppleStoreTransactionVerifier, AppleStoreTransactionVerifier>();
builder.Services.AddScoped<IGooglePlaySubscriptionVerifier, GooglePlaySubscriptionVerifier>();
builder.Services.AddScoped<ISubscriptionEntitlementService, SubscriptionEntitlementService>();
builder.Services.AddScoped<ISubscriptionBillingWebhookService, SubscriptionBillingWebhookService>();
builder.Services.AddScoped<IStripeBillingService, StripeBillingService>();
builder.Services.AddScoped<IStripeBillingWebhookHandler, StripeBillingWebhookHandler>();
// Single upload root for everything user-uploaded (documents/, profiles/) — see
// FinanceApp.Application.Interfaces.Services.IFileStorage for why this is behind an
// interface rather than services touching File/Directory directly.
builder.Services.AddSingleton<IFileStorage>(sp =>
{
    var env = sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
    return new LocalFileStorage(Path.Combine(env.WebRootPath, "uploads"));
});
builder.Services.AddScoped<ISupportingDocumentService>(sp =>
{
    var repo = sp.GetRequiredService<IRepository<FinanceApp.Domain.Entities.SupportingDocument>>();
    var fileStorage = sp.GetRequiredService<IFileStorage>();
    return new SupportingDocumentService(repo, fileStorage);
});
builder.Services.AddScoped<IAccountDeletionService>(sp =>
{
    var context = sp.GetRequiredService<FinanceDbContext>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var fileStorage = sp.GetRequiredService<IFileStorage>();
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AccountDeletionService>>();
    return new AccountDeletionService(context, userManager, fileStorage, logger);
});
if (jobsEnabled)
    builder.Services.AddHostedService<DailyActivityReminderJob>();

// Identity (required for JWT login). AddDefaultTokenProviders is required so
// password-reset tokens can be generated for the forgot/reset password flow.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<FinanceDbContext>()
    .AddDefaultTokenProviders();

// Reset-link tokens stay valid for 2h (matches FinanceApp.Web).
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(2));

// Email — used by the forgot-password flow. Priority:
//   1. Brevo HTTP API (preferred for production deliverability)
//   2. SMTP (works with Brevo's SMTP relay or any other provider)
//   3. NoOp (e.g. Testing) so requests don't blow up.
builder.Services.Configure<BrevoSettings>(builder.Configuration.GetSection("Brevo"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpClient(BrevoEmailService.HttpClientName);

if (!string.IsNullOrWhiteSpace(builder.Configuration["Brevo:ApiKey"]))
    builder.Services.AddTransient<IEmailService, BrevoEmailService>();
else if (!string.IsNullOrWhiteSpace(builder.Configuration["EmailSettings:SmtpServer"]))
    builder.Services.AddTransient<IEmailService, EmailService>();
else
    builder.Services.AddSingleton<IEmailService, NoOpEmailService>();
builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();

// Branded email rendering — single source of truth for layout / brand tokens /
// localized copy across every email call site in the API.
builder.Services.Configure<EmailBrandingOptions>(builder.Configuration.GetSection(EmailBrandingOptions.SectionName));
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<LocalizedEmailTemplates>();
builder.Services.AddScoped<IBrandedEmailSender, BrandedEmailSender>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
// The repo's appsettings.json historically shipped a real, working default here — a
// publicly known signing key. Refuse to boot on it outside Development, rather than
// silently accepting requests signed by anyone who has read the source.
const string KnownPublicDefaultJwtKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
if (jwtKey == KnownPublicDefaultJwtKey && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "Jwt:Key is still set to the placeholder value committed in appsettings.json. " +
        "Set a real, randomly generated value via Jwt__Key before deploying — see GOING_LIVE.md.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinanceApp.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FinanceApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Enums (notably Currency) serialize as their string name ("TZS"), not their ordinal,
// so a currency's identity in the API contract never depends on the enum's declaration
// order — matching how it's now stored in the database.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddHttpClient();

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en")
        .AddSupportedCultures(SupportedLanguages.Codes)
        .AddSupportedUICultures(SupportedLanguages.Codes);
    options.RequestCultureProviders =
    [
        new AcceptLanguageHeaderRequestCultureProvider(),
        new QueryStringRequestCultureProvider { QueryStringKey = "culture", UIQueryStringKey = "ui-culture" }
    ];
});

// OpenAPI (built-in .NET 10; no Swashbuckle to avoid assembly conflicts in integration tests)
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// CORS — locked to explicitly configured origins (Cors:AllowedOrigins:0, :1, ...) in
// every environment except Development, where an empty list falls back to allow-any
// so Swagger/local tooling keeps working without per-dev config. Native mobile HTTP
// calls aren't subject to CORS at all; this matters for Swagger, Expo Web, and any
// future browser-based client. See GOING_LIVE.md.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader();
        else if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        else
            policy.DisallowCredentials(); // no origins configured in prod: deny by default rather than open
    });
});

// Forwarded headers — required behind Caddy/nginx/any reverse proxy, or
// Request.Scheme reads "http" and UseHttpsRedirection can loop. See GOING_LIVE.md.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The proxy runs in the same docker-compose network, not a fixed public IP, so
    // clear the default known-networks restriction rather than enumerate the bridge.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<FinanceDbContext>();

var app = builder.Build();

// Schema setup. Testing uses a throwaway SQLite file, so EnsureCreated is correct
// there (no migration history to preserve). For SQL Server, calling EnsureCreated
// against a FRESH database creates the schema with no __EFMigrationsHistory row,
// which then permanently breaks `dotnet ef database update` — see GOING_LIVE.md.
// Production applies migrations as a separate, explicit CD step (before this process
// ever starts) rather than at app startup; Database:AutoMigrate is an opt-in for local
// dev convenience only, off by default.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    if (isTesting)
        await db.Database.EnsureCreatedAsync();
    else if (builder.Configuration.GetValue("Database:AutoMigrate", false))
        await db.Database.MigrateAsync();
}

// Seed roles on startup (same as Web)
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await RoleSeeder.SeedRolesAndAdminAsync(userManager, roleManager, config, logger);
}

app.MapOpenApi(); // serves /openapi/v1.json
if (app.Environment.IsDevelopment())
{
    // Optional: add Swagger UI package later if needed
}

app.UseForwardedHeaders();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseCors();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
