/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
ScriptableObject che rappresenta un singolo "AI Agent" Unity selezionabile dalla chat.

Un agent è una combinazione di:
- provider (Ollama / OpenAI / Anthropic)
- modello specifico (override del modello di default del provider)
- system prompt specializzato (es. "Code Reviewer", "Refactor Specialist")
- parametri di generazione opzionali (override)

Lo stesso agent viene riselezionabile dal dropdown UI e applicato alla prossima richiesta.
La selezione persiste finché l'utente non la cambia (vedi LLMAgentRegistrySO.activeAgentId).
*/

using RadicaDesign.UnityOllamaAssistant.Editor.Core;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    [CreateAssetMenu(
        fileName = "LLMAgent",
        menuName = "RadicaDesign/AI Assistant/LLM Agent"
    )]
    public class LLMAgentSO : ScriptableObject
    {
        [Header("Identity")]
        /*
        Identificatore stabile usato per persistere la selezione dell'agent attivo
        e per associare i messaggi assistant all'agent che li ha generati.
        Deve essere unico nel registry.
        */
        public string agentId = "unity-code-generator";

        public string displayName = "Unity Code Generator";

        [TextArea(2, 4)]
        public string description = "Genera script C# per Unity 6+, codice pulito e completo.";

        [Header("Provider Binding")]
        public LLMProviderType providerType = LLMProviderType.Ollama;

        /*
        Modello specifico che questo agent deve usare.
        Override del modelName impostato nei settings del provider.
        Lasciare vuoto per usare il modelName di default del provider.
        */
        public string modelOverride = "";

        [Header("System Prompt Specialization")]
        [TextArea(4, 15)]
        public string systemPromptSpecialization =
            "Specializzato nella generazione di script C# per Unity.";

        /*
        Se true, ignora completamente il LLMGlobalPolicySO e usa
        SOLO systemPromptSpecialization come system prompt finale.
        Usare in casi speciali (es. agent in lingua diversa, agent con regole opposte).
        */
        public bool overrideGlobalPolicy = false;

        [Header("UI")]
        public Color tagColor = new Color(0.35f, 0.65f, 1f);
    }
}
