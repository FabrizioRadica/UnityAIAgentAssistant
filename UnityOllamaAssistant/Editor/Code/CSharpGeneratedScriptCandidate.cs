/*
Autore: Fabrizio Radica
Versione: 1.1
Data: 2026-05-10
Descrizione: Modello dati editor-only per rappresentare uno script C# generato dall'assistente e salvabile su disco.
*/

namespace RadicaDesign.UnityOllamaAssistant.Editor.Code
{
    public sealed class CSharpGeneratedScriptCandidate
    {
        public string Code { get; }
        public string ClassName { get; }
        public string SuggestedFileName { get; }
        public bool IsValid { get; }
        public string ValidationMessage { get; }

        public CSharpGeneratedScriptCandidate(
            string code,
            string className,
            string suggestedFileName,
            bool isValid,
            string validationMessage
        )
        {
            Code = code;
            ClassName = className;
            SuggestedFileName = suggestedFileName;
            IsValid = isValid;
            ValidationMessage = validationMessage;
        }
    }
}
