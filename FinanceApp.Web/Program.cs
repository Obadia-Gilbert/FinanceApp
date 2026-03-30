using FinanceApp.Application.Interfaces;
using FinanceApp.Infrastructure.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FinanceApp.Infrastructure.Identity;   
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Twitter;
using FinanceApp.Application.Interfaces.Services; // application service interfaces
using FinanceApp.Application.Services;
using FinanceApp.Infrastructure.Services; // EmailService, UserService, SubscriptionEntitlementService
using FinanceApp.Infrastructure.Subscription;
using FinanceApp.Localization;
using FinanceApp.Web.Infrastructure;
using FinanceApp.Web.Services;
using Microsoft.AspNetCore.Localization;
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

// Add services to the container.
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ICategoryBudgetService, CategoryBudgetService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IIncomeService, IncomeService>();
builder.Services.AddScoped<IRecurringTemplateService, RecurringTemplateService>();
builder.Services.AddHostedService<FinanceApp.Infrastructure.Services.RecurringTransactionJob>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<ISupportingDocumentService>(sp =>
{
    var repo = sp.GetRequiredService<IRepository<FinanceApp.Domain.Entities.SupportingDocument>>();
    var env  = sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
    var uploadRoot = Path.Combine(env.WebRootPath, "uploads", "documents");
    return new SupportingDocumentService(repo, uploadRoot);
});
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBudgetNotificationService, BudgetNotificationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IExpenseQueryService, FinanceApp.Infrastructure.Services.ExpenseQueryService>();
builder.Services.AddScoped<IMonthlyReportService, MonthlyReportService>();
builder.Services.AddSingleton<SubscriptionProductMapper>();
builder.Services.AddScoped<IAppleStoreTransactionVerifier, AppleStoreTransactionVerifier>();
builder.Services.AddScoped<IGooglePlaySubscriptionVerifier, GooglePlaySubscriptionVerifier>();
builder.Services.AddScoped<ISubscriptionEntitlementService, SubscriptionEntitlementService>();
builder.Services.AddScoped<ISharedReportService, SharedReportService>();
builder.Services.AddSingleton<ICurrencyConversionService, CurrencyConversionService>();
//builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        // Optional but recommended for FinanceApp
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<FinanceDbContext>();

var authenticationBuilder = builder.Services.AddAuthentication();
var isDevelopment = builder.Environment.IsDevelopment();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        // Ensure email/name claims are available for Identity external login (avoids empty ClaimTypes.Email).
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Email, "email");
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.GivenName, "given_name");
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Surname, "family_name");
        if (isDevelopment)
        {
            // Avoid OAuth correlation cookie issues during local HTTP testing.
            options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        }
    });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authenticationBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        if (isDevelopment)
        {
            // Avoid OAuth correlation cookie issues during local HTTP testing.
            options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        }
    });
}

var twitterConsumerKey = builder.Configuration["Authentication:Twitter:ConsumerKey"];
var twitterConsumerSecret = builder.Configuration["Authentication:Twitter:ConsumerSecret"];
if (!string.IsNullOrWhiteSpace(twitterConsumerKey) && !string.IsNullOrWhiteSpace(twitterConsumerSecret))
{
    authenticationBuilder.AddTwitter(options =>
    {
        options.ConsumerKey = twitterConsumerKey;
        options.ConsumerSecret = twitterConsumerSecret;
        if (isDevelopment)
        {
            // Avoid OAuth correlation cookie issues during local HTTP testing.
            options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        }
    });
}

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(2));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, IdentityEmailSender>();
builder.Services.AddRazorPages(); // For Identity UI

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en")
        .AddSupportedCultures(SupportedLanguages.Codes)
        .AddSupportedUICultures(SupportedLanguages.Codes);
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new UserLanguageRequestCultureProvider(),
        new QueryStringRequestCultureProvider { QueryStringKey = "culture", UIQueryStringKey = "ui-culture" },
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(o =>
    {
        o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var config = services.GetRequiredService<IConfiguration>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await RoleSeeder.SeedRolesAndAdminAsync(userManager, roleManager, config, logger);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();

app.UseAuthentication(); // Add this before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Users}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages(); // For Identity UI 

app.Run();
