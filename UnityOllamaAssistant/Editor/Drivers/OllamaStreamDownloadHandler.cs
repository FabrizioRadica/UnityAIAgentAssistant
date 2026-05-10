/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione: DownloadHandler dedicato alla lettura progressiva delle righe JSON restituite da Ollama in modalità streaming.
*/

using System;
using System.Text;
using UnityEngine.Networking;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Drivers
{
    public class OllamaStreamDownloadHandler : DownloadHandlerScript
    {
        private readonly StringBuilder pendingBuffer = new StringBuilder();
        private readonly Action<string> onJsonLineReceived;

        public OllamaStreamDownloadHandler(Action<string> onJsonLineReceived)
            : base(new byte[8192])
        {
            this.onJsonLineReceived = onJsonLineReceived;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength <= 0)
            {
                return true;
            }

            string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
            pendingBuffer.Append(chunk);
            FlushCompletedLines();

            return true;
        }

        protected override void CompleteContent()
        {
            string remaining = pendingBuffer.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(remaining))
            {
                onJsonLineReceived?.Invoke(remaining);
            }

            pendingBuffer.Clear();
        }

        private void FlushCompletedLines()
        {
            string text = pendingBuffer.ToString();
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    onJsonLineReceived?.Invoke(line);
                }
            }

            pendingBuffer.Clear();
            pendingBuffer.Append(lines[lines.Length - 1]);
        }
    }
}
