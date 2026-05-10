/*
Autore: Fabrizio Radica
Versione: 1.1
Data: 2026-05-10
Descrizione: Estrae esclusivamente blocchi markdown C# generati dall'assistente e li valida prima del salvataggio.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Code
{
    public static class CSharpScriptExtractor
    {
        private const string DefaultScriptName = "GeneratedScript";

        public static CSharpScriptExtractionResult Extract(string assistantResponse)
        {
            CSharpScriptExtractionResult result = new CSharpScriptExtractionResult();

            if (string.IsNullOrWhiteSpace(assistantResponse))
            {
                return result;
            }

            List<string> codeBlocks = ExtractMarkdownCSharpBlocks(assistantResponse);

            for (int i = 0; i < codeBlocks.Count; i++)
            {
                string code = NormalizeCode(codeBlocks[i]);
                string validationMessage;
                string className = ExtractPrimaryClassName(code);
                bool isValid = ValidateGeneratedScript(code, className, out validationMessage);
                string suggestedFileName = BuildSuggestedFileName(className, i);

                result.Add(
                    new CSharpGeneratedScriptCandidate(
                        code,
                        className,
                        suggestedFileName,
                        isValid,
                        validationMessage
                    )
                );
            }

            return result;
        }

        private static List<string> ExtractMarkdownCSharpBlocks(string text)
        {
            List<string> blocks = new List<string>();
            int searchIndex = 0;

            while (searchIndex < text.Length)
            {
                int fenceStart = text.IndexOf("```", searchIndex, StringComparison.Ordinal);

                if (fenceStart < 0)
                {
                    break;
                }

                int languageStart = fenceStart + 3;
                int firstLineEnd = FindLineEnd(text, languageStart);

                if (firstLineEnd < 0)
                {
                    break;
                }

                string language = text.Substring(languageStart, firstLineEnd - languageStart).Trim().ToLowerInvariant();
                int codeStart = firstLineEnd + GetLineBreakLength(text, firstLineEnd);
                int fenceEnd = text.IndexOf("```", codeStart, StringComparison.Ordinal);

                if (fenceEnd < 0)
                {
                    break;
                }

                if (IsCSharpLanguageMarker(language))
                {
                    string block = text.Substring(codeStart, fenceEnd - codeStart);
                    blocks.Add(block);
                }

                searchIndex = fenceEnd + 3;
            }

            return blocks;
        }

        private static bool IsCSharpLanguageMarker(string language)
        {
            return language == "csharp" ||
                   language == "cs" ||
                   language == "c#";
        }

        private static int FindLineEnd(string text, int startIndex)
        {
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '\n' || text[i] == '\r')
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetLineBreakLength(string text, int lineEndIndex)
        {
            if (lineEndIndex < 0 || lineEndIndex >= text.Length)
            {
                return 0;
            }

            if (text[lineEndIndex] == '\r' &&
                lineEndIndex + 1 < text.Length &&
                text[lineEndIndex + 1] == '\n')
            {
                return 2;
            }

            return 1;
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            return code.Replace("\r\n", "\n").Replace("\r", "\n").Trim() + "\n";
        }

        private static bool ValidateGeneratedScript(
            string code,
            string className,
            out string validationMessage
        )
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                validationMessage = "Blocco C# vuoto.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(className))
            {
                validationMessage = "Nessuna classe C# rilevata nel blocco generato.";
                return false;
            }

            if (!HasBalancedCurlyBraces(code))
            {
                validationMessage = "Parentesi graffe non bilanciate. Lo script non viene salvato.";
                return false;
            }

            if (!LooksLikeUnityOrCSharpScript(code))
            {
                validationMessage = "Il blocco non sembra uno script C# completo.";
                return false;
            }

            validationMessage = "Script C# generato rilevato e salvabile.";
            return true;
        }

        private static bool LooksLikeUnityOrCSharpScript(string code)
        {
            return code.Contains(" class ") ||
                   code.Contains(" class\n") ||
                   code.Contains(" class\t") ||
                   code.Contains("public class") ||
                   code.Contains("internal class") ||
                   code.Contains("sealed class") ||
                   code.Contains("partial class");
        }

        private static bool HasBalancedCurlyBraces(string code)
        {
            int depth = 0;
            bool inString = false;
            bool inChar = false;
            bool inSingleLineComment = false;
            bool inMultiLineComment = false;
            bool escaped = false;

            for (int i = 0; i < code.Length; i++)
            {
                char current = code[i];
                char next = i + 1 < code.Length ? code[i + 1] : '\0';

                if (inSingleLineComment)
                {
                    if (current == '\n')
                    {
                        inSingleLineComment = false;
                    }

                    continue;
                }

                if (inMultiLineComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inMultiLineComment = false;
                        i++;
                    }

                    continue;
                }

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (inChar)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (current == '\'')
                    {
                        inChar = false;
                    }

                    continue;
                }

                if (current == '/' && next == '/')
                {
                    inSingleLineComment = true;
                    i++;
                    continue;
                }

                if (current == '/' && next == '*')
                {
                    inMultiLineComment = true;
                    i++;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    continue;
                }

                if (current == '\'')
                {
                    inChar = true;
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;

                    if (depth < 0)
                    {
                        return false;
                    }
                }
            }

            return depth == 0 &&
                   !inString &&
                   !inChar &&
                   !inMultiLineComment;
        }

        private static string ExtractPrimaryClassName(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            string[] tokens = TokenizeForClassSearch(code);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (tokens[i] == "class")
                {
                    return SanitizeIdentifier(tokens[i + 1]);
                }
            }

            return string.Empty;
        }

        private static string[] TokenizeForClassSearch(string code)
        {
            StringBuilder builder = new StringBuilder(code.Length);

            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];

                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append(' ');
                }
            }

            return builder.ToString().Split(
                new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries
            );
        }

        private static string SanitizeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < identifier.Length; i++)
            {
                char c = identifier[i];

                if ((i == 0 && (char.IsLetter(c) || c == '_')) ||
                    (i > 0 && (char.IsLetterOrDigit(c) || c == '_')))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static string BuildSuggestedFileName(string className, int index)
        {
            if (!string.IsNullOrWhiteSpace(className))
            {
                return className + ".cs";
            }

            if (index <= 0)
            {
                return DefaultScriptName + ".cs";
            }

            return DefaultScriptName + "_" + (index + 1) + ".cs";
        }
    }
}
