/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
Factory editor-only che, dato un agent e il registry, costruisce
il driver concreto (Ollama/OpenAI/Anthropic) pronto per la sessione.

Centralizza la decisione di quale ILLMProvider istanziare in base al providerType
dell'agent corrente, applicando il modelOverride se presente.
*/

using RadicaDesign.UnityOllamaAssistant.Editor.Drivers;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Core
{
    public static class LLMProviderFactory
    {
        public static ILLMProvider Create(LLMAgentSO agent, LLMAgentRegistrySO registry)
        {
            if (agent == null)
            {
                Debug.LogError("LLMProviderFactory: agent null.");
                return null;
            }

            if (registry == null)
            {
                Debug.LogError("LLMProviderFactory: registry null.");
                return null;
            }

            switch (agent.providerType)
            {
                case LLMProviderType.Ollama:
                    return CreateOllama(agent, registry);

                case LLMProviderType.OpenAI:
                    return CreateOpenAI(agent, registry);

                case LLMProviderType.Anthropic:
                    return CreateAnthropic(agent, registry);

                default:
                    Debug.LogError("LLMProviderFactory: providerType non supportato: " + agent.providerType);
                    return null;
            }
        }

        private static ILLMProvider CreateOllama(LLMAgentSO agent, LLMAgentRegistrySO registry)
        {
            if (registry.ollamaSettings == null)
            {
                Debug.LogError("LLMProviderFactory: ollamaSettings non assegnato nel registry.");
                return null;
            }

            return new UnityOllamaEditorDriver(registry.ollamaSettings, agent.modelOverride);
        }

        private static ILLMProvider CreateOpenAI(LLMAgentSO agent, LLMAgentRegistrySO registry)
        {
            if (registry.openAISettings == null)
            {
                Debug.LogError("LLMProviderFactory: openAISettings non assegnato nel registry.");
                return null;
            }

            return new UnityOpenAIEditorDriver(registry.openAISettings, agent.modelOverride);
        }

        private static ILLMProvider CreateAnthropic(LLMAgentSO agent, LLMAgentRegistrySO registry)
        {
            if (registry.anthropicSettings == null)
            {
                Debug.LogError("LLMProviderFactory: anthropicSettings non assegnato nel registry.");
                return null;
            }

            return new UnityAnthropicEditorDriver(registry.anthropicSettings, agent.modelOverride);
        }
    }
}
