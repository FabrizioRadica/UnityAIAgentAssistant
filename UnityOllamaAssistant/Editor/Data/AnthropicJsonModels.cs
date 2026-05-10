/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Classi serializzabili per richieste e risposte JSON compatibili con Anthropic Messages API.
Compatibili con UnityEngine.JsonUtility (no dictionary, solo campi pubblici).

Specificità Anthropic:
- Il system prompt va in un campo "system" SEPARATO dai messages.
- I messages devono contenere solo role "user" e "assistant".
- La response in modalità non-streaming ha "content" come array di blocchi tipizzati.
- Lo streaming usa Server-Sent Events con eventi nominati.

Riferimento: https://docs.anthropic.com/en/api/messages
*/

using System;
using System.Collections.Generic;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Data
{
    [Serializable]
    public class AnthropicMessageRequest
    {
        public string model;
        public int max_tokens;
        public string system;
        public List<LLMChatMessage> messages;
        public bool stream;
        public float temperature;
        public float top_p;
    }

    [Serializable]
    public class AnthropicMessageResponse
    {
        public string id;
        public string type;
        public string role;
        public List<AnthropicContentBlock> content;
        public string model;
        public string stop_reason;
        public string stop_sequence;
        public AnthropicUsage usage;
    }

    [Serializable]
    public class AnthropicContentBlock
    {
        public string type;
        public string text;
    }

    [Serializable]
    public class AnthropicUsage
    {
        public int input_tokens;
        public int output_tokens;
    }

    /*
    Evento di streaming Anthropic.
    I tipi rilevanti per il testo sono:
    - "content_block_delta" con delta.type == "text_delta" e delta.text == chunk
    - "message_stop" segna la fine
    */
    [Serializable]
    public class AnthropicStreamEvent
    {
        public string type;
        public int index;
        public AnthropicStreamDelta delta;
    }

    [Serializable]
    public class AnthropicStreamDelta
    {
        public string type;
        public string text;
        public string stop_reason;
    }
}
