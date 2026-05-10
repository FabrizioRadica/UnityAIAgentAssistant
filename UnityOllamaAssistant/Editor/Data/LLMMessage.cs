/*
Autore: Fabrizio Radica
Versione: 1.1
Data: 2026-05-10
Descrizione:
Classe dati serializzabile per rappresentare un messaggio LLM (role + content).
Compatibile con Ollama /api/chat, OpenAI /v1/chat/completions e Anthropic /v1/messages.

NOTA: la classe si chiama LLMChatMessage e NON LLMMessage per evitare conflitto
con la classe globale LLMMessage definita in Assets/UnityLLM/LLMMessage.cs
(no namespace, assembly runtime). La regola C# di name resolution non permette
alias 'using' quando esiste un tipo globale con lo stesso nome (CS0576),
quindi il rename è la via più chirurgica.

Il nome del file resta LLMMessage.cs per coerenza con il .meta esistente.
*/

using System;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Data
{
    [Serializable]
    public class LLMChatMessage
    {
        public string role;
        public string content;

        public LLMChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}
