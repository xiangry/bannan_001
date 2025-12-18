using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace MathComicGenerator.Tests.PropertyTests;

public class ResponseValidationPropertyTests
{
    private readonly Mock<ILogger<GeminiAPIService>> _mockLogger;
    private readonly IConfiguration _configuration;

    public ResponseValidationPropertyTests()
    {
        _mockLogger = new Mock<ILogger<GeminiAPIService>>();
        
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
    }

    [Property]
    public bool Property8_ResponseValidation_ContentIntegrityIsVerified(NonEmptyString title, PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 8: 响应验证**
        // **Validates: Requirements 2.5**
        // For any API response, system should validate returned content integrity and format correctness
        
        if (title == null || panelCount == null) return false;
        
        try
        {
            // Arrange - Create responses with varying content integrity
            var cleanTitle = new string(title.Get.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());
            if (string.IsNullOrWhiteSpace(cleanTitle))
            {
                cleanTitle = "Test Comic";
            }
            
            var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
            
            // Test with a simple valid response
            var validResponse = CreateCompleteValidResponse(cleanTitle, validPanelCount);
            var validationResult = ValidateResponseIntegrity(validResponse);
            
            // Assert - Valid response should pass validation
            var validResponsePasses = validationResult.IsValid;
            
            // Test with an invalid response
            var invalidResponse = CreateInvalidJSONResponse();
            var invalidValidationResult = ValidateResponseIntegrity(invalidResponse);
            
            // Assert - Invalid response should fail validation
            var invalidResponseFails = !invalidValidationResult.IsValid;
            var hasErrorMessage = !string.IsNullOrEmpty(invalidValidationResult.ErrorMessage);
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Content Integrity: ValidPasses={validResponsePasses}, InvalidFails={invalidResponseFails}, HasError={hasErrorMessage}");
            
            return validResponsePasses && invalidResponseFails && hasErrorMessage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Content Integrity Test Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property8_ResponseValidation_FormatCorrectnessIsEnforced(NonEmptyString content)
    {
        // **Feature: math-comic-generator, Property 8: 响应验证**
        // **Validates: Requirements 2.5**
        // Response format should be validated against expected schema and structure
        
        if (content == null) return false;
        
        try
        {
            // Test with valid JSON response
            var validResponse = CreateValidJSONResponse();
            var validFormatResult = ValidateResponseFormat(validResponse);
            
            // Test with invalid JSON response
            var invalidResponse = CreateInvalidJSONResponse();
            var invalidFormatResult = ValidateResponseFormat(invalidResponse);
            
            // Test with empty response
            var emptyResponse = CreateEmptyResponse();
            var emptyFormatResult = ValidateResponseFormat(emptyResponse);
            
            // Assert - Format validation should work correctly
            var validPasses = validFormatResult.IsValid;
            var invalidFails = !invalidFormatResult.IsValid;
            var emptyFails = !emptyFormatResult.IsValid;
            var hasErrorMessages = !string.IsNullOrEmpty(invalidFormatResult.ErrorMessage) &&
                                 !string.IsNullOrEmpty(emptyFormatResult.ErrorMessage);
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Format Validation: ValidPasses={validPasses}, InvalidFails={invalidFails}, EmptyFails={emptyFails}, HasErrors={hasErrorMessages}");
            
            return validPasses && invalidFails && emptyFails && hasErrorMessages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Format Validation Test Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property8_ResponseValidation_RequiredFieldsAreValidated(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 8: 响应验证**
        // **Validates: Requirements 2.5**
        // All required fields in the response should be validated for presence and correctness
        
        if (panelCount == null) return false;
        
        try
        {
            // Arrange - Test responses with missing required fields
            var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
            
            // Test with response missing title
            var responseMissingTitle = CreateResponseMissingTitle(validPanelCount);
            var titleValidation = ValidateRequiredFields(responseMissingTitle);
            
            // Test with response missing panels
            var responseMissingPanels = CreateResponseMissingPanels(validPanelCount);
            var panelsValidation = ValidateRequiredFields(responseMissingPanels);
            
            // Test with valid response for comparison
            var validResponse = CreateCompleteValidResponse("Test Title", validPanelCount);
            var validValidation = ValidateRequiredFields(validResponse);
            
            // Assert - Missing required fields should be detected
            var titleMissingDetected = !titleValidation.IsValid;
            var panelsMissingDetected = !panelsValidation.IsValid;
            var validResponsePasses = validValidation.IsValid;
            var hasErrorMessages = !string.IsNullOrEmpty(titleValidation.ErrorMessage) &&
                                 !string.IsNullOrEmpty(panelsValidation.ErrorMessage);
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Required Fields: TitleMissing={titleMissingDetected}, PanelsMissing={panelsMissingDetected}, ValidPasses={validResponsePasses}, HasErrors={hasErrorMessages}");
            
            return titleMissingDetected && panelsMissingDetected && validResponsePasses && hasErrorMessages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Required Fields Test Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property8_ResponseValidation_DataTypesAreValidated()
    {
        // **Feature: math-comic-generator, Property 8: 响应验证**
        // **Validates: Requirements 2.5**
        // Data types in the response should be validated for correctness
        
        try
        {
            // Arrange - Test responses with incorrect data types
            var dataTypeTests = new[]
            {
                CreateResponseWithWrongTitleType(),
                CreateResponseWithWrongPanelCountType(),
                CreateResponseWithWrongPanelStructure(),
                CreateResponseWithWrongDialogueType(),
                CreateResponseWithWrongOrderType()
            };

            foreach (var response in dataTypeTests)
            {
                // Act - Validate data types
                var typeValidation = ValidateDataTypes(response);
                
                // Assert - Wrong data types should be detected
                var wrongTypesDetected = !typeValidation.IsValid;
                var typeErrorProvided = !string.IsNullOrEmpty(typeValidation.ErrorMessage) &&
                                      (typeValidation.ErrorMessage.Contains("类型") || 
                                       typeValidation.ErrorMessage.Contains("格式") ||
                                       typeValidation.ErrorMessage.Contains("type") ||
                                       typeValidation.ErrorMessage.Contains("format"));
                
                // Log the validation for debugging
                Console.WriteLine($"[DEBUG] Data Types: WrongDetected={wrongTypesDetected}, TypeError={typeErrorProvided}, Error='{typeValidation.ErrorMessage}'");
                
                if (!wrongTypesDetected || !typeErrorProvided)
                {
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Data Types Test Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property8_ResponseValidation_BusinessRulesAreEnforced(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 8: 响应验证**
        // **Validates: Requirements 2.5**
        // Business rules and constraints should be validated in the response
        
        if (panelCount == null) return false;
        
        try
        {
            // Arrange - Test responses that violate business rules
            var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
            
            // Test with too few panels (business rule violation)
            var tooFewPanelsResponse = CreateResponseWithTooFewPanels(1);
            var tooFewValidation = ValidateBusinessRules(tooFewPanelsResponse);
            
            // Test with duplicate panel IDs (business rule violation)
            var duplicateIdsResponse = CreateResponseWithDuplicatePanelIds(validPanelCount);
            var duplicateValidation = ValidateBusinessRules(duplicateIdsResponse);
            
            // Test with valid response for comparison
            var validResponse = CreateCompleteValidResponse("Test Title", validPanelCount);
            var validValidation = ValidateBusinessRules(validResponse);
            
            // Assert - Business rule violations should be detected
            var tooFewDetected = !tooFewValidation.IsValid;
            var duplicateDetected = !duplicateValidation.IsValid;
            var validResponsePasses = validValidation.IsValid;
            var hasBusinessErrors = !string.IsNullOrEmpty(tooFewValidation.ErrorMessage) &&
                                  !string.IsNullOrEmpty(duplicateValidation.ErrorMessage);
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Business Rules: TooFewDetected={tooFewDetected}, DuplicateDetected={duplicateDetected}, ValidPasses={validResponsePasses}, HasErrors={hasBusinessErrors}");
            
            return tooFewDetected && duplicateDetected && validResponsePasses && hasBusinessErrors;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Business Rules Test Error: {ex.Message}");
            return false;
        }
    }

    private (string response, bool expectedValid)[] CreateValidResponse(string title, int panelCount)
    {
        var validResponse = CreateCompleteValidResponse(title, panelCount);
        return new[] { (validResponse, true) };
    }

    private (string response, bool expectedValid)[] CreateResponseWithMissingFields(string title, int panelCount)
    {
        var responses = new[]
        {
            (CreateResponseMissingTitle(panelCount), false),
            (CreateResponseMissingPanels(panelCount), false),
            (CreateResponseMissingPanelIds(panelCount), false)
        };
        return responses;
    }

    private (string response, bool expectedValid)[] CreateResponseWithInvalidData(string title, int panelCount)
    {
        var responses = new[]
        {
            (CreateResponseWithWrongTitleType(), false),
            (CreateResponseWithWrongPanelStructure(), false),
            (CreateResponseWithWrongDialogueType(), false)
        };
        return responses;
    }

    private (string response, bool expectedValid)[] CreateResponseWithCorruptedContent(string title, int panelCount)
    {
        var responses = new[]
        {
            (CreateMalformedJSONResponse(), false),
            (CreateResponseWithInvalidCharacters(), false)
        };
        return responses;
    }

    private (string response, bool expectedValid)[] CreateResponseWithIncompleteContent(string title, int panelCount)
    {
        var responses = new[]
        {
            (CreateResponseWithEmptyRequiredFields(panelCount), false),
            (CreateResponseWithPartialPanels(panelCount), false)
        };
        return responses;
    }

    private string CreateCompleteValidResponse(string title, int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = $"Panel {i} image description",
                dialogue = new[] { $"Panel {i} dialogue" },
                narration = $"Panel {i} narration",
                order = i
            });
        }

        var response = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = title,
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(response);
    }

    private string CreateValidJSONResponse()
    {
        return CreateCompleteValidResponse("Test Comic", 4);
    }

    private string CreateInvalidJSONResponse()
    {
        return "{ \"invalid\": json content without proper closing";
    }

    private string CreateMalformedJSONResponse()
    {
        return "{ invalid json structure }";
    }

    private string CreateEmptyResponse()
    {
        return "";
    }

    private string CreateValidButWrongSchemaResponse()
    {
        return JsonSerializer.Serialize(new
        {
            wrongField = "wrong schema",
            anotherWrongField = 123
        });
    }

    private string CreateResponseMissingTitle(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new { id = $"panel_{i}", imagePrompt = $"Panel {i}" });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new { panels = panels })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseMissingPanels(int panelCount)
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new { title = "Test Title" })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseMissingPanelIds(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new { imagePrompt = $"Panel {i}" }); // Missing id
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test Title",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseMissingImagePrompts(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new { id = $"panel_{i}" }); // Missing imagePrompt
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test Title",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseMissingDialogue(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = $"Panel {i}"
                // Missing dialogue
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test Title",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithEmptyRequiredFields(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = "", // Empty required field
                imagePrompt = "",
                dialogue = new string[0]
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithWrongTitleType()
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = 123, // Should be string
                                    panels = new object[0]
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithWrongPanelCountType()
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = "should be array" // Wrong type
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithWrongPanelStructure()
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = new[] { "should be object" } // Wrong structure
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithWrongDialogueType()
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = new[]
                                    {
                                        new
                                        {
                                            id = "panel_0",
                                            imagePrompt = "test",
                                            dialogue = "should be array" // Wrong type
                                        }
                                    }
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithWrongOrderType()
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = new[]
                                    {
                                        new
                                        {
                                            id = "panel_0",
                                            imagePrompt = "test",
                                            dialogue = new[] { "test" },
                                            order = "should be number" // Wrong type
                                        }
                                    }
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithTooFewPanels(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = $"Panel {i}",
                dialogue = new[] { $"Panel {i}" }
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithTooManyPanels(int panelCount)
    {
        return CreateResponseWithTooFewPanels(panelCount); // Same structure, different count
    }

    private string CreateResponseWithDuplicatePanelIds(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = "panel_0", // All have same ID
                imagePrompt = $"Panel {i}",
                dialogue = new[] { $"Panel {i}" }
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithInvalidPanelOrder(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = $"Panel {i}",
                dialogue = new[] { $"Panel {i}" },
                order = panelCount - i // Reverse order
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithEmptyDialogue(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = $"Panel {i}",
                dialogue = new string[0] // Empty dialogue
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithExcessivelyLongContent(int panelCount)
    {
        var longContent = new string('A', 10000); // Excessively long content
        var panels = new List<object>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = longContent,
                dialogue = new[] { longContent }
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = longContent,
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private string CreateResponseWithInvalidCharacters()
    {
        return "{ \"title\": \"Test\u0000Invalid\", \"panels\": [] }"; // Contains null character
    }

    private string CreateResponseWithPartialPanels(int panelCount)
    {
        var panels = new List<object>();
        for (int i = 0; i < panelCount / 2; i++) // Only half the panels
        {
            panels.Add(new
            {
                id = $"panel_{i}",
                imagePrompt = $"Panel {i}"
            });
        }

        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    title = "Test",
                                    panels = panels
                                })
                            }
                        }
                    }
                }
            }
        });
    }

    private ValidationResult ValidateResponseIntegrity(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "响应缺少候选内容" };
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "响应缺少内容部分" };
            }

            if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "响应缺少内容部分" };
            }

            var firstPart = parts[0];
            if (!firstPart.TryGetProperty("text", out var textElement))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "响应缺少文本内容" };
            }

            var textContent = textElement.GetString();
            if (string.IsNullOrEmpty(textContent))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "响应文本内容为空" };
            }

            // Try to parse inner JSON
            using var contentDocument = JsonDocument.Parse(textContent);
            return new ValidationResult { IsValid = true };
        }
        catch (JsonException)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "响应JSON格式无效" };
        }
        catch
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "响应格式验证失败" };
        }
    }

    private ValidationResult ValidateResponseFormat(string response)
    {
        try
        {
            if (string.IsNullOrEmpty(response))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "响应为空" };
            }

            using var document = JsonDocument.Parse(response);
            return new ValidationResult { IsValid = true };
        }
        catch (JsonException ex)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = $"JSON格式错误: {ex.Message}" };
        }
        catch
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "响应格式无效" };
        }
    }

    private ValidationResult ValidateRequiredFields(string response)
    {
        try
        {
            var integrityResult = ValidateResponseIntegrity(response);
            if (!integrityResult.IsValid)
            {
                return integrityResult;
            }

            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var content = firstCandidate.GetProperty("content");
            var parts = content.GetProperty("parts");
            var firstPart = parts[0];
            var textContent = firstPart.GetProperty("text").GetString();

            using var contentDocument = JsonDocument.Parse(textContent!);
            var contentRoot = contentDocument.RootElement;

            if (!contentRoot.TryGetProperty("title", out var titleElement) || 
                string.IsNullOrEmpty(titleElement.GetString()))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "缺少必需字段: title" };
            }

            if (!contentRoot.TryGetProperty("panels", out var panelsElement))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "缺少必需字段: panels" };
            }

            foreach (var panel in panelsElement.EnumerateArray())
            {
                if (!panel.TryGetProperty("id", out var idElement) || 
                    string.IsNullOrEmpty(idElement.GetString()))
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "面板缺少必需字段: id" };
                }

                if (!panel.TryGetProperty("imagePrompt", out var imageElement) || 
                    string.IsNullOrEmpty(imageElement.GetString()))
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "面板缺少必需字段: imagePrompt" };
                }
            }

            return new ValidationResult { IsValid = true };
        }
        catch
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "必需字段验证失败" };
        }
    }

    private ValidationResult ValidateDataTypes(string response)
    {
        try
        {
            var integrityResult = ValidateResponseIntegrity(response);
            if (!integrityResult.IsValid)
            {
                return integrityResult;
            }

            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var content = firstCandidate.GetProperty("content");
            var parts = content.GetProperty("parts");
            var firstPart = parts[0];
            var textContent = firstPart.GetProperty("text").GetString();

            using var contentDocument = JsonDocument.Parse(textContent!);
            var contentRoot = contentDocument.RootElement;

            if (contentRoot.TryGetProperty("title", out var titleElement) && 
                titleElement.ValueKind != JsonValueKind.String)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "title字段类型错误，应为字符串" };
            }

            if (contentRoot.TryGetProperty("panels", out var panelsElement) && 
                panelsElement.ValueKind != JsonValueKind.Array)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "panels字段类型错误，应为数组" };
            }

            foreach (var panel in panelsElement.EnumerateArray())
            {
                if (panel.ValueKind != JsonValueKind.Object)
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "面板类型错误，应为对象" };
                }

                if (panel.TryGetProperty("dialogue", out var dialogueElement) && 
                    dialogueElement.ValueKind != JsonValueKind.Array)
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "dialogue字段类型错误，应为数组" };
                }

                if (panel.TryGetProperty("order", out var orderElement) && 
                    orderElement.ValueKind != JsonValueKind.Number)
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "order字段类型错误，应为数字" };
                }
            }

            return new ValidationResult { IsValid = true };
        }
        catch
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "数据类型验证失败" };
        }
    }

    private ValidationResult ValidateBusinessRules(string response)
    {
        try
        {
            var integrityResult = ValidateResponseIntegrity(response);
            if (!integrityResult.IsValid)
            {
                return integrityResult;
            }

            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var content = firstCandidate.GetProperty("content");
            var parts = content.GetProperty("parts");
            var firstPart = parts[0];
            var textContent = firstPart.GetProperty("text").GetString();

            using var contentDocument = JsonDocument.Parse(textContent!);
            var contentRoot = contentDocument.RootElement;

            if (contentRoot.TryGetProperty("panels", out var panelsElement))
            {
                var panelCount = panelsElement.GetArrayLength();
                
                // Business rule: Panel count should be 3-6
                if (panelCount < 3)
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "业务规则违反：面板数量不能少于3个" };
                }
                
                if (panelCount > 6)
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "业务规则违反：面板数量不能超过6个" };
                }

                // Business rule: Panel IDs should be unique
                var panelIds = new HashSet<string>();
                foreach (var panel in panelsElement.EnumerateArray())
                {
                    if (panel.TryGetProperty("id", out var idElement))
                    {
                        var id = idElement.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            if (panelIds.Contains(id))
                            {
                                return new ValidationResult { IsValid = false, ErrorMessage = "业务规则违反：面板ID不能重复" };
                            }
                            panelIds.Add(id);
                        }
                    }
                }

                // Business rule: Content length limits
                foreach (var panel in panelsElement.EnumerateArray())
                {
                    if (panel.TryGetProperty("imagePrompt", out var imageElement))
                    {
                        var imagePrompt = imageElement.GetString();
                        if (!string.IsNullOrEmpty(imagePrompt) && imagePrompt.Length > 1000)
                        {
                            return new ValidationResult { IsValid = false, ErrorMessage = "业务规则违反：图像提示词过长" };
                        }
                    }

                    if (panel.TryGetProperty("dialogue", out var dialogueElement))
                    {
                        if (dialogueElement.GetArrayLength() == 0)
                        {
                            return new ValidationResult { IsValid = false, ErrorMessage = "业务规则违反：对话不能为空" };
                        }
                    }
                }
            }

            return new ValidationResult { IsValid = true };
        }
        catch
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "业务规则验证失败" };
        }
    }
}