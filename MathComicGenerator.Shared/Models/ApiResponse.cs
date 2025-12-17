namespace MathComicGenerator.Shared.Models;

/// <summary>
/// API响应包装类
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string ProcessingTime { get; set; } = string.Empty;
}

/// <summary>
/// 无数据的API响应
/// </summary>
public class ApiResponse : ApiResponse<object>
{
}