# Debate-Prep

Moderator-controlled multi-agent debate app for structured practice and analysis. Create personas, control who speaks next, rate responses, and export transcripts. Built for Windows-only desktop with .NET 8 + WinUI 3. Open-sourced by Columbia Cloudworks LLC under the MIT license.

## Features

- **Moderator Control**: Moderator decides who speaks next; no automatic rounds
- **Custom AI Participants**: Persistent personas with critique memory and learning
- **Streaming Responses**: Real-time token streaming with cost estimates
- **Local Persistence**: SQLite storage; exports to Markdown/HTML/TXT
- **Secure Configuration**: Encrypted key storage (Windows DPAPI)
- **Critique Memory**: ML.NET-powered similarity detection for constructive feedback

## Architecture

```
DebatePrep (WinUI 3 App)
├── MainWindow.xaml - Main application window
├── App.xaml - Application entry point
└── (ViewModels and additional views)

DebatePrep.Core (Business Logic)
├── Models/ - Data entities (Session, Participant, Turn, etc.)
├── Data/ - Database context and migrations
├── Services/ - Business logic services
├── Providers/ - AI model provider abstractions
└── (Turn generation, prompt engineering)

DebatePrep.Tests (Unit Tests)
├── SessionServiceTests.cs - Core functionality tests
└── Golden/ - Reference files for regression testing
```

## Current Implementation Status

### ✅ Phase 1 Complete
- **Core Infrastructure**: Clean project separation, SQLite persistence, complete data models
- **Core Services**: Session management, configuration service, export service, critique memory
- **Provider Abstraction**: IModelProvider interface with HuggingFace implementation
- **WinUI 3 Shell**: Modern Windows UI with responsive layout and accessibility
- **Testing Framework**: Unit tests with golden file regression testing

### Key Services Implemented

#### SessionService
- Create and manage debate sessions
- Add participants with personas (position, constraints, disallowed behaviors)
- Record turns and manage debate transcripts
- Rate turns with thumbs up/down and constructive feedback

#### CritiqueMemoryService
- ML.NET-powered similarity detection for critique rules
- Cosine similarity threshold (≥0.80) for rule merging
- Upvote decay system (-0.02 for unused rules)
- Deterministic floating-point operations (2-decimal precision)

#### ExportService
- Export sessions to Markdown, HTML, and Plain Text formats
- Proper metadata ordering and timestamp formatting
- Support for archived participants
- UTF-8 encoding with LF line endings

## Tech Stack

- **Shell/UI**: Windows App SDK (WinUI 3, .NET 8)
- **Language**: C# (.NET 8)
- **Data**: SQLite (`Microsoft.Data.Sqlite`)
- **ML**: ML.NET for critique similarity detection
- **Networking**: `HttpClient`, `System.Text.Json`
- **Key Storage**: DPAPI (`System.Security.Cryptography.ProtectedData`)
- **Providers**: Hugging Face (v1), Ollama (planned)

## Getting Started

### Prerequisites
- .NET 8 SDK
- Windows 10 version 1903 (build 18362) or later
- Visual Studio 2022 with Windows App SDK workload (recommended)

### Build and Run
```powershell
# Clone and restore dependencies
dotnet restore

# Build the solution
dotnet build DebatePrep.sln

# Run the application
dotnet run --project src/DebatePrep/DebatePrep.csproj
```

### Run Tests
```bash
dotnet test tests/DebatePrep.Tests/DebatePrep.Tests.csproj
```

### Build Installer
```powershell
# MSIX via Windows App SDK or MSI via WiX (once configured)
dotnet publish src/DebatePrep -c Release -r win10-x64 --self-contained false
```

## Configuration

- Add your model provider API key in the app Settings
- Keys are encrypted at rest using Windows DPAPI
- Default provider is Hugging Face; local providers (e.g., Ollama) are planned
- Generation parameters are configurable per provider

## Security

- **No Telemetry**: No data collection by default
- **Encrypted Storage**: API keys stored using Windows DPAPI encryption
- **Local First**: All data stored locally in SQLite database
- **Secure Providers**: Provider abstraction supports secure API communication

## Next Phase Roadmap

### Phase 2 - Core Functionality
1. **Turn Generation System**: AI-powered debate turn generation
2. **MVVM Implementation**: ViewModels and proper data binding
3. **Prompt Engineering**: Context-aware prompt system with critique guidance
4. **Session Management UI**: Complete session creation and management interface
5. **Real-time Streaming**: Streaming token display during generation

### Phase 3 - Polish and Features
1. **Asset Creation**: Application icons and branding
2. **Error Handling**: Comprehensive error recovery and user feedback
3. **Performance Optimization**: Token budgeting and memory management
4. **Additional Providers**: Ollama integration and provider extensibility
5. **Advanced Export**: PDF export capability

## Compliance

This implementation follows PRD specifications including:
- ✅ Deterministic algorithms with 2-decimal precision
- ✅ Fixed metadata ordering in exports
- ✅ ML.NET implementation for critique memory
- ✅ SQLite schema as specified
- ✅ Windows DPAPI for key encryption
- ✅ UTF-8 encoding with LF newlines
- ✅ Token chunk interface as defined

## Detailed Specifications

For detailed goals, acceptance criteria, and milestones, see `PRD.md`.

## License

MIT © 2025 Columbia Cloudworks LLC. See [LICENSE](./LICENSE).
