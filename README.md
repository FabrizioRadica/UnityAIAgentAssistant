# Radica Unity AI Assistant


**AI Development Assistant verticale per Unity Editor.**
Multi-provider (Ollama / OpenAI / Anthropic) e multi-agent.
100% editor-only, zero dipendenze runtime.

Versione corrente: **v1.2.0** — FASE 2 completata (Provider System).

---

<img width="1691" height="1374" alt="Screenshot 2026-05-10 182049" src="https://github.com/user-attachments/assets/23af181f-ef9b-45a9-804f-fb75517b98f1" />
<img width="2313" height="1423" alt="Screenshot 2026-05-10 182206" src="https://github.com/user-attachments/assets/da2572a6-d6bd-4a1c-9128-c9baec9ee1da" />
<img width="1654" height="1366" alt="Screenshot 2026-05-10 182113" src="https://github.com/user-attachments/assets/e95013e3-87ab-4a17-a170-c69b41240d86" />
<img width="1636" height="1363" alt="Screenshot 2026-05-10 182059" src="https://github.com/user-attachments/assets/7e7549b6-5ded-43e0-b4e9-7ee285fcf57f" />


## Idea di progetto

Radica Unity AI Assistant non è un semplice chatbot integrato in Unity.

L'obiettivo è costruire un **AI IDE Assistant verticale** specializzato nello sviluppo professionale Unity/C#, in grado di:

- generare script C# completi production-ready
- fare review tecnica del codice esistente
- effettuare refactoring chirurgico
- spiegare codice e architetture Unity
- rispondere a domande veloci direttamente nell'editor

Il sistema permette di **scegliere il modello AI giusto per ogni operazione** tramite un menu a tendina nella chat: usi un LLM locale gratuito per le domande rapide, e modelli più potenti (Claude Opus, GPT-4o) per task critici come generazione codice o code review.

---

## Caratteristiche

### Multi-provider
- **Ollama** — modelli locali, gratuiti, privacy totale, nessuna API key
- **OpenAI** — Chat Completions API (GPT-4o, GPT-4 Turbo, ecc.)
- **Anthropic** — Messages API (Claude Opus, Claude Sonnet, ecc.)

### Multi-agent
Ogni "AI Agent" è un profilo preconfigurato che combina:
- provider
- modello specifico
- system prompt specializzato
- parametri di generazione

La selezione persiste tra riavvii Unity (EditorPrefs). Cambiare agent a metà conversazione **mantiene la history**: la nuova specializzazione viene applicata ai messaggi successivi senza perdere il contesto.

### System prompt layered
Ogni richiesta combina automaticamente:
- system prompt **globale** (regole comuni: lingua, Unity 6+, no API deprecate, ecc.)
- system prompt **specializzato** dell'agent attivo
- positive / negative prompt globali

### Sicurezza API key
Le API key di OpenAI e Anthropic sono salvate in **EditorPrefs** (per-utente, per-macchina) e **mai** dentro asset committati. Zero rischio di leak su Git.

### Streaming nativo
Tutti e 3 i provider supportano streaming:
- Ollama: chunked JSON
- OpenAI: Server-Sent Events (`data: ...`)
- Anthropic: SSE con eventi nominati (`content_block_delta`)

### Code extraction & save
Estrazione automatica di blocchi C# dalla risposta. Pulsanti dedicati per copiare e salvare lo script come file `.cs` nel progetto.

---

## Architettura

```
Editor Window
│
├── UnityLLMEditorSession
│   ├── Active Agent
│   └── History (user/assistant only — system dinamico)
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
├── Streaming Handlers (SSE)
│
└── Code Extraction & Save
```

**Editor-only**: nessuna classe del core eredita `MonoBehaviour`. Niente runtime overhead.

---

## Requisiti

