using MathComicGenerator.Api.Services;
using MathComicGenerator.Api.Middleware;
using MathComicGenerator.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors();

// Add HTTP client for Gemini API
builder.Services.AddHttpClient<IGeminiAPIService, GeminiAPIService>();

// Add HTTP client for DeepSeek API
builder.Services.AddHttpClient<IDeepSeekAPIService, DeepSeekAPIService>();

// Register services
builder.Services.AddScoped<IGeminiAPIService, GeminiAPIService>();
builder.Services.AddScoped<IDeepSeekAPIService, DeepSeekAPIService>();
builder.Services.AddScoped<IImageGenerationService, ImageGenerationService>();
builder.Services.AddScoped<IComicGenerationService, ComicGenerationService>();
builder.Services.AddScoped<IPromptGenerationService, MathComicGenerator.Shared.Services.PromptGenerationService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddSingleton<ResourceManagementService>();
builder.Services.AddSingleton<ErrorLoggingService>();
builder.Services.AddSingleton<ConfigurationValidationService>();

var app = builder.Build();

// 验证配置
using (var scope = app.Services.CreateScope())
{
    var configValidator = scope.ServiceProvider.GetRequiredService<ConfigurationValidationService>();
    if (!configValidator.ValidateConfiguration())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError("应用启动失败：配置验证不通过");
        logger.LogInformation("请检查appsettings.json文件中的配置项");
        
        // 在开发环境中继续运行，但记录警告
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("配置验证失败，应用无法启动");
        }
        else
        {
            logger.LogWarning("开发环境：忽略配置验证错误，继续启动");
        }
    }
}

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
