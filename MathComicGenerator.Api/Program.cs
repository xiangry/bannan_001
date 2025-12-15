using MathComicGenerator.Api.Services;
using MathComicGenerator.Api.Middleware;
using MathComicGenerator.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors();

// Add HTTP client for Gemini API
builder.Services.AddHttpClient<IGeminiAPIService, GeminiAPIService>();

// Register services
builder.Services.AddScoped<IGeminiAPIService, GeminiAPIService>();
builder.Services.AddScoped<IComicGenerationService, ComicGenerationService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddSingleton<ResourceManagementService>();
builder.Services.AddSingleton<ErrorLoggingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Use global error handling in production
    app.UseMiddleware<GlobalErrorHandlingMiddleware>();
}

// Add custom middleware
app.UseMiddleware<SecurityMiddleware>();
app.UseMiddleware<RequestValidationMiddleware>();
app.UseMiddleware<ResponseFormattingMiddleware>();

// Add CORS
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();
