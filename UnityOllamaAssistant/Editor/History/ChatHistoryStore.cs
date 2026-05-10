/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Gestione history persistente su disco in formato JSON per l'assistente editor Unity.
*/

using System;
using System.Collections.Generic;
using System.IO;
using RadicaDesign.UnityOllamaAssistant.Editor.Data;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.History
{
    public class ChatHistoryStore
    {
        [Serializable]
        private class ChatHistoryData
        {
            public List<LLMChatMessage> messages = new List<LLMChatMessage>();
        }

        private readonly OllamaEditorSettingsSO settings;

        public ChatHistoryStore(OllamaEditorSettingsSO settings)
        {
            this.settings = settings;
        }

        public List<LLMChatMessage> Load()
        {
            string path = GetHistoryPath();

            if (!File.Exists(path))
            {
                return new List<LLMChatMessage>();
            }

            try
            {
                string json = File.ReadAllText(path);
                ChatHistoryData data = JsonUtility.FromJson<ChatHistoryData>(json);

                if (data == null || data.messages == null)
                {
                    return new List<LLMChatMessage>();
                }

                return data.messages;
            }
            catch (Exception ex)
            {
                Debug.LogError("Errore lettura history JSON: " + ex.Message);
                return new List<LLMChatMessage>();
            }
        }

        public void Save(List<LLMChatMessage> messages)
        {
            if (messages == null)
            {
                return;
            }

            try
            {
                string path = GetHistoryPath();
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                ChatHistoryData data = new ChatHistoryData
                {
                    messages = messages
                };

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Errore salvataggio history JSON: " + ex.Message);
            }
        }

        public void Clear()
        {
            string path = GetHistoryPath();

            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogError("Errore cancellazione history JSON: " + ex.Message);
            }
        }

        public string GetHistoryPath()
        {
            string folder = settings != null && !string.IsNullOrWhiteSpace(settings.historyFolderName)
                ? settings.historyFolderName
                : "RadicaDesign/UnityOllamaAssistant";

            string fileName = settings != null && !string.IsNullOrWhiteSpace(settings.historyFileName)
                ? settings.historyFileName
                : "history.json";

            return Path.Combine(
                Application.dataPath.Replace("/Assets", string.Empty),
                "ProjectSettings",
                folder,
                fileName
            );
        }
    }
}
