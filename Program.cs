using CBOS.Components;
using CBOS.Components.Pages.Admin;
using DotNetEnv;
using CBOS.Components.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

Env.Load();
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

var options = new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,        // added from second
    AutoConnectRealtime = true
};

var supabase = new Supabase.Client(url, key, options);
await supabase.InitializeAsync();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(supabase);

builder.Services.AddScoped<AdminSupabase>();
builder.Services.AddScoped<AuthService>();         // kept from first
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>                              // kept from first (more complete)
    {
        opt.LoginPath         = "/login";
        opt.LogoutPath        = "/logout";
        opt.AccessDeniedPath  = "/login";
        opt.ExpireTimeSpan    = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState(); // added from second
builder.Services.AddHttpContextAccessor();          // kept from first

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();