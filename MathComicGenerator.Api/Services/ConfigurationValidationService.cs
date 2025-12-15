using Microsoft.Extensions.Options;

namespace MathComicGenerator.Api.Services;

public class ConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IConfiguration _configuration;

    public ConfigurationValidationService(
        ILogger<ConfigurationValidationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public bool ValidateConfiguration()
    {
        var isValid = true;
        var issues = new List<string>();

        // 验证Gemini API配置
        var geminiApiKey = _configuration["GeminiAPI:ApiKey"];
        if (string.IsNullOrEmpty(geminiApiKey) || geminiApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            issues.Add("Gemini API密钥未配置或使用默认占位符");
            isValid = false;
        }

        var geminiBaseUrl = _configuration["GeminiAPI:BaseUrl"];
        if (string.IsNullOrEmpty(geminiBaseUrl))
        {
            issues.Add("Gemini API基础URL未配置");
            isValid = false;
        }

        // 验证存储配置
        var storagePath = _configuration["Storage:BasePath"];
        if (string.IsNullOrEmpty(storagePath))
        {
            issues.Add("存储路径未配置");
            isValid = false;
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(storagePath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    _logger.LogInformation("已创建存储目录: {Path}", fullPath);
                }
            }
            catch (Exception ex)
            {
                issues.Add($"无法创建存储目录: {ex.Message}");
                isValid = false;
            }
        }

        // 验证资源管理配置
        var maxConcurrentRequests = _configuration.GetValue<int>("ResourceManagement:MaxConcurrentRequests");
        if (maxConcurrentRequests <= 0)
        {
            issues.Add("最大并发请求数配置无效");
            isValid = false;
        }

        // 记录验证结果
        if (isValid)
        {
            _logger.LogInformation("配置验证通过");
        }
        else
        {
            _logger.LogError("配置验证失败，发现以下问题:");
            foreach (var issue in issues)
            {
                _logger.LogError("- {Issue}", issue);
            }
        }

        return isValid;
    }

    public Dictionary<string, object> GetConfigurationSummary()
    {
        return new Dictionary<string, object>
        {
            ["GeminiAPI"] = new
            {
                BaseUrl = _configuration["GeminiAPI:BaseUrl"],
                HasApiKey = !string.IsNullOrEmpty(_configuration["GeminiAPI:ApiKey"]) && 
                           _configuration["GeminiAPI:ApiKey"] != "YOUR_GEMINI_API_KEY_HERE",
                TimeoutSeconds = _configuration.GetValue<int>("GeminiAPI:TimeoutSeconds"),
                MaxRetries = _configuration.GetValue<int>("GeminiAPI:MaxRetries")
            },
            ["Storage"] = new
            {
                BasePath = _configuration["Storage:BasePath"],
                MaxStorageSize = _configuration.GetValue<long>("Storage:MaxStorageSize"),
                MaxComicsPerUser = _configuration.GetValue<int>("Storage:MaxComicsPerUser")
            },
            ["ResourceManagement"] = new
            {
                MaxConcurrentRequests = _configuration.GetValue<int>("ResourceManagement:MaxConcurrentRequests"),
                RequestTimeoutMs = _configuration.GetValue<int>("ResourceManagement:RequestTimeoutMs"),
                MaxMemoryUsagePercent = _configuration.GetValue<double>("ResourceManagement:MaxMemoryUsagePercent")
            },
            ["Security"] = new
            {
                EnableRateLimit = _configuration.GetValue<bool>("Security:EnableRateLimit"),
                MaxRequestsPerMinute = _configuration.GetValue<int>("Security:MaxRequestsPerMinute"),
                EnableOriginValidation = _configuration.GetValue<bool>("Security:EnableOriginValidation")
            }
        };
    }
}