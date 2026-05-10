/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
Storage editor-only delle API key per provider cloud (OpenAI, Anthropic).
Le chiavi vengono salvate in EditorPrefs (per-utente, per-macchina) e
NON dentro ScriptableObject committati nel repository.
Questo evita commit accidentali di credenziali in Git.
*/

using UnityEditor;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Settings
{
    public static class ApiKeyStore
    {
        private const string KeyPrefix = "RadicaDesign.UnityLLMAssistant.ApiKey.";

        public static string Load(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return string.Empty;
            }

            return EditorPrefs.GetString(BuildKey(slotName), string.Empty);
        }

        public static void Save(string slotName, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return;
            }

            EditorPrefs.SetString(BuildKey(slotName), apiKey ?? string.Empty);
        }

        public static void Delete(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return;
            }

            EditorPrefs.DeleteKey(BuildKey(slotName));
        }

        public static bool HasKey(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return false;
            }

            return EditorPrefs.HasKey(BuildKey(slotName));
        }

        public static string Mask(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return string.Empty;
            }

            int length = apiKey.Length;

            if (length <= 8)
            {
                return new string('*', length);
            }

            string head = apiKey.Substring(0, 4);
            string tail = apiKey.Substring(length - 4, 4);

            return head + new string('*', length - 8) + tail;
        }

        private static string BuildKey(string slotName)
        {
            return KeyPrefix + slotName;
        }
    }
}
