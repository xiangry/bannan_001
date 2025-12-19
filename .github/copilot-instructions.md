# Copilot instructions for MathComicGenerator

Purpose
- Help an AI coding agent be productive immediately: explain architecture, dev workflows, important files, and common pitfalls.

Big-picture architecture
- Solution contains 4 main projects:
  - MathComicGenerator.Api (ASP.NET Core Web API) — controllers, DeepSeek/Gemini integrations, logging. Key files: `Program.cs`, `Controllers/ComicController.cs`, `Services/DeepSeekAPIService.cs`.
  - MathComicGenerator.Web (Blazor) — UI pages and components. Key files: `Pages/Index.razor`, `Shared/`, `Components/`.
  - MathComicGenerator.Shared — shared models, `ApiResponse<T>`, services used by API and Web. Key files: `Models/`, `Services/PromptGenerationService.cs`.
  - MathComicGenerator.Tests — unit & property tests.
- Data flows: UI -> API (ComicController) -> PromptGenerationService -> DeepSeekAPIService (external LLM) -> API returns ApiResponse<T> envelope to UI. DeepSeek responses are instrumented to disk under `MathComicGenerator.Api/logs`.

Key conventions and patterns (project-specific)
- Controller responses use an envelope `ApiResponse<T>` and set a `ProcessingTime` header. Front-end expects this envelope; when modifying endpoints keep envelope shape.
- DeepSeek calls are made by `DeepSeekAPIService`. Avoid reusing `HttpContent` across retries — the codebase already changed to create new StringContent per attempt.
- Timeouts and retries are configured in `MathComicGenerator.Api/appsettings.json` under `DeepSeekAPI`.
- Diagnostic artifacts are written to `MathComicGenerator.Api/logs`:
  - `deepseek-response-<timestamp>_*.json` contains request preview, response preview and DurationMs.
  - `logs/errors/*.json` contains structured exception logs.
- PowerShell / curl quoting: many diagnostics use PowerShell scripts under `scripts/`. Prefer file-based payloads (write JSON to `payload.json`) or use `Invoke-RestMethod` to avoid escaping issues.

Build / run / debug
- Start API (dev):
  - cd MathComicGenerator.Api
  - dotnet run --no-launch-profile
  - By default dev API listens on http://localhost:5000 (check console output).
- Start Web (dev): run `dotnet run` in `MathComicGenerator.Web` or use the provided `start-dev.bat` / `start-dev-improved.bat`.
- Tests: run `dotnet test MathComicGenerator.Tests` or `dotnet test MathComicGenerator.sln`.
- If build fails due to locked dlls, stop stray dotnet processes (e.g., `Get-Process dotnet | Stop-Process -Force`) before rebuilding.

Diagnostics and reproducible requests
- Use the helper script: `scripts/run_deepseek_diagnostic.ps1` which:
  - writes `payload.json`, posts to `/api/comic/generate-prompt`, saves `response.json`, and prints the latest `deepseek-response-*.json` and recent error logs.
- When calling endpoints from shell prefer:
  - Invoke-RestMethod -Uri 'http://localhost:5000/api/comic/generate-prompt' -Method Post -Body (Get-Content .\payload.json -Raw) -ContentType 'application/json'
  - Or curl with `--data-binary @payload.json` to avoid quoting problems.

Integration points and secrets
- External LLM integrations configured in `appsettings.json`:
  - `DeepSeekAPI` (BaseUrl, ApiKey, Model, TimeoutSeconds, MaxRetries)
  - `GeminiAPI` (fallback)
- Keep keys out of commits; `appsettings.template.json` exists as example.

Where to look first when debugging the UI stall / prompt failures
- `MathComicGenerator.Api/logs/deepseek-response-*.json` — check `debug.DurationMs` and `ResponseBodyPreview` for actual LLM latency and result size.
- `MathComicGenerator.Api/logs/errors/*.json` — structured exception with stack trace (e.g., DeepSeekAPIException for timeouts).
- `MathComicGenerator.Shared/Services/PromptGenerationService.cs` — orchestration and logging around external calls.
- `MathComicGenerator.Web/Pages/Index.razor` — how the frontend parses `ApiResponse<T>` and expects `ProcessingTime` header.

Small but important rules for code changes
- When changing external call logic (DeepSeek/Gemini): update both `appsettings.json` defaults and ensure `HttpContent` is re-created per attempt.
- Keep `ApiResponse<T>` envelope stable unless you update both controller and front-end parsing simultaneously.
- Add logging to `MathComicGenerator.Api.Services.DeepSeekAPIService` using the existing file-based debug pattern so diagnostics are automatically produced.

Files & scripts to know
- scripts/install_git_hooks.ps1 — installs local git hooks used by the agent workflow.
- scripts/run_deepseek_diagnostic.ps1 — diagnostic helper (see above).
- start-dev.bat / start-dev-improved.bat — developer convenience startup scripts.
- MathComicGenerator.Api/appsettings.json and appsettings.template.json — environment-sensitive configuration.

If anything is unclear or you want this trimmed/expanded, tell me which area to refine (architecture, troubleshooting steps, or recommended quick fixes).
