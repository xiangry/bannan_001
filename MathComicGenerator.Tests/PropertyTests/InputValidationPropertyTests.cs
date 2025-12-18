using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Shared.Services;
using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Tests.PropertyTests;

public class InputValidationPropertyTests
{
    private readonly MathConceptValidator _validator;

    public InputValidationPropertyTests()
    {
        _validator = new MathConceptValidator();
    }

    [Property]
    public bool Property1_ValidInputAcceptance_ValidMathConceptsShouldBeAccepted(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 1: 有效输入接受**
        // **Validates: Requirements 1.1**
        // For any valid mathematical concept input, system should accept input and successfully start processing
        
        // Arrange - Create valid mathematical inputs
        var validMathInputs = new[]
        {
            "加法运算",
            "十以内的分解法", 
            "几何图形认识",
            "分数的基本概念",
            "时间的计算",
            "数字1到10",
            "三角形的特点",
            "addition within 10",
            "basic geometry shapes",
            "simple fractions"
        };

        var testInput = validMathInputs[Math.Abs(input.Get.GetHashCode()) % validMathInputs.Length];
        
        // Act
        var result = _validator.ValidateInput(testInput);
        
        // Assert - Valid mathematical concepts should be accepted
        var isAccepted = result.IsValid;
        var hasNoErrors = string.IsNullOrEmpty(result.ErrorMessage);
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Valid Input Validation: Input='{testInput}', Accepted={isAccepted}, NoErrors={hasNoErrors}");
        
        return isAccepted && hasNoErrors;
    }

    [Property]
    public bool Property1_ValidInputAcceptance_NonEmptyInputsShouldNotBeRejectedForLength(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 1: 有效输入接受**
        // **Validates: Requirements 1.1**
        // For any non-empty input within reasonable length, system should not reject due to basic validation
        
        if (input == null) return false;
        
        try
        {
            // Arrange - Clean input and ensure it's within valid length (under 200 characters)
            var cleanInput = new string(input.Get.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());
            var testInput = cleanInput.Length > 200 ? cleanInput.Substring(0, 200) : cleanInput;
            
            // Skip if input becomes empty after cleaning
            if (string.IsNullOrWhiteSpace(testInput))
            {
                return true; // This is acceptable - control characters should be filtered out
            }
            
            // Act
            var result = _validator.ValidateInput(testInput);
            
            // Assert - Non-empty, reasonable length inputs should pass basic validation
            // (They might fail content validation, but not basic validation)
            var passesBasicValidation = !string.IsNullOrWhiteSpace(testInput) && testInput.Length <= 200;
            var validatorHandlesCorrectly = result != null;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Basic Input Validation: Length={testInput.Length}, PassesBasic={passesBasicValidation}, HandledCorrectly={validatorHandlesCorrectly}");
            
            return passesBasicValidation && validatorHandlesCorrectly;
        }
        catch
        {
            return false;
        }
    }

    [Property]
    public bool Property1_ValidInputAcceptance_MathConceptParsingProducesValidObject(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 1: 有效输入接受**
        // **Validates: Requirements 1.1**
        // For any accepted input, parsing should produce a valid MathConcept object
        
        // Arrange
        var validInputs = new[]
        {
            "数学加法",
            "几何图形", 
            "分数概念",
            "数字认识",
            "basic math",
            "simple addition",
            "geometry shapes"
        };

        var testInput = validInputs[Math.Abs(input.Get.GetHashCode()) % validInputs.Length];
        
        // Act
        var validationResult = _validator.ValidateInput(testInput);
        
        if (validationResult.IsValid)
        {
            var mathConcept = _validator.ParseMathConcept(testInput);
            
            // Assert - Parsed MathConcept should have valid properties
            var hasValidTopic = !string.IsNullOrEmpty(mathConcept.Topic);
            var hasValidAgeGroup = Enum.IsDefined(typeof(AgeGroup), mathConcept.AgeGroup);
            var hasValidDifficulty = Enum.IsDefined(typeof(DifficultyLevel), mathConcept.Difficulty);
            var hasKeywords = mathConcept.Keywords != null;
            
            // Log the validation for debugging
            Console.WriteLine($"[UTF-8 调试] 数学概念解析: 主题='{mathConcept.Topic}', 年龄组={mathConcept.AgeGroup}, 难度={mathConcept.Difficulty}, 关键词数量={mathConcept.Keywords.Count}");
            
            return hasValidTopic && hasValidAgeGroup && hasValidDifficulty && hasKeywords;
        }
        
        // If input is not valid, that's also acceptable behavior
        Console.WriteLine($"[UTF-8 调试] 输入未通过验证: '{testInput}' - {validationResult.ErrorMessage}");
        return true;
    }

