using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using StemOMatiek.Components;
using StemOMatiek.Data;
using StemOMatiek.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=stematiek.db"));

// MudBlazor
builder.Services.AddMudServices();

// Services
builder.Services.AddScoped<ApiKeyProvider>();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<AnalyseService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Importeer partijprogramma's uit /programmas
await ProgrammaSeeder.SeedAsync(app.Services);

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
