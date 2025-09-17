# Product Requirements Document — Debate-Prep (Moderator‑Controlled Multi‑Agent Debate)

## Document Control

- **Owner**: Columbia Cloudworks LLC
- **Authors**: Product, Engineering
- **Version**: 1.0 (Initial)
- **License**: MIT
- **Project**: Debate-Prep
- **Status**: Draft → Review → Approved (target: v1.0 GA)
- **Last Updated**: 2025‑09‑17

### 1) Summary

Debate-Prep is a desktop debate playground where users create AI participants with custom personas and a human moderator controls who speaks next. Each response can be rated; negative feedback updates a participant’s critique memory to adjust future replies. Sessions are saved locally and can be exported.

### 2) Problem Statement

People preparing for debates need fast, offline‑friendly tools to simulate multi‑party arguments with tight control over turn‑taking and cost. Existing chat apps auto‑advance or lack structured feedback loops, making deliberate practice inefficient.

### 3) Goals (v1)

- Deliver a reliable Windows‑first desktop app with native installer.
- Enable moderator‑controlled turns; no automatic agent rounds.
- Support arbitrary participants with persistent personas and critique memory.
- Provide first‑class exports (Markdown/HTML/TXT). PDF in v1.1.
- Pluggable model providers: start with Hugging Face; add local (Ollama) next.

### 4) Non‑Goals (v1)

- No accounts or cloud sync.
- No voice/avatars; text only.
- No automatic multi‑round debates without moderator action.

### 5) Users & Personas

- Debaters and students: practice arguments, rebuttals, and cross‑examination.
- Policy analysts/journalists: quickly test positions and counter‑positions.
- Coaches/teachers: structure classroom or team drills.

### 6) Core Use Cases

- Create a session on a topic with 2‑5 AI participants and custom personas.
- Moderator selects who speaks next; agents never speak without being called.
- Rate each response; a thumbs‑down stores a succinct critique and guides the next reply.
- Export transcript for review or sharing.

### 7) Functional Requirements (FR)

- **FR‑1 Sessions**: Create, rename, delete sessions; auto‑save transcript.
- **FR‑2 Participants**: Add, edit, remove participants; persona text; optional color.
- **FR‑3 Turn Control**: Moderator can call on any participant to generate a response; streaming tokens display live.
- **FR‑4 Feedback**: Thumbs up/down per message; optional reason for downvotes.
- **FR‑5 Adaptation**: Next time a participant is called, inject self‑critique/adjustment guidance derived from prior downvotes.
- **FR‑6 Provider Settings**: Configure provider, model, API key (stored encrypted), decoding params.
- **FR‑7 Exports**: Export session as Markdown/HTML/TXT.
- **FR‑8 Search & List**: List sessions with search by title.
- **FR‑9 Cost/Token Info**: Show per‑message token/cost estimate when available.

### 8) Non‑Functional Requirements (NFR)

- **NFR‑1 Performance**: First token within ≤2.5s on a typical hosted model; smooth streaming without UI jank.
- **NFR‑2 Reliability**: No data loss on crash; last message persisted upon completion.
- **NFR‑3 Security**: API keys encrypted at rest (Windows DPAPI; OS keyring on others). No telemetry by default.
- **NFR‑4 Offline**: App runs without network; model calls obviously require connectivity or local provider.
- **NFR‑5 Installability**: Signed MSI builds; versioned releases with app icon and metadata.

### 9) UX Overview

- **Sidebar**: sessions list, search, new session.
- **Main**: transcript with colored bubbles; message actions: copy, thumbs up/down.
- **Top Toolbar**: model picker, temperature/token limit, "Call on …" dropdown, moderator input, export menu.
- **Feedback UX**: Downvoted messages tinted; optional text reason captured inline.

### 10) System & Data Architecture (High Level)

- **App Shell**: Tauri (Rust backend, WebView UI via React + Vite + TypeScript).
- **Backend**: Rust (Tokio, Reqwest, Serde, Rusqlite, DPAPI/keyring).
- **Providers**: Hugging Face Inference API (v1). Ollama (v1.1).
- **Streaming**: Tokens emitted as Tauri events to UI.
- **Storage**: SQLite tables for settings, sessions, participants, messages. Keys encrypted.

### 11) Data Model (SQLite)

