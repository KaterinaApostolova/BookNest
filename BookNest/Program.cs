using System.Globalization;
using BookNest.Data;
using BookNest.Interfaces;
using BookNest.Models;
using BookNest.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.Configure<LyraOptions>(builder.Configuration.GetSection("Lyra"));

builder.Services.AddDbContext<BookNestDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddErrorDescriber<AppIdentityErrorDescriber>()
.AddEntityFrameworkStores<BookNestDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<HomeViewService>();
builder.Services.AddScoped<IReadingAssistantService, RuleBasedReadingAssistantService>();
builder.Services.AddHttpClient<ILyraService, OllamaLyraService>(client =>
{
    var url = builder.Configuration["Lyra:Ollama:Url"] ?? "http://localhost:11434/";
    if (Uri.TryCreate(url, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHttpClient<IWebSearchService, FreeWebSearchService>(client =>
{
    client.BaseAddress = new Uri("https://html.duckduckgo.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BookNest Lyra reading assistant");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<QuoteService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<BookNestDbContext>();
    db.Database.Migrate();
    await SeedData.InitializeAsync(services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("bg") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
