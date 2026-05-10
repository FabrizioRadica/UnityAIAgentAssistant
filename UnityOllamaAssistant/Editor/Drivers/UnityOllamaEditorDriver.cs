/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: Driver editor-only per comunicare con Ollama locale tramite UnityWebRequest e supporto streaming.
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
/*
Alias di namespace (NON di tipo, così non scatta CS0576) per disambiguare
OllamaModels.OllamaChatRequest/Options/Response/ResponseMessage dalle classi omonime
definite nel package legacy Assets/UnityLLM/JSONClass.cs (no namespace,
assembly runtime). La regola C# di name resolution preferisce il global
namespace ai using directives, quindi accediamo qui via OllamaModels.* .
*/
using OllamaModels = RadicaDesign.UnityOllamaAssistant.Editor.Data;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Drivers
{
    public class UnityOllamaEditorDriver : ILLMProvider
    {
        private readonly OllamaEditorSettingsSO settings;
        private readonly string modelOverride;

        public LLMProviderType ProviderType => LLMProviderType.Ollama;

        public string ProviderId => "ollama";

        public string DisplayName => "Ollama (local)";

        public bool SupportsStreaming => true;

        public UnityOllamaEditorDriver(OllamaEditorSettingsSO settings)
            : this(settings, null)
        {
        }

        public UnityOllamaEditorDriver(OllamaEditorSettingsSO settings, string modelOverride)
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
                Debug.LogError("Ollama settings non assegnato.");
                return null;
            }

            string url = settings.baseUrl + settings.endpoint;

            string resolvedModel = !string.IsNullOrWhiteSpace(modelOverride)
                ? modelOverride
                : settings.modelName;

            OllamaModels.OllamaChatRequest requestData = new OllamaModels.OllamaChatRequest
            {
                model = resolvedModel,
                messages = messages,
                stream = settings.stream,
                options = new OllamaModels.OllamaOptions
                {
                    temperature = settings.temperature,
                    top_k = settings.top_k,
                    top_p = settings.top_p,
                    repeat_penalty = settings.repeat_penalty,
                    num_predict = settings.num_predict
                }
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            if (settings.stream)
            {
                return await SendStreamingRequestAsync(
                    url,
                    jsonBody,
                    onPartialResponse,
                    cancellationToken
                );
            }

            return await SendBufferedRequestAsync(
                url,
                jsonBody,
                onPartialResponse,
                cancellationToken
            );
        }

        private async Task<string> SendBufferedRequestAsync(
            string url,
            string jsonBody,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        )
        {
            using UnityWebRequest request = CreateRequest(url, jsonBody);
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
                Debug.LogError("Errore Ollama: " + request.error);
                return null;
            }

            string jsonResponse = request.downloadHandler.text;
            OllamaModels.OllamaChatResponse response = JsonUtility.FromJson<OllamaModels.OllamaChatResponse>(jsonResponse);
            string content = ExtractContent(response);

            if (!string.IsNullOrWhiteSpace(content))
            {
                onPartialResponse?.Invoke(content);
            }

            return content;
        }

        private async Task<string> SendStreamingRequestAsync(
            string url,
            string jsonBody,
            Action<string> onPartialResponse,
            CancellationToken cancellationToken
        )
        {
            StringBuilder finalResponse = new StringBuilder();

            using UnityWebRequest request = CreateRequest(url, jsonBody);

            request.downloadHandler = new OllamaStreamDownloadHandler(
                jsonLine =>
                {
                    OllamaModels.OllamaChatResponse response = JsonUtility.FromJson<OllamaModels.OllamaChatResponse>(jsonLine);
                    string content = ExtractContent(response);

                    if (!string.IsNullOrEmpty(content))
                    {
                        finalResponse.Append(content);
                        onPartialResponse?.Invoke(content);
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
                Debug.LogError("Errore Ollama streaming: " + request.error);
                return null;
            }

            return finalResponse.ToString();
        }

        private UnityWebRequest CreateRequest(string url, string jsonBody)
        {
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = settings.timeout;

            return request;
        }

        private string ExtractContent(OllamaModels.OllamaChatResponse response)
        {
            if (response == null)
            {
                return null;
            }

            if (response.message == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(response.message.content))
            {
                return response.message.content;
            }

            if (!string.IsNullOrEmpty(response.message.thinking))
            {
                Debug.Log("Ollama thinking chunk: " + response.message.thinking);
            }

            return null;
        }
    }
}
