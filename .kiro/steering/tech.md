# Technology Stack & Build System

## Framework & Runtime
- **.NET 8**: Primary framework with C# language features
- **ASP.NET Core**: Web API and hosting infrastructure
- **Blazor Server**: Frontend framework for interactive web UI

## Key Libraries & Dependencies
- **Polly**: Resilience and transient-fault-handling (retry policies, circuit breakers)
- **xUnit**: Primary testing framework
- **FsCheck**: Property-based testing for comprehensive test coverage
- **Moq**: Mocking framework for unit tests
- **coverlet.collector**: Code coverage analysis

## Project Configuration
- **Nullable Reference Types**: Enabled across all projects (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled for cleaner code (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Target Framework**: net8.0 for all projects

## Common Commands

### Build & Restore
```bash
dotnet restore          # Restore NuGet packages
dotnet build           # Build entire solution
dotnet build --configuration Release  # Release build
```

### Testing
```bash
dotnet test            # Run all tests
dotnet test --collect:"XPlat Code Coverage"  # Run with coverage
```

### Running Applications
```bash
# API (typically runs on https://localhost:7xxx)
dotnet run --project MathComicGenerator.Api

# Web UI (typically runs on https://localhost:5xxx)
dotnet run --project MathComicGenerator.Web
```

### Development
```bash
dotnet watch run --project MathComicGenerator.Api   # API with hot reload
dotnet watch run --project MathComicGenerator.Web   # Web with hot reload
```

## External Dependencies
- **Gemini API**: AI service for comic content generation
- **HTTP Client**: Configured for external API communication with proper error handling