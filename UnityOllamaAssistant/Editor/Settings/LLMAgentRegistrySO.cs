/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
ScriptableObject "registro" centrale del sistema multi-agent.
Contiene:
- riferimento al LLMGlobalPolicySO (system prompt base condiviso)
- riferimenti ai 3 settings provider (Ollama, OpenAI, Anthropic)
- elenco degli agents disponibili nel dropdown UI
- agent di default usato se nessuna selezione persistente è presente

Questo asset è il "single entry point" usato dalla EditorWindow per:
- popolare il dropdown agents
- risolvere il provider+modello+systemPrompt al momento dell'invio messaggio
*/

using System.Collections.Generic;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    [CreateAssetMenu(
        fileName = "LLMAgentRegistry",
        menuName = "RadicaDesign/AI Assistant/LLM Agent Registry"
    )]
    public class LLMAgentRegistrySO : ScriptableObject
    {
        [Header("Global Policy")]
        public LLMGlobalPolicySO globalPolicy;

        [Header("Provider Settings")]
        public OllamaEditorSettingsSO ollamaSettings;

        public OpenAIEditorSettingsSO openAISettings;

        public AnthropicEditorSettingsSO anthropicSettings;

        [Header("Agents")]
        public List<LLMAgentSO> agents = new List<LLMAgentSO>();

        /*
        Agent usato al primo avvio quando nessuna selezione persistente è presente.
        Deve essere uno degli agents nella lista sopra.
        */
        public LLMAgentSO defaultAgent;

        public LLMAgentSO FindAgentById(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return null;
            }

            for (int i = 0; i < agents.Count; i++)
            {
                LLMAgentSO agent = agents[i];

                if (agent == null)
                {
                    continue;
                }

                if (agent.agentId == agentId)
                {
                    return agent;
                }
            }

            return null;
        }
    }
}
