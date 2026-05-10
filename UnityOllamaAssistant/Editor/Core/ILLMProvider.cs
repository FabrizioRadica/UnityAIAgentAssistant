/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Interfaccia editor-only di alto livello per provider LLM.
Estende ILLMDriver con metadata identificativi per il sistema multi-provider e multi-agent.
*/

namespace RadicaDesign.UnityOllamaAssistant.Editor.Core
{
    public interface ILLMProvider : ILLMDriver
    {
        LLMProviderType ProviderType { get; }

        string ProviderId { get; }

        string DisplayName { get; }

        bool SupportsStreaming { get; }
    }
}
