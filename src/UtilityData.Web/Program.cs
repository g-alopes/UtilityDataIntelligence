var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient("Api", c =>
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7041"));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<UtilityData.Web.Components.App>().AddInteractiveServerRenderMode();
app.Run();
