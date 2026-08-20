# Rogarion

A native Windows desktop chat interface to locally-running [Ollama](https://ollama.com) models, purpose-built for quick coding questions and snippet-level help — not a general chatbot, not an autonomous coding agent. Think of it as a lightweight, private "ask a senior dev" tool: paste a method and ask for a refactor, drop a file and ask how a piece of it works, or ask a quick syntax question. Everything runs against Ollama on `localhost` — no cloud calls, no telemetry.

It's a learning/portfolio project, not a finished product.

---

## Features

| Feature | Description |
|---|---|
| Streaming chat | Token-by-token streaming responses from any locally installed Ollama model, with a Stop button to cancel mid-response |
| Model picker | Populated live from Ollama's `/api/tags` — never hardcoded |
| Code-aware rendering | Fenced code blocks render with syntax highlighting (ColorCode) and a Copy button, distinct from prose; plain text while a block is still streaming, highlighted once it completes |
| Preset modes | Refactor / Explain / Find Bugs adjust the system prompt sent alongside your message; built-in modes can't be edited or deleted, but any mode can be duplicated as a starting point for your own custom mode (Settings screen) |
| Drag & drop files | Drop a code file onto the input area to fold its contents into your next message; multiple files supported, each removable before sending; oversized (>200KB) or binary files are rejected with a message |
| Edit / Retry / Delete | Per-message actions on your own questions — edit the text and get a fresh answer, retry unchanged for a different answer, or delete a question (and everything after it) from the conversation |
| Automatic history | Every session persisted locally as you go, no manual save step; sidebar lists sessions by most recent activity, auto-titled from the first exchange |
| Context window indicator | A live "context: X%" estimate of how much of the model's context window the current conversation is using; past ~80%, the oldest turns are dropped from what's sent to the model (not from the visible history) rather than erroring |
| Ollama connectivity check | Pings Ollama on launch; if it's unreachable, shows setup instructions instead of retrying silently — Rogarion never manages Ollama or its models for you |

---

## Known limitations

- **Preset mode consistency can vary by model, especially in longer conversations.** A system prompt for the currently selected mode is re-sent with every message (positioned right before your latest question, with explicit wording asking the model to follow it over its own earlier replies), but a model can still lean on the style of its own prior answers in the same conversation rather than fully switching modes — this was observed testing against `qwen2.5-coder:7b`, a 7B model; it's a model instruction-following limitation, not a wiring issue. Starting a new chat avoids it entirely.
- **The token/context-window estimate is approximate** (characters ÷ 4), not a real tokenizer. Good enough to warn before truncation, not exact.
- **No multi-file / whole-codebase reasoning.** Rogarion relies on the model's own context window and your judgment about what to paste or drop — it doesn't index a project or reason across files you haven't shown it.

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WinUI 3 with Windows App SDK |
| Language | C# on .NET 9 |
| MVVM | CommunityToolkit.Mvvm (source generators) |
| Ollama client | Raw `HttpClient` against Ollama's local REST API (`/api/chat`, `/api/tags`, `/api/show`) — no SDK wrapper |
| Syntax highlighting | ColorCode.WinUI |
| Local storage | LiteDB |
| Design | Fluent UI, Mica background, Windows 11 design language |

---

## Architecture

Three projects in one solution:

- **Rogarion.App** — the WinUI 3 desktop app. Views are XAML, logic lives in ViewModels.
- **Rogarion.Core** — class library with zero UI dependency: models, interfaces, and the message content parser (fenced-code-block detection), injected through interfaces via `Microsoft.Extensions.DependencyInjection`.
- **Rogarion.Services** — Ollama HTTP client and LiteDB-backed persistence (chat history, preset modes), implementing the interfaces defined in `Rogarion.Core`.
- **Rogarion.Installer** — a WiX Toolset project that packages a self-contained build of the app into one MSI installer. Lives outside the `.sln` (a different toolchain, not something you build/debug day to day) — see "Building the installer" below.

App data (`rogarion.db`) lives at `%LOCALAPPDATA%\Rogarion\` — the app is unpackaged, so no MSIX/`ApplicationData` persistence is used.

---

## Getting Started

### Prerequisites

- Windows 10 version 1809 or later (Windows 11 recommended)
- Visual Studio 2022 (or later) with the **Windows application development** workload
- .NET 9 SDK
- Windows App SDK 1.6
- [Ollama](https://ollama.com) installed and running, with at least one chat-capable model pulled

### Setting up Ollama

Rogarion doesn't install, manage, or download models — that's intentionally out of scope. Before running the app:

1. Install Ollama from [ollama.com](https://ollama.com).
2. Pull a model, e.g.:
   ```
   ollama pull qwen2.5-coder:7b
   ```
   Any general-purpose or coding model works; a coding-tuned model like `qwen2.5-coder` gives better results for code-focused questions. A general model is enough to try the app's core functionality.
3. Make sure Ollama is running before launching Rogarion. If it isn't, Rogarion shows a message pointing back to these steps instead of retrying silently — restart the app once Ollama is up.

### Build and run

```
git clone https://github.com/AndreasGkesos/Rogarion.git
cd Rogarion
# Open Rogarion.sln in Visual Studio
# Set Rogarion.App as the startup project
# Build and run (F5)
```

### Building the installer

For a standalone install with no .NET SDK or Visual Studio required on the target machine, run:

```
build-installer.bat
```

from the repo root. This publishes `Rogarion.App` as a self-contained win-x64 executable, then packages it into `Rogarion.Installer\bin\Release\Rogarion.Installer.msi`. The resulting MSI is fully self-contained (embeds its payload) and can be copied/run anywhere.

The installer is unsigned — Windows SmartScreen may warn on first run of the MSI; choose "More info" → "Run anyway" if you trust the source. Installing a newer version over an older one on the same machine upgrades in place (same install folder, chat history untouched); running an older MSI over a newer install is blocked with a clear message instead of silently downgrading.

Run it again after changing app code to cut a new installer build. Bump `<Version>` in `Rogarion.App.csproj` and `Product.wxs`'s `Package/Version` together before a real release.

---

## License

MIT
