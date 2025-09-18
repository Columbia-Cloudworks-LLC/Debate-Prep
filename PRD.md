# Product Requirements Document — Debate-Prep (Moderator-Controlled Multi-Agent Debate)

## 0) Change Log

* **v2.0 (2025-09-18)**: Added interaction flows & state machine, prompt/critique memory spec, error taxonomy & recovery, rigorous testing plan, release engineering, provider capability matrix, performance budgets, accessibility, analytics schema (opt-in), backup/restore, threat model, sample data/exports, provider adapter interface, database migrations, coding standards, and glossary.
* **v1.0 (2025-09-17)**: Initial PRD.

## 1) Document Control

* **Owner**: Columbia Cloudworks LLC
* **Authors**: Product, Engineering
* **License**: MIT
* **Project**: Debate-Prep
* **Status**: Draft → Review → Approved (target: v1.0 GA)
* **Last Updated**: 2025-09-18

---

## 2) Summary

Debate-Prep is a Windows-first, moderator-controlled, multi-agent debate sandbox. Users define AI participants with personas. The moderator explicitly selects who speaks next, can rate responses, and uses downvotes (with reasons) to update a participant’s “critique memory” that guides future replies. Sessions persist locally and can be exported.

---

## 3) Problem Statement

Debaters, students, and analysts need offline-friendly practice with surgical control of turn-taking and rapid learning loops. Existing LLM chats either auto-advance or lack structured, reusable critique. Debate-Prep creates reliable, cost-aware drills with deterministic turns and adaptive feedback.

---

## 4) Goals (v1)

* Windows-first native desktop app with signed installer.
* Moderator-controlled turns (no auto rounds).
* Arbitrary participants, persistent personas & critique memory.
* First-class exports (Markdown/HTML/TXT). PDF in v1.1.
* Pluggable model providers (start: Hugging Face; add Ollama next).
* Zero cloud dependency for state (local SQLite).

### Non-Goals (v1)

* No accounts/cloud sync.
* No voice/avatars.
* No automatic multi-rounds without moderator action.

---

## 5) Users & Personas

* **Debaters/Students** — practice arguments & cross-examination.
* **Policy Analysts/Journalists** — test positions & counters rapidly.
* **Coaches/Teachers** — structure drills with fine-grained control.

Key needs: determinism over turn order, low cost, fast iteration, archived transcripts, reusable persona libraries.

---

## 6) Core Use Cases & Acceptance (v1)

1. **Create a session** with 2–5 participants; set topic & rules.

   * *Accept*: Session saved; empty transcript visible; participants listed.
2. **Moderator calls on a participant** to respond.

   * *Accept*: Streaming tokens appear ≤2.5s (P50) from submit (hosted baseline).
3. **Provide feedback** with thumbs up/down; optional downvote reason.

   * *Accept*: Downvote stores compact critique; next turn injects guidance.
4. **Export transcript** as MD/HTML/TXT with timestamps & colors.

   * *Accept*: Export completes and opens/save dialog without data loss.
5. **Configure model provider** & decoding params; store key encrypted.

   * *Accept*: App restarts with settings intact; key still valid.

---

## 7) System Overview & Architecture

### 7.1 App Shell & UI

