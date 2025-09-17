# Debate-Prep

[![CI Pipeline](https://github.com/Columbia-Cloudworks-LLC/Debate-Prep/actions/workflows/ci.yml/badge.svg)](https://github.com/Columbia-Cloudworks-LLC/Debate-Prep/actions/workflows/ci.yml)

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

## Development

### Prerequisites

- **Rust** (stable) and Cargo - [Install Rust](https://rustup.rs/)
- **Node.js** 18+ and a package manager (pnpm recommended) - [Install Node.js](https://nodejs.org/)

### Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/Columbia-Cloudworks-LLC/Debate-Prep.git
   cd Debate-Prep
   ```

2. Install frontend dependencies:
   ```bash
   pnpm install
   ```

3. Run in development mode:
   ```bash
   pnpm tauri:dev
   ```

4. Build for production:
   ```bash
   pnpm tauri:build
   ```

### Code Quality

This project uses automated CI/CD to ensure code quality:

- **Rust**: Code formatting with `rustfmt`, linting with `clippy`, and comprehensive testing
- **Frontend**: TypeScript type checking, ESLint linting, and build validation
- **Integration**: Full Tauri application builds on Ubuntu and Windows

Run quality checks locally:

```bash
# Rust checks
cargo fmt --all -- --check
cargo clippy --all-targets --all-features -- -D warnings
cargo test

# Frontend checks  
pnpm type-check
pnpm lint
pnpm build

# Full Tauri build
pnpm tauri:build
```

### Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes and ensure all CI checks pass
4. Submit a pull request

All pull requests must pass the automated CI pipeline before merging.

## License

MIT © 2025 Columbia Cloudworks LLC. See [LICENSE](./LICENSE).