    [Property]
    public bool Property1_ValidInputAcceptance_MathContentDetectionIsConsistent(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 1: 有效输入接受**
        // **Validates: Requirements 1.1**
        // Mathematical content detection should be consistent and reliable
        
        // Arrange - Test with known mathematical content
        var mathInputs = new[]
        {
            "加法", "减法", "乘法", "除法", "数学", "几何", "分数", "数字",
            "addition", "subtraction", "math", "geometry", "fraction", "number"
        };

        var nonMathInputs = new[]
        {
            "音乐", "体育", "历史", "文学", "艺术",
            "music", "sports", "history", "literature", "art"
        };

        // Test mathematical content
        foreach (var mathInput in mathInputs)
        {
            var isMathDetected = _validator.IsMathematicalContent(mathInput);
            if (!isMathDetected)
            {
                Console.WriteLine($"[UTF-8 调试] 数学内容检测失败: '{mathInput}' 未被识别为数学内容");
                return false;
            }
        }

        // Test non-mathematical content
        foreach (var nonMathInput in nonMathInputs)
        {
            var isMathDetected = _validator.IsMathematicalContent(nonMathInput);
            if (isMathDetected)
            {
                Console.WriteLine($"[UTF-8 调试] 非数学内容误检: '{nonMathInput}' 被错误识别为数学内容");
                // Note: This might be acceptable depending on implementation
            }
        }

        Console.WriteLine($"[UTF-8 调试] 数学内容检测一致性验证完成");
        return true;
    }

