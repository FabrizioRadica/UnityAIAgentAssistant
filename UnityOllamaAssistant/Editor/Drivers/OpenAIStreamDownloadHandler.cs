/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
DownloadHandler dedicato alla lettura progressiva degli eventi SSE (Server-Sent Events)
restituiti da OpenAI Chat Completions in modalità streaming.

Formato SSE OpenAI:
   data: {"id":"...","choices":[{"delta":{"content":"..."}}]}\n\n
   data: [DONE]\n\n
*/

using System;
using System.Text;
using UnityEngine.Networking;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Drivers
{
    public class OpenAIStreamDownloadHandler : DownloadHandlerScript
    {
        private const string DataPrefix = "data: ";
        private const string DoneToken = "[DONE]";

        private readonly StringBuilder pendingBuffer = new StringBuilder();
        private readonly Action<string> onJsonPayloadReceived;

        public OpenAIStreamDownloadHandler(Action<string> onJsonPayloadReceived)
            : base(new byte[8192])
        {
            this.onJsonPayloadReceived = onJsonPayloadReceived;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength <= 0)
            {
                return true;
            }

            string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
            pendingBuffer.Append(chunk);
            FlushCompletedEvents();

            return true;
        }

        protected override void CompleteContent()
        {
            string remaining = pendingBuffer.ToString();

            if (!string.IsNullOrWhiteSpace(remaining))
            {
                ProcessEventBlock(remaining);
            }

            pendingBuffer.Clear();
        }

        private void FlushCompletedEvents()
        {
            string text = pendingBuffer.ToString();

            int splitIndex;

            while ((splitIndex = text.IndexOf("\n\n", StringComparison.Ordinal)) >= 0)
            {
                string eventBlock = text.Substring(0, splitIndex);
                text = text.Substring(splitIndex + 2);

                ProcessEventBlock(eventBlock);
            }

            pendingBuffer.Clear();
            pendingBuffer.Append(text);
        }

        private void ProcessEventBlock(string eventBlock)
        {
            if (string.IsNullOrWhiteSpace(eventBlock))
            {
                return;
            }

            string[] lines = eventBlock.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (!line.StartsWith(DataPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string payload = line.Substring(DataPrefix.Length).Trim();

                if (payload == DoneToken)
                {
                    continue;
                }

                onJsonPayloadReceived?.Invoke(payload);
            }
        }
    }
}
