using System.Text;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Application.Services;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Repositories;
using FinanceApp.Infrastructure.Services;
using FinanceApp.Infrastructure.Subscription;
using FinanceApp.Localization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration.Json;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHostedService<FinanceApp.Infrastructure.Services.RecurringTransactionJob>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBudgetNotificationService, BudgetNotificationService>();
builder.Services.AddScoped<IExpenseQueryService, FinanceApp.Infrastructure.Services.ExpenseQueryService>();
builder.Services.AddScoped<IMonthlyReportService, MonthlyReportService>();
builder.Services.AddSingleton<SubscriptionProductMapper>();
builder.Services.AddScoped<IAppleStoreTransactionVerifier, AppleStoreTransactionVerifier>();
builder.Services.AddScoped<IGooglePlaySubscriptionVerifier, GooglePlaySubscriptionVerifier>();
builder.Services.AddScoped<ISubscriptionEntitlementService, SubscriptionEntitlementService>();
builder.Services.AddScoped<ISupportingDocumentService>(sp =>
{
    var repo = sp.GetRequiredService<IRepository<FinanceApp.Domain.Entities.SupportingDocument>>();
    var env  = sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
    var uploadRoot = Path.Combine(env.WebRootPath, "uploads", "documents");
    return new SupportingDocumentService(repo, uploadRoot);
});

// Identity (required for JWT login)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<FinanceDbContext>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
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

builder.Services.AddControllers();

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

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure DB schema exists (required for SQLite in Testing; no-op for SQL Server with migrations)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await db.Database.EnsureCreatedAsync();
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

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseCors();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
