/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
ScriptableObject che contiene la policy LLM globale comune a tutti gli agents.
Definisce il system prompt base (lingua, regole Unity, anti-pattern da evitare)
che viene poi specializzato da ciascun LLMAgentSO.

Strategia layered:
   final_system = globalPolicy.systemPrompt
                + agent.systemPromptSpecialization
                + globalPolicy.positivePrompt
                + globalPolicy.negativePrompt

Se l'agent ha overrideGlobalPolicy = true, il global policy viene IGNORATO
e si usa solo systemPromptSpecialization dell'agent.
*/

using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    [CreateAssetMenu(
        fileName = "LLMGlobalPolicy",
        menuName = "RadicaDesign/AI Assistant/LLM Global Policy"
    )]
    public class LLMGlobalPolicySO : ScriptableObject
    {
        [Header("Base System Prompt")]
        [TextArea(5, 20)]
        public string systemPrompt =
            "Sei un assistente Unity professionale. " +
            "Rispondi in italiano tecnico, chiaro e diretto. " +
            "Usa Unity 6.3+, C# moderno e New Input System quando rilevante. " +
            "Non inventare API. Non modificare codice senza richiesta esplicita. " +
            "Quando generi uno script C#, restituisci sempre codice completo dentro un blocco markdown ```csharp. " +
            "Non inserire nello stesso blocco testo descrittivo o contenuto non C#.";

        [Header("Positive Prompt")]
        [TextArea(3, 10)]
        public string positivePrompt =
            "Codice pulito, modulare, leggibile, mantenibile e adatto a sviluppo professionale Unity.";

        [Header("Negative Prompt")]
        [TextArea(3, 10)]
        public string negativePrompt =
            "Evita codice legacy, Input Manager legacy, API deprecate, risposte vaghe e riscritture non richieste.";
    }
}
