/*
Autore: Fabrizio Radica
Versione: 1.1
Data: 2026-05-10
Descrizione: ScriptableObject di configurazione per l'assistente editor basato su Ollama locale.
*/

using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    [CreateAssetMenu(
        fileName = "OllamaEditorSettings",
        menuName = "RadicaDesign/AI Assistant/Ollama Editor Settings"
    )]
    public class OllamaEditorSettingsSO : ScriptableObject
    {
        [Header("Connection")]
        public string baseUrl = "http://localhost:11434";
        public string endpoint = "/api/chat";
        public int timeout = 120;

        [Header("Model")]
        public string modelName = "gemma4:e2b";

        [Header("Prompts")]
        [TextArea(5, 20)]
        public string systemPrompt =
            "Sei un assistente Unity professionale. " +
            "Rispondi in italiano tecnico, chiaro e diretto. " +
            "Usa Unity 6.3+, C# moderno e New Input System quando rilevante. " +
            "Non inventare API. Non modificare codice senza richiesta esplicita. " +
            "Quando generi uno script C#, restituisci sempre codice completo dentro un blocco markdown ```csharp. " +
            "Non inserire nello stesso blocco testo descrittivo o contenuto non C#.";

        [TextArea(3, 10)]
        public string positivePrompt =
            "Codice pulito, modulare, leggibile, mantenibile e adatto a sviluppo professionale Unity.";

        [TextArea(3, 10)]
        public string negativePrompt =
            "Evita codice legacy, Input Manager legacy, API deprecate, risposte vaghe e riscritture non richieste.";

        [Header("Generation Parameters")]
        [Range(0f, 2f)]
        public float temperature = 0.4f;

        [Range(1, 100)]
        public int top_k = 40;

        [Range(0f, 1f)]
        public float top_p = 0.9f;

        [Range(0f, 5f)]
        public float repeat_penalty = 1.1f;

        public int num_predict = 2048;

        [Header("Streaming")]
        public bool stream = true;

        [Header("History")]
        public string historyFolderName = "RadicaDesign/UnityOllamaAssistant";
        public string historyFileName = "history.json";
    }
}
