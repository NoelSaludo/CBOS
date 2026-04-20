using CBOS.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using CBOS.Services;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<VerificationService>();

var supabaseUrl = builder.Configuration["Supabase:Url"]!;
var supabaseAnonKey = builder.Configuration["Supabase:AnonKey"]!;

builder.Services.AddSingleton(_ => new Client(supabaseUrl, supabaseAnonKey));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var supabaseClient = scope.ServiceProvider.GetRequiredService<Client>();
    await supabaseClient.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
