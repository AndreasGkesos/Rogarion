Rogarion
========

A native Windows chat interface to locally-running Ollama models, purpose-
built for quick coding questions and snippet-level help. Personal learning/
portfolio project.

Everything runs against Ollama on localhost — no cloud calls, no telemetry.


Before you start
------------------

Rogarion needs Ollama installed and running with at least one chat-capable
model pulled.

1. Install Ollama from https://ollama.com if you haven't already.
2. Pull a model, e.g.:
     ollama pull qwen2.5-coder:7b
   (any general-purpose or coding model works; a coding-tuned model like
   qwen2.5-coder gives better results for code-focused questions).
3. Make sure Ollama is running before launching Rogarion.

If Rogarion can't reach Ollama at startup, it shows a message pointing back
to these steps instead of retrying silently.


Using preset modes
--------------------

The Mode dropdown next to the message box lets you steer a question toward
Refactor, Explain, or Find Bugs without typing instructions yourself. You
can add your own custom modes from the Settings screen (bottom of the
sidebar) — Refactor/Explain/Find Bugs are built in and can't be edited or
deleted, but any mode can be duplicated as a starting point for your own.


Your data
----------

Chat history and settings live in:
  %LOCALAPPDATA%\Rogarion\

Uninstalling Rogarion leaves this folder alone by default, so your history
survives a reinstall or upgrade.


License
--------

MIT License. See License.rtf, included with this installer.
