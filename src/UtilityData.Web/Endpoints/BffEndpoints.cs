
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UtilityData.Web.Models;

namespace UtilityData.Web.Endpoints;

public static class BffEndpoints
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapBffEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff");

        group.MapGet("/login", LoginPageAsync)
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();

        return app;
    }

    private static IResult LoginPageAsync(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);

        var token = HtmlEncoder.Default.Encode(
            tokens.RequestToken ?? string.Empty);

        var error = context.Request.Query["error"].ToString();

        var errorMessage = error switch
        {
            "missing" => "Enter your email address and password.",
            "invalid" => "The email address or password is incorrect.",
            "tenant" => "Your tenant could not be loaded or is inactive.",
            _ => null
        };

        var errorHtml = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $"<div class=\"alert-error\">{HtmlEncoder.Default.Encode(errorMessage)}</div>";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Sign in | Utility Data Intelligence</title>
                <link rel="stylesheet" href="/app.css">
            </head>
            <body>
                <div class="auth-container">
                    <div class="auth-card">
                        <div class="eyebrow">UTILITY DATA INTELLIGENCE</div>

                        <h1>Welcome back</h1>

                        <p class="muted">
                            Sign in to access your implementation workspace.
                        </p>

                        <form method="post" action="/bff/login">
                            <input type="hidden"
                                   name="__RequestVerificationToken"
                                   value="{{token}}">

                            <div class="form-group">
                                <label for="email">Email address</label>

                                <input id="email"
                                       name="email"
                                       type="email"
                                       class="form-control"
                                       placeholder="admin@company.com"
                                       autocomplete="username"
                                       required>
                            </div>

                            <div class="form-group">
                                <label for="password">Password</label>

                                <input id="password"
                                       name="password"
                                       type="password"
                                       class="form-control"
                                       placeholder="Enter your password"
                                       autocomplete="current-password"
                                       required>
                            </div>

                            {{errorHtml}}

                            <button type="submit" class="btn-primary">
                                Sign in
                            </button>
                        </form>

                        <p class="auth-footer">
                            Don't have a workspace?
                            <a href="/register">Create a tenant</a>
                        </p>
                    </div>
                </div>
            </body>
            </html>
            """;

        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IHttpClientFactory factory,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        await antiforgery.ValidateRequestAsync(context);

        var form = await context.Request.ReadFormAsync(ct);

        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect("/bff/login?error=missing");
        }

        var api = factory.CreateClient("Api");

        using var loginResponse = await api.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequest(email, password),
            ct);

        if (!loginResponse.IsSuccessStatusCode)
        {
            return Results.Redirect("/bff/login?error=invalid");
        }

        var auth = await loginResponse.Content
            .ReadFromJsonAsync<AuthResponse>(Json, ct);

        if (auth is null ||
            string.IsNullOrWhiteSpace(auth.AccessToken))
        {
            return Results.Redirect("/bff/login?error=invalid");
        }

        using var tenantRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "api/tenant/me");

        tenantRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        using var tenantResponse = await api.SendAsync(
            tenantRequest,
            ct);

        if (!tenantResponse.IsSuccessStatusCode)
        {
            return Results.Redirect("/bff/login?error=tenant");
        }

        var tenant = await tenantResponse.Content
            .ReadFromJsonAsync<TenantInfoDto>(Json, ct);

        if (tenant is null || !tenant.IsActive)
        {
            return Results.Redirect("/bff/login?error=tenant");
        }

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                tenant.UserId.ToString()),

            new(
                ClaimTypes.Email,
                tenant.Email ?? email),

            new(
                ClaimTypes.Name,
                tenant.Email ?? email),

            new(
                ClaimTypes.Role,
                tenant.Role),

            new(
                "tenant_id",
                tenant.TenantId.ToString()),

            new(
                "tenant_name",
                tenant.TenantName),

            new(
                "tenant_code",
                tenant.TenantCode),

            new(
                "access_token",
                auth.AccessToken)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Results.Redirect("/dashboard");
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(context);

        await context.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Redirect("/bff/login");
    }
}