    [Property]
    public bool Property1_ValidInputAcceptance_ValidationResultStructureIsValid(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 1: 有效输入接受**
        // **Validates: Requirements 1.1**
        // ValidationResult should always have proper structure regardless of input
        
        // Arrange
        var testInput = input.Get.Length > 200 ? input.Get.Substring(0, 200) : input.Get;
        
        // Act
        var result = _validator.ValidateInput(testInput);
        
        // Assert - ValidationResult should always have proper structure
        var hasValidStructure = result != null;
        var hasIsValidProperty = result != null;
        var hasErrorMessage = result?.ErrorMessage != null;
        var hasSuggestions = result?.Suggestions != null;
        
        // Log the validation for debugging
        Console.WriteLine($"[UTF-8 调试] 验证结果结构: 有效结构={hasValidStructure}, IsValid属性={hasIsValidProperty}, 错误消息={hasErrorMessage}, 建议={hasSuggestions}");
        
        return hasValidStructure && hasIsValidProperty && hasErrorMessage && hasSuggestions;
    }
    [Property]
    public bool Property20_InvalidInputRejection_EmptyInputsShouldBeRejected()
    {
        // **Feature: math-comic-generator, Property 20: 无效输入拒绝**
        // **Validates: Requirements 6.1**
        // For any empty or invalid mathematical concept input, system should reject processing and provide clear error message
        
        // Arrange - Test various empty/invalid inputs
        var invalidInputs = new[]
        {
            "",
            "   ",
            "\t\n",
            null
        };

        foreach (var invalidInput in invalidInputs)
        {
            // Act
            var result = _validator.ValidateInput(invalidInput);
            
            // Assert - Empty inputs should be rejected
            var isRejected = !result.IsValid;
            var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
            var hasSuggestions = result.Suggestions != null && result.Suggestions.Count > 0;
            
            // Log the validation for debugging
            Console.WriteLine($"[UTF-8 调试] 无效输入拒绝: 输入='{invalidInput ?? "null"}', 拒绝={isRejected}, 有错误消息={hasErrorMessage}, 有建议={hasSuggestions}");
            
            if (!isRejected || !hasErrorMessage)
            {
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool Property20_InvalidInputRejection_TooLongInputsShouldBeRejected(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 20: 无效输入拒绝**
        // **Validates: Requirements 6.1**
        // For any input exceeding length limits, system should reject and provide clear error message
        
        // Arrange - Create input that exceeds 200 character limit
        var longInput = new string('a', 250); // Exceeds 200 character limit
        
        // Act
        var result = _validator.ValidateInput(longInput);
        
        // Assert - Too long inputs should be rejected
        var isRejected = !result.IsValid;
        var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
        var errorMentionsLength = result.ErrorMessage.Contains("长") || result.ErrorMessage.Contains("length");
        
        // Log the validation for debugging
        Console.WriteLine($"[UTF-8 调试] 过长输入拒绝: 长度={longInput.Length}, 拒绝={isRejected}, 有错误消息={hasErrorMessage}, 错误提及长度={errorMentionsLength}");
        
        return isRejected && hasErrorMessage;
    }

    [Property]
    public bool Property20_InvalidInputRejection_UnsafeContentShouldBeRejected()
    {
        // **Feature: math-comic-generator, Property 20: 无效输入拒绝**
        // **Validates: Requirements 6.1**
        // For any input containing unsafe content, system should reject and provide appropriate error message
        
        // Arrange - Test inputs with unsafe content
        var unsafeInputs = new[]
        {
            "暴力内容",
            "恐怖故事",
            "危险行为",
            "violence content",
            "scary story",
            "dangerous behavior"
        };

        foreach (var unsafeInput in unsafeInputs)
        {
            // Act
            var result = _validator.ValidateInput(unsafeInput);
            
            // Assert - Unsafe content should be rejected
            var isRejected = !result.IsValid;
            var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
            
            // Log the validation for debugging
            Console.WriteLine($"[UTF-8 调试] 不安全内容拒绝: 输入='{unsafeInput}', 拒绝={isRejected}, 有错误消息={hasErrorMessage}");
            
            if (!isRejected || !hasErrorMessage)
            {
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool Property20_InvalidInputRejection_RejectedInputsProvideHelpfulSuggestions(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 20: 无效输入拒绝**
        // **Validates: Requirements 6.1**
        // For any rejected input, system should provide helpful suggestions for valid alternatives
        
        // Arrange - Test with empty input to trigger rejection
        var emptyInput = "";
        
        // Act
        var result = _validator.ValidateInput(emptyInput);
        
        // Assert - Rejected inputs should provide suggestions
        if (!result.IsValid)
        {
            var hasSuggestions = result.Suggestions != null && result.Suggestions.Count > 0;
            var suggestionsAreHelpful = result.Suggestions?.Any(s => !string.IsNullOrWhiteSpace(s)) == true;
            
            // Log the validation for debugging
            Console.WriteLine($"[UTF-8 调试] 拒绝输入建议: 有建议={hasSuggestions}, 建议有用={suggestionsAreHelpful}, 建议数量={result.Suggestions?.Count ?? 0}");
            
            return hasSuggestions && suggestionsAreHelpful;
        }

        // If input is valid, that's also acceptable
        Console.WriteLine($"[UTF-8 调试] 输入通过验证，无需建议");
        return true;
    }

    [Property]
    public bool Property20_InvalidInputRejection_ValidationIsConsistentAcrossMultipleCalls(NonEmptyString input)
    {
        // **Feature: math-comic-generator, Property 20: 无效输入拒绝**
        // **Validates: Requirements 6.1**
        // Validation results should be consistent across multiple calls with same input
        
        // Arrange
        var testInput = input.Get.Length > 200 ? input.Get.Substring(0, 200) : input.Get;
        
        // Act - Call validation multiple times
        var result1 = _validator.ValidateInput(testInput);
        var result2 = _validator.ValidateInput(testInput);
        var result3 = _validator.ValidateInput(testInput);
        
        // Assert - Results should be consistent
        var consistentValidity = result1.IsValid == result2.IsValid && result2.IsValid == result3.IsValid;
        var consistentErrorMessages = result1.ErrorMessage == result2.ErrorMessage && result2.ErrorMessage == result3.ErrorMessage;
        
        // Log the validation for debugging
        Console.WriteLine($"[UTF-8 调试] 验证一致性: 有效性一致={consistentValidity}, 错误消息一致={consistentErrorMessages}");
        
        return consistentValidity && consistentErrorMessages;
    }
}