# Radica Unity Ollama Assistant
## BASELINE COMPLETA SESSIONE
## Versione: 1.2.0
## Data Baseline: 2026-05-10

# AUTORE

Fabrizio Radica
RadicaDesign

www.radicadesign.com
fabrizio@radicadesign.com

# IDENTITÀ DEL PROGETTO

Radica Unity Ollama Assistant è un sistema AI professionale
integrato dentro Unity Editor, progettato per funzionare
completamente in locale tramite Ollama e Large Language Models.

Il sistema NON è un semplice chatbot.

L'obiettivo reale è costruire un:

Unity AI Development Assistant

specializzato nello sviluppo professionale Unity/C#.

# STATO ATTUALE

VERSIONE STABILE:
v1.2.0

# FASE 1 COMPLETATA

Funzionalità implementate:

- Editor Window dedicata
- sistema chat editor-only
- driver Ollama separato
- streaming risposte
- history persistente JSON
- gestione sessione
- ScriptableObject configurazione
- separazione runtime/editor
- code extraction C#
- save esclusivo script .cs
- code preview panel
- toolbar editor
- auto scroll
- stop generation
- copy response
- copy code
- settings panel
- prompt area stabile
- supporto scrolling codice lungo
- action bar separata dalla chat

# REVISIONI FASE 1

## v1.1
Prima introduzione:
- copy/paste
- save script
- scroll

Problemi:
- overload errate
- UI instabile

## v1.1.1
Ripristino:
- sessione reale
- streaming
- toolbar
- settings

Problemi:
- layout scroll errato

## v1.1.2
Refactor layout:
- action bar separata
- code preview separata
- scroll stabile

Problemi:
- overflow orizzontale prompt

## v1.1.3
Tentativo fix scrollbar prompt.

Problemi:
- overload BeginScrollView incompatibili

## v1.1.4
Nuovo fix prompt:
- word wrap
- width forcing

Problemi:
- overload non compatibili Unity IMGUI

## v1.1.5
Fix definitivo:
- BeginScrollView compatibile
- prompt stabile
- scrollbar orizzontale eliminata
- compilazione corretta

# FASE 2 COMPLETATA

Provider System multi-provider e multi-agent.

Funzionalità implementate:

- ILLMProvider interface (estende ILLMDriver)
- LLMProviderType enum (Ollama, OpenAI, Anthropic)
- driver OpenAI (Chat Completions API + SSE)
- driver Anthropic (Messages API + SSE eventi nominati)
- driver Ollama refactored per modelOverride
- ApiKeyStore via EditorPrefs (no API key in asset committati)
- LLMGlobalPolicySO (system prompt base condiviso)
- LLMAgentSO (agent specializzato per task)
- LLMAgentRegistrySO (registro centrale)
- SystemPromptBuilder (composizione layered globale + agent)
- LLMProviderFactory (risolve driver da agent)
- UnityLLMEditorSession refactored multi-agent
- continuità history quando si cambia agent
- dropdown agent persistente (EditorPrefs)
- pannello settings multi-provider con tab
- 5 agents preconfigurati Unity (setup automatico)

# REVISIONI FASE 2

## v1.2.0
Provider System completo:
- multi-provider Ollama / OpenAI / Anthropic
- sistema agents con dropdown UI
- selezione persistente cross-session
- system prompt layered (global + per-agent)
- API key sicure in EditorPrefs
- setup automatico via menu

# ARCHITETTURA ATTUALE