* **Windows App SDK** (WinUI 3, .NET 8, C#, MVVM)
* Optional **WebView2** for help/doc rendering.

### 7.2 Backend & Storage

* **.NET 8** services: Provider abstraction, Streaming, Critique Memory, Export, Persistence
* **SQLite** (Microsoft.Data.Sqlite) local DB
* **DPAPI** (ProtectedData) for API keys at rest

### 7.3 Providers (v1: Hugging Face Inference API; v1.1: Ollama)

* Streaming via HTTP/SSE or chunked reads
* Adapter interface (see Appendix B)

### 7.4 Threading & Streaming

* Background read loop → main thread dispatch for UI token appends
* Cancellation tokens for stop/abort
* Backpressure: UI batch append cadence 30–100ms

---

## 8) Data Model (SQLite)

### 8.1 Tables

* `usersettings(id, provider, model_id, api_key_encrypted, params_json, created_at)`
* `sessions(id, title, topic, rules, created_at, updated_at, tags_json)`
* `participants(id, session_id, name, color, persona, memory_json, provider_overrides_json, created_at)`
* `messages(id, session_id, participant_id, role, content, tokens_out, cost_estimate, feedback, feedback_reason, created_at)`
* `artifacts(id, session_id, kind, path, created_at)` — exports/logs

### 8.2 Indices & Constraints

* FK: `participants.session_id → sessions.id`, `messages.session_id → sessions.id`
* Indices: `sessions.updated_at`, `messages.session_id_created_at`, `participants.session_id_name`

### 8.3 Migrations (v2 changes)

* Add `topic`, `rules`, `tags_json`, `feedback_reason`, `artifacts` table.

---

## 9) Interaction Flows & State Machine

### 9.1 Happy Path (2 participants)

1. New session → add participants (A, B) → set topic/rules
2. Moderator selects **Call on A** → stream → message saved
3. Moderator *downvotes* A, adds reason
4. Moderator **Call on B** → B’s prompt includes critique memory for B (if any), and current context
5. Export transcript

### 9.2 Edge Cases

* Delete participant mid-session → retain transcript lines; mark participant as archived; prevent future calls.
* Provider timeout → show retry; preserve prompt & partial tokens (if any); log error.

### 9.3 State Machine (high-level)

* **Session**: {Idle, Generating, Cancelled, Error}
* Transitions:

  * Idle → Generating (on “Call on …”)
  * Generating → Idle (on stream end)
  * Generating → Cancelled (on user cancel)
  * Any → Error (on provider error)

---

## 10) Prompt Engineering & Critique Memory Spec

### 10.1 Prompt Roles

* **System**: Global rules for debate conduct, safety, format discipline.
* **Persona** (Participant-specific): Goals, stance, tone, constraints.
* **Context**: Topic, session rules, recent transcript window (token-bounded).
* **Critique Injection** (per participant): Compact “What to improve next time” synthesized from downvotes.
* **User**: Moderator instruction/turn request.

### 10.2 System Template (v1)

```
You are taking part in a structured debate. Speak only when called.
Follow these rules:
- Stay within your assigned persona and stance.
- Prefer concise, numbered arguments; cite premises explicitly.
- Avoid hidden chain-of-thought; provide concise reasoning only.
- Do not simulate other speakers or the moderator.
- Never invent citations or facts; if unsure, mark as uncertain.
- Hard cap: ≤{max_tokens_out} tokens.
Format:
- One short thesis line, then 2–5 bullet points, then 1–2 counter-vulnerabilities to your own position.
```

### 10.3 Persona Template (stored with participant)

```
Name: {name}
Position: {one-sentence stance}
Constraints: {tone/voice constraints, examples of style}
Disallowed: {logical fallacies or tactics to avoid}
Key Sources/Frames: {optional short list; no fabricated citations}
```

### 10.4 Critique Memory

* **Goal**: Convert downvotes into durable, compact guidance.
* **Storage** (`memory_json` ring buffer of “rules”):

```json
[
  {
    "id": "cm_01",
    "rule": "Avoid straw-manning opponent; restate their strongest form first.",
    "bad_pattern": "Mischaracterized opposing claim without steelman.",
    "guidance": "Begin by steelmanning opponent in 1 sentence.",
    "evidence": "Downvote #12: 'You ignored their best evidence.'",
    "strength": 0.8,
    "created_at": "2025-09-18T15:02:00Z",
    "last_used": "2025-09-18T15:10:00Z",
    "hits": 3
  }
]
```

* **Budget**: ≤ 800 chars total; if overflow → summarize oldest by semantic clustering.
* **Injection** (prepend, after persona):

  * Header: “Improve by applying these 1–3 adjustments:”
  * Include top-N rules by `strength` & recency (N=3).
* **Update Algorithm**:

  * On downvote: create rule from feedback\_reason; if similar to existing (`cosine ≥ 0.8` on bag-of-words), merge and increment `strength` (cap 1.0).
  * On upvote: decay unrelated rules slightly (−0.02).
  * On successful application (detected via moderator marking “addressed” or subsequent upvote), increment `hits`, bump `last_used`.

### 10.5 Turn Prompt Assembly (order)

1. System
2. Persona
3. Critique Memory (selected rules)
4. Topic/Rules
5. Recent Transcript Window (truncate oldest messages to fit token budget)
6. Moderator Turn Instruction

### 10.6 Token Budgeting

* Reserve: 20% for generation output, 10% for critique memory, 10% for persona, 10% for system, remainder for context window.
* If over budget, drop transcript oldest first, then compress critique memory (K-sentence summary), then compress persona (short form).

### 10.7 Safety Guardrails

* Disallow impersonation of other participants/moderator.
* Disallow calls to violence or targeted harassment.
* If unsafe content requested, respond with brief refusal + safer alternative framing.

---

## 11) Functional Requirements (Detailed)

**FR-1 Sessions**: CRUD, autosave every 2s or on state change.
**FR-2 Participants**: CRUD; color pick; persona editor with char counter & linter (fallacy hints).
**FR-3 Turn Control**: Call menu; live streaming; cancel; retry; spinner with TTFB display.
**FR-4 Feedback**: Thumb up/down; downvote reason (min 8 chars prompt).
**FR-5 Adaptation**: See §10; immediate effect next time speaker is called.
**FR-6 Provider Settings**: Global & per-participant overrides; encrypted key; test connection button.
**FR-7 Exports**: MD/HTML/TXT. Include metadata (topic, rules, model, timestamps).
**FR-8 Search & List**: Search by title/topic/tags.
**FR-9 Cost/Token Info**: Show usage if provider returns; else estimate (chars/4 ≈ tokens).

---

## 12) Non-Functional Requirements (Budgets)

* **Perf**: TTFB ≤2.5s P50, ≤4.0s P95 (hosted baseline); UI frame >50 FPS during stream.
* **Reliability**: No transcript loss if app crashes after stream completion; autosave cadence ≤2s.
* **Security**: DPAPI CurrentUser; keys never exported; no telemetry by default.
* **Offline**: App fully usable without network; provider calls fail gracefully; local provider support v1.1.
* **Installability**: Signed MSIX/MSI; Winget manifest; clean uninstall leaves DB unless user opts to purge.

---

## 13) Provider Capability Matrix

| Capability     | Hugging Face Inference (v1) | Ollama (v1.1)                    |
| -------------- | --------------------------- | -------------------------------- |
| Streaming      | Yes (model-dependent)       | Yes                              |
| Usage (token)  | Varies; not guaranteed      | Yes                              |
| Latency        | Network-dependent           | Local-machine dependent          |
| Cost           | API-based                   | Free runtime; local compute cost |
| Safety Filters | Provider-specific           | Local model; app rules apply     |

---

## 14) Error Taxonomy & Recovery

| Code      | Scenario          | UX                          | Recovery                                |
| --------- | ----------------- | --------------------------- | --------------------------------------- |
| E-TIMEOUT | TTFB/stream stall | Toast + “Retry”             | Exponential backoff: 1s, 2s, 4s (max 3) |
| E-AUTH    | Bad API key       | Inline error in settings    | Re-enter key; “Test connection”         |
| E-RATE    | Rate limited      | Banner + next retry ETA     | Backoff via `Retry-After`               |
| E-NET     | Network down      | Offline badge + queue call  | Store pending; retry when online        |
| E-PROV    | Provider 5xx      | Alert + fallback suggestion | Retry or switch model                   |
| E-CANCEL  | User cancel       | Silent stop                 | Keep partial tokens if opted            |
| E-SAVE    | DB write fail     | Modal with path & reason    | Retry save; export temp file            |

All errors logged to local file.

---

## 15) Logging, Analytics & Privacy

* **Default**: Only local logs (`%LOCALAPPDATA%\DebatePrep\logs\yyyy-mm-dd.log`).
* **Opt-in Telemetry (future)**: Performance metrics only (TTFB, token/s, error codes), no content.
* **Redaction**: Remove API keys & PII; hashes for session IDs.
* **Export Logs**: Menu action to package recent logs as zip.

---

## 16) Backup & Restore

* **Backup**: Export DB + `/artifacts` folder (keys excluded).
* **Restore**: Import DB → app prompts to re-enter API keys.
* **DPAPI Note**: Keys bound to user profile; cannot be restored across machines.

---

## 17) Accessibility & Localization

* Keyboard-first navigation; tab order validated.
* Labels & ARIA on interactive elements.
* Color contrast ≥ WCAG AA in chat bubbles.
* v1 English only; strings via resource files to enable future i18n.

---

## 18) Exports (Spec & Samples)

### 18.1 Markdown

* Header block: session title, topic, rules, date, provider/model
* Each message:

```
### [HH:mm] {Participant Name}
> Thesis: ...
- Point 1
- Point 2
_Weaknesses_: …
```

### 18.2 HTML

* Standalone HTML with embedded CSS; print-friendly.
* Optional dark/light CSS toggle.

### 18.3 TXT

* Plain text; timestamped; UTF-8.

### 18.4 Sample Files

* See Appendix A for snippets.

---

## 19) Release Engineering

* **Versioning**: SemVer (`MAJOR.MINOR.PATCH`), build metadata `+win` as needed.
* **CI**: Build, unit/integration tests, code signing, artifact upload to GitHub Releases.
* **Installers**: MSIX (primary), MSI (fallback).
* **Distribution**: GitHub Releases + Winget manifest.
* **Updates**: In-app “Check for update”; no auto-update in v1.

---

## 20) Testing & QA Plan

### 20.1 Unit

* Prompt assembly: respects budgets & order; deterministic given seed.
* Critique memory: merge/decay rules, ring buffer summaries.
* DPAPI roundtrip: encrypt/decrypt.
* SQLite migrations: v1 → v2 idempotent.

### 20.2 Integration

* 2-participant flow: A→B→A with downvote altering A’s next turn.
* Provider timeout path with retry and preserved prompt.
* Export correctness (MD/HTML/TXT) diff vs golden files.

### 20.3 E2E

* “New session → participants → 5 turns → export” on clean Windows 11 VM.
* Install/uninstall without residuals (unless user selects purge).

### 20.4 Performance

* TTFB sampling across 10 calls; pass thresholds.
* UI responsiveness during 10k-char stream (no jank).

### 20.5 Accessibility

* Keyboard traversal, screen reader labels on critical UI.

---

## 21) Security

* **Secrets**: DPAPI CurrentUser; zero plaintext on disk.
* **Transport**: HTTPS; certificate pinning optional (stretch).
* **Permissions**: Least-privilege file access under `%LOCALAPPDATA%`.
* **Threat Model**: See Appendix C (STRIDE summary & mitigations).

---

## 22) Release Plan

* **v1.0**: HF provider, moderator control, streaming, critique memory, MD/HTML/TXT exports, installers.
* **v1.1**: Ollama provider, PDF export, session tags, local provider wizard.
* **v1.2**: Judge assistant (post-hoc scoring), CSV analytics export (talk time, downvote ratio).

---

## 23) Success Metrics & KPIs

* TTFB: ≤2.5s P50, ≤4.0s P95 on hosted baseline.
* Export success: ≥99%.
* Crash-free sessions (7-day): ≥99.5%.
* Moderator-initiated messages: ≥90%.
* Downvote with reason used in ≥50% sessions.
* (Opt-in) Addressed-critique rate: ≥30% within 3 turns.

---

## 24) Open Questions

* Default HF model shortlist by latency/quality?
* Ship persona & debate-format templates (Policy, LD) as starter kits?
* Include optional (opt-in) perf telemetry in v1.0 or defer to v1.1?

---

## 25) Licensing

* **MIT** for app code.
* Third-party deps under respective licenses.
* User personas/exports belong to the user; no upload by default.

---

## 26) Coding Standards & Project Structure

* **Language/Runtime**: C# / .NET 8; WinUI 3; MVVM (CommunityToolkit.Mvvm).
* **Solution Layout**:

```
/App       (WinUI views, viewmodels)
/Core      (domain: prompt assembly, critique memory, exports)
/Provider  (adapters: HuggingFace, Ollama)
/Infra     (SQLite, DPAPI, logging, settings)
/Tests     (unit, integration, golden files)
/Installer (MSIX/MSI configs)
```

* **Style**: EditorConfig enforcing nullable refs, analyzers (CA\*\*\*\*, IDE\*\*\*\*).
* **Dependency Rules**: Views→ViewModels→Core; Provider & Infra referenced only by Core.

---

## 27) Glossary

* **Moderator**: Human controlling turn order.
* **Participant**: AI persona confined to role/stance.
* **Critique Memory**: Compact rules distilled from downvotes to guide future turns.
* **TTFB**: Time to first token.
* **Adapter**: Provider-specific implementation of a common interface.

---

## Appendix A — Samples

### A.1 Sample Persona

```json
{
  "name": "Policy Hawk",
  "position": "Supports aggressive fiscal tightening within 12 months.",
  "constraints": "Formal, cites macro indicators, avoids moralizing.",
  "disallowed": "Ad hominem, red herrings.",
  "key_sources": ["BLS CPI", "FOMC minutes (general)"]
}
```

### A.2 Downvote → Critique Rule

* Downvote reason: “You dodged their strongest inflation counter-example.”
* Rule stored:

```json
{
  "rule": "Address opponent's strongest counter-example first in 1 sentence.",
  "bad_pattern": "Ignored strongest counter example.",
  "guidance": "Start with: 'Steelman: ...' then respond.",
  "strength": 0.7
}
```

### A.3 Export (Markdown excerpt)

```
# Debate-Prep Transcript — 2025-09-18
Topic: Should fiscal tightening begin within 12 months?
Model: hf/themodel vX.Y

[10:02] Policy Hawk
> Thesis: Tightening should start within 12 months.
- Inflation expectations remain above target...
- ...

_Weaknesses_: Sensitive to labor slack misreads.
```

---

## Appendix B — Provider Adapter Interface (Pseudo-C#)

```csharp
public interface IModelProvider
{
    string Name { get; }
    Task<ProviderHealth> TestAsync(CancellationToken ct);
    IAsyncEnumerable<TokenChunk> StreamAsync(
        PromptBundle prompt,
        GenerationParams gen,
        CancellationToken ct);
    Task<Usage?> TryGetLastUsageAsync(); // tokens, cost if available
}

public sealed record PromptBundle(
    string System, string Persona, string Critique, string Context, string User);

public sealed record GenerationParams(
    int MaxTokens, float Temperature, float TopP, float PresencePenalty, float FrequencyPenalty);
```

---

## Appendix C — Threat Model (STRIDE Snapshot)

* **Spoofing**: Key theft → Mitigation: DPAPI CurrentUser + no plaintext + no clipboard copy of key.
* **Tampering**: DB edits → Mitigation: checksums on export; schema constraints.
* **Repudiation**: Local logs timestamped; user export.
* **Info Disclosure**: Logs redact content; exports user-controlled.
* **DoS**: Rate limiting retries; cancel operations.
* **Elevation**: App runs user-mode; no admin required.

---

## Appendix D — Error Messages (User-Facing Copy)

* Timeout: “The model didn’t respond in time. You can retry or switch models.”
* Auth: “Your API key looks invalid. Re-enter it and try again.”
* Rate limit: “The model is busy. We’ll retry in {n}s.”
* Network: “You’re offline. We’ll reconnect automatically.”
