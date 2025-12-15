# 数学漫画生成器 (Math Comic Generator)

> 🎨 使用AI技术将数学概念转化为生动有趣的多格漫画，让孩子们爱上数学学习！

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple.svg)](https://blazor.net/)
[![Tests](https://img.shields.io/badge/Tests-79%20Passing-green.svg)](./MathComicGenerator.Tests/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

## ✨ 特性

- 🧮 **智能概念识别** - 自动验证和处理数学概念输入
- 🎨 **AI驱动生成** - 使用Gemini API创建个性化漫画
- 👶 **年龄适配** - 支持不同年龄组的内容定制
- 📱 **响应式界面** - 现代化的Blazor Server界面
- 📚 **历史管理** - 完整的漫画保存和导出功能
- 🔒 **安全可靠** - 内容过滤和儿童安全保护

## 🚀 快速开始

### 前置要求
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Gemini API密钥](https://ai.google.dev/)

### 安装和运行

```bash
# 克隆项目
git clone https://github.com/your-username/MathComicGenerator.git
cd MathComicGenerator

# 恢复依赖
dotnet restore

# 配置API密钥
cp MathComicGenerator.Api/appsettings.json.template MathComicGenerator.Api/appsettings.json
# 编辑 appsettings.json 添加你的 Gemini API 密钥

# 运行测试
dotnet test

# 启动应用（需要同时启动API和Web）

# 方法1: 使用启动脚本（推荐）
start-dev.bat  # Windows
./start-dev.sh # Linux/Mac

# 方法2: 手动启动
# 终端1: 启动API服务
dotnet run --project MathComicGenerator.Api

# 终端2: 启动Web服务  
dotnet run --project MathComicGenerator.Web
```

访问 https://localhost:5001 开始使用！
- **Web界面**: https://localhost:5001
- **API服务**: https://localhost:7109

## 📖 使用示例

### 基础用法
1. 输入数学概念：`分数的加法`
2. 选择年龄组：`小学高年级 (9-12岁)`
3. 设置面板数量：`4个面板`
4. 点击生成，等待AI创建漫画
5. 查看、保存或分享生成的漫画

### API调用
```bash
curl -X POST "https://localhost:7001/api/comic/generate" \
  -H "Content-Type: application/json" \
  -d '{
    "mathConcept": "分数的加法",
    "options": {
      "panelCount": 4,
      "ageGroup": "Elementary",
      "visualStyle": "Cartoon"
    }
  }'
```

## 🏗️ 项目架构

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Blazor Web    │───▶│   ASP.NET API   │───▶│  Gemini AI API  │
│   (前端界面)     │    │   (业务逻辑)     │    │   (内容生成)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  用户交互组件    │    │   共享服务层     │    │   本地存储      │
│  历史记录管理    │    │   数据验证      │    │   文件系统      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 📁 项目结构

```
MathComicGenerator/
├── 📁 MathComicGenerator.Api/      # Web API后端
├── 📁 MathComicGenerator.Web/      # Blazor前端
├── 📁 MathComicGenerator.Shared/   # 共享类库
├── 📁 MathComicGenerator.Tests/    # 测试项目
├── 📄 用户使用手册.md               # 用户文档
├── 📄 开发者文档.md                # 开发文档
└── 📄 README.md                   # 项目说明
```

## 🧪 测试

项目包含完整的测试套件：

```bash
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "ClassName=ComicGenerationServiceTests"

# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

**测试统计**: 79个测试全部通过 ✅
- 单元测试：验证核心业务逻辑
- 属性测试：使用FsCheck进行大规模随机测试
- 集成测试：端到端功能验证

## 📚 文档

- 📖 [用户使用手册](./用户使用手册.md) - 详细的使用指南
- 🔧 [开发者文档](./开发者文档.md) - 技术架构和开发指南
- 🚨 [故障排除指南](./故障排除指南.md) - 常见问题解决方案
- 🌐 [API文档](https://localhost:7001/swagger) - 在线API文档

## 🛠️ 技术栈

- **后端**: ASP.NET Core 8.0, C# 12
- **前端**: Blazor Server, HTML5, CSS3
- **AI服务**: Gemini Nano Banana Pro API
- **测试**: xUnit, FsCheck, Moq
- **工具**: Polly (重试), Serilog (日志)

## 🔒 安全特性

- ✅ 输入验证和清理
- ✅ 内容安全过滤
- ✅ 速率限制保护
- ✅ HTTPS加密传输
- ✅ 儿童安全内容控制

## 🚀 部署

### Docker部署
```bash
# 构建镜像
docker build -t mathcomicgenerator .

# 运行容器
docker run -p 5000:80 -e GEMINI_API_KEY=your-key mathcomicgenerator
```

### 生产部署
```bash
# 发布应用
dotnet publish -c Release -o ./publish

# 运行生产版本
cd publish && dotnet MathComicGenerator.Web.dll
```

## 🤝 贡献

欢迎贡献代码！请查看 [开发者文档](./开发者文档.md) 了解详细信息。

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- [Gemini API](https://ai.google.dev/) - 提供强大的AI内容生成能力
- [.NET Community](https://dotnet.microsoft.com/community) - 优秀的开发框架和社区支持
- [Blazor](https://blazor.net/) - 现代化的Web UI框架

## 📞 联系我们

- 📧 Email: your-email@example.com
- 🐛 Issues: [GitHub Issues](https://github.com/your-username/MathComicGenerator/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/your-username/MathComicGenerator/discussions)

---

<div align="center">
  <p>用❤️制作，让数学学习更有趣！</p>
  <p>Made with ❤️ to make math learning fun!</p>
</div>