Editor Window
│
├── UnityLLMEditorSession
│   ├── Active Agent (LLMAgentSO)
│   └── History (user/assistant only)
│
├── LLMAgentRegistrySO
│   ├── LLMGlobalPolicySO
│   ├── LLMAgentSO[]
│   ├── OllamaEditorSettingsSO
│   ├── OpenAIEditorSettingsSO
│   └── AnthropicEditorSettingsSO
│
├── LLMProviderFactory
│   └── ILLMProvider
│       ├── UnityOllamaEditorDriver
│       ├── UnityOpenAIEditorDriver
│       └── UnityAnthropicEditorDriver
│
├── SystemPromptBuilder (layered)
│
├── ApiKeyStore (EditorPrefs)
│
├── Streaming System
│   ├── OllamaStreamDownloadHandler
│   ├── OpenAIStreamDownloadHandler (SSE)
│   └── AnthropicStreamDownloadHandler (SSE)
│
├── JSON History (user/assistant only)
│
├── Code Extraction
│
├── Script Save System
│
└── Settings System (multi-provider)

# PROBLEMI RISOLTI

- TextArea non selezionabili
- scroll instabile
- toolbar sparita
- settings rimossi
- streaming non collegato
- overload IMGUI errati
- bottoni dentro scroll chat
- save indiscriminato
- salvataggio non limitato ai .cs
- parsing codice fragile

# COMPORTAMENTO ATTUALE

Il sistema:
- salva SOLO script .cs generati
- NON salva testo generico
- mantiene history locale JSON
- supporta streaming Ollama
- funziona editor-only
- non usa MonoBehaviour nel core

# REGOLE OBBLIGATORIE

- Non semplificare mai il codice
- Non eliminare commenti esistenti
- Non riscrivere intere classi inutilmente
- Intervenire in modo chirurgico
- Mantenere SRP
- Architettura modulare
- Codice production-ready
- Nessuna dipendenza runtime
- Editor-only architecture
- Unity 6+
- Nessuna API deprecated

# REFACTOR COMPLETATO

Architettura provider-based attiva:

ILLMProvider
│
├── UnityOllamaEditorDriver
├── UnityOpenAIEditorDriver
└── UnityAnthropicEditorDriver

# OBIETTIVI IMMEDIATI

FASE 2 completata.

Prossimi obiettivi (FASE 3):

1.
Tool System (create/read/modify/delete file).

2.
Tool calling per provider che lo supportano (OpenAI, Anthropic).

3.
Sandbox di esecuzione tool con conferma utente.

# ARCHITETTURA FUTURA

Editor Window
│
├── UnityLLMEditorSession
│
├── ILLMProvider
│   ├── OllamaProvider
│   ├── OpenAIProvider
│   └── AnthropicProvider
│
├── Streaming System
│
├── Tool System
│
├── Versioning System
│
├── Context Builder
│
└── Future RAG System

# ROADMAP

## FASE 2
Provider System:
- ILLMProvider
- OpenAI
- Anthropic

## FASE 3
Tool System:
- create file
- read file
- modify file
- delete file

## FASE 4
Versioning:
- snapshot
- restore
- metadata

## FASE 5
Project Context:
- scansione Assets
- namespace
- dipendenze

## FASE 6
Roslyn Integration:
- AST parsing
- diagnostics
- safe modification

## FASE 7
RAG Locale:
- embeddings
- vector db
- retrieval

# NOTE IMPORTANTI

Il progetto NON deve diventare:
- un semplice wrapper Ollama
- un runtime chatbot
- un editor fragile
- un sistema monolitico

Il progetto deve diventare:
un AI IDE Assistant verticale per Unity.

# FILE NECESSARI PER REFACTOR

Richiesti:

- UnityLLMEditorSession.cs
- Ollama driver completi
- request/response Ollama
- OllamaEditorSettingsSO.cs
- RadicaUnityAssistantWindow.cs
- LLMMessage.cs
- history manager
- extractor/saver codice

# BASELINE UFFICIALE

Versione stabile corrente:

Radica Unity AI Assistant v1.2.0
(ex Radica Unity Ollama Assistant)

Questa baseline rappresenta:
- stato attuale reale (multi-provider attivo)
- FASE 1 completata (Ollama editor base)
- FASE 2 completata (provider system + agents)
- problemi risolti
- roadmap tecnica
- architettura attiva
- prossima FASE 3 (Tool System)
