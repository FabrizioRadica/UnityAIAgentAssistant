/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
Driver editor-only per comunicare con OpenAI Chat Completions API tramite UnityWebRequest.
Supporta streaming SSE (Server-Sent Events) e modalità buffered.
La API key viene letta da EditorPrefs (mai serializzata in asset).
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RadicaDesign.UnityOllamaAssistant.Editor.Core;
using RadicaDesign.UnityOllamaAssistant.Editor.Data;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;
using UnityEngine;
using UnityEngine.Networking;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Drivers
{
    public class UnityOpenAIEditorDriver : ILLMProvider
    {
        private readonly OpenAIEditorSettingsSO settings;
        private readonly string modelOverride;

        public LLMProviderType ProviderType => LLMProviderType.OpenAI;

        public string ProviderId => "openai";

        public string DisplayName => "OpenAI";

        public bool SupportsStreaming => true;

        public UnityOpenAIEditorDriver(OpenAIEditorSettingsSO settings)
            : this(settings, null)
        {
        }

        public UnityOpenAIEditorDriver(OpenAIEditorSettingsSO settings, string modelOverride)
        {
            this.settings = settings;
            this.modelOverride = modelOverride;
        }

        public async Task<string> SendChatAsync(
            List<LLMChatMessage> messages,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        )
        {
            if (settings == null)
            {
                Debug.LogError("OpenAI settings non assegnato.");
                return null;
            }

            string apiKey = ApiKeyStore.Load(settings.apiKeySlotName);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError(
                    "OpenAI API key non trovata. Slot: " + settings.apiKeySlotName +
                    ". Imposta la chiave dal pannello Settings."
                );
                return null;
            }

            string url = settings.baseUrl + settings.endpoint;

            string resolvedModel = !string.IsNullOrWhiteSpace(modelOverride)
                ? modelOverride
                : settings.modelName;

            OpenAIChatRequest requestData = new OpenAIChatRequest
            {
                model = resolvedModel,
                messages = messages,
                stream = settings.stream,
                temperature = settings.temperature,
                top_p = settings.top_p,
                frequency_penalty = settings.frequency_penalty,
                presence_penalty = settings.presence_penalty,
                max_tokens = settings.max_tokens
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            if (settings.stream)
            {
                return await SendStreamingRequestAsync(
                    url,
                    apiKey,
                    jsonBody,
                    onPartialResponse,
                    cancellationToken
                );
            }

            return await SendBufferedRequestAsync(
                url,
                apiKey,
                jsonBody,
                onPartialResponse,
                cancellationToken
            );
        }

        private async Task<string> SendBufferedRequestAsync(
            string url,
            string apiKey,
            string jsonBody,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        )
        {
            using UnityWebRequest request = CreateRequest(url, apiKey, jsonBody);
            request.downloadHandler = new DownloadHandlerBuffer();

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    return null;
                }

                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Errore OpenAI: " + request.error + " | " + request.downloadHandler.text);
                return null;
            }

            string jsonResponse = request.downloadHandler.text;
            OpenAIChatResponse response = JsonUtility.FromJson<OpenAIChatResponse>(jsonResponse);
            string content = ExtractBufferedContent(response);

            if (!string.IsNullOrWhiteSpace(content))
            {
                onPartialResponse?.Invoke(content);
            }

            return content;
        }

        private async Task<string> SendStreamingRequestAsync(
            string url,
            string apiKey,
            string jsonBody,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        )
        {
            StringBuilder finalResponse = new StringBuilder();

            using UnityWebRequest request = CreateRequest(url, apiKey, jsonBody);

            request.downloadHandler = new OpenAIStreamDownloadHandler(
                jsonPayload =>
                {
                    OpenAIChatResponse partial = JsonUtility.FromJson<OpenAIChatResponse>(jsonPayload);
                    string deltaContent = ExtractStreamingDelta(partial);

                    if (!string.IsNullOrEmpty(deltaContent))
                    {
                        finalResponse.Append(deltaContent);
                        onPartialResponse?.Invoke(deltaContent);
                    }
                }
            );

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    return null;
                }

                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Errore OpenAI streaming: " + request.error);
                return null;
            }

            return finalResponse.ToString();
        }

        private UnityWebRequest CreateRequest(string url, string apiKey, string jsonBody)
        {
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.timeout = settings.timeout;

            return request;
        }

        private string ExtractBufferedContent(OpenAIChatResponse response)
        {
            if (response == null || response.choices == null || response.choices.Count == 0)
            {
                return null;
            }

            OpenAIChoice choice = response.choices[0];

            if (choice == null || choice.message == null)
            {
                return null;
            }

            return choice.message.content;
        }

        private string ExtractStreamingDelta(OpenAIChatResponse response)
        {
            if (response == null || response.choices == null || response.choices.Count == 0)
            {
                return null;
            }

            OpenAIChoice choice = response.choices[0];

            if (choice == null || choice.delta == null)
            {
                return null;
            }

            return choice.delta.content;
        }
    }
}