- `usersettings(id, provider, model_id, api_key_encrypted, params_json, created_at)`
- `sessions(id, title, created_at, updated_at)`
- `participants(id, session_id, name, color, persona, memory_json, provider_overrides_json)`
- `messages(id, session_id, participant_id, role, content, tokens_out, cost_estimate, feedback, created_at)`

### 12) Privacy & Security

- No server‑side storage.
- API keys encrypted at rest; never exported.
- Configurable token caps; safe‑prompting guardrails in system prompts.

### 13) Assumptions & Dependencies

- Users can provide their own hosted model API key (Hugging Face) or use local models later.
- Windows 10/11 supported; macOS/Linux later if demand.
- Tauri packaging produces MSI with appropriate certificates when available.

### 14) Risks & Mitigations

- **Provider variability**: Response format/latency differs across models → abstract provider with strict interface and robust streaming fallback.
- **Prompt bloat**: Memory grows and degrades quality → compact critique memory, rotate older items, summarize context.
- **Key storage portability**: DPAPI is Windows‑specific → use OS keyring abstraction on other OSes.
- **Cost surprises**: Users overspend with long responses → show live token/cost estimates and enforce caps.

### 15) Success Metrics & KPIs

- Time to first token: ≤2.5s P50, ≤4.0s P95 (hosted model baseline).
- Session export success rate: ≥99%.
- Crash‑free sessions (7‑day): ≥99.5%.
- Moderator control usage: ≥90% of messages initiated via "Call on …".
- Downvote utilization: ≥50% of sessions include at least one downvote with reason.

### 16) Release Plan

- **v1.0**: HF provider, moderator control, streaming, feedback learning, MD/HTML/TXT exports, MSI installer.
- **v1.1**: Ollama provider, PDF export, session tags.
- **v1.2**: Judge assistant, CSV analytics export (talk time, downvote ratio).

### 17) Acceptance Criteria (v1.0)

- Can create sessions, add participants, and call on any participant; messages stream and persist.
- Thumbs‑down stores a compact critique; next turn includes self‑critique/adjustment block.
- API keys stored encrypted; app functions after restart with settings intact.
- Exports generate valid Markdown/HTML/TXT with participant colors and timestamps.
- MSI builds cleanly and installs on a clean Windows 11 machine.

### 18) Open Questions

- Which default HF model(s) to recommend for good latency/quality?
- Do we provide seed prompt templates for common debate formats (e.g., Policy, Lincoln‑Douglas)?
- Should we include optional telemetry (opt‑in) for performance metrics only?

---

### 19) Licensing

- License: MIT. Users may copy, modify, merge, publish, distribute, sublicense, and sell copies with attribution per the LICENSE file.
- Copyright (c) 2025 Columbia Cloudworks LLC.
- Third-party dependencies remain under their respective licenses.

## Appendix A: Previous notes (kept for reference)

Yes—let’s do this in Rust and ship real installers. The cleanest path is **Tauri**: Rust backend + a lightweight web UI in the same app window. You get a modern “ChatGPT-style” interface, native menus, file dialogs, and cross-platform packaging (MSI on Windows) with minimal overhead. Under the hood we’ll keep the multi-agent orchestration in Rust so it’s fast, safe, and easy to test.

Here’s the full plan, then the step-by-step build, alternatives, and a ready-to-paste PRD.

---

## Direct answer (stack & shape)

