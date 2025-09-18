# Debate-Prep

Moderator-controlled multi-agent debate app for structured practice and analysis. Create personas, control who speaks next, rate responses, and export transcripts. Built for Windows-only desktop with .NET 8 + WinUI 3. Open-sourced by Columbia Cloudworks LLC under the MIT license.

## Features

- Moderator decides who speaks next; no automatic rounds
- Custom AI participants with persistent personas and critique memory
- Streaming responses with token/cost estimates
- Local persistence (SQLite); exports to Markdown/HTML/TXT
- Secure key storage (Windows DPAPI; OS keyring elsewhere)

## Tech Stack

- Shell/UI: Windows App SDK (WinUI 3, .NET 8)
- Language: C# (.NET 8)
- Data: SQLite (`Microsoft.Data.Sqlite`)
- Networking: `HttpClient`, `System.Text.Json`
- Key storage: DPAPI (`System.Security.Cryptography.ProtectedData`)
- Providers: Hugging Face (v1), Ollama (planned)

## Getting Started (development)

Prerequisites:

- .NET 8 SDK
- Windows 10/11 (x64)

Run the app (from solution directory):

```powershell
dotnet restore
dotnet build
dotnet run --project src/DebatePrep.App
```

Build installer:

```powershell
# MSIX via Windows App SDK or MSI via WiX (once configured)
dotnet publish src/DebatePrep.App -c Release -r win10-x64 --self-contained false
```

## Configuration

- Add your model provider API key in the app Settings. Keys are encrypted at rest.
- Default provider is Hugging Face; local providers (e.g., Ollama) are planned.

## Exports

- Markdown, HTML, and plaintext are supported in v1. PDF planned for v1.1.

## Security

- No telemetry by default. API keys are stored encrypted (DPAPI on Windows).

## Roadmap

- See `PRD.md` for detailed goals, acceptance criteria, and milestones.

## License

MIT © 2025 Columbia Cloudworks LLC. See [LICENSE](./LICENSE).
