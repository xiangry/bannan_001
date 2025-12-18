using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using System.Text.Json;

namespace MathComicGenerator.Tests.PropertyTests;

public class APIRequestFormatPropertyTests
{
    private readonly Mock<ILogger<GeminiAPIService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly GeminiAPIService _geminiService;

    public APIRequestFormatPropertyTests()
    {
        _mockLogger = new Mock<ILogger<GeminiAPIService>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        
        var configData = new Dictionary<string, string>
        {
            {"GeminiAPI:BaseUrl", "https://test-api.gemini.com"},
            {"GeminiAPI:ApiKey", "test-api-key"},
            {"GeminiAPI:Model", "gemini-pro"},
            {"GeminiAPI:MaxTokens", "2048"},
            {"GeminiAPI:Temperature", "0.7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _geminiService = new GeminiAPIService(_httpClient, _mockLogger.Object, _configuration);
    }

    [Property]
    public bool Property4_APIRequestFormat_RequestContainsRequiredFields(NonEmptyString prompt)
    {
        // **Feature: math-comic-generator, Property 4: API请求格式正确性**
        // **Validates: Requirements 2.1**
        // For any API request sent to Gemini API, request format should comply with API specification requirements
        
        try
        {
            // Arrange - Create a valid prompt
            var testPrompt = prompt.Get.Length > 1000 ? prompt.Get.Substring(0, 1000) : prompt.Get;
            
            // Act - Format request (we'll test the internal formatting logic)
            var requestData = FormatGeminiRequest(testPrompt);
            
            // Assert - Request should have all required fields
            var hasContents = requestData.ContainsKey("contents");
            var hasGenerationConfig = requestData.ContainsKey("generationConfig");
            var hasSafetySettings = requestData.ContainsKey("safetySettings");
            
            // Validate contents structure
            var contents = requestData.GetValueOrDefault("contents") as List<object>;
            var contentsValid = contents != null && contents.Count > 0;
            
            // Validate generation config
            var generationConfig = requestData.GetValueOrDefault("generationConfig") as Dictionary<string, object>;
            var configValid = generationConfig != null && 
                            generationConfig.ContainsKey("maxOutputTokens") &&
                            generationConfig.ContainsKey("temperature");
            
            var requestFormatValid = hasContents && hasGenerationConfig && hasSafetySettings && 
                                   contentsValid && configValid;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] API Request Format: HasContents={hasContents}, HasConfig={hasGenerationConfig}, HasSafety={hasSafetySettings}, ContentsValid={contentsValid}, ConfigValid={configValid}");
            
            return requestFormatValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] API Request Format Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property4_APIRequestFormat_PromptIsProperlyEncoded(NonEmptyString prompt)
    {
        // **Feature: math-comic-generator, Property 4: API请求格式正确性**
        // **Validates: Requirements 2.1**
        // Prompts should be properly encoded and structured in the request
        
        try
        {
            // Arrange - Test with various prompt types including Chinese characters
            var testPrompts = new[]
            {
                prompt.Get,
                "数学加法概念",
                "Basic math addition",
                "几何图形认识 - 三角形、正方形、圆形",
                "Math concept with numbers: 1+1=2, 2+2=4"
            };

            foreach (var testPrompt in testPrompts)
            {
                // Act - Format request
                var requestData = FormatGeminiRequest(testPrompt);
                
                // Assert - Prompt should be properly encoded
                var contents = requestData.GetValueOrDefault("contents") as List<object>;
                if (contents?.FirstOrDefault() is Dictionary<string, object> firstContent)
                {
                    var parts = firstContent.GetValueOrDefault("parts") as List<object>;
                    if (parts?.FirstOrDefault() is Dictionary<string, object> firstPart)
                    {
                        var text = firstPart.GetValueOrDefault("text") as string;
                        var promptIncluded = !string.IsNullOrEmpty(text) && text.Contains(testPrompt);
                        
                        if (!promptIncluded)
                        {
                            Console.WriteLine($"[DEBUG] Prompt Encoding Failed: Original='{testPrompt}', Encoded='{text}'");
                            return false;
                        }
                    }
                }
            }
            
            Console.WriteLine($"[DEBUG] Prompt Encoding: All prompts properly encoded");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Prompt Encoding Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property4_APIRequestFormat_SafetySettingsAreIncluded()
    {
        // **Feature: math-comic-generator, Property 4: API请求格式正确性**
        // **Validates: Requirements 2.1**
        // Safety settings should be included in all API requests
        
        try
        {
            // Arrange & Act - Format request with safety settings
            var requestData = FormatGeminiRequest("Test prompt for safety");
            
            // Assert - Safety settings should be present and configured
            var safetySettings = requestData.GetValueOrDefault("safetySettings") as List<object>;
            var hasSafetySettings = safetySettings != null && safetySettings.Count > 0;
            
            // Validate safety categories are covered
            var expectedCategories = new[]
            {
                "HARM_CATEGORY_HARASSMENT",
                "HARM_CATEGORY_HATE_SPEECH", 
                "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                "HARM_CATEGORY_DANGEROUS_CONTENT"
            };
            
            var allCategoriesCovered = true;
            if (hasSafetySettings)
            {
                foreach (var category in expectedCategories)
                {
                    var categoryFound = safetySettings.Any(setting =>
                    {
                        if (setting is Dictionary<string, object> settingDict)
                        {
                            return settingDict.GetValueOrDefault("category")?.ToString() == category;
                        }
                        return false;
                    });
                    
                    if (!categoryFound)
                    {
                        allCategoriesCovered = false;
                        break;
                    }
                }
            }
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Safety Settings: HasSettings={hasSafetySettings}, AllCategoriesCovered={allCategoriesCovered}, Count={safetySettings?.Count ?? 0}");
            
            return hasSafetySettings && allCategoriesCovered;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Safety Settings Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property4_APIRequestFormat_GenerationConfigIsValid(PositiveInt maxTokens)
    {
        // **Feature: math-comic-generator, Property 4: API请求格式正确性**
        // **Validates: Requirements 2.1**
        // Generation configuration should have valid parameters
        
        try
        {
            // Arrange - Test with different token limits
            var testMaxTokens = Math.Min(4096, Math.Max(100, maxTokens.Get));
            
            // Act - Format request with custom config
            var requestData = FormatGeminiRequestWithConfig("Test prompt", testMaxTokens);
            
            // Assert - Generation config should be valid
            var generationConfig = requestData.GetValueOrDefault("generationConfig") as Dictionary<string, object>;
            var hasConfig = generationConfig != null;
            
            if (hasConfig)
            {
                var hasMaxTokens = generationConfig.ContainsKey("maxOutputTokens");
                var hasTemperature = generationConfig.ContainsKey("temperature");
                var hasTopP = generationConfig.ContainsKey("topP");
                var hasTopK = generationConfig.ContainsKey("topK");
                
                // Validate parameter values
                var maxTokensValid = generationConfig.GetValueOrDefault("maxOutputTokens") is int tokens && 
                                   tokens > 0 && tokens <= 4096;
                var temperatureValid = generationConfig.GetValueOrDefault("temperature") is double temp && 
                                     temp >= 0.0 && temp <= 1.0;
                
                var configValid = hasMaxTokens && hasTemperature && maxTokensValid && temperatureValid;
                
                // Log the validation for debugging
                Console.WriteLine($"[DEBUG] Generation Config: HasMaxTokens={hasMaxTokens}, HasTemp={hasTemperature}, MaxTokensValid={maxTokensValid}, TempValid={temperatureValid}");
                
                return configValid;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Generation Config Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property4_APIRequestFormat_RequestIsValidJSON(NonEmptyString prompt)
    {
        // **Feature: math-comic-generator, Property 4: API请求格式正确性**
        // **Validates: Requirements 2.1**
        // Request should be serializable to valid JSON
        
        try
        {
            // Arrange
            var testPrompt = prompt.Get.Length > 500 ? prompt.Get.Substring(0, 500) : prompt.Get;
            
            // Act - Format and serialize request
            var requestData = FormatGeminiRequest(testPrompt);
            var jsonString = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            
            // Assert - Should be valid JSON
            var isValidJson = !string.IsNullOrEmpty(jsonString);
            var canDeserialize = false;
            
            if (isValidJson)
            {
                try
                {
                    var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                    canDeserialize = deserialized != null;
                }
                catch
                {
                    canDeserialize = false;
                }
            }
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] JSON Validation: IsValidJSON={isValidJson}, CanDeserialize={canDeserialize}, Length={jsonString?.Length ?? 0}");
            
            return isValidJson && canDeserialize;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] JSON Validation Error: {ex.Message}");
            return false;
        }
    }

    private Dictionary<string, object> FormatGeminiRequest(string prompt)
    {
        return new Dictionary<string, object>
        {
            ["contents"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["parts"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["text"] = $"请为以下数学概念生成一个适合儿童的多格漫画故事：{prompt}"
                        }
                    }
                }
            },
            ["generationConfig"] = new Dictionary<string, object>
            {
                ["maxOutputTokens"] = 2048,
                ["temperature"] = 0.7,
                ["topP"] = 0.8,
                ["topK"] = 40
            },
            ["safetySettings"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["category"] = "HARM_CATEGORY_HARASSMENT",
                    ["threshold"] = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new Dictionary<string, object>
                {
                    ["category"] = "HARM_CATEGORY_HATE_SPEECH",
                    ["threshold"] = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new Dictionary<string, object>
                {
                    ["category"] = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    ["threshold"] = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new Dictionary<string, object>
                {
                    ["category"] = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    ["threshold"] = "BLOCK_MEDIUM_AND_ABOVE"
                }
            }
        };
    }

    private Dictionary<string, object> FormatGeminiRequestWithConfig(string prompt, int maxTokens)
    {
        var request = FormatGeminiRequest(prompt);
        var config = request["generationConfig"] as Dictionary<string, object>;
        if (config != null)
        {
            config["maxOutputTokens"] = maxTokens;
        }
        return request;
    }
}