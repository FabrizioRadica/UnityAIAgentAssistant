/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: ScriptableObject di configurazione per provider OpenAI (Chat Completions API).
Le API key NON sono memorizzate qui: vengono lette da EditorPrefs tramite ApiKeyStore
usando il campo apiKeySlotName. Questo evita commit accidentali di credenziali nel repository.
*/

using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    [CreateAssetMenu(
        fileName = "OpenAIEditorSettings",
        menuName = "RadicaDesign/AI Assistant/OpenAI Editor Settings"
    )]
    public class OpenAIEditorSettingsSO : ScriptableObject
    {
        [Header("Connection")]
        public string baseUrl = "https://api.openai.com";
        public string endpoint = "/v1/chat/completions";
        public int timeout = 180;

        [Header("Authentication")]
        /*
        Slot logico per recuperare l'API key da EditorPrefs.
        La chiave reale NON è mai serializzata in questo asset.
        */
        public string apiKeySlotName = "openai_default";

        [Header("Model")]
        public string modelName = "gpt-4o";

        [Header("Generation Parameters")]
        [Range(0f, 2f)]
        public float temperature = 0.4f;

        [Range(0f, 1f)]
        public float top_p = 1f;

        [Range(-2f, 2f)]
        public float frequency_penalty = 0f;

        [Range(-2f, 2f)]
        public float presence_penalty = 0f;

        public int max_tokens = 2048;

        [Header("Streaming")]
        public bool stream = true;
    }
}