- **Unity 6.0+** (testato su Unity 6.3)
- **C# 9+** (gestione `using`, `await`, ecc.)
- Per usare Ollama: [Ollama installato in locale](https://ollama.com) (default: `http://localhost:11434`)
- Per usare OpenAI: account OpenAI con API key
- Per usare Anthropic: account Anthropic con API key

---

## Installazione

### Opzione A — Clone diretto in `Assets/`

```bash
cd <tuo_progetto_unity>/Assets
git clone https://github.com/<owner>/<repo>.git RadicaDesign
```

### Opzione B — Copia manuale

Copia la cartella `RadicaDesign/` (o solo `RadicaDesign/UnityOllamaAssistant/`) nella cartella `Assets/` del tuo progetto Unity.

### Verifica installazione

Dopo l'import, Unity compilerà gli script. Se tutto è ok vedrai:
- nuova voce di menu **Tools > RadicaDesign > Unity AI Assistant**
- nuova voce di menu **Tools > RadicaDesign > AI Assistant > Setup Default Assets**

---

## Setup iniziale

### 1. Genera asset preconfigurati

Menu Unity:
```
Tools > RadicaDesign > AI Assistant > Setup Default Assets
```

Questo crea automaticamente in `Assets/RadicaDesign/UnityOllamaAssistant/Editor/GeneratedAssets/`:

- `LLMGlobalPolicy.asset` — system prompt base condiviso
- `OllamaEditorSettings.asset` — config Ollama
- `OpenAIEditorSettings.asset` — config OpenAI
- `AnthropicEditorSettings.asset` — config Anthropic
- 5 agent preconfigurati Unity (vedi sotto)
- `LLMAgentRegistry.asset` — registro centrale

L'operazione è **idempotente**: si può lanciare più volte, gli asset esistenti vengono riusati.

### 2. Apri la finestra

```
Tools > RadicaDesign > Unity AI Assistant
```

### 3. Inserisci le API key (solo per OpenAI / Anthropic)

1. Click su **Settings** in toolbar
2. Tab **OpenAI** o **Anthropic**
3. Campo **New Key** → incolla la tua API key
4. Click **Save Key**

La chiave viene salvata in EditorPrefs (Windows: registro di sistema; macOS: `~/Library/Preferences`). **Mai dentro asset committati.**

### 4. Verifica Ollama (opzionale)

Se vuoi usare gli agent locali, installa Ollama e scarica un modello:

```bash
ollama pull gemma2:2b
ollama pull llama3:8b
```

Poi imposta il modello nei settings dell'agent o nel campo `modelName` di `OllamaEditorSettings`.

---

## Uso

### Selezione agent

Nella seconda toolbar della finestra c'è un dropdown **Agent**. Seleziona l'agent giusto per il task:

- domanda veloce → `Quick Q&A` (Ollama, gratis)
- generazione script complesso → `Unity Code Generator` (Anthropic)
- review codice → `Unity Code Reviewer` (OpenAI)
- refactoring → `Refactor Specialist` (Anthropic)
- spiegazione codice → `Unity Explainer` (Ollama)

La selezione **persiste** finché non la cambi manualmente (anche dopo riavvio Unity).

### Conversazione

1. Scrivi il prompt in basso
2. Click **Invia** (o `Ctrl+Enter` se configurato)
3. La risposta viene streamata in tempo reale nella chat
4. Se la risposta contiene codice C# in blocco markdown, viene estratto automaticamente in **Code Preview**
5. Puoi:
   - **Copia Risposta** — copia tutto il testo nella clipboard
   - **Copia Script .cs** — copia solo il codice C# estratto
   - **Salva Script .cs** — apre dialog per salvare lo script come file

### Cambio agent in conversazione

Cambiare agent **non cancella la history**. La conversazione precedente viene ripresentata al nuovo agent con il suo system prompt specializzato. Utile per workflow tipo:

```
Quick Q&A: "qual è la differenza tra Awake e Start?"
[risposta veloce locale]

Switch a → Unity Code Generator
"ok ora generami uno script che dimostra la differenza"
[generazione script Claude]

Switch a → Unity Code Reviewer
"ora rivedi il codice che hai generato"
[review tecnica GPT-4]
```

### Nuova chat

Click **Nuova Chat** in toolbar per pulire la history (su disco e in memoria).

---

## Agent preconfigurati

| Agent | Provider | Modello | Quando usarlo |
|---|---|---|---|
| **Quick Q&A** | Ollama | (default locale) | domande tecniche brevi, gratis e veloci |
| **Unity Code Generator** | Anthropic | claude-opus-4-7 | generazione script C# completi |
| **Unity Code Reviewer** | OpenAI | gpt-4o | review strutturata (BLOCCANTE/MAGGIORE/MINORE) |
| **Refactor Specialist** | Anthropic | claude-opus-4-7 | refactoring chirurgico, no riscritture |
| **Unity Explainer** | Ollama | (default locale) | spiega codice C# Unity esistente |

I system prompt e i parametri sono completamente personalizzabili: seleziona l'asset `LLMAgent_*.asset` e modifica dall'Inspector.

### Creare nuovi agent

```
Project window > Create > RadicaDesign > AI Assistant > LLM Agent
```

Compila i campi:
- `agentId` (univoco)
- `displayName`
- `providerType` (Ollama/OpenAI/Anthropic)
- `modelOverride` (es. `claude-sonnet-4-6`, lasciare vuoto per default del provider)
- `systemPromptSpecialization` (specializzazione)
- `overrideGlobalPolicy` (se true, ignora la global policy)

Poi aggiungi l'asset alla lista `agents` nel `LLMAgentRegistry.asset`.

---

## Roadmap

- ✅ **FASE 1** (v1.0 - v1.1.5) — Editor base, Ollama, streaming, history, code extraction
- ✅ **FASE 2** (v1.2.0) — Provider System multi-provider + multi-agent
- ⏳ **FASE 3** — Tool System (create/read/modify/delete file con conferma)
- ⏳ **FASE 4** — Versioning System (snapshot/restore script generati)
- ⏳ **FASE 5** — Project Context (scansione Assets, namespace, dipendenze)
- ⏳ **FASE 6** — Roslyn integration (AST parsing, safe modification)
- ⏳ **FASE 7** — RAG locale (embeddings, vector db, retrieval)

---

## Struttura del progetto

```
Assets/RadicaDesign/UnityOllamaAssistant/Editor/
├── Code/         # estrazione e save script .cs
├── Core/         # interfacce, factory, session, prompt builder
├── Data/         # JSON models (LLMChatMessage, Ollama/OpenAI/Anthropic)
├── Drivers/      # implementazioni provider (HTTP + SSE)
├── History/      # persistenza JSON conversation
├── Settings/     # ScriptableObject di configurazione
├── Setup/        # menu setup automatico asset
└── Window/       # EditorWindow principale
```

---

## Note tecniche

- **JsonUtility**: tutti i modelli JSON sono serializzabili con `UnityEngine.JsonUtility`. Non sono usate librerie esterne (Newtonsoft, ecc.).
- **Async**: utilizzo di `async/await` con `UnityWebRequestAsyncOperation` per non bloccare l'editor.
- **Cancellation**: `CancellationToken` propagato fino al driver per stop pulito delle richieste.
- **History**: salvata in `<project>/ProjectSettings/RadicaDesign/UnityOllamaAssistant/history.json`. Solo `user`/`assistant`. Il `system` è dinamico per agent.
- **Streaming Anthropic**: parsing degli eventi SSE nominati, processiamo solo `content_block_delta` con `delta.type == "text_delta"`.

---

## Sicurezza

- API key **mai** salvate in asset committabili
- Storage via `EditorPrefs` (chiavi locali per-utente)
- Possibilità di rimuovere chiavi via UI (`Clear Key`)
- Mascheramento delle chiavi in UI (`sk-A****Xy12`)

---

## Autore

**Fabrizio Radica**
RadicaDesign

- Web: [www.radicadesign.com](https://www.radicadesign.com)
- Email: fabrizio@radicadesign.com

---

## Licenza

MIT

---

## Contributi

Pull request e issue sono benvenute. Prima di aprire una PR, leggi il `CLAUDE.md` con le regole architetturali del progetto:

- nessuna semplificazione del codice esistente
- intervento chirurgico, no riscritture inutili
- mantenere SRP e architettura modulare
- nessuna dipendenza runtime
- nessuna API Unity deprecata
