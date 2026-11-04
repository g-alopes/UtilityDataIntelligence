
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using UtilityData.Web.Components;
using UtilityData.Web.Endpoints;
using UtilityData.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var keysDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    ".keys");

Directory.CreateDirectory(keysDirectory);

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("UtilityDataIntelligence");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-UtilityData.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("Api", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? "https://localhost:7041/";

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<UiAuthSession>();
builder.Services.AddScoped<UtilityApiClient>();
builder.Services.AddScoped<UiAuthService>();
builder.Services.AddScoped<DashboardApiService>();
builder.Services.AddScoped<CustomerAccountsApiService>();
builder.Services.AddScoped<ImportsApiService>();
builder.Services.AddScoped<CustomFieldsApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapBffEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
