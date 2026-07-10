using JobSearchTracker.Data;
using JobSearchTracker.Data.Services;
using JobSearchTracker.Web.Components;
using JobSearchTracker.Web.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Raise the SignalR message size limit so resume uploads (up to 10 MB) fit through the Blazor Server circuit.
builder.Services.Configure<HubOptions>(options =>
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddSingleton<ResumeStorageService>();
builder.Services.AddHttpClient<LinkedInJobSearchService>();
builder.Services.AddScoped<SearchRequestService>();
builder.Services.AddScoped<JobPostingService>();
builder.Services.AddScoped<JobSearchRunnerService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

var resumesRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, app.Configuration["ResumeStorage:RootPath"] ?? "Resumes"));
Directory.CreateDirectory(resumesRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(resumesRoot),
    RequestPath = "/resumes"
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