- **App shell & packaging:** **Tauri** (Rust backend, WebView UI). Ships as MSI/DMG/deb; small, fast, signed releases.
- **UI:** React (Vite) in the WebView. Sidebar of sessions, main chat, per-message copy, export buttons, thumbs up/down.
- **Rust core (orchestration):**

  - Multi-agent debate engine with **Moderator-driven turns** (no auto-rounds; user picks who speaks).
  - Provider trait to talk to **Hugging Face Inference API** (and optional **Ollama**/**vLLM** later).
  - Streaming tokens (SSE or chunked) bridged to the UI via Tauri events.
  - **Persona & memory** per participant, including “critique memory” that applies negative feedback to future prompts.
- **Storage:** SQLite (via `rusqlite`) for sessions, messages, participants, provider settings. User API keys encrypted with **Windows DPAPI** (`dpapi-rs`) on Windows; fallback keyring on macOS/Linux.
- **Exports:** Markdown/HTML/plaintext in v1. PDF in v1.1 (via “print to PDF” from the WebView or a Rust PDF backend).
- **Telemetry/cost control:** Per-message token/cost estimate; moderator decides who speaks next to control spend.

---

## Step-by-step (how to build it)

1. **Scaffold the app**

   - `cargo install create-tauri-app`
   - `pnpm create tauri-app` → choose React + TypeScript; or set up Vite manually.
   - Add Rust crates:
     `reqwest` (HTTP), `tokio` (async), `serde`/`serde_json`, `rusqlite`, `dpapi-rs` (Windows), `anyhow`, `thiserror`, `async-stream`, `regex`.
   - Optional providers later: `ollama-rs` (or plain HTTP to `localhost:11434`).

2. **Data model (SQLite)**

   - `usersettings(id, provider, model_id, api_key_encrypted, params_json, created_at)`
   - `sessions(id, title, created_at, updated_at)`
   - `participants(id, session_id, name, persona, provider_overrides_json, memory_json)`
   - `messages(id, session_id, participant_id NULLABLE for moderator, role, content, tokens_out, cost_estimate, created_at, feedback ENUM('up','down',NULL))`
   - Indexes on session\_id and created\_at for fast loading.

3. **Provider abstraction**

   ```rust
   #[derive(Clone, Debug)]
   pub struct GenParams {
       pub temperature: f32,
       pub top_p: f32,
       pub max_new_tokens: u32,
       pub stop: Vec<String>,
   }

   #[async_trait::async_trait]
   pub trait LlmProvider: Send + Sync {
       async fn stream_completion(
           &self,
           model_id: &str,
           api_key: &str,
           messages: &[ChatMessage], // {role: system|user|assistant, content: String}
           params: &GenParams,
       ) -> anyhow::Result<Pin<Box<dyn Stream<Item = Result<String, anyhow::Error>> + Send>>>;
   }
   ```

   - Implement `HuggingFaceProvider` using `reqwest` to the HF Inference API text-generation endpoint.
   - For **streaming**, prefer models/endpoints that support SSE; otherwise poll partials or stream line-by-line chunks.

4. **Orchestration (strict moderator control)**

   - Keep a **conversation context** in Rust: a vector of `ChatMessage` per participant plus a “shared transcript.”
   - When the moderator clicks “Call on Alice,” the backend:

     - Builds the **system prompt** (global debate rules + Alice’s persona + Alice’s “critique memory”).
     - Builds a **compact context**: recent turns plus any moderator interrupt.
     - Starts a streaming generation and **dispatches tokens** to the UI via `tauri::AppHandle::emit_all("token", …)`.
     - Finalizes the message, computes token count/cost estimate, and persists to SQLite.

5. **Persona drift & learning from feedback**

   - For each participant, store two memories:

     - **Style/strategy memory**: compact bullet list learned over the whole session (“use concrete examples,” “address counterarguments quickly,” “avoid jargon unless defined”).
     - **Critique memory**: distilled “why the last response was unpersuasive” snippets from thumbs-down.
   - On thumbs-down, immediately write a small **feedback note** into critique memory:

     - Example note: “User not convinced: lacked evidence; ignored grid reliability; too long.”
   - **At next turn**, prepend a short **Self-Critique → Adjustment** block to the participant’s system message:

     ```
     SELF-CRITIQUE (from moderator feedback): 
     - Last attempt lacked concrete evidence and ignored X.
     ADJUSTMENT (for this reply):
     - Provide one sourced stat, address X succinctly, keep ≤180 words.
     ```

   - This gives you **behavioral adaptation** without risky fine-tuning.

6. **UI (React in Tauri)**

   - Left sidebar: scrollable list of sessions; new session button; search.
   - Main pane: message list (assistant bubbles by participant color), per-message copy, thumbs up/down.
   - Top bar: topic/title, model, temperature, token limit, **“Call on…”** dropdown listing participants (and a “Moderator says…” input).
   - Export menu: Markdown/HTML/plaintext; save with file dialog (Tauri API).
   - UX detail: show token & cost estimate for each message; show a red bar on thumbs-down messages.

7. **Secure key storage**

   - On Windows, use `dpapi-rs` to encrypt API key before writing to DB.
   - On macOS/Linux, use OS keyring (via `keyring` crate) or sealed file encryption.

8. **Exports**

   - **Markdown**: convert session messages into a fenced transcript with metadata.
   - **HTML**: static HTML with minimal CSS; include participant colors; emoji for feedback.
   - **Plaintext**: simple prefixed lines.
   - **PDF (v1.1)**: either invoke “print to PDF” on the WebView or render HTML with a Rust PDF crate.

9. **Packaging**

   - Tauri’s `tauri build` → MSI on Windows. Sign if you have a cert; set app icon and versioning.

---

## Alternative perspectives / options

- **Pure Rust UI** (no web): `egui`/`eframe` gives a native feel and single-binary simplicity. Exporting HTML/PDF and rich layout is harder, and you’ll re-implement basic UI niceties. Tauri keeps you productive.
- **Local models first**: Add an **Ollama provider** so users can run Llama/Qwen/Mistral locally with no API key. Keep Hugging Face as a toggle for people who want hosted accuracy or specific models.
- **Judging agent**: Add an optional neutral “Judge” participant that summarizes persuasiveness and flags fallacies on demand—not automatic (to save cost), but handy.

---

## Practical summary / action plan

1. Initialize Tauri app (React + TS) and Rust backend.
2. Add SQLite + DPAPI and create the DB schema.
3. Implement `LlmProvider` trait + `HuggingFaceProvider`.
4. Build the **moderator-driven** speak-next flow and streaming to UI.
5. Add per-message feedback → update participant critique memory → inject into next turn’s system prompt.
6. Implement exports (MD/HTML/TXT). Ship v1.
7. Add Ollama provider and PDF export in v1.1.

---

# PRD.md (drop-in)

```markdown
# Debate-Prep — A Moderator-Controlled Multi-Agent Debate App (Rust + Tauri)

## 1. Summary
Debate-Prep is a desktop debate playground. Users define any number of AI participants with custom personas. A human moderator controls who speaks next; agents never auto-advance. Each response can be rated; thumbs-down teaches that participant to adjust future replies to be more persuasive. Sessions are saved, searchable, and exportable.

## 2. Goals
- Local, fast, safe desktop app (Windows-first).
- Moderator fully controls turns to manage token/cost spend.
- Arbitrary participants with persistent personas & adaptive “critique memory.”
- First-class exports (Markdown/HTML/TXT; PDF in v1.1).
- Pluggable providers (start with Hugging Face; add Ollama later).

## 3. Non-Goals (v1)
- No online accounts, no cloud sync.
- No voice/avatars; text only.
- No background auto-rounds (always moderator-driven).

## 4. Key Concepts
- **Moderator:** the user, controls floor.
- **Participant:** an AI persona with memory and provider config.
- **Message:** one turn’s text from a participant or the moderator.
- **Feedback:** thumbs up/down per message; downvotes feed critique memory.

## 5. UX
- **Sidebar:** sessions list + search + “New Session.”
- **Main:** chat transcript with colored bubbles; top toolbar:
  - Model picker, temperature, token limit.
  - “Call on …” (dropdown of participants).
  - Moderator input (“Ask or steer”), Send.
  - Export menu (MD/HTML/TXT).
- **Message actions:** Copy, Thumbs up/down. Downvoted messages tint red and annotate why.
- **Settings:** Provider (Hugging Face), encrypted API key, default model, decoding params.

## 6. System Behavior
- **Turn model:** No auto-debate; moderator calls on a participant at any time.
- **Prompting:**
  - Global policy: debate etiquette, address audience, cite succinctly, no role-switching.
  - Persona: participant’s role, stance, constraints (brevity cap).
  - Critique memory: short bullets from prior thumbs-down → immediate adjustment plan.
  - Context: compact rolling transcript (summarized beyond N tokens).
- **Learning loop:** Thumbs-down → store short critique → injected next time in system prelude.

## 7. Architecture
- **UI:** Tauri WebView (React + Vite + TS), shadcn/ui.
- **Backend:** Rust (Tokio, Reqwest, Rusqlite, DPAPI on Windows).
- **Providers:** 
  - `HuggingFaceProvider` (v1).
  - `OllamaProvider` (v1.1).
- **Streaming:** Server-sent tokens emitted as Tauri events to the UI.

## 8. Data Model (SQLite)
- `usersettings(id, provider, model_id, api_key_encrypted, params_json, created_at)`
- `sessions(id, title, created_at, updated_at)`
- `participants(id, session_id, name, color, persona, memory_json, provider_overrides_json)`
- `messages(id, session_id, participant_id, role, content, tokens_out, cost_estimate, feedback, created_at)`

## 9. Security
- API keys encrypted at rest (DPAPI on Windows; keyring on others).
- No telemetry; local logs only.
- Configurable token caps per message.

## 10. Exports
- **Markdown:** full transcript + metadata.
- **HTML:** styled static page.
- **Plaintext:** prefixed lines.
- **PDF (v1.1):** print-to-PDF or Rust HTML→PDF pipeline.

## 11. Provider API
Trait:
```

stream\_completion(model\_id, api\_key, messages\[], params) -> Stream<String>

```
Messages follow chat format `{role: system|user|assistant, content}`.

## 12. Feedback → Adaptation
- Store compact reasons for downvotes (user-editable).
- Inject “Self-Critique / Adjustment” block into the next system message.
- Keep sizes small; rotate old items to avoid prompt bloat.

## 13. Performance
- Context summarization beyond window.
- Backpressure on streaming to keep UI smooth.
- Token/cost estimate shown live.

## 14. Roadmap
- v1.0: HF provider, moderator control, feedback learning, MD/HTML/TXT exports, MSI installer.
- v1.1: Ollama provider, PDF export, session tags.
- v1.2: “Judge” assistant, CSV analytics export (talk time, downvote ratio).

## 15. Done Criteria (v1.0)
- Create/run sessions; add participants; call on any participant to speak.
- Streaming works; messages persist; feedback updates next turn behavior.
- Exports function; keys stored encrypted; installer builds cleanly.
```

---

## Key Rust snippets to get you rolling

**Provider trait & HF stub**

```rust
use futures_core::Stream;
use std::{pin::Pin};
use serde::{Deserialize, Serialize};

#[derive(Clone, Serialize, Deserialize)]
pub struct ChatMessage { pub role: String, pub content: String }

#[derive(Clone)]
pub struct HuggingFaceProvider;

#[async_trait::async_trait]
impl LlmProvider for HuggingFaceProvider {
    async fn stream_completion(
        &self,
        model_id: &str,
        api_key: &str,
        messages: &[ChatMessage],
        params: &GenParams,
    ) -> anyhow::Result<Pin<Box<dyn Stream<Item = Result<String, anyhow::Error>> + Send>>> {
        // Build request body per HF model’s text-generation/chat format.
        // Start reqwest streaming and wrap into an async stream of tokens.
        // Map errors to anyhow.
        todo!("Implement HF streaming")
    }
}
```

**Thumbs-down → critique memory**

```rust
pub fn record_downvote(db: &Connection, message_id: i64, reason: &str) -> anyhow::Result<()> {
    db.execute("UPDATE messages SET feedback='down' WHERE id=?", [message_id])?;
    let (participant_id, content): (i64, String) = db.query_row(
        "SELECT participant_id, content FROM messages WHERE id=?",
        [message_id],
        |r| Ok((r.get(0)?, r.get(1)?)),
    )?;
    let mut mem: CritiqueMemory = load_participant_memory(db, participant_id)?;
    mem.items.push(CritiqueItem::from_message(&content, reason));
    mem.compact(6); // keep last 6 compact items
    save_participant_memory(db, participant_id, &mem)?;
    Ok(())
}
```

**System prompt assembly**

```rust
fn build_system_prompt(global: &str, persona: &str, critique: &CritiqueMemory) -> String {
    let critique_block = if critique.items.is_empty() {
        String::new()
    } else {
        let bullets = critique.items.iter().map(|c| format!("- {}", c.as_bullet())).collect::<Vec<_>>().join("\n");
        format!("SELF-CRITIQUE:\n{}\nADJUSTMENT:\n- Address the critique explicitly in this turn.\n- Be concise and persuasive.\n", bullets)
    };
    format!("{global}\nROLE & PERSONA:\n{persona}\n{critique_block}")
}
```

---

This gives you a robust, Windows-friendly Rust app that behaves exactly like your “debate playground”: many participants, moderator-driven turns, exports, and feedback that actually shapes future responses—without fine-tuning or cloud dependencies.

Want me to sketch the initial Tauri commands, DB schema migrations, and the React component structure next so you can paste them into Cursor and go?
