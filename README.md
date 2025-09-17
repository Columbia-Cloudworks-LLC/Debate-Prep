# Debate-Prep

Moderator-controlled multi-agent debate app for structured practice and analysis. Create personas, control who speaks next, rate responses, and export transcripts. Built for Windows-first desktop with Rust + Tauri and a React UI. Open-sourced by Columbia Cloudworks LLC under the MIT license.

## Features

- Moderator decides who speaks next; no automatic rounds
- Custom AI participants with persistent personas and critique memory
- Streaming responses with token/cost estimates
- Local persistence (SQLite); exports to Markdown/HTML/TXT
- Secure key storage (Windows DPAPI; OS keyring elsewhere)

## Tech Stack

- Shell: Tauri (Rust backend + WebView)
- UI: React + Vite + TypeScript
- Backend: Rust (Tokio, Reqwest, Serde, Rusqlite)
- Providers: Hugging Face (v1), Ollama (planned)

## Getting Started (development)

Prerequisites:

- Rust (stable) and Cargo
- Node.js 18+ and a package manager (pnpm recommended)

Install dependencies and run:

```bash
# from the project root
pnpm install
pnpm tauri dev
```

Build installers:

```bash
pnpm tauri build
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
