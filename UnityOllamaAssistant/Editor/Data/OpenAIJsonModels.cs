/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Classi serializzabili per richieste e risposte JSON compatibili con OpenAI Chat Completions API.
Compatibili con UnityEngine.JsonUtility (no dictionary, solo campi pubblici).

Riferimento: https://platform.openai.com/docs/api-reference/chat
*/

using System;
using System.Collections.Generic;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Data
{
    [Serializable]
    public class OpenAIChatRequest
    {
        public string model;
        public List<LLMChatMessage> messages;
        public bool stream;
        public float temperature;
        public float top_p;
        public float frequency_penalty;
        public float presence_penalty;
        public int max_tokens;
    }

    [Serializable]
    public class OpenAIChatResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public List<OpenAIChoice> choices;
    }

    [Serializable]
    public class OpenAIChoice
    {
        public int index;
        public OpenAIMessage message;
        public OpenAIDelta delta;
        public string finish_reason;
    }

    [Serializable]
    public class OpenAIMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class OpenAIDelta
    {
        public string role;
        public string content;
    }
}
