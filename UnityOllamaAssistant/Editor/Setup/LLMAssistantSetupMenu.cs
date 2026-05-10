/*
Autore: Fabrizio Radica
Versione: 1.0
Data: 2026-05-10
Descrizione:
Setup automatico degli asset di configurazione del sistema multi-provider/multi-agent.

Genera una sola volta tutti i ScriptableObject necessari:
- LLMGlobalPolicy
- OllamaEditorSettings (se assente)
- OpenAIEditorSettings
- AnthropicEditorSettings
- 5 LLMAgentSO preconfigurati Unity
- LLMAgentRegistry (con tutti i riferimenti popolati)

Gli asset vengono creati in:
   Assets/RadicaDesign/UnityOllamaAssistant/Editor/GeneratedAssets/

Se un asset esiste già viene RIUSATO (idempotente). Lo script può essere lanciato più volte
senza generare duplicati.

L'invocazione avviene da menu:
   Tools > RadicaDesign > AI Assistant > Setup Default Assets
*/

using System.IO;
using RadicaDesign.UnityOllamaAssistant.Editor.Core;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Setup
{
    public static class LLMAssistantSetupMenu
    {
        private const string AssetsRoot =
            "Assets/RadicaDesign/UnityOllamaAssistant/Editor/GeneratedAssets";

        [MenuItem("Tools/RadicaDesign/AI Assistant/Setup Default Assets")]
        public static void SetupDefaultAssets()
        {
            EnsureFolder(AssetsRoot);

            LLMGlobalPolicySO globalPolicy =
                LoadOrCreate<LLMGlobalPolicySO>(
                    AssetsRoot + "/LLMGlobalPolicy.asset"
                );

            OllamaEditorSettingsSO ollamaSettings =
                LoadOrCreate<OllamaEditorSettingsSO>(
                    AssetsRoot + "/OllamaEditorSettings.asset"
                );

            OpenAIEditorSettingsSO openAISettings =
                LoadOrCreate<OpenAIEditorSettingsSO>(
                    AssetsRoot + "/OpenAIEditorSettings.asset"
                );

            AnthropicEditorSettingsSO anthropicSettings =
                LoadOrCreate<AnthropicEditorSettingsSO>(
                    AssetsRoot + "/AnthropicEditorSettings.asset"
                );

            LLMAgentSO codeGenerator =
                CreateAgentIfMissing(
                    AssetsRoot + "/Agent_UnityCodeGenerator.asset",
                    "unity-code-generator",
                    "Unity Code Generator",
                    "Genera script C# completi per Unity 6+, codice production-ready, blocco ```csharp singolo.",
                    LLMProviderType.Anthropic,
                    "claude-opus-4-7",
                    "Sei specializzato nella generazione di script C# completi per Unity 6+. " +
                    "Restituisci sempre codice production-ready con namespace, using, classi e SRP rispettata. " +
                    "Usa il New Input System. " +
                    "Restituisci il codice in un SINGOLO blocco markdown ```csharp senza testo descrittivo dentro al blocco.",
                    new Color(0.35f, 0.65f, 1f)
                );

            LLMAgentSO codeReviewer =
                CreateAgentIfMissing(
                    AssetsRoot + "/Agent_UnityCodeReviewer.asset",
                    "unity-code-reviewer",
                    "Unity Code Reviewer",
                    "Review tecnica del codice C# Unity. Identifica bug, anti-pattern, API deprecate.",
                    LLMProviderType.OpenAI,
                    "gpt-4o",
                    "Sei un Unity Code Reviewer senior. Esegui review tecnica del codice C# fornito identificando: " +
                    "bug, race conditions, GC pressure, anti-pattern Unity (es. GetComponent in Update), API deprecate, " +
                    "violazioni SRP, leak di risorse. " +
                    "Struttura la review per livelli: BLOCCANTE / MAGGIORE / MINORE / SUGGERIMENTO. " +
                    "NON riscrivere il codice: indica COSA cambiare e DOVE, con riferimenti a riga/sezione.",
                    new Color(1f, 0.55f, 0.25f)
                );

            LLMAgentSO quickQA =
                CreateAgentIfMissing(
                    AssetsRoot + "/Agent_QuickQA.asset",
                    "quick-qa",
                    "Quick Q&A",
                    "Risposte rapide a domande Unity. Locale via Ollama, gratuito e veloce.",
                    LLMProviderType.Ollama,
                    "",
                    "Rispondi in modo conciso e diretto a domande tecniche Unity. " +
                    "Massimo 3-4 frasi. " +
                    "Se la domanda richiede generazione di codice complesso, suggerisci di passare all'agent 'Unity Code Generator'.",
                    new Color(0.45f, 0.85f, 0.5f)
                );

            LLMAgentSO refactorSpecialist =
                CreateAgentIfMissing(
                    AssetsRoot + "/Agent_RefactorSpecialist.asset",
                    "refactor-specialist",
                    "Refactor Specialist",
                    "Refactoring chirurgico di codice Unity esistente. Non riscrive classi intere.",
                    LLMProviderType.Anthropic,
                    "claude-opus-4-7",
                    "Sei specializzato nel refactoring CHIRURGICO di codice C# Unity esistente. " +
                    "Regole obbligatorie: " +
                    "NON riscrivere intere classi. " +
                    "NON eliminare commenti esistenti. " +
                    "Modifica SOLO le parti necessarie. " +
                    "Mostra solo le sezioni modificate con il contesto necessario per applicarle (3-5 righe attorno). " +
                    "Spiega in 1-2 frasi il perché di ogni modifica.",
                    new Color(0.85f, 0.45f, 1f)
                );

            LLMAgentSO explainer =
                CreateAgentIfMissing(
                    AssetsRoot + "/Agent_UnityExplainer.asset",
                    "unity-explainer",
                    "Unity Explainer",
                    "Spiega codice C# Unity esistente. Locale via Ollama.",
                    LLMProviderType.Ollama,
                    "",
                    "Spiega in modo chiaro e tecnico il codice C# Unity fornito. " +
                    "Identifica: cosa fa, quali API Unity usa, ordine di esecuzione (Awake/Start/Update/FixedUpdate), " +
                    "possibili side-effect, dipendenze nascoste. " +
                    "NON generare codice nuovo. Solo spiegazione testuale strutturata.",
                    new Color(0.55f, 0.75f, 0.95f)
                );

            LLMAgentRegistrySO registry =
                LoadOrCreate<LLMAgentRegistrySO>(
                    AssetsRoot + "/LLMAgentRegistry.asset"
                );

            registry.globalPolicy = globalPolicy;
            registry.ollamaSettings = ollamaSettings;
            registry.openAISettings = openAISettings;
            registry.anthropicSettings = anthropicSettings;

            registry.agents.Clear();
            registry.agents.Add(codeGenerator);
            registry.agents.Add(codeReviewer);
            registry.agents.Add(quickQA);
            registry.agents.Add(refactorSpecialist);
            registry.agents.Add(explainer);

            registry.defaultAgent = quickQA;

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);

            Debug.Log(
                "[RadicaDesign AI Assistant] Setup completato. Registry: " +
                AssetDatabase.GetAssetPath(registry)
            );
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);

            if (existing != null)
            {
                return existing;
            }

            T instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);

            return instance;
        }

        private static LLMAgentSO CreateAgentIfMissing(
            string path,
            string agentId,
            string displayName,
            string description,
            LLMProviderType providerType,
            string modelOverride,
            string systemPromptSpecialization,
            Color tagColor)
        {
            LLMAgentSO existing = AssetDatabase.LoadAssetAtPath<LLMAgentSO>(path);

            if (existing != null)
            {
                return existing;
            }

            LLMAgentSO agent = ScriptableObject.CreateInstance<LLMAgentSO>();
            agent.agentId = agentId;
            agent.displayName = displayName;
            agent.description = description;
            agent.providerType = providerType;
            agent.modelOverride = modelOverride;
            agent.systemPromptSpecialization = systemPromptSpecialization;
            agent.tagColor = tagColor;
            agent.overrideGlobalPolicy = false;

            AssetDatabase.CreateAsset(agent, path);
            return agent;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }

            string fullDiskPath = Path.Combine(
                Application.dataPath.Replace("/Assets", string.Empty),
                folderPath
            );

            if (!Directory.Exists(fullDiskPath))
            {
                Directory.CreateDirectory(fullDiskPath);
            }
        }
    }
}
