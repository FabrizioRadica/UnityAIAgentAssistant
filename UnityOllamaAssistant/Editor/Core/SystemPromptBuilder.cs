/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
Helper editor-only che compone il system prompt finale con strategia "layered":
   final = globalPolicy.systemPrompt
         + agent.systemPromptSpecialization
         + globalPolicy.positivePrompt
         + globalPolicy.negativePrompt

Se l'agent ha overrideGlobalPolicy = true, viene usato SOLO
agent.systemPromptSpecialization, ignorando il global policy.

Il prompt risultante non viene MAI salvato nella history JSON: è dinamico,
e cambia automaticamente quando l'utente cambia agent attivo, garantendo
continuità della conversation con la nuova specializzazione.
*/

using System.Text;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Core
{
    public static class SystemPromptBuilder
    {
        public static string Build(LLMGlobalPolicySO globalPolicy, LLMAgentSO agent)
        {
            if (agent != null && agent.overrideGlobalPolicy)
            {
                return agent.systemPromptSpecialization ?? string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            if (globalPolicy != null && !string.IsNullOrWhiteSpace(globalPolicy.systemPrompt))
            {
                builder.Append(globalPolicy.systemPrompt);
            }

            if (agent != null && !string.IsNullOrWhiteSpace(agent.systemPromptSpecialization))
            {
                AppendBlock(builder, "Specializzazione Agent:", agent.systemPromptSpecialization);
            }

            if (globalPolicy != null && !string.IsNullOrWhiteSpace(globalPolicy.positivePrompt))
            {
                AppendBlock(builder, "Positive Prompt:", globalPolicy.positivePrompt);
            }

            if (globalPolicy != null && !string.IsNullOrWhiteSpace(globalPolicy.negativePrompt))
            {
                AppendBlock(builder, "Negative Prompt:", globalPolicy.negativePrompt);
            }

            return builder.ToString();
        }

        private static void AppendBlock(StringBuilder builder, string header, string body)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(header);
            builder.Append("\n");
            builder.Append(body);
        }
    }
}
