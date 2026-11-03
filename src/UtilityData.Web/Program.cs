using UtilityData.Web.Components;
using UtilityData.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient("Api", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7041/";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddScoped<UiAuthSession>();
builder.Services.AddScoped<UtilityApiClient>();
builder.Services.AddScoped<UiAuthService>();
builder.Services.AddScoped<DashboardApiService>();
builder.Services.AddScoped<CustomerAccountsApiService>();
builder.Services.AddScoped<ImportsApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
