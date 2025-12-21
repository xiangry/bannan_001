using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Keep controllers (if any) and serve static files from wwwroot
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

// Fallback to index.html for SPA routes
app.MapFallbackToFile("index.html");

app.Run();
