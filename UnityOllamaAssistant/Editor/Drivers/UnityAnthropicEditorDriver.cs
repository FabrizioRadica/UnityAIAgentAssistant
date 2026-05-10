/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
Driver editor-only per comunicare con Anthropic Messages API tramite UnityWebRequest.
Supporta streaming SSE e modalità buffered.

Specificità Anthropic gestite qui:
- Il messaggio con role "system" viene estratto dalla history e mappato nel campo "system" della request.
- L'header di autenticazione è "x-api-key" (NON Bearer token).
- È richiesto l'header "anthropic-version".
- La response in modalità non-streaming ha "content" come array di blocchi tipizzati;
  viene estratto il testo concatenato dei blocchi "text".
- In streaming vengono processati solo gli eventi di tipo "content_block_delta" con delta "text_delta".
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
    public class UnityAnthropicEditorDriver : ILLMProvider
    {
        private const string EventTypeContentBlockDelta = "content_block_delta";
        private const string DeltaTypeTextDelta = "text_delta";

        private readonly AnthropicEditorSettingsSO settings;
        private readonly string modelOverride;

        public LLMProviderType ProviderType => LLMProviderType.Anthropic;

        public string ProviderId => "anthropic";

        public string DisplayName => "Anthropic";

        public bool SupportsStreaming => true;

        public UnityAnthropicEditorDriver(AnthropicEditorSettingsSO settings)
            : this(settings, null)
        {
        }

        public UnityAnthropicEditorDriver(AnthropicEditorSettingsSO settings, string modelOverride)
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
                Debug.LogError("Anthropic settings non assegnato.");
                return null;
            }

            string apiKey = ApiKeyStore.Load(settings.apiKeySlotName);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError(
                    "Anthropic API key non trovata. Slot: " + settings.apiKeySlotName +
                    ". Imposta la chiave dal pannello Settings."
                );
                return null;
            }

            string url = settings.baseUrl + settings.endpoint;

            string resolvedModel = !string.IsNullOrWhiteSpace(modelOverride)
                ? modelOverride
                : settings.modelName;

            string systemPrompt;
            List<LLMChatMessage> conversationMessages = SeparateSystemPrompt(messages, out systemPrompt);

            AnthropicMessageRequest requestData = new AnthropicMessageRequest
            {
                model = resolvedModel,
                max_tokens = settings.max_tokens,
                system = systemPrompt,
                messages = conversationMessages,
                stream = settings.stream,
                temperature = settings.temperature,
                top_p = settings.top_p
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

        /*
        Anthropic NON accetta role "system" dentro l'array messages.
        Il system prompt va estratto e passato nel campo "system" della request.
        Se sono presenti più messaggi system in history, vengono concatenati con doppia newline.
        */
        private List<LLMChatMessage> SeparateSystemPrompt(
            List<LLMChatMessage> messages,
            out string systemPrompt
        )
        {
            StringBuilder systemBuilder = new StringBuilder();
            List<LLMChatMessage> conversation = new List<LLMChatMessage>(messages.Count);

            for (int i = 0; i < messages.Count; i++)
            {
                LLMChatMessage message = messages[i];

                if (message == null)
                {
                    continue;
                }

                if (message.role == "system")
                {
                    if (!string.IsNullOrWhiteSpace(message.content))
                    {
                        if (systemBuilder.Length > 0)
                        {
                            systemBuilder.Append("\n\n");
                        }

                        systemBuilder.Append(message.content);
                    }

                    continue;
                }

                conversation.Add(message);
            }

            systemPrompt = systemBuilder.ToString();
            return conversation;
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
                Debug.LogError("Errore Anthropic: " + request.error + " | " + request.downloadHandler.text);
                return null;
            }

            string jsonResponse = request.downloadHandler.text;
            AnthropicMessageResponse response = JsonUtility.FromJson<AnthropicMessageResponse>(jsonResponse);
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

            request.downloadHandler = new AnthropicStreamDownloadHandler(
                jsonPayload =>
                {
                    AnthropicStreamEvent streamEvent =
                        JsonUtility.FromJson<AnthropicStreamEvent>(jsonPayload);

                    string deltaContent = ExtractStreamingDelta(streamEvent);

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
                Debug.LogError("Errore Anthropic streaming: " + request.error);
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
            request.SetRequestHeader("x-api-key", apiKey);
            request.SetRequestHeader("anthropic-version", settings.apiVersion);
            request.timeout = settings.timeout;

            return request;
        }

        private string ExtractBufferedContent(AnthropicMessageResponse response)
        {
            if (response == null || response.content == null || response.content.Count == 0)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < response.content.Count; i++)
            {
                AnthropicContentBlock block = response.content[i];

                if (block == null)
                {
                    continue;
                }

                if (block.type == "text" && !string.IsNullOrEmpty(block.text))
                {
                    builder.Append(block.text);
                }
            }

            return builder.ToString();
        }

        private string ExtractStreamingDelta(AnthropicStreamEvent streamEvent)
        {
            if (streamEvent == null)
            {
                return null;
            }

            if (streamEvent.type != EventTypeContentBlockDelta)
            {
                return null;
            }

            if (streamEvent.delta == null)
            {
                return null;
            }

            if (streamEvent.delta.type != DeltaTypeTextDelta)
            {
                return null;
            }

            return streamEvent.delta.text;
        }
    }
}
