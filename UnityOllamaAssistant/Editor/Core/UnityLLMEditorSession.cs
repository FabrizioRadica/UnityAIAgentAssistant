/*
Autore: Fabrizio Radica
Versione: 1.2.0
Data: 2026-05-10
Descrizione:
Sessione editor-only multi-agent.
Gestisce:
- conversation history persistente JSON (solo user/assistant, NIENTE system)
- agent corrente (cambiabile a runtime con continuità della history)
- composizione layered del system prompt al momento dell'invio (global policy + agent)
- streaming via ILLMProvider risolto da LLMProviderFactory in base all'agent corrente

Cambia agent NON cancella la history: i messaggi precedenti vengono ripresentati al nuovo agent
con il suo system prompt dinamico, garantendo continuità della conversation.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RadicaDesign.UnityOllamaAssistant.Editor.Data;
using RadicaDesign.UnityOllamaAssistant.Editor.History;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Core
{
    public class UnityLLMEditorSession
    {
        private readonly LLMAgentRegistrySO registry;
        private readonly ChatHistoryStore historyStore;
        private readonly List<LLMChatMessage> history = new List<LLMChatMessage>();

        private LLMAgentSO currentAgent;

        public bool IsWaiting { get; private set; }

        public LLMAgentSO CurrentAgent => currentAgent;

        public UnityLLMEditorSession(LLMAgentRegistrySO registry, LLMAgentSO initialAgent)
        {
            this.registry = registry;
            this.currentAgent = initialAgent;

            /*
            Lo store history riusa il path configurato in OllamaEditorSettingsSO
            per coerenza con la baseline v1.1.5. Se non disponibile usa default sani.
            */
            OllamaEditorSettingsSO pathSettings = registry != null
                ? registry.ollamaSettings
                : null;

            historyStore = new ChatHistoryStore(pathSettings);

            LoadHistoryFromDisk();
        }

        public void SetActiveAgent(LLMAgentSO agent)
        {
            /*
            Cambio agent a caldo:
            - NON tocca la history (continuità garantita)
            - il prossimo AskAsync userà il nuovo system prompt e il nuovo provider
            */
            currentAgent = agent;
        }

        public async Task<string> AskAsync(
            string prompt,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        )
        {
            if (IsWaiting)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return null;
            }

            if (currentAgent == null)
            {
                Debug.LogError("Sessione: nessun agent selezionato.");
                return null;
            }

            if (registry == null)
            {
                Debug.LogError("Sessione: registry non assegnato.");
                return null;
            }

            ILLMProvider driver = LLMProviderFactory.Create(currentAgent, registry);

            if (driver == null)
            {
                Debug.LogError("Sessione: impossibile costruire driver per agent " + currentAgent.agentId);
                return null;
            }

            IsWaiting = true;

            history.Add(new LLMChatMessage("user", prompt));
            historyStore.Save(history);

            string finalResponse = null;

            try
            {
                List<LLMChatMessage> requestMessages = BuildRequestMessages();

                finalResponse = await driver.SendChatAsync(
                    requestMessages,
                    onPartialResponse,
                    cancellationToken
                );

                if (!string.IsNullOrWhiteSpace(finalResponse))
                {
                    history.Add(new LLMChatMessage("assistant", finalResponse));
                    historyStore.Save(history);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Errore sessione LLM editor: " + ex.Message);
            }

            IsWaiting = false;
            return finalResponse;
        }

        public void ClearHistory()
        {
            history.Clear();
            historyStore.Clear();
            historyStore.Save(history);
        }

        public IReadOnlyList<LLMChatMessage> GetHistory()
        {
            return history;
        }

        public string GetHistoryPath()
        {
            return historyStore.GetHistoryPath();
        }

        /*
        Costruisce la lista messaggi per il driver:
        - 1 messaggio system dinamico (layered: global policy + agent specialization)
        - tutti i messaggi user/assistant accumulati nella history

        Il messaggio system NON viene salvato su disco.
        Cambiare agent applica automaticamente un system diverso al prossimo invio.
        */
        private List<LLMChatMessage> BuildRequestMessages()
        {
            List<LLMChatMessage> messages = new List<LLMChatMessage>(history.Count + 1);

            LLMGlobalPolicySO globalPolicy = registry != null
                ? registry.globalPolicy
                : null;

            string systemPrompt = SystemPromptBuilder.Build(globalPolicy, currentAgent);

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new LLMChatMessage("system", systemPrompt));
            }

            for (int i = 0; i < history.Count; i++)
            {
                LLMChatMessage entry = history[i];

                if (entry == null)
                {
                    continue;
                }

                /*
                Filtra eventuali messaggi system residui da history persistenti
                generate dalla baseline v1.1.x (quando il system veniva salvato).
                Vengono ignorati: il system attuale è dinamico.
                */
                if (entry.role == "system")
                {
                    continue;
                }

                messages.Add(entry);
            }

            return messages;
        }

        private void LoadHistoryFromDisk()
        {
            history.Clear();

            List<LLMChatMessage> loaded = historyStore.Load();

            /*
            Migrazione da baseline v1.1.x:
            le vecchie history possono contenere un messaggio system come primo elemento.
            Lo escludiamo perché ora il system è dinamico.
            */
            for (int i = 0; i < loaded.Count; i++)
            {
                LLMChatMessage entry = loaded[i];

                if (entry == null)
                {
                    continue;
                }

                if (entry.role == "system")
                {
                    continue;
                }

                history.Add(entry);
            }
        }
    }
}
