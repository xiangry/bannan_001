using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Shared.Services;
using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Tests.PropertyTests;

public class AgeGroupParameterPropertyTests
{
    private readonly GenerationOptionsProcessor _processor;

    public AgeGroupParameterPropertyTests()
    {
        _processor = new GenerationOptionsProcessor();
    }

    [Property]
    public bool Property11_AgeGroupParameterResponse_AgeGroupAffectsContentGeneration()
    {
        // **Feature: math-comic-generator, Property 11: 年龄组参数响应**
        // **Validates: Requirements 4.1**
        // For any specified target age group setting, generated content should have corresponding adjustments in language complexity
        
        // Arrange - Test different age groups
        var ageGroups = new[] { AgeGroup.Preschool, AgeGroup.Elementary };
        
        foreach (var ageGroup in ageGroups)
        {
            var options = new GenerationOptions
            {
                PanelCount = 5,
                AgeGroup = ageGroup,
                VisualStyle = VisualStyle.Realistic,
                Language = Language.Chinese,
                EnablePinyin = true
            };

            // Act - Adjust options for age group
            var adjustedOptions = _processor.AdjustForAgeGroup(options, ageGroup);
            
            // Assert - Age group should affect the options
            var ageGroupApplied = adjustedOptions.AgeGroup == ageGroup;
            var appropriateAdjustments = ValidateAgeGroupAdjustments(ageGroup, adjustedOptions);
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Age Group Adjustment: AgeGroup={ageGroup}, Applied={ageGroupApplied}, Appropriate={appropriateAdjustments}");
            
            if (!ageGroupApplied || !appropriateAdjustments)
            {
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool Property11_AgeGroupParameterResponse_PreschoolHasAppropriateConstraints()
    {
        // **Feature: math-comic-generator, Property 11: 年龄组参数响应**
        // **Validates: Requirements 4.1**
        // Preschool age group should have appropriate constraints (max 4 panels, cartoon style)
        
        // Arrange - Create options for preschool
        var preschoolOptions = new GenerationOptions
        {
            PanelCount = 6, // Exceeds preschool limit
            AgeGroup = AgeGroup.Preschool,
            VisualStyle = VisualStyle.Realistic, // Not suitable for preschool
            Language = Language.Chinese,
            EnablePinyin = true
        };

        // Act - Adjust for preschool age group
        var adjustedOptions = _processor.AdjustForAgeGroup(preschoolOptions, AgeGroup.Preschool);
        
        // Assert - Preschool constraints should be applied
        var panelCountAdjusted = adjustedOptions.PanelCount <= 4;
        var visualStyleAdjusted = adjustedOptions.VisualStyle == VisualStyle.Cartoon;
        var ageGroupSet = adjustedOptions.AgeGroup == AgeGroup.Preschool;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Preschool Constraints: PanelCount={adjustedOptions.PanelCount}<=4: {panelCountAdjusted}, VisualStyle=Cartoon: {visualStyleAdjusted}, AgeGroup=Preschool: {ageGroupSet}");
        
        return panelCountAdjusted && visualStyleAdjusted && ageGroupSet;
    }

    [Property]
    public bool Property11_AgeGroupParameterResponse_ElementaryHasFlexibleOptions()
    {
        // **Feature: math-comic-generator, Property 11: 年龄组参数响应**
        // **Validates: Requirements 4.1**
        // Elementary age group should allow more flexible options
        
        // Arrange - Create options for elementary
        var elementaryOptions = new GenerationOptions
        {
            PanelCount = 6,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Realistic,
            Language = Language.Chinese,
            EnablePinyin = false
        };

        // Act - Adjust for elementary age group
        var adjustedOptions = _processor.AdjustForAgeGroup(elementaryOptions, AgeGroup.Elementary);
        
        // Assert - Elementary should maintain original options when appropriate
        var panelCountMaintained = adjustedOptions.PanelCount == elementaryOptions.PanelCount;
        var visualStyleMaintained = adjustedOptions.VisualStyle == elementaryOptions.VisualStyle;
        var ageGroupSet = adjustedOptions.AgeGroup == AgeGroup.Elementary;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Elementary Flexibility: PanelCount={adjustedOptions.PanelCount}, VisualStyle={adjustedOptions.VisualStyle}, AgeGroup={adjustedOptions.AgeGroup}");
        
        return panelCountMaintained && visualStyleMaintained && ageGroupSet;
    }

    [Property]
    public bool Property11_AgeGroupParameterResponse_OptionsConsistencyValidation(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 11: 年龄组参数响应**
        // **Validates: Requirements 4.1**
        // Options consistency should be validated based on age group constraints
        
        // Arrange - Test consistency validation
        var testPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
        
        var preschoolOptions = new GenerationOptions
        {
            PanelCount = testPanelCount,
            AgeGroup = AgeGroup.Preschool,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        var elementaryOptions = new GenerationOptions
        {
            PanelCount = testPanelCount,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Realistic,
            Language = Language.Chinese
        };

        // Act - Check consistency
        var preschoolConsistent = _processor.AreOptionsConsistent(preschoolOptions);
        var elementaryConsistent = _processor.AreOptionsConsistent(elementaryOptions);
        
        // Assert - Consistency should match age group constraints
        var preschoolExpectedConsistency = testPanelCount <= 4; // Preschool constraint
        var elementaryExpectedConsistency = testPanelCount >= 3 && testPanelCount <= 6; // Elementary constraint
        
        var preschoolCorrect = preschoolConsistent == preschoolExpectedConsistency;
        var elementaryCorrect = elementaryConsistent == elementaryExpectedConsistency;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Consistency Validation: PanelCount={testPanelCount}, Preschool={preschoolConsistent}(expected:{preschoolExpectedConsistency}), Elementary={elementaryConsistent}(expected:{elementaryExpectedConsistency})");
        
        return preschoolCorrect && elementaryCorrect;
    }

    [Property]
    public bool Property11_AgeGroupParameterResponse_DefaultOptionsAreValid()
    {
        // **Feature: math-comic-generator, Property 11: 年龄组参数响应**
        // **Validates: Requirements 4.1**
        // Default options should be valid and age-appropriate
        
        // Act - Get default options
        var defaultOptions = _processor.ApplyDefaults(null);
        
        // Assert - Default options should be valid
        var validationResult = _processor.ValidateOptions(defaultOptions);
        var isConsistent = _processor.AreOptionsConsistent(defaultOptions);
        
        var isValid = validationResult.IsValid;
        var hasAppropriateDefaults = defaultOptions.AgeGroup == AgeGroup.Elementary && 
                                   defaultOptions.PanelCount >= 3 && 
                                   defaultOptions.PanelCount <= 6;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Default Options: Valid={isValid}, Consistent={isConsistent}, Appropriate={hasAppropriateDefaults}, AgeGroup={defaultOptions.AgeGroup}, PanelCount={defaultOptions.PanelCount}");
        
        return isValid && isConsistent && hasAppropriateDefaults;
    }

    private bool ValidateAgeGroupAdjustments(AgeGroup ageGroup, GenerationOptions adjustedOptions)
    {
        return ageGroup switch
        {
            AgeGroup.Preschool => adjustedOptions.PanelCount <= 4 && 
                                adjustedOptions.VisualStyle == VisualStyle.Cartoon,
            AgeGroup.Elementary => adjustedOptions.PanelCount >= 3 && 
                                 adjustedOptions.PanelCount <= 6,
            _ => false
        };
    }
}
    [Property]
    public bool Property12_PanelCountControl_UserSpecifiedPanelCountIsRespected(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 12: 面板数量控制**
        // **Validates: Requirements 4.2**
        // For any user specified panel count, generated comic should contain exact panel count
        
        // Arrange - Use valid panel count range
        var requestedPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
        
        var options = new GenerationOptions
        {
            PanelCount = requestedPanelCount,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act - Validate and process options
        var validationResult = _processor.ValidateOptions(options);
        var processedOptions = _processor.ApplyDefaults(options);
        
        // Assert - Panel count should be preserved when valid
        var isValid = validationResult.IsValid;
        var panelCountPreserved = processedOptions.PanelCount == requestedPanelCount;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Panel Count Control: Requested={requestedPanelCount}, Processed={processedOptions.PanelCount}, Valid={isValid}, Preserved={panelCountPreserved}");
        
        return isValid && panelCountPreserved;
    }

    [Property]
    public bool Property12_PanelCountControl_InvalidPanelCountsAreRejected(int panelCount)
    {
        // **Feature: math-comic-generator, Property 12: 面板数量控制**
        // **Validates: Requirements 4.2**
        // Invalid panel counts should be rejected with appropriate error messages
        
        // Arrange - Use invalid panel counts
        var invalidCounts = new[] { 0, 1, 2, 7, 8, 9, 10, -1, -5 };
        var testPanelCount = invalidCounts[Math.Abs(panelCount) % invalidCounts.Length];
        
        var options = new GenerationOptions
        {
            PanelCount = testPanelCount,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act - Validate options
        var validationResult = _processor.ValidateOptions(options);
        
        // Assert - Invalid panel counts should be rejected
        var shouldBeInvalid = testPanelCount < 3 || testPanelCount > 6;
        var isRejected = !validationResult.IsValid;
        var hasErrorMessage = !string.IsNullOrEmpty(validationResult.ErrorMessage);
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Invalid Panel Count Rejection: Count={testPanelCount}, ShouldBeInvalid={shouldBeInvalid}, Rejected={isRejected}, HasError={hasErrorMessage}");
        
        return shouldBeInvalid == isRejected && (isRejected ? hasErrorMessage : true);
    }

    [Property]
    public bool Property12_PanelCountControl_DefaultPanelCountIsAppliedWhenInvalid(int panelCount)
    {
        // **Feature: math-comic-generator, Property 12: 面板数量控制**
        // **Validates: Requirements 4.2**
        // When invalid panel count is provided, system should apply default value
        
        // Arrange - Use invalid panel count
        var invalidCounts = new[] { 0, 1, 2, 7, 8, 9, 10, -1 };
        var invalidPanelCount = invalidCounts[Math.Abs(panelCount) % invalidCounts.Length];
        
        var options = new GenerationOptions
        {
            PanelCount = invalidPanelCount,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act - Apply defaults to fix invalid options
        var correctedOptions = _processor.ApplyDefaults(options);
        
        // Assert - Invalid panel count should be corrected to default (4)
        var wasInvalid = invalidPanelCount < 3 || invalidPanelCount > 6;
        var defaultApplied = correctedOptions.PanelCount == 4; // Default panel count
        var isNowValid = correctedOptions.PanelCount >= 3 && correctedOptions.PanelCount <= 6;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Default Panel Count Application: Invalid={invalidPanelCount}, WasInvalid={wasInvalid}, Corrected={correctedOptions.PanelCount}, DefaultApplied={defaultApplied}, NowValid={isNowValid}");
        
        return wasInvalid ? (defaultApplied && isNowValid) : true;
    }
    [Property]
    public bool Property13_ParameterValidation_AllParametersAreValidated()
    {
        // **Feature: math-comic-generator, Property 13: 参数验证**
        // **Validates: Requirements 4.4**
        // For any user provided custom parameters, system should validate their validity
        
        // Arrange - Test various parameter combinations
        var testCases = new[]
        {
            new GenerationOptions { PanelCount = 4, AgeGroup = AgeGroup.Elementary, VisualStyle = VisualStyle.Cartoon, Language = Language.Chinese },
            new GenerationOptions { PanelCount = 3, AgeGroup = AgeGroup.Preschool, VisualStyle = VisualStyle.Cartoon, Language = Language.English },
            new GenerationOptions { PanelCount = 6, AgeGroup = AgeGroup.Elementary, VisualStyle = VisualStyle.Realistic, Language = Language.Chinese },
            new GenerationOptions { PanelCount = 0, AgeGroup = AgeGroup.Elementary, VisualStyle = VisualStyle.Cartoon, Language = Language.Chinese }, // Invalid panel count
            new GenerationOptions { PanelCount = 4, AgeGroup = (AgeGroup)999, VisualStyle = VisualStyle.Cartoon, Language = Language.Chinese }, // Invalid age group
            new GenerationOptions { PanelCount = 4, AgeGroup = AgeGroup.Elementary, VisualStyle = (VisualStyle)999, Language = Language.Chinese }, // Invalid visual style
            new GenerationOptions { PanelCount = 4, AgeGroup = AgeGroup.Elementary, VisualStyle = VisualStyle.Cartoon, Language = (Language)999 } // Invalid language
        };

        foreach (var testCase in testCases)
        {
            // Act - Validate parameters
            var validationResult = _processor.ValidateOptions(testCase);
            
            // Assert - Validation should correctly identify valid/invalid parameters
            var expectedValidity = IsOptionsValid(testCase);
            var actualValidity = validationResult.IsValid;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Parameter Validation: PanelCount={testCase.PanelCount}, AgeGroup={testCase.AgeGroup}, Expected={expectedValidity}, Actual={actualValidity}");
            
            if (expectedValidity != actualValidity)
            {
                Console.WriteLine($"[DEBUG] Validation Mismatch: Expected={expectedValidity}, Actual={actualValidity}");
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool Property13_ParameterValidation_NullParametersAreHandled()
    {
        // **Feature: math-comic-generator, Property 13: 参数验证**
        // **Validates: Requirements 4.4**
        // Null parameters should be handled gracefully with appropriate error messages
        
        // Act - Validate null options
        var validationResult = _processor.ValidateOptions(null);
        var defaultOptions = _processor.ApplyDefaults(null);
        
        // Assert - Null should be handled appropriately
        var nullRejected = !validationResult.IsValid;
        var hasErrorMessage = !string.IsNullOrEmpty(validationResult.ErrorMessage);
        var defaultsProvided = defaultOptions != null;
        var defaultsValid = defaultsProvided && _processor.ValidateOptions(defaultOptions).IsValid;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Null Parameter Handling: NullRejected={nullRejected}, HasError={hasErrorMessage}, DefaultsProvided={defaultsProvided}, DefaultsValid={defaultsValid}");
        
        return nullRejected && hasErrorMessage && defaultsProvided && defaultsValid;
    }

    [Property]
    public bool Property13_ParameterValidation_ValidationMessagesAreHelpful(PositiveInt seed)
    {
        // **Feature: math-comic-generator, Property 13: 参数验证**
        // **Validates: Requirements 4.4**
        // Validation error messages should be helpful and provide suggestions
        
        // Arrange - Create invalid options
        var invalidOptions = new[]
        {
            new GenerationOptions { PanelCount = 0, AgeGroup = AgeGroup.Elementary, VisualStyle = VisualStyle.Cartoon, Language = Language.Chinese },
            new GenerationOptions { PanelCount = 10, AgeGroup = AgeGroup.Elementary, VisualStyle = VisualStyle.Cartoon, Language = Language.Chinese },
            new GenerationOptions { PanelCount = 4, AgeGroup = (AgeGroup)999, VisualStyle = VisualStyle.Cartoon, Language = Language.Chinese }
        };

        var testOption = invalidOptions[seed.Get % invalidOptions.Length];
        
        // Act - Validate invalid options
        var validationResult = _processor.ValidateOptions(testOption);
        
        // Assert - Should provide helpful error messages and suggestions
        var isInvalid = !validationResult.IsValid;
        var hasErrorMessage = !string.IsNullOrEmpty(validationResult.ErrorMessage);
        var hasSuggestions = validationResult.Suggestions != null && validationResult.Suggestions.Count > 0;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Validation Messages: Invalid={isInvalid}, HasError={hasErrorMessage}, HasSuggestions={hasSuggestions}, ErrorMsg='{validationResult.ErrorMessage}'");
        
        return isInvalid && hasErrorMessage && hasSuggestions;
    }

    private bool IsOptionsValid(GenerationOptions options)
    {
        if (options == null) return false;
        
        // Check panel count
        if (options.PanelCount < 3 || options.PanelCount > 6) return false;
        
        // Check enum values
        if (!Enum.IsDefined(typeof(AgeGroup), options.AgeGroup)) return false;
        if (!Enum.IsDefined(typeof(VisualStyle), options.VisualStyle)) return false;
        if (!Enum.IsDefined(typeof(Language), options.Language)) return false;
        
        return true;
    }
    [Property]
    public bool Property14_ParameterConsistency_ParametersRemainConsistentThroughoutGeneration(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 14: 参数一致性**
        // **Validates: Requirements 4.5**
        // For any applied custom settings, they should remain consistent throughout the generation process
        
        // Arrange - Create consistent options
        var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
        var originalOptions = new GenerationOptions
        {
            PanelCount = validPanelCount,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese,
            EnablePinyin = true
        };

        // Act - Process options through various stages
        var processedOptions1 = _processor.ApplyDefaults(originalOptions);
        var processedOptions2 = _processor.AdjustForAgeGroup(processedOptions1, originalOptions.AgeGroup);
        var processedOptions3 = _processor.ApplyDefaults(processedOptions2);
        
        // Assert - Options should remain consistent
        var panelCountConsistent = processedOptions1.PanelCount == processedOptions2.PanelCount && 
                                 processedOptions2.PanelCount == processedOptions3.PanelCount;
        var ageGroupConsistent = processedOptions1.AgeGroup == processedOptions2.AgeGroup && 
                               processedOptions2.AgeGroup == processedOptions3.AgeGroup;
        var visualStyleConsistent = processedOptions1.VisualStyle == processedOptions2.VisualStyle && 
                                  processedOptions2.VisualStyle == processedOptions3.VisualStyle;
        var languageConsistent = processedOptions1.Language == processedOptions2.Language && 
                               processedOptions2.Language == processedOptions3.Language;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Parameter Consistency: PanelCount={panelCountConsistent}, AgeGroup={ageGroupConsistent}, VisualStyle={visualStyleConsistent}, Language={languageConsistent}");
        
        return panelCountConsistent && ageGroupConsistent && visualStyleConsistent && languageConsistent;
    }

    [Property]
    public bool Property14_ParameterConsistency_ConsistencyValidationWorks()
    {
        // **Feature: math-comic-generator, Property 14: 参数一致性**
        // **Validates: Requirements 4.5**
        // Consistency validation should correctly identify inconsistent parameter combinations
        
        // Arrange - Test consistent and inconsistent options
        var consistentOptions = new GenerationOptions
        {
            PanelCount = 4,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        var inconsistentOptions = new GenerationOptions
        {
            PanelCount = 6, // Too many panels for preschool
            AgeGroup = AgeGroup.Preschool,
            VisualStyle = VisualStyle.Realistic, // Not suitable for preschool
            Language = Language.Chinese
        };

        // Act - Check consistency
        var consistentResult = _processor.AreOptionsConsistent(consistentOptions);
        var inconsistentResult = _processor.AreOptionsConsistent(inconsistentOptions);
        
        // Assert - Consistency validation should work correctly
        var consistentDetected = consistentResult == true;
        var inconsistentDetected = inconsistentResult == false;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Consistency Validation: ConsistentDetected={consistentDetected}, InconsistentDetected={inconsistentDetected}");
        
        return consistentDetected && inconsistentDetected;
    }

    [Property]
    public bool Property14_ParameterConsistency_AgeGroupConstraintsAreEnforced(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 14: 参数一致性**
        // **Validates: Requirements 4.5**
        // Age group constraints should be consistently enforced across all operations
        
        // Arrange - Test preschool constraints
        var testPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
        var preschoolOptions = new GenerationOptions
        {
            PanelCount = testPanelCount,
            AgeGroup = AgeGroup.Preschool,
            VisualStyle = VisualStyle.Realistic,
            Language = Language.Chinese
        };

        // Act - Apply age group adjustments
        var adjustedOptions = _processor.AdjustForAgeGroup(preschoolOptions, AgeGroup.Preschool);
        var isConsistent = _processor.AreOptionsConsistent(adjustedOptions);
        
        // Assert - Preschool constraints should be consistently applied
        var panelCountAdjusted = adjustedOptions.PanelCount <= 4; // Preschool constraint
        var visualStyleAdjusted = adjustedOptions.VisualStyle == VisualStyle.Cartoon; // Preschool constraint
        var consistencyEnforced = isConsistent;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Age Group Constraints: OriginalPanels={testPanelCount}, AdjustedPanels={adjustedOptions.PanelCount}, PanelAdjusted={panelCountAdjusted}, VisualAdjusted={visualStyleAdjusted}, Consistent={consistencyEnforced}");
        
        return panelCountAdjusted && visualStyleAdjusted && consistencyEnforced;
    }

    [Property]
    public bool Property14_ParameterConsistency_MultipleProcessingStepsPreserveConsistency(PositiveInt seed)
    {
        // **Feature: math-comic-generator, Property 14: 参数一致性**
        // **Validates: Requirements 4.5**
        // Multiple processing steps should preserve parameter consistency
        
        // Arrange - Create various option combinations
        var ageGroups = new[] { AgeGroup.Preschool, AgeGroup.Elementary };
        var visualStyles = new[] { VisualStyle.Cartoon, VisualStyle.Realistic };
        var languages = new[] { Language.Chinese, Language.English };
        
        var selectedAgeGroup = ageGroups[seed.Get % ageGroups.Length];
        var selectedVisualStyle = visualStyles[seed.Get % visualStyles.Length];
        var selectedLanguage = languages[seed.Get % languages.Length];
        
        var options = new GenerationOptions
        {
            PanelCount = 4,
            AgeGroup = selectedAgeGroup,
            VisualStyle = selectedVisualStyle,
            Language = selectedLanguage,
            EnablePinyin = true
        };

        // Act - Process through multiple steps
        var step1 = _processor.ApplyDefaults(options);
        var step2 = _processor.AdjustForAgeGroup(step1, selectedAgeGroup);
        var step3 = _processor.ApplyDefaults(step2);
        
        // Assert - Final result should be consistent
        var finalConsistency = _processor.AreOptionsConsistent(step3);
        var finalValidation = _processor.ValidateOptions(step3);
        
        var isConsistent = finalConsistency;
        var isValid = finalValidation.IsValid;
        
        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Multi-Step Processing: AgeGroup={selectedAgeGroup}, FinalConsistent={isConsistent}, FinalValid={isValid}");
        
        return isConsistent && isValid;
    }