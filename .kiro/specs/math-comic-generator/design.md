# 设计文档

## 概述

数学漫画生成器是一个基于Web的应用程序，它利用多个AI服务（Gemini API和DeepSeek API）为儿童创建教育性的多格漫画。系统采用模块化架构，包含前端用户界面、后端API服务、AI集成层和数据存储层。该系统已实现完整的.NET 8解决方案，包含API、Web前端、共享库和测试项目。

## 架构

系统采用分层架构模式：

```
┌─────────────────────────────────────┐
│           前端界面层                 │
│         (Blazor Server)            │
│    MathInputComponent.razor        │
│   ComicDisplayComponent.razor      │
│    HistoryComponent.razor          │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│           API控制器层                │
│        (ASP.NET Core Web API)      │
│     ComicController.cs             │
│     ImagesController.cs            │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│          业务逻辑层                  │
│   ComicGenerationService.cs        │
│   PromptGenerationService.cs       │
│   MathConceptValidator.cs          │
│   GenerationOptionsProcessor.cs    │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│         AI集成层                    │
│    GeminiAPIService.cs             │
│    DeepSeekAPIService.cs           │
│    ImageGenerationService.cs       │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│          数据存储层                  │
│      StorageService.cs             │
│   (文件系统 + JSON元数据)           │
└─────────────────────────────────────┘
```

## 组件和接口

### 1. 前端组件

#### MathInputComponent
- **职责**: 接收用户输入的数学知识点
- **接口**: 
  - `onSubmit(mathConcept: string, options: GenerationOptions): void`
  - `validateInput(input: string): ValidationResult`

#### ComicDisplayComponent  
- **职责**: 显示生成的多格漫画
- **接口**:
  - `displayComic(comic: MultiPanelComic): void`
  - `onSave(comic: MultiPanelComic): void`
  - `onShare(comic: MultiPanelComic): void`

#### HistoryComponent
- **职责**: 管理和显示历史生成记录
- **接口**:
  - `loadHistory(): ComicHistory[]`
  - `deleteComic(id: string): void`

#### ConsoleLoggingService
- **职责**: 管理浏览器控制台的调试输出
- **接口**:
  - `logUserAction(action: string, details: object): void`
  - `logError(error: Error, context: string): void`
  - `logAPIRequest(url: string, params: object, timestamp: Date): void`
  - `logAPIResponse(status: number, dataSize: number, processingTime: number): void`

### 2. 后端服务

#### ComicGenerationService
- **职责**: 协调漫画生成流程
- **接口**:
  - `generateComic(concept: MathConcept, options: GenerationOptions): Promise<MultiPanelComic>`
  - `validateConcept(concept: string): ValidationResult`

#### GeminiAPIService
- **职责**: 与Gemini API交互生成漫画内容
- **接口**:
  - `GenerateComicAsync(prompt: string): Task<string>`
  - `GenerateImageAsync(prompt: string): Task<string>`

#### DeepSeekAPIService  
- **职责**: 与DeepSeek API交互进行内容生成
- **接口**:
  - `GenerateContentAsync(request: PromptGenerationRequest): Task<string>`
  - `ValidateApiKeyAsync(): Task<bool>`

#### ImageGenerationService
- **职责**: 管理图像生成和处理
- **接口**:
  - `GenerateImageAsync(prompt: string): Task<string>`
  - `ProcessImageAsync(imageData: byte[]): Task<string>`

#### StorageService
- **职责**: 管理漫画存储和检索
- **接口**:
  - `SaveComicAsync(comic: MultiPanelComic): Task<string>`
  - `LoadComicAsync(id: string): Task<MultiPanelComic>`
  - `ListComicsAsync(): Task<List<ComicMetadata>>`
  - `DeleteComicAsync(id: string): Task<bool>`
  - `GetComicsByFilterAsync(filter: ComicFilter): Task<List<ComicMetadata>>`

#### ErrorLoggingService
- **职责**: 统一错误日志管理
- **接口**:
  - `LogErrorAsync(error: Exception, context: string): Task`
  - `LogWarningAsync(message: string, context: string): Task`

#### ResourceManagementService
- **职责**: 系统资源监控和管理
- **接口**:
  - `CheckMemoryUsage(): ResourceStatus`
  - `CheckDiskSpace(): ResourceStatus`
  - `ValidateResourceLimits(): bool`

#### ConfigurationValidationService
- **职责**: 配置验证和管理
- **接口**:
  - `ValidateApiConfiguration(): ValidationResult`
  - `ValidateStorageConfiguration(): ValidationResult`

