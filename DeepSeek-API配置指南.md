# 🤖 DeepSeek API 配置指南

## 📋 概述

本指南将帮助您配置DeepSeek API来替代原有的Gemini API，实现更强大的提示词生成功能。

## 🔧 配置步骤

### 1. 获取DeepSeek API密钥

1. 访问 [DeepSeek官网](https://www.deepseek.com/)
2. 注册账户并登录
3. 进入API管理页面
4. 创建新的API密钥
5. 复制API密钥备用

### 2. 配置应用程序

#### 方法1: 修改appsettings.json

编辑 `MathComicGenerator.Api/appsettings.json` 文件：

```json
{
  "DeepSeekAPI": {
    "BaseUrl": "https://api.deepseek.com/v1",
    "ApiKey": "YOUR_DEEPSEEK_API_KEY_HERE",
    "Model": "deepseek-chat",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "Temperature": 0.7,
    "MaxTokens": 2048,
    "TopP": 0.95,
    "FrequencyPenalty": 0.0,
    "PresencePenalty": 0.0
  }
}
```

#### 方法2: 使用环境变量

设置环境变量：
```bash
# Windows
set DeepSeekAPI__ApiKey=YOUR_DEEPSEEK_API_KEY_HERE

# Linux/Mac
export DeepSeekAPI__ApiKey=YOUR_DEEPSEEK_API_KEY_HERE
```

### 3. 参数说明

| 参数 | 说明 | 默认值 | 建议值 |
|------|------|--------|--------|
| `BaseUrl` | DeepSeek API基础URL | `https://api.deepseek.com/v1` | 保持默认 |
| `ApiKey` | 您的API密钥 | 空 | **必须配置** |
| `Model` | 使用的模型 | `deepseek-chat` | 保持默认 |
| `Temperature` | 创造性控制 | `0.7` | 0.6-0.8 |
| `MaxTokens` | 最大输出长度 | `2048` | 1024-4096 |
| `TopP` | 核采样参数 | `0.95` | 0.9-0.95 |
| `TimeoutSeconds` | 请求超时时间 | `30` | 30-60 |
| `MaxRetries` | 最大重试次数 | `3` | 3-5 |

## 🎯 功能特性

### DeepSeek API vs 原有系统

| 特性 | 原有智能模拟 | DeepSeek API |
|------|-------------|-------------|
| **创造性** | 预设模板 | ✅ 真实AI生成 |
| **多样性** | 有限变化 | ✅ 无限创意 |
| **适应性** | 关键词匹配 | ✅ 深度理解 |
| **响应速度** | 1秒 | 2-5秒 |
| **离线工作** | ✅ 支持 | ❌ 需要网络 |
| **成本** | 免费 | 按使用付费 |

### 支持的功能

✅ **智能提示词生成**
- 根据知识点自动生成详细的漫画创作提示词
- 支持多学科内容（数学、科学、历史、语言、艺术等）
- 自适应年龄组和视觉风格

✅ **提示词优化**
- 自动优化用户编辑的提示词
- 提供专业的改进建议
- 确保教育价值和趣味性

✅ **多语言支持**
- 中文和英文提示词生成
- 本地化的教育内容

## 🧪 测试验证

### 1. 运行集成测试

```powershell
# 启动服务
./start-dev.bat

# 运行DeepSeek API测试
./test-deepseek-integration.ps1
```

### 2. 手动测试

1. 访问 `https://localhost:5001`
2. 输入任意知识点
3. 点击"生成提示词"
4. 观察生成的内容质量和创造性

### 3. API直接测试

```powershell
# 测试提示词生成
$body = @{
    MathConcept = "量子力学的基本原理"
    Options = @{
        PanelCount = 4
        AgeGroup = 2
        VisualStyle = 0
        Language = 0
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7109/api/comic/generate-prompt" -Method POST -Body $body -ContentType "application/json" -SkipCertificateCheck
```

## 🔄 回退机制

如果DeepSeek API不可用，系统会自动回退到智能模拟数据：

1. **API密钥未配置** → 使用模拟数据
2. **网络连接失败** → 使用模拟数据
3. **API配额用完** → 使用模拟数据
4. **请求超时** → 重试后使用模拟数据

## 📊 性能优化

### 1. 缓存策略

```json
{
  "DeepSeekAPI": {
    "EnableCaching": true,
    "CacheExpirationMinutes": 60
  }
}
```

### 2. 并发控制

```json
{
  "DeepSeekAPI": {
    "MaxConcurrentRequests": 5,
    "RequestQueueSize": 20
  }
}
```

### 3. 成本控制

```json
{
  "DeepSeekAPI": {
    "MaxTokensPerDay": 100000,
    "MaxRequestsPerHour": 100
  }
}
```

## 🛠️ 故障排除

### 常见问题

#### 1. API密钥错误
```
错误: Unauthorized (401)
解决: 检查API密钥是否正确配置
```

#### 2. 请求超时
```
错误: Request timeout
解决: 增加TimeoutSeconds值或检查网络连接
```

#### 3. 配额超限
```
错误: Quota exceeded
解决: 检查API使用量或升级套餐
```

#### 4. 模型不可用
```
错误: Model not found
解决: 确认模型名称是否正确
```

### 调试模式

启用详细日志：

```json
{
  "Logging": {
    "LogLevel": {
      "MathComicGenerator.Api.Services.DeepSeekAPIService": "Debug"
    }
  }
}
```

## 📈 监控和分析

### 1. API使用统计

- 请求次数
- 响应时间
- 成功率
- 错误类型

### 2. 内容质量评估

- 提示词长度分布
- 用户满意度
- 编辑频率
- 生成成功率

## 🔐 安全考虑

### 1. API密钥保护

- ❌ 不要在代码中硬编码API密钥
- ✅ 使用环境变量或安全配置
- ✅ 定期轮换API密钥
- ✅ 限制API密钥权限

### 2. 内容过滤

- ✅ 输入内容安全检查
- ✅ 输出内容适宜性验证
- ✅ 敏感信息过滤

## 📚 相关文档

- [DeepSeek API官方文档](https://api-docs.deepseek.com/)
- [系统架构文档](./开发者文档.md)
- [用户使用手册](./用户使用手册.md)
- [故障排除指南](./故障排除指南.md)

## 🎉 总结

通过配置DeepSeek API，您的数学漫画生成器将获得：

✅ **更强的创造力** - 真实AI生成，不再局限于预设模板
✅ **更好的适应性** - 深度理解用户输入，生成更贴切的内容
✅ **更高的质量** - 专业的提示词优化和建议
✅ **更广的覆盖** - 支持任意学科和复杂概念

配置完成后，您的应用将能够生成更加丰富、创新和教育价值更高的漫画内容！