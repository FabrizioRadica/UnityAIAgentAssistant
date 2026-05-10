/*
Autore: Fabrizio Radica
Versione: 1.1
Data: 2026-05-10
Descrizione: Risultato editor-only dell'estrazione controllata di codice C# da una risposta LLM.
*/

using System.Collections.Generic;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Code
{
    public sealed class CSharpScriptExtractionResult
    {
        private readonly List<CSharpGeneratedScriptCandidate> candidates =
            new List<CSharpGeneratedScriptCandidate>();

        public IReadOnlyList<CSharpGeneratedScriptCandidate> Candidates => candidates;

        public bool HasValidScripts
        {
            get
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i] != null && candidates[i].IsValid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Add(CSharpGeneratedScriptCandidate candidate)
        {
            if (candidate == null)
            {
                return;
            }

            candidates.Add(candidate);
        }
    }
}
