/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: ScriptableObject di configurazione per provider Anthropic (Messages API).
Le API key NON sono memorizzate qui: vengono lette da EditorPrefs tramite ApiKeyStore
usando il campo apiKeySlotName. Questo evita commit accidentali di credenziali nel repository.

Riferimento: https://docs.anthropic.com/en/api/messages
*/

using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    [CreateAssetMenu(
        fileName = "AnthropicEditorSettings",
        menuName = "RadicaDesign/AI Assistant/Anthropic Editor Settings"
    )]
    public class AnthropicEditorSettingsSO : ScriptableObject
    {
        [Header("Connection")]
        public string baseUrl = "https://api.anthropic.com";
        public string endpoint = "/v1/messages";
        public int timeout = 180;

        /*
        Versione API Anthropic richiesta nell'header anthropic-version.
        Riferimento: https://docs.anthropic.com/en/api/versioning
        */
        public string apiVersion = "2023-06-01";

        [Header("Authentication")]
        /*
        Slot logico per recuperare l'API key da EditorPrefs.
        La chiave reale NON è mai serializzata in questo asset.
        */
        public string apiKeySlotName = "anthropic_default";

        [Header("Model")]
        public string modelName = "claude-opus-4-7";

        [Header("Generation Parameters")]
        [Range(0f, 1f)]
        public float temperature = 0.4f;

        [Range(0f, 1f)]
        public float top_p = 1f;

        public int max_tokens = 2048;

        [Header("Streaming")]
        public bool stream = true;
    }
}
