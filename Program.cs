using CBOS.Components;
using CBOS.Components.Pages.Admin;
using DotNetEnv;
using CBOS.Components.Services;
using CBOS.Components.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

Env.Load();
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
{
    throw new InvalidOperationException("SUPABASE_URL and SUPABASE_KEY environment variables must be set.");
}

var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true,
    AutoRefreshToken = true
};

var supabase = new Supabase.Client(url, key, options);
try
{
    await supabase.InitializeAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to initialize Supabase: {ex.Message}");
    Console.WriteLine("The application will continue running, but Supabase features may not work.");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(supabase);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminSupabase>();

// ── MISSING: Cookie authentication config ──────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath         = "/login";
        opt.LogoutPath        = "/logout";
        opt.AccessDeniedPath  = "/login";
        opt.ExpireTimeSpan    = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });

// ── MISSING: Authorization + Blazor auth state + HttpContext ───────────────
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// ── MISSING: Authentication & Authorization middleware ─────────────────────
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();