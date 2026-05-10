/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Interfaccia editor-only per driver LLM con supporto a risposta completa e streaming incrementale.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RadicaDesign.UnityOllamaAssistant.Editor.Data;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Core
{
    public interface ILLMDriver
    {
        Task<string> SendChatAsync(
            List<LLMChatMessage> messages,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        );
    }
}
