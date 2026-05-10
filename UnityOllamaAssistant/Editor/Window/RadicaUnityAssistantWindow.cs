/*
Autore: Fabrizio Radica
Versione: 1.2.0
Data: 2026-05-10
Descrizione:
Finestra editor principale del sistema
Unity AI Assistant (multi-provider, multi-agent).

Revisione 1.2.0 (FASE 2 - Provider System):
- supporto multi-provider (Ollama, OpenAI, Anthropic)
- sistema agents con dropdown selettore in toolbar
- selezione agent persistente in EditorPrefs (sopravvive a riavvio Unity)
- continuità della history quando si cambia agent
- pannello settings con tab Ollama/OpenAI/Anthropic/Global
- pulsanti "Set API Key" per provider cloud (chiavi salvate in EditorPrefs)
- titolo aggiornato: "Unity AI Assistant"

Revisione 1.1.5 (preservata):
- fix compilazione overload BeginScrollView in Unity Editor
- rimozione scrollbar orizzontale dal Prompt senza overload non compatibili
- toolbar stabile
- settings ripristinati
- action bar separata dalla chat
- code preview separata
- supporto streaming
- supporto history JSON
- supporto salvataggio esclusivo .cs
*/

using System;
using System.Collections.Generic;
using System.Threading;
using RadicaDesign.UnityOllamaAssistant.Editor.Code;
using RadicaDesign.UnityOllamaAssistant.Editor.Core;
using RadicaDesign.UnityOllamaAssistant.Editor.Data;
using RadicaDesign.UnityOllamaAssistant.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Window
{
    public sealed class RadicaUnityAssistantWindow : EditorWindow
    {
        private const string WindowTitle =
            "Unity AI Assistant";

        private const string ActiveAgentPrefKey =
            "RadicaDesign.UnityLLMAssistant.ActiveAgentId";

        private UnityLLMEditorSession session;

        private LLMAgentRegistrySO registry;

        private readonly List<EditorChatMessage> messages =
            new List<EditorChatMessage>();

        private Vector2 chatScroll;
        private Vector2 promptScroll;
        private Vector2 codePreviewScroll;
        private Vector2 settingsScroll;

        private string promptInput = "";

        private string currentStreamingResponse = "";

        private string lastAssistantMessage = "";

        private string lastExtractedCode = "";

        private string statusMessage = "Ready.";

        private bool autoScroll = true;

        private bool showSettings;

        private bool isSending;

        private CancellationTokenSource cancellationTokenSource;

        private GUIStyle userStyle;
        private GUIStyle assistantStyle;
        private GUIStyle codeStyle;
        private GUIStyle headerStyle;
        private GUIStyle promptTextAreaStyle;
        private GUIStyle agentTagStyle;

        /*
        Tab attivo nel pannello settings.
        0 = Ollama, 1 = OpenAI, 2 = Anthropic, 3 = Global Policy
        */
        private int activeSettingsTab;

        /*
        Cache per i campi API key durante l'editing in Settings.
        Le chiavi reali stanno in EditorPrefs e vengono caricate on-demand.
        */
        private string openAiKeyDraft = "";
        private string anthropicKeyDraft = "";

        [MenuItem("Tools/RadicaDesign/Unity AI Assistant")]
        public static void Open()
        {
            RadicaUnityAssistantWindow window =
                GetWindow<RadicaUnityAssistantWindow>();

            window.titleContent =
                new GUIContent(WindowTitle);

            window.minSize =
                new Vector2(1000, 720);

            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();

            LoadRegistry();

            BuildSession();

            LoadHistory();
        }

        private void OnDisable()
        {
            CancelCurrentRequest();
        }

        private void InitializeStyles()
        {
            userStyle =
                new GUIStyle(EditorStyles.textArea);

            userStyle.wordWrap = true;
            userStyle.fontSize = 13;
            userStyle.padding =
                new RectOffset(8, 8, 8, 8);

            assistantStyle =
                new GUIStyle(EditorStyles.textArea);

            assistantStyle.wordWrap = false;
            assistantStyle.fontSize = 13;
            assistantStyle.padding =
                new RectOffset(8, 8, 8, 8);

            codeStyle =
                new GUIStyle(EditorStyles.textArea);

            codeStyle.wordWrap = false;
            codeStyle.fontSize = 13;
            codeStyle.padding =
                new RectOffset(10, 10, 10, 10);

            headerStyle =
                new GUIStyle(EditorStyles.boldLabel);

            headerStyle.fontSize = 12;

            /*
            FAB v1.1.5
            Style dedicato al Prompt.

            Nota:
            La rimozione della scrollbar orizzontale non viene fatta
            usando overload non compatibili di EditorGUILayout.BeginScrollView.
            Viene fatta contenendo la larghezza della TextArea.
            */

            promptTextAreaStyle =
                new GUIStyle(EditorStyles.textArea);

            promptTextAreaStyle.wordWrap = true;
            promptTextAreaStyle.stretchWidth = true;
            promptTextAreaStyle.richText = false;
            promptTextAreaStyle.fontSize = 13;

            promptTextAreaStyle.padding =
                new RectOffset(
                    8,
                    8,
                    8,
                    8
                );

            agentTagStyle =
                new GUIStyle(EditorStyles.miniBoldLabel);

            agentTagStyle.alignment = TextAnchor.MiddleLeft;
        }

        private void LoadRegistry()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:LLMAgentRegistrySO"
                );

            if (guids == null ||
                guids.Length == 0)
            {
                statusMessage =
                    "LLMAgentRegistry asset non trovato. Crealo da Create > RadicaDesign > AI Assistant.";

                return;
            }

            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]
                );

            registry =
                AssetDatabase.LoadAssetAtPath<LLMAgentRegistrySO>(
                    path
                );
        }

        private void BuildSession()
        {
            if (registry == null)
            {
                return;
            }

            LLMAgentSO initialAgent =
                ResolveInitialAgent();

            session =
                new UnityLLMEditorSession(
                    registry,
                    initialAgent
                );
        }

        private LLMAgentSO ResolveInitialAgent()
        {
            if (registry == null)
            {
                return null;
            }

            string savedAgentId =
                EditorPrefs.GetString(
                    ActiveAgentPrefKey,
                    string.Empty
                );

            if (!string.IsNullOrWhiteSpace(savedAgentId))
            {
                LLMAgentSO matched =
                    registry.FindAgentById(savedAgentId);

                if (matched != null)
                {
                    return matched;
                }
            }

            if (registry.defaultAgent != null)
            {
                return registry.defaultAgent;
            }

            for (int i = 0; i < registry.agents.Count; i++)
            {
                LLMAgentSO candidate =
                    registry.agents[i];

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void LoadHistory()
        {
            if (session == null)
            {
                return;
            }

            IReadOnlyList<LLMChatMessage> history =
                session.GetHistory();

            messages.Clear();

            for (int i = 0; i < history.Count; i++)
            {
                LLMChatMessage message =
                    history[i];

                if (message.role == "system")
                {
                    continue;
                }

                messages.Add(
                    new EditorChatMessage(
                        message.role,
                        message.content
                    )
                );
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            DrawAgentBar();

            if (showSettings)
            {
                DrawSettingsPanel();
            }

            DrawChatPanel();

            DrawActionBar();

            DrawCodePreview();

            DrawPromptArea();

            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(
                EditorStyles.toolbar
            );

            if (GUILayout.Button(
                "Nuova Chat",
                EditorStyles.toolbarButton,
                GUILayout.Width(120)))
            {
                CreateNewChat();
            }

            GUI.enabled =
                isSending;

            if (GUILayout.Button(
                "Stop",
                EditorStyles.toolbarButton,
                GUILayout.Width(80)))
            {
                CancelCurrentRequest();
            }

            GUI.enabled = true;

            showSettings =
                GUILayout.Toggle(
                    showSettings,
                    "Settings",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(100)
                );

            GUILayout.FlexibleSpace();

            autoScroll =
                GUILayout.Toggle(
                    autoScroll,
                    "Auto Scroll",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(120)
                );

            EditorGUILayout.EndHorizontal();
        }

        /*
        FAB v1.2.0
        Barra dedicata al selettore Agent.
        Il dropdown elenca tutti i LLMAgentSO presenti nel registry.
        Il cambio di selezione:
        - persiste in EditorPrefs (sopravvive a riavvio Unity)
        - notifica la session (continuità: la history NON viene cancellata)
        */
        private void DrawAgentBar()
        {
            EditorGUILayout.BeginHorizontal(
                EditorStyles.toolbar
            );

            if (registry == null)
            {
                EditorGUILayout.LabelField(
                    "Agent Registry mancante.",
                    EditorStyles.miniLabel
                );

                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.LabelField(
                "Agent:",
                agentTagStyle,
                GUILayout.Width(60)
            );

            string[] agentLabels =
                BuildAgentLabels();

            int currentIndex =
                ResolveCurrentAgentIndex();

            int newIndex =
                EditorGUILayout.Popup(
                    currentIndex,
                    agentLabels,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(360)
                );

            if (newIndex != currentIndex &&
                newIndex >= 0 &&
                newIndex < registry.agents.Count)
            {
                LLMAgentSO selected =
                    registry.agents[newIndex];

                if (selected != null)
                {
                    SwitchAgent(selected);
                }
            }

            LLMAgentSO active =
                session != null
                    ? session.CurrentAgent
                    : null;

            if (active != null)
            {
                GUILayout.Space(8);

                EditorGUILayout.LabelField(
                    "[" + active.providerType +
                    (string.IsNullOrWhiteSpace(active.modelOverride)
                        ? "]"
                        : " / " + active.modelOverride + "]"),
                    EditorStyles.miniLabel
                );
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private string[] BuildAgentLabels()
        {
            int count =
                registry.agents.Count;

            string[] labels =
                new string[count];

            for (int i = 0; i < count; i++)
            {
                LLMAgentSO agent =
                    registry.agents[i];

                if (agent == null)
                {
                    labels[i] = "<missing>";
                    continue;
                }

                labels[i] = agent.displayName + " (" + agent.providerType + ")";
            }

            return labels;
        }

        private int ResolveCurrentAgentIndex()
        {
            if (session == null ||
                session.CurrentAgent == null)
            {
                return -1;
            }

            string activeId =
                session.CurrentAgent.agentId;

            for (int i = 0; i < registry.agents.Count; i++)
            {
                LLMAgentSO agent =
                    registry.agents[i];

                if (agent == null)
                {
                    continue;
                }

                if (agent.agentId == activeId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SwitchAgent(LLMAgentSO agent)
        {
            if (session == null)
            {
                return;
            }

            session.SetActiveAgent(agent);

            EditorPrefs.SetString(
                ActiveAgentPrefKey,
                agent.agentId
            );

            statusMessage =
                "Agent attivo: " + agent.displayName;
        }

        private void DrawSettingsPanel()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Height(280)
            );

            activeSettingsTab =
                GUILayout.Toolbar(
                    activeSettingsTab,
                    new[] { "Ollama", "OpenAI", "Anthropic", "Global" }
                );

            settingsScroll =
                EditorGUILayout.BeginScrollView(
                    settingsScroll
                );

            switch (activeSettingsTab)
            {
                case 0:
                    DrawOllamaSettings();
                    break;

                case 1:
                    DrawOpenAISettings();
                    break;

                case 2:
                    DrawAnthropicSettings();
                    break;

                case 3:
                    DrawGlobalSettings();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                "Save Settings"))
            {
                SaveAllProviderSettings();
            }

            if (GUILayout.Button(
                "Reload Session"))
            {
                BuildSession();
                LoadHistory();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawOllamaSettings()
        {
            OllamaEditorSettingsSO settings =
                registry != null
                    ? registry.ollamaSettings
                    : null;

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "OllamaEditorSettings non assegnato nel Registry.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);

            settings.baseUrl =
                EditorGUILayout.TextField("Base Url", settings.baseUrl);

            settings.endpoint =
                EditorGUILayout.TextField("Endpoint", settings.endpoint);

            settings.modelName =
                EditorGUILayout.TextField("Model", settings.modelName);

            settings.timeout =
                EditorGUILayout.IntField("Timeout", settings.timeout);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            settings.temperature =
                EditorGUILayout.Slider("Temperature", settings.temperature, 0f, 2f);

            settings.top_k =
                EditorGUILayout.IntSlider("Top K", settings.top_k, 1, 100);

            settings.top_p =
                EditorGUILayout.Slider("Top P", settings.top_p, 0f, 1f);

            settings.repeat_penalty =
                EditorGUILayout.Slider("Repeat Penalty", settings.repeat_penalty, 0f, 5f);

            settings.num_predict =
                EditorGUILayout.IntField("Num Predict", settings.num_predict);

            settings.stream =
                EditorGUILayout.Toggle("Streaming", settings.stream);
        }

        private void DrawOpenAISettings()
        {
            OpenAIEditorSettingsSO settings =
                registry != null
                    ? registry.openAISettings
                    : null;

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "OpenAIEditorSettings non assegnato nel Registry.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);

            settings.baseUrl =
                EditorGUILayout.TextField("Base Url", settings.baseUrl);

            settings.endpoint =
                EditorGUILayout.TextField("Endpoint", settings.endpoint);

            settings.modelName =
                EditorGUILayout.TextField("Model", settings.modelName);

            settings.timeout =
                EditorGUILayout.IntField("Timeout", settings.timeout);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);

            settings.apiKeySlotName =
                EditorGUILayout.TextField("Key Slot Name", settings.apiKeySlotName);

            DrawApiKeyEditor(
                settings.apiKeySlotName,
                ref openAiKeyDraft,
                "OpenAI"
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            settings.temperature =
                EditorGUILayout.Slider("Temperature", settings.temperature, 0f, 2f);

            settings.top_p =
                EditorGUILayout.Slider("Top P", settings.top_p, 0f, 1f);

            settings.frequency_penalty =
                EditorGUILayout.Slider("Frequency Penalty", settings.frequency_penalty, -2f, 2f);

            settings.presence_penalty =
                EditorGUILayout.Slider("Presence Penalty", settings.presence_penalty, -2f, 2f);

            settings.max_tokens =
                EditorGUILayout.IntField("Max Tokens", settings.max_tokens);

            settings.stream =
                EditorGUILayout.Toggle("Streaming", settings.stream);
        }

        private void DrawAnthropicSettings()
        {
            AnthropicEditorSettingsSO settings =
                registry != null
                    ? registry.anthropicSettings
                    : null;

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "AnthropicEditorSettings non assegnato nel Registry.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);

            settings.baseUrl =
                EditorGUILayout.TextField("Base Url", settings.baseUrl);

            settings.endpoint =
                EditorGUILayout.TextField("Endpoint", settings.endpoint);

            settings.modelName =
                EditorGUILayout.TextField("Model", settings.modelName);

            settings.apiVersion =
                EditorGUILayout.TextField("API Version", settings.apiVersion);

            settings.timeout =
                EditorGUILayout.IntField("Timeout", settings.timeout);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);

            settings.apiKeySlotName =
                EditorGUILayout.TextField("Key Slot Name", settings.apiKeySlotName);

            DrawApiKeyEditor(
                settings.apiKeySlotName,
                ref anthropicKeyDraft,
                "Anthropic"
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            settings.temperature =
                EditorGUILayout.Slider("Temperature", settings.temperature, 0f, 1f);

            settings.top_p =
                EditorGUILayout.Slider("Top P", settings.top_p, 0f, 1f);

            settings.max_tokens =
                EditorGUILayout.IntField("Max Tokens", settings.max_tokens);

            settings.stream =
                EditorGUILayout.Toggle("Streaming", settings.stream);
        }

        private void DrawGlobalSettings()
        {
            LLMGlobalPolicySO policy =
                registry != null
                    ? registry.globalPolicy
                    : null;

            if (policy == null)
            {
                EditorGUILayout.HelpBox(
                    "LLMGlobalPolicy non assegnato nel Registry.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField("System Prompt (base)", EditorStyles.boldLabel);

            policy.systemPrompt =
                EditorGUILayout.TextArea(
                    policy.systemPrompt,
                    GUILayout.MinHeight(80)
                );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Positive Prompt", EditorStyles.boldLabel);

            policy.positivePrompt =
                EditorGUILayout.TextArea(
                    policy.positivePrompt,
                    GUILayout.MinHeight(50)
                );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Negative Prompt", EditorStyles.boldLabel);

            policy.negativePrompt =
                EditorGUILayout.TextArea(
                    policy.negativePrompt,
                    GUILayout.MinHeight(50)
                );
        }

        /*
        Editor sicuro per API key:
        - mostra la chiave masked (es. "sk-A****Xy12") se presente in EditorPrefs
        - permette di inserire una nuova chiave senza visualizzarla mentre si digita
        - il pulsante Save scrive in EditorPrefs (mai in asset)
        - il pulsante Clear cancella lo slot
        */
        private void DrawApiKeyEditor(
            string slotName,
            ref string keyDraft,
            string providerLabel)
        {
            string existingKey =
                ApiKeyStore.Load(slotName);

            EditorGUILayout.LabelField(
                "Stored Key",
                ApiKeyStore.HasKey(slotName)
                    ? ApiKeyStore.Mask(existingKey)
                    : "<empty>"
            );

            keyDraft =
                EditorGUILayout.PasswordField(
                    "New Key",
                    keyDraft
                );

            EditorGUILayout.BeginHorizontal();

            GUI.enabled =
                !string.IsNullOrWhiteSpace(keyDraft);

            if (GUILayout.Button(
                "Save " + providerLabel + " Key"))
            {
                ApiKeyStore.Save(slotName, keyDraft);
                keyDraft = "";

                statusMessage =
                    providerLabel + " API key salvata in EditorPrefs.";

                GUI.FocusControl(null);
            }

            GUI.enabled =
                ApiKeyStore.HasKey(slotName);

            if (GUILayout.Button(
                "Clear " + providerLabel + " Key"))
            {
                ApiKeyStore.Delete(slotName);

                statusMessage =
                    providerLabel + " API key rimossa.";
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void SaveAllProviderSettings()
        {
            if (registry == null)
            {
                return;
            }

            if (registry.ollamaSettings != null)
            {
                EditorUtility.SetDirty(registry.ollamaSettings);
            }

            if (registry.openAISettings != null)
            {
                EditorUtility.SetDirty(registry.openAISettings);
            }

            if (registry.anthropicSettings != null)
            {
                EditorUtility.SetDirty(registry.anthropicSettings);
            }

            if (registry.globalPolicy != null)
            {
                EditorUtility.SetDirty(registry.globalPolicy);
            }

            EditorUtility.SetDirty(registry);

            AssetDatabase.SaveAssets();

            statusMessage = "Settings salvati.";
        }

        private void DrawChatPanel()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.ExpandHeight(true)
            );

            chatScroll =
                EditorGUILayout.BeginScrollView(
                    chatScroll,
                    true,
                    true,
                    GUILayout.ExpandHeight(true)
                );

            for (int i = 0; i < messages.Count; i++)
            {
                DrawMessage(
                    messages[i]
                );
            }

            if (!string.IsNullOrEmpty(
                currentStreamingResponse))
            {
                DrawStreamingMessage();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            if (autoScroll &&
                Event.current.type ==
                EventType.Repaint)
            {
                chatScroll.y =
                    Mathf.Infinity;
            }
        }

        private void DrawMessage(
            EditorChatMessage message)
        {
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                message.Role == "assistant"
                    ? "ASSISTENTE"
                    : "UTENTE",
                headerStyle
            );

            GUIStyle style =
                message.Role == "assistant"
                    ? assistantStyle
                    : userStyle;

            float height =
                Mathf.Max(
                    80,
                    style.CalcHeight(
                        new GUIContent(
                            message.Content
                        ),
                        position.width - 80
                    ) + 20
                );

            EditorGUILayout.TextArea(
                message.Content,
                style,
                GUILayout.MinHeight(height)
            );

            GUILayout.Space(8);
        }

        private void DrawStreamingMessage()
        {
            EditorGUILayout.LabelField(
                "ASSISTENTE",
                headerStyle
            );

            float height =
                Mathf.Max(
                    80,
                    assistantStyle.CalcHeight(
                        new GUIContent(
                            currentStreamingResponse
                        ),
                        position.width - 80
                    ) + 20
                );

            EditorGUILayout.TextArea(
                currentStreamingResponse,
                assistantStyle,
                GUILayout.MinHeight(height)
            );
        }

        private void DrawActionBar()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );

            EditorGUILayout.BeginHorizontal();

            GUI.enabled =
                !string.IsNullOrWhiteSpace(
                    lastAssistantMessage
                );

            if (GUILayout.Button(
                "Copia Risposta",
                GUILayout.Height(28)))
            {
                EditorGUIUtility.systemCopyBuffer =
                    lastAssistantMessage;

                statusMessage =
                    "Risposta copiata.";
            }

            bool hasCode =
                !string.IsNullOrWhiteSpace(
                    lastExtractedCode
                );

            GUI.enabled = hasCode;

            if (GUILayout.Button(
                "Copia Script .cs",
                GUILayout.Height(28)))
            {
                EditorGUIUtility.systemCopyBuffer =
                    lastExtractedCode;

                statusMessage =
                    "Script copiato.";
            }

            if (GUILayout.Button(
                "Salva Script .cs",
                GUILayout.Height(28)))
            {
                SaveCurrentScript();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCodePreview()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Height(180)
            );

            EditorGUILayout.LabelField(
                "Code Preview",
                EditorStyles.boldLabel
            );

            codePreviewScroll =
                EditorGUILayout.BeginScrollView(
                    codePreviewScroll,
                    true,
                    true
                );

            EditorGUILayout.TextArea(
                string.IsNullOrWhiteSpace(
                    lastExtractedCode)
                    ? "Nessun codice C# rilevato."
                    : lastExtractedCode,
                codeStyle,
                GUILayout.ExpandHeight(true)
            );

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawPromptArea()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Height(170)
            );

            EditorGUILayout.LabelField(
                "Prompt",
                EditorStyles.boldLabel
            );

            /*
            FAB v1.1.5

            Fix compilazione:
            EditorGUILayout.BeginScrollView NON viene chiamato con
            bool + GUIStyle + GUIStyle perché in questa versione/contesto
            l'overload non è disponibile.

            Fix scrollbar orizzontale:
            la TextArea viene forzata alla larghezza disponibile
            e il wordWrap è attivo nello style.
            */

            promptScroll =
                EditorGUILayout.BeginScrollView(
                    promptScroll,
                    GUILayout.Height(90)
                );

            float promptWidth =
                Mathf.Max(
                    100f,
                    position.width - 42f
                );

            promptInput =
                EditorGUILayout.TextArea(
                    promptInput,
                    promptTextAreaStyle,
                    GUILayout.Width(promptWidth),
                    GUILayout.ExpandHeight(true)
                );

            EditorGUILayout.EndScrollView();

            GUILayout.Space(4);

            GUI.enabled =
                !isSending &&
                !string.IsNullOrWhiteSpace(
                    promptInput
                );

            if (GUILayout.Button(
                "Invia",
                GUILayout.Height(34)))
            {
                SendPrompt();
            }

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(
                EditorStyles.toolbar
            );

            EditorGUILayout.LabelField(
                statusMessage,
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndHorizontal();
        }

        private async void SendPrompt()
        {
            if (session == null)
            {
                return;
            }

            string prompt =
                promptInput.Trim();

            promptInput = "";

            currentStreamingResponse = "";

            isSending = true;

            messages.Add(
                new EditorChatMessage(
                    "user",
                    prompt
                )
            );

            cancellationTokenSource =
                new CancellationTokenSource();

            try
            {
                string response =
                    await session.AskAsync(
                        prompt,
                        OnStreamingChunk,
                        cancellationTokenSource.Token
                    );

                if (!string.IsNullOrWhiteSpace(
                    response))
                {
                    lastAssistantMessage =
                        response;

                    messages.Add(
                        new EditorChatMessage(
                            "assistant",
                            response
                        )
                    );

                    ExtractCodeFromResponse(
                        response
                    );
                }

                currentStreamingResponse = "";

                statusMessage =
                    "Risposta completata.";
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                statusMessage =
                    "Errore: " + ex.Message;
            }
            finally
            {
                isSending = false;

                cancellationTokenSource?.Dispose();

                cancellationTokenSource = null;

                Repaint();
            }
        }

        private void OnStreamingChunk(
            string chunk)
        {
            currentStreamingResponse += chunk;

            Repaint();
        }

        private void ExtractCodeFromResponse(
            string response)
        {
            CSharpScriptExtractionResult result =
                CSharpScriptExtractor.Extract(
                    response
                );

            if (result == null ||
                !result.HasValidScripts)
            {
                lastExtractedCode = "";

                return;
            }

            CSharpGeneratedScriptCandidate candidate =
                result.Candidates[0];

            if (candidate == null)
            {
                return;
            }

            lastExtractedCode =
                candidate.Code;
        }

        private void SaveCurrentScript()
        {
            CSharpScriptExtractionResult result =
                CSharpScriptExtractor.Extract(
                    lastAssistantMessage
                );

            if (result == null ||
                !result.HasValidScripts)
            {
                statusMessage =
                    "Nessuno script C# valido.";

                return;
            }

            bool saved =
                CSharpScriptSaver.SaveGeneratedScript(
                    result.Candidates[0]
                );

            statusMessage =
                saved
                    ? "Script salvato."
                    : "Salvataggio annullato.";
        }

        private void CancelCurrentRequest()
        {
            if (cancellationTokenSource == null)
            {
                return;
            }

            cancellationTokenSource.Cancel();

            statusMessage =
                "Richiesta interrotta.";
        }

        private void CreateNewChat()
        {
            CancelCurrentRequest();

            messages.Clear();

            currentStreamingResponse = "";

            lastAssistantMessage = "";

            lastExtractedCode = "";

            promptInput = "";

            if (session != null)
            {
                session.ClearHistory();
            }

            statusMessage =
                "Nuova chat.";
        }

        private sealed class EditorChatMessage
        {
            public string Role { get; }

            public string Content { get; }

            public EditorChatMessage(
                string role,
                string content)
            {
                Role = role;
                Content = content;
            }
        }
    }
}