#### LoggingService
- **职责**: 统一管理系统日志输出
- **接口**:
  - `logToConsole(level: LogLevel, message: string, data?: object): void`
  - `formatUTF8Message(message: string, data: object): string`
  - `trackUserInteraction(interaction: UserInteraction): void`

## 数据模型

### MathConcept
```csharp
public class MathConcept
{
    public string Topic { get; set; }           // 数学主题
    public DifficultyLevel Difficulty { get; set; }  // 难度级别
    public AgeGroup AgeGroup { get; set; }      // 目标年龄组
    public List<string> Keywords { get; set; }  // 关键词
}
```

### MultiPanelComic
```csharp
public class MultiPanelComic
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<ComicPanel> Panels { get; set; }
    public ComicMetadata Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### ComicPanel
```csharp
public class ComicPanel
{
    public string Id { get; set; }
    public string ImageUrl { get; set; }
    public List<string> Dialogue { get; set; }
    public string? Narration { get; set; }
    public int Order { get; set; }
}
```

### GenerationOptions
```csharp
public class GenerationOptions
{
    public int PanelCount { get; set; }         // 3-6个面板
    public AgeGroup AgeGroup { get; set; }      // 目标年龄组
    public VisualStyle VisualStyle { get; set; } // 视觉风格
    public Language Language { get; set; }      // 语言设置
}
```

### ComicMetadata
```csharp
public class ComicMetadata
{
    public string MathConcept { get; set; }
    public GenerationOptions GenerationOptions { get; set; }
    public long FileSize { get; set; }
    public ImageFormat Format { get; set; }
    public List<string> Tags { get; set; }
}
```

### LogEntry
```csharp
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; }
    public string? Context { get; set; }
    public object? Data { get; set; }
    public string? UserId { get; set; }
}
```

### UserInteraction
```csharp
public class UserInteraction
{
    public string ActionType { get; set; }      // "input", "click", "select", etc.
    public string ComponentName { get; set; }   // 组件名称
    public object? ActionData { get; set; }     // 交互数据
    public DateTime Timestamp { get; set; }
    public string? SessionId { get; set; }
}
```

## 正确性属性

*属性是指在系统的所有有效执行中都应该保持为真的特征或行为——本质上是关于系统应该做什么的正式声明。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 有效输入接受
*对于任何*有效的数学知识点输入，系统应该接受输入并成功启动处理流程
**验证: 需求 1.1**

### 属性 2: 面板数量约束
*对于任何*漫画生成请求，生成的Multi_Panel_Comic应该包含3-6个面板
**验证: 需求 1.3**

### 属性 3: 输出完整性
*对于任何*成功的生成请求，返回的Multi_Panel_Comic应该包含所有必需的字段（id、title、panels、metadata、createdAt）
**验证: 需求 1.5**

### 属性 4: API请求格式正确性
*对于任何*向Gemini API发送的请求，请求格式应该符合API规范要求
**验证: 需求 2.1**

### 属性 5: 成功响应解析
*对于任何*来自API的成功响应，系统应该能够正确解析并提取漫画内容
**验证: 需求 2.2**

### 属性 6: 错误处理完整性
*对于任何*API请求失败的情况，系统应该提供有意义的错误信息给用户
**验证: 需求 2.3**

### 属性 7: 超时处理机制
*对于任何*超过预设阈值的API响应时间，系统应该实施超时处理机制
**验证: 需求 2.4**

### 属性 8: 响应验证
*对于任何*API响应，系统应该验证返回内容的完整性和格式正确性
**验证: 需求 2.5**

### 属性 9: 内容安全过滤
*对于任何*生成的漫画内容，不应包含暴力、恐怖或其他不当关键词
**验证: 需求 3.2**

### 属性 10: 语言复杂度控制
*对于任何*生成的对话和说明文字，语言复杂度应该适合目标年龄组
**验证: 需求 3.4**

### 属性 11: 年龄组参数响应
*对于任何*指定的目标年龄组设置，生成的内容应该在语言复杂度上有相应调整
**验证: 需求 4.1**

### 属性 12: 面板数量控制
*对于任何*用户指定的面板数量，生成的漫画应该包含确切的面板数
**验证: 需求 4.2**

### 属性 13: 参数验证
*对于任何*用户提供的自定义参数，系统应该验证其有效性
**验证: 需求 4.4**

### 属性 14: 参数一致性
*对于任何*应用的自定义设置，在整个生成过程中应该保持一致
**验证: 需求 4.5**

### 属性 15: 保存功能可用性
*对于任何*完成的漫画生成，系统应该提供保存选项
**验证: 需求 5.1**

### 属性 16: 存储格式规范
*对于任何*保存的漫画，应该以常见的图像格式存储
**验证: 需求 5.2**

### 属性 17: 元数据完整性
*对于任何*保存的漫画，应该包含完整的元数据信息
**验证: 需求 5.3**

### 属性 18: 历史记录功能
*对于任何*历史记录请求，系统应该显示之前生成的漫画列表
**验证: 需求 5.4**

### 属性 19: 分享功能可用性
*对于任何*生成的漫画，系统应该提供导出和分享功能
**验证: 需求 5.5**

### 属性 20: 无效输入拒绝
*对于任何*空白或无效的数学知识点输入，系统应该拒绝处理并提供错误提示
**验证: 需求 6.1**

### 属性 21: 非数学内容检测
*对于任何*包含非数学相关内容的输入，系统应该检测并引导用户提供合适的数学概念
**验证: 需求 6.2**

### 属性 22: 资源限制处理
*对于任何*系统资源不足的情况，系统应该优雅地处理并通知用户
**验证: 需求 6.3**

### 属性 23: 错误记录和用户通知
*对于任何*意外错误，系统应该记录错误信息并提供用户友好的错误消息
**验证: 需求 6.4**

### 属性 24: 系统恢复功能
*对于任何*系统恢复正常的情况，应该允许用户重新尝试操作
**验证: 需求 6.5**

### 属性 25: 用户操作日志记录
*对于任何*用户操作，系统应该在浏览器控制台输出UTF-8格式的操作提示信息
**验证: 需求 7.1**

### 属性 26: 错误信息控制台输出
*对于任何*系统错误，控制台应该包含详细的错误信息和堆栈跟踪
**验证: 需求 7.2**

### 属性 27: 用户交互跟踪
*对于任何*用户输入或选择操作，控制台应该记录用户交互的详细信息
**验证: 需求 7.3**

### 属性 28: API请求日志记录
*对于任何*发起的API请求，控制台应该输出请求的详细信息包括URL、参数和时间戳
**验证: 需求 7.4**

### 属性 29: API响应日志记录
*对于任何*接收的API响应，控制台应该输出响应状态、数据大小和处理时间
**验证: 需求 7.5**

## 错误处理

### 输入验证错误
- **空输入**: 返回"请输入数学知识点"提示
- **非数学内容**: 返回"请输入有效的数学概念"并提供示例
- **过长输入**: 限制输入长度并提供截断提示

### API集成错误
- **网络错误**: 实施重试机制，最多3次重试
- **API限额**: 显示限额信息和重试时间
- **响应格式错误**: 记录错误并请求用户重试
- **超时错误**: 显示超时提示并允许重新生成

### 存储错误
- **磁盘空间不足**: 提示清理空间或选择其他存储位置
- **权限错误**: 提供权限设置指导
- **文件损坏**: 提供重新生成选项

### 系统资源错误
- **内存不足**: 优化处理流程或建议减少面板数量
- **CPU过载**: 实施队列机制延迟处理

## 测试策略

### 单元测试方法
系统使用xUnit作为单元测试框架，重点测试：
- 输入验证逻辑的具体示例
- API请求/响应处理的边界情况
- 数据模型的序列化/反序列化
- 错误处理的特定场景

**当前实现状态：**
- ✅ 完整的单元测试套件已实现
- ✅ 所有主要服务都有对应的测试类
- ✅ 使用Moq进行依赖模拟
- ✅ 测试覆盖包括：ComicGenerationService、GeminiAPIService、DeepSeekAPIService、StorageService、ErrorLoggingService、ResourceManagementService等

### 基于属性的测试方法
系统使用FsCheck作为属性测试库，配置要求：
- 每个属性测试运行最少100次迭代
- 每个属性测试必须用注释明确引用设计文档中的正确性属性
- 使用格式：'**Feature: math-comic-generator, Property {number}: {property_text}**'

**当前实现状态：**
- ✅ 基础属性测试框架已设置（BasicPropertyTests.cs）
- ✅ FsCheck集成已配置
- ⚠️ 需要实现29个正确性属性的完整属性测试套件

属性测试将验证：
- 输入验证在所有可能输入上的一致性
- 生成输出格式在所有参数组合下的正确性
- 错误处理在各种失败场景下的稳健性
- 数据完整性在整个处理流程中的保持

### 集成测试
- API集成测试使用模拟的Gemini服务
- 端到端用户流程测试
- 性能和负载测试

### 测试数据生成
- 数学概念生成器：创建各种难度和主题的数学概念
- 参数组合生成器：生成有效和无效的用户参数组合
- 错误场景模拟器：模拟各种系统和网络错误情况