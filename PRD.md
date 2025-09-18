# Product Requirements Document — Debate-Prep (Moderator-Controlled Multi-Agent Debate)

## 0) Change Log

* **v4.0 (2025-09-18)**:

  * Targeted specificity

* **v3.0 (2025-09-18)**:

  * Added deterministic algorithms (critique memory merge/decay, autosave precedence, token truncation order).
  * Defined `TokenChunk` record structure.
  * Locked export metadata ordering with golden file spec.
  * Explicitly mapped UX elements to WinUI 3 controls.
  * Clarified archived participant behavior in exports.
  * Added Appendix E — Implementation Notes with pinned libraries and test golden files.

* **v2.0 (2025-09-18)**: Added interaction flows & state machine, prompt/critique memory spec, error taxonomy & recovery, rigorous testing plan, release engineering, provider capability matrix, performance budgets, accessibility, analytics schema (opt-in), backup/restore, threat model, sample data/exports, provider adapter interface, database migrations, coding standards, and glossary.

* **v1.0 (2025-09-17)**: Initial PRD.

---

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

* WinUI 3 / .NET 8 (MVVM).
* **Toast = InfoBar**; **Banner = TeachingTip**; **Modal = ContentDialog**. Explicit mapping avoids ambiguity.
* All controls support tab order, Enter = confirm, Escape = cancel. 
* TeachingTips must be dismissible via Escape. 
* ContentDialogs always default focus on the primary button.

### 7.2 Backend & Storage

* **.NET 8** services: Provider abstraction, Streaming, Critique Memory, Export, Persistence
* **SQLite** (Microsoft.Data.Sqlite) local DB
* **DPAPI** (ProtectedData) for API keys at rest
* **autosave precedence clarified**: state-change save cancels the 2s timer if concurrent.

### 7.3 Providers

* Streaming via HTTP/SSE or chunked reads
* Adapter interface (see Appendix B)

---

## 8) Data Model (SQLite)

Same schema as v2. **Clarification**: `participants.archived` column added (boolean).

* Archived participants remain visible in session exports with suffix “(archived)” after name.
* They cannot generate new turns.

---

## 9) Interaction Flows & State Machine

**Autosave clarified**:

* Timer triggers every 2s *unless* a state change (Idle→Generating, etc.) fires.
* State change saves take precedence; timer resets.
* If provider stream ends malformed (no IsFinal chunk), app finalizes with “incomplete” flag in transcript. 
* User may retry; autosave still commits partial state.

---

## 10) Prompt Engineering & Critique Memory Spec

### 10.4 Critique Memory

**Algorithm clarified**:

* Merge implemented in C# with ML.NET using TextFeaturizingEstimator (bigrams enabled, English stop words). No Python interop is used.
* Cosine similarity threshold ≥0.8.
* On upvote, decrement all rules not used in last turn by 0.02.

### 10.6 Token Budgeting

Order fixed:

1. Transcript (oldest truncated first).
2. Critique memory (summarized via in-app regex summarizer, not LLM).
3. Persona (short form truncation rules in Appendix E).
4. All float comparisons (cosine similarity, decay strength) are rounded to 2 decimal places before threshold checks. Token counts are treated as integers, never floats.
5. Always round cosine similarity to 2 decimals before comparison. All thresholds use rounded values. Token counts must be cast to int before arithmetic.

---

## 11–23 (Functional, Non-Functional, Error Handling, Logging, Backup, Accessibility, Exports, Release, Testing, Security, Release Plan, KPIs)

No structural changes. All ambiguous references to “toast/banner/modal” now mapped to WinUI controls.

---

## 24) Open Questions

Unchanged from v3.

---

## 25) Licensing

Unchanged.

---

## 26) Coding Standards & Project Structure

Unchanged, except:

* `/Tests/Golden/` directory added to hold canonical exports and log samples.

---

## 27) Glossary

Added:

* **Golden File**: Canonical sample output checked into `/Tests/Golden/` used for regression testing (exports, logs, error dialogs).

---

## Appendix A — Samples

### A.1 Sample Persona

json
{
  "name": "Policy Hawk",
  "position": "Supports aggressive fiscal tightening within 12 months.",
  "constraints": "Formal, cites macro indicators, avoids moralizing.",
  "disallowed": "Ad hominem, red herrings.",
  "key_sources": ["BLS CPI", "FOMC minutes (general)"]
}

### A.2 Downvote → Critique Rule *Downvote reason: “You dodged their strongest inflation counter-example.”* Rule stored

json
{
  "rule": "Address opponent's strongest counter-example first in 1 sentence.",
  "bad_pattern": "Ignored strongest counter example.",
  "guidance": "Start with: 'Steelman: ...' then respond.",
  "strength": 0.7
}

### A.3 Export (Markdown excerpt)

Topic: Should fiscal tightening begin within 12 months?
Model: hf/themodel vX.Y

[10:02] Policy Hawk
> Thesis: Tightening should start within 12 months.
* Inflation expectations remain above target...
* ...

*Weaknesses*: Sensitive to labor slack misreads.

---

## Appendix B — Provider Adapter Interface (Pseudo-C#)

```csharp
public sealed record TokenChunk(
    string Text,    // raw text chunk
    int TokenCount, // number of tokens in this chunk
    bool IsFinal    // true if provider signals end-of-stream
);
```

---

## Appendix C — Threat Model

* **Spoofing**: Key theft → Mitigation: DPAPI CurrentUser + no plaintext + no clipboard copy of key.
* **Tampering**: DB edits → Mitigation: checksums on export; schema constraints.
* **Repudiation**: Local logs timestamped; user export.
* **Info Disclosure**: Logs redact content; exports user-controlled.
* **DoS**: Rate limiting retries; cancel operations.
* **Elevation**: App runs user-mode; no admin required.

---

## Appendix D — Error Messages

* Timeout: “The model didn’t respond in time. You can retry or switch models.”
* Auth: “Your API key looks invalid. Re-enter it and try again.”
* Rate limit: “The model is busy. We’ll retry in {n}s.”
* Network: “You’re offline. We’ll reconnect automatically.”

---

## Appendix E — Implementation Notes (New)

### E.1 Critique Memory

* Library: `scikit-learn` (TfidfVectorizer, cosine\_similarity).
* Threshold: merge if cosine ≥ 0.80 (rounded to 2 decimals).
* Floating-point comparisons use Math.Round(value, 2)
* Decay: −0.02 applied to all non-recent rules on every upvote.
* Decay always applied with 2-decimal rounding. Determinism enforced across platforms.
* Summarization: truncate to N sentences using regex (strip after final period). No LLM summarizer.

### E.2 Autosave

* Timer interval: 2000ms.
* If state change occurs before timer fires, perform save immediately and reset timer.
* Treat any state-change event occurring within the same 1ms tick as timer expiry as winning. Implementation may use monotonic clock tick comparisons.

### E.3 Token Budget

* Transcript dropped oldest first.
* Critique memory compressed using regex summarizer.
* Persona truncated to `{name, stance, constraints}` if budget exceeded.

### E.4 Exports

* Metadata field order: Title → Topic → Rules → Date → Provider/Model → Participants.
* Golden files stored in `/Tests/Golden/exports/`.
* Any diff against golden = test failure.
* All exports use UTF-8 encoding with LF newlines.
* Timestamps use ISO-8601 (yyyy-MM-dd HH:mm:ss, 24-hour, zero-padded). 
* Locale fixed to en-US. Any diff in encoding/newlines is considered a test failure.

### E.5 Logging

* Format: `[yyyy-MM-dd HH:mm:ss][LEVEL][Code] Message`.
* API keys redacted with `***`.
* Golden logs stored in `/Tests/Golden/logs/`.
* Logs are UTF-8 with LF newlines, fixed to en-US locale. Golden logs follow same standard.

### E.6 UI Mapping

* Toast = `InfoBar`.
* Banner = `TeachingTip`.
* Modal = `ContentDialog`.

### E.7 Provider Adapters

* `TokenChunk` schema fixed (see Appendix B).
* `TryGetLastUsageAsync` returns `{Tokens:int, Cost:decimal?}`. Null if not supported.

---
