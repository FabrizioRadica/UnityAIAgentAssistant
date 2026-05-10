/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Classi serializzabili per richieste e risposte JSON compatibili con Ollama /api/chat.
*/

using System;
using System.Collections.Generic;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Data
{
    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public List<LLMChatMessage> messages;
        public bool stream;
        public OllamaOptions options;
    }

    [Serializable]
    public class OllamaOptions
    {
        public float temperature;
        public int top_k;
        public float top_p;
        public float repeat_penalty;
        public int num_predict;
    }

    [Serializable]
    public class OllamaChatResponse
    {
        public string model;
        public string created_at;
        public OllamaResponseMessage message;
        public bool done;
        public string done_reason;
    }

    [Serializable]
    public class OllamaResponseMessage
    {
        public string role;
        public string content;
        public string thinking;
    }
}
