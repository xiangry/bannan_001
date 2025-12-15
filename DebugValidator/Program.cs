using MathComicGenerator.Shared.Services;

var validator = new MathConceptValidator();

// 测试数学内容检测
var testInput = "加法运算";
Console.WriteLine($"测试输入: '{testInput}'");

var cleanedInput = testInput.Trim();
Console.WriteLine($"清理后输入: '{cleanedInput}'");

var words = cleanedInput.Split(new char[] { ' ', '，', ',', '。', '.' }, 
    StringSplitOptions.RemoveEmptyEntries);
Console.WriteLine($"分词结果: [{string.Join(", ", words.Select(w => $"'{w}'"))}]");

var isMath = validator.IsMathematicalContent(testInput);
Console.WriteLine($"是否为数学内容: {isMath}");

var validationResult = validator.ValidateInput(testInput);
Console.WriteLine($"验证结果: IsValid={validationResult.IsValid}, ErrorMessage='{validationResult.ErrorMessage}'");

var concept = validator.ParseMathConcept(testInput);
Console.WriteLine($"解析结果: Topic='{concept.Topic}', Keywords=[{string.Join(", ", concept.Keywords)}]");