using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog rolling file
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(path: "logs/dashboard-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
