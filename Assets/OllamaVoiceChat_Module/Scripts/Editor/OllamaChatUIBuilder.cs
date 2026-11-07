#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using OllamaIntegration;
using OllamaUI;
using OllamaVoice;
using Whisper;
using Nobi.UiRoundedCorners;

namespace OllamaEditor
{
    public static class OllamaChatUIBuilder
    {
        private const string RobotoFontPath = "Assets/TextMesh Pro/Fonts/Roboto-VariableFont_wdth,wght SDF.asset";
        private const string ChatScenePath = "Assets/Scenes/InterfazModelosOjosCompletos.unity";
        private const string IconBase = "Assets/Layer Lab/2D Icons-PictoIconPack01/Icons/PictoIcon_256/";
        private const string IconRecordOff = IconBase + "Icon_PictoIcon_Record_Off.Png";
        private const string IconRecord = IconBase + "Icon_PictoIcon_Record.Png";
        private const string IconVolume = IconBase + "Icon_PictoIcon_Volume.Png";
        private const string IconSend = IconBase + "Icon_PictoIcon_Send_1.Png";

        // Paleta de la aplicación (azul oscuro + cian)
        private static readonly Color PanelColor = new Color(0.039f, 0.067f, 0.094f, 0.97f);
        private static readonly Color HeaderColor = new Color(0.06f, 0.10f, 0.145f, 1f);
        private static readonly Color ScrollBgColor = new Color(0.03f, 0.05f, 0.075f, 0.92f);
        private static readonly Color InputBgColor = new Color(0.07f, 0.12f, 0.18f, 1f);
        private static readonly Color AccentCyan = new Color(0.0f, 0.75f, 0.85f, 1f);
        private static readonly Color ToggleCyan = new Color(0.0f, 0.55f, 0.65f, 0.95f);
        private static readonly Color MutedBlue = new Color(0.20f, 0.24f, 0.32f, 1f);
        private static readonly Color DangerRed = new Color(0.75f, 0.25f, 0.25f, 1f);
        private static readonly Color StatusText = new Color(0.65f, 0.85f, 0.90f, 1f);

        private static TMP_FontAsset LoadAppFont()
        {
            var roboto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RobotoFontPath);
            return roboto != null ? roboto : TMP_Settings.defaultFontAsset;
        }

        private static Sprite LoadIcon(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Round(GameObject go, float radius)
        {
            if (go == null) return;
            if (go.GetComponent<ImageWithIndependentRoundedCorners>() != null) return;
            var rounded = go.AddComponent<ImageWithIndependentRoundedCorners>();
            rounded.r = new Vector4(radius, radius, radius, radius);
        }

        [MenuItem("Ollama/Create Chat UI Canvas (with Voice)", false, 10)]
        [MenuItem("GameObject/UI/Ollama Chat UI with Voice", false, 10)]
        public static void CreateChatUI()
        {
            BuildFreshUI();
        }

        /// <summary>
        /// Reconstruye el canvas del chat con el diseño actual (burbujas, chips,
        /// push-to-talk, iconos). Conserva el Ollama_Manager y su configuración.
        /// </summary>
        [MenuItem("Ollama/Actualizar Chat UI (estilo + selector)", false, 11)]
        public static void UpgradeChatUI()
        {
            var uiController = Object.FindAnyObjectByType<ChatUIController>(FindObjectsInactive.Include);
            if (uiController == null)
            {
                bool openScene = EditorUtility.DisplayDialog(
                    "Ollama",
                    "No hay ningún ChatUIController en las escenas abiertas.\n\nAbre la escena 'InterfazModelosOjosCompletos' y vuelve a ejecutar.\n\n¿Abrir esa escena ahora?",
                    "Abrir escena",
                    "Cancelar");
                if (openScene)
                {
                    var opened = EditorSceneManager.OpenScene(ChatScenePath, OpenSceneMode.Single);
                    if (opened.IsValid())
                    {
                        uiController = Object.FindAnyObjectByType<ChatUIController>(FindObjectsInactive.Include);
                    }
                }
                if (uiController == null)
                {
                    return;
                }
            }

            BuildFreshUI();
        }

        /// <summary>
        /// Fija el modelo Gemini vigente en todos los managers de la escena.
        /// </summary>
        [MenuItem("Ollama/Fijar modelo Gemini Flash latest", false, 12)]
        public static void SetGeminiModel()
        {
            var managers = Object.FindObjectsByType<OllamaChatManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers.Length == 0)
            {
                EditorUtility.DisplayDialog("Ollama", "No hay ningún OllamaChatManager en la escena.", "Entendido");
                return;
            }

            foreach (var m in managers)
            {
                var so = new SerializedObject(m);
                var prop = so.FindProperty("geminiModel");
                if (prop != null)
                {
                    prop.stringValue = "gemini-flash-latest";
                    so.ApplyModifiedProperties();
                }
                Undo.RecordObject(m, "Set Gemini Model");
                EditorUtility.SetDirty(m);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[OllamaChatUIBuilder] Modelo Gemini fijado a gemini-flash-latest.");
        }

        private static void BuildFreshUI()
        {
            // Limpiar canvas anterior (se reconstruye; el manager se conserva)
            var oldCanvas = GameObject.Find("Ollama_Chat_Canvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            var oldToggle = GameObject.Find("Ollama_Chat_Toggle_Button");
            if (oldToggle != null) Object.DestroyImmediate(oldToggle);

            var oldPanel = GameObject.Find("Ollama_Chat_Panel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel);

            BuildChatUI();
        }

        private static void BuildChatUI()
        {
            // 1. Canvas dedicado
            GameObject canvasObj = new GameObject("Ollama_Chat_Canvas");
            canvasObj.transform.localScale = Vector3.one;
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Ollama Canvas");

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            TMP_FontAsset appFont = LoadAppFont();
            Sprite recordOff = LoadIcon(IconRecordOff);
            Sprite recordOn = LoadIcon(IconRecord);
            Sprite volumeIcon = LoadIcon(IconVolume);
            Sprite sendIcon = LoadIcon(IconSend);

            // 2. Panel principal (arranca cerrado)
            GameObject panelObj = new GameObject("Ollama_Chat_Panel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 130f);
            panelRect.sizeDelta = new Vector2(980f, 1320f);
            panelObj.GetComponent<Image>().color = PanelColor;
            Round(panelObj, 28f);
            panelObj.SetActive(false);

            // 3. Header
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(panelObj.transform, false);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 95f);
            headerObj.GetComponent<Image>().color = HeaderColor;

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.70f, 1f);
            titleRect.offsetMin = new Vector2(25f, 0f);
            titleRect.offsetMax = new Vector2(-10f, 0f);
            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "<b>Asistente ocular</b> <size=70%><color=#4DD0E1>(Salud visual)</color></size>";
            titleTMP.fontSize = 32;
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (appFont != null) titleTMP.font = appFont;

            GameObject clearBtnObj = CreateButton(headerObj.transform, "ClearButton", "Borrar", MutedBlue, appFont, 22);
            var clearRect = clearBtnObj.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(1f, 0.5f);
            clearRect.anchorMax = new Vector2(1f, 0.5f);
            clearRect.pivot = new Vector2(1f, 0.5f);
            clearRect.anchoredPosition = new Vector2(-115f, 0f);
            clearRect.sizeDelta = new Vector2(100f, 65f);
            Round(clearBtnObj, 14f);

            GameObject closeBtnObj = CreateButton(headerObj.transform, "CloseButton", "X", DangerRed, appFont, 32);
            var closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-15f, 0f);
            closeRect.sizeDelta = new Vector2(75f, 65f);
            Round(closeBtnObj, 14f);

            // 4. Selector de proveedor
            TMP_Dropdown providerDropdown = CreateProviderDropdown(panelObj.transform, appFont);

            // 5. Estado
            GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(panelObj.transform, false);
            var statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -164f);
            statusRect.sizeDelta = new Vector2(-40f, 32f);
            var statusTMP = statusObj.GetComponent<TextMeshProUGUI>();
            statusTMP.text = "Listo";
            statusTMP.fontSize = 22;
            statusTMP.color = StatusText;
            statusTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (appFont != null) statusTMP.font = appFont;

            // 6. Scroll de burbujas
            GameObject scrollObj = new GameObject("ChatScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(panelObj.transform, false);
            var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(20f, 175f);
            scrollRectTransform.offsetMax = new Vector2(-20f, -208f);
            scrollObj.GetComponent<Image>().color = ScrollBgColor;

            var scrollRectComponent = scrollObj.GetComponent<ScrollRect>();
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            scrollRectComponent.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scrollRectComponent.viewport = viewportRect;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 100f);

            var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = contentObj.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRectComponent.content = contentRect;

            // 7. Indicador "escribiendo"
            GameObject loadingObj = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
            loadingObj.transform.SetParent(panelObj.transform, false);
            var loadingRect = loadingObj.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0f, 0f);
            loadingRect.anchorMax = new Vector2(1f, 0f);
            loadingRect.pivot = new Vector2(0.5f, 0f);
            loadingRect.anchoredPosition = new Vector2(0f, 128f);
            loadingRect.sizeDelta = new Vector2(-50f, 35f);
            var loadingTMP = loadingObj.GetComponent<TextMeshProUGUI>();
            loadingTMP.text = "El asistente está escribiendo…";
            loadingTMP.fontSize = 24;
            loadingTMP.color = new Color(0.4f, 0.85f, 0.9f, 1f);
            loadingTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (appFont != null) loadingTMP.font = appFont;
            loadingObj.SetActive(false);

            // 8. Área inferior: Input + Mic PTT + Enviar
            GameObject inputAreaObj = new GameObject("InputArea", typeof(RectTransform));
            inputAreaObj.transform.SetParent(panelObj.transform, false);
            var inputAreaRect = inputAreaObj.GetComponent<RectTransform>();
            inputAreaRect.anchorMin = new Vector2(0f, 0f);
            inputAreaRect.anchorMax = new Vector2(1f, 0f);
            inputAreaRect.pivot = new Vector2(0.5f, 0f);
            inputAreaRect.anchoredPosition = new Vector2(0f, 10f);
            inputAreaRect.sizeDelta = new Vector2(-40f, 110f);

            GameObject inputFieldObj = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputFieldObj.transform.SetParent(inputAreaObj.transform, false);
            var inputRect = inputFieldObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = new Vector2(-242f, 0f);
            inputFieldObj.GetComponent<Image>().color = InputBgColor;
            Round(inputFieldObj, 24f);

            var tmpInput = inputFieldObj.GetComponent<TMP_InputField>();

            GameObject textAreaObj = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaObj.transform.SetParent(inputFieldObj.transform, false);
            var textAreaRect = textAreaObj.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(20f, 10f);
            textAreaRect.offsetMax = new Vector2(-20f, -10f);
            tmpInput.textViewport = textAreaRect;

            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(textAreaObj.transform, false);
            var phRect = placeholderObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;
            var phTMP = placeholderObj.GetComponent<TextMeshProUGUI>();
            phTMP.text = "Escribe o mantén el micrófono…";
            phTMP.fontSize = 26;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.color = new Color(0.55f, 0.65f, 0.72f, 0.8f);
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (appFont != null) phTMP.font = appFont;
            tmpInput.placeholder = phTMP;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textAreaObj.transform, false);
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var textTMP = textObj.GetComponent<TextMeshProUGUI>();
            textTMP.fontSize = 26;
            textTMP.color = Color.white;
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (appFont != null) textTMP.font = appFont;
            tmpInput.textComponent = textTMP;

            // Botón micrófono PTT circular con icono (sin texto)
            GameObject micBtnObj = CreateIconButton(inputAreaObj.transform, "MicButton", AccentCyan, recordOff, 62f, appFont, "Mic");
            var micRect = micBtnObj.GetComponent<RectTransform>();
            micRect.anchorMin = new Vector2(1f, 0.5f);
            micRect.anchorMax = new Vector2(1f, 0.5f);
            micRect.pivot = new Vector2(1f, 0.5f);
            micRect.anchoredPosition = new Vector2(-108f, 0f);
            micRect.sizeDelta = new Vector2(110f, 110f);
            Round(micBtnObj, 55f);
            micBtnObj.AddComponent<HoldToTalkButton>();

            // Botón enviar circular con icono
            GameObject sendBtnObj = CreateIconButton(inputAreaObj.transform, "SendButton", new Color(0.18f, 0.70f, 0.45f, 1f), sendIcon, 52f, appFont, "Enviar");
            var sendRect = sendBtnObj.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(1f, 0.5f);
            sendRect.anchorMax = new Vector2(1f, 0.5f);
            sendRect.pivot = new Vector2(1f, 0.5f);
            sendRect.anchoredPosition = Vector2.zero;
            sendRect.sizeDelta = new Vector2(96f, 96f);
            Round(sendBtnObj, 48f);

            // 10. Botón flotante (solo texto legible)
            GameObject toggleBtnObj = CreateButton(canvasObj.transform, "Ollama_Chat_Toggle_Button", "Chat", ToggleCyan, appFont, 30);
            var toggleRect = toggleBtnObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(1f, 1f);
            toggleRect.anchorMax = new Vector2(1f, 1f);
            toggleRect.pivot = new Vector2(1f, 1f);
            toggleRect.anchoredPosition = new Vector2(-30f, -30f);
            toggleRect.sizeDelta = new Vector2(190f, 90f);
            Round(toggleBtnObj, 22f);

            // 11. Manager (se conserva si existe)
            GameObject managerObj = GameObject.Find("Ollama_Manager");
            if (managerObj == null)
            {
                managerObj = new GameObject("Ollama_Manager");
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Ollama Manager");
            }

            var chatManager = managerObj.GetComponent<OllamaChatManager>() ?? managerObj.AddComponent<OllamaChatManager>();
            var whisperMgr = managerObj.GetComponent<WhisperManager>() ?? managerObj.AddComponent<WhisperManager>();
            whisperMgr.ModelPath = "whisper/ggml-tiny.bin";
            whisperMgr.IsModelPathInStreamingAssets = true;
            whisperMgr.language = "es";

            var micRecorder = managerObj.GetComponent<MicRecorder>() ?? managerObj.AddComponent<MicRecorder>();
            var sttService = managerObj.GetComponent<SpeechToTextService>() ?? managerObj.AddComponent<SpeechToTextService>();
            var ttsService = managerObj.GetComponent<TextToSpeechService>() ?? managerObj.AddComponent<TextToSpeechService>();
            var voiceController = managerObj.GetComponent<VoiceChatController>() ?? managerObj.AddComponent<VoiceChatController>();

            // 12. Referencias
            var uiController = canvasObj.GetComponent<ChatUIController>() ?? canvasObj.AddComponent<ChatUIController>();
            var micBg = micBtnObj.GetComponent<Image>();
            var micGlyph = micBtnObj.transform.Find("Icon")?.GetComponent<Image>();
            uiController.SetupReferences(
                chatManager,
                panelObj,
                toggleBtnObj.GetComponent<Button>(),
                closeBtnObj.GetComponent<Button>(),
                clearBtnObj.GetComponent<Button>(),
                tmpInput,
                sendBtnObj.GetComponent<Button>(),
                scrollRectComponent,
                contentObj.transform,
                statusTMP,
                loadingObj,
                providerDropdown,
                titleTMP,
                volumeIcon,
                appFont
            );

            voiceController.SetupReferences(
                micRecorder,
                sttService,
                ttsService,
                chatManager,
                uiController,
                micBtnObj.GetComponent<Button>(),
                micBg,
                null,
                micGlyph,
                recordOff,
                recordOn
            );

            var holdButton = micBtnObj.GetComponent<HoldToTalkButton>();
            if (holdButton != null) holdButton.Setup(voiceController);

            // Canvas activo, panel cerrado: solo se ve el botón flotante
            canvasObj.SetActive(true);
            panelObj.SetActive(false);

            Undo.RegisterCreatedObjectUndo(panelObj, "Create Ollama Chat Panel with Voice");
            Undo.RegisterCreatedObjectUndo(toggleBtnObj, "Create Ollama Toggle Button");
            Selection.activeGameObject = panelObj;
            EditorUtility.SetDirty(panelObj);
            EditorUtility.SetDirty(managerObj);
            EditorSceneManager.MarkSceneDirty(panelObj.scene);
            EditorSceneManager.SaveScene(panelObj.scene);

            Debug.Log("[OllamaChatUIBuilder] Chat UI reconstruida: burbujas, chips, PTT, iconos y selector.");
        }

        private static TMP_Dropdown CreateProviderDropdown(Transform parent, TMP_FontAsset font)
        {
            GameObject ddObj = new GameObject("ProviderDropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            ddObj.transform.SetParent(parent, false);
            var ddRect = ddObj.GetComponent<RectTransform>();
            ddRect.anchorMin = new Vector2(0f, 1f);
            ddRect.anchorMax = new Vector2(1f, 1f);
            ddRect.pivot = new Vector2(0.5f, 1f);
            ddRect.offsetMin = new Vector2(20f, -160f);
            ddRect.offsetMax = new Vector2(-20f, -102f);
            ddObj.GetComponent<Image>().color = InputBgColor;
            Round(ddObj, 14f);

            var dropdown = ddObj.GetComponent<TMP_Dropdown>();

            GameObject captionObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionObj.transform.SetParent(ddObj.transform, false);
            var captionRect = captionObj.GetComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(18f, 4f);
            captionRect.offsetMax = new Vector2(-52f, -4f);
            var captionTMP = captionObj.GetComponent<TextMeshProUGUI>();
            captionTMP.fontSize = 24;
            captionTMP.color = Color.white;
            captionTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) captionTMP.font = font;
            dropdown.captionText = captionTMP;

            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowObj.transform.SetParent(ddObj.transform, false);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-14f, 0f);
            arrowRect.sizeDelta = new Vector2(36f, 0f);
            var arrowTMP = arrowObj.GetComponent<TextMeshProUGUI>();
            arrowTMP.text = "▾";
            arrowTMP.fontSize = 30;
            arrowTMP.color = new Color(0.4f, 0.85f, 0.9f, 1f);
            arrowTMP.alignment = TextAlignmentOptions.Center;
            if (font != null) arrowTMP.font = font;

            GameObject templateObj = new GameObject("Template", typeof(RectTransform), typeof(Image));
            templateObj.transform.SetParent(ddObj.transform, false);
            var templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 150f);
            templateObj.GetComponent<Image>().color = new Color(0.05f, 0.09f, 0.14f, 0.98f);

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportObj.transform.SetParent(templateObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);
            viewportObj.GetComponent<Image>().color = new Color(0.05f, 0.09f, 0.14f, 0.98f);

            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 150f);

            GameObject itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 70f);

            GameObject bgObj = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(itemObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.07f, 0.12f, 0.18f, 1f);

            GameObject checkObj = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            checkObj.transform.SetParent(itemObj.transform, false);
            var checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = new Vector2(28f, 0f);
            checkRect.sizeDelta = new Vector2(28f, 28f);
            checkObj.GetComponent<Image>().color = new Color(0.0f, 0.85f, 0.9f, 1f);

            GameObject itemLabelObj = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(64f, 4f);
            itemLabelRect.offsetMax = new Vector2(-12f, -4f);
            var itemLabelTMP = itemLabelObj.GetComponent<TextMeshProUGUI>();
            itemLabelTMP.fontSize = 24;
            itemLabelTMP.color = Color.white;
            itemLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) itemLabelTMP.font = font;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabelTMP;
            templateObj.SetActive(false);

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "Ollama · Local", "Gemini · Nube" });
            dropdown.value = 0;
            dropdown.RefreshShownValue();

            return dropdown;
        }

        /// <summary>
        /// Botón circular con icono (sin texto). Si no hay sprite, usa etiqueta de texto.
        /// </summary>
        private static GameObject CreateIconButton(Transform parent, string name, Color bgColor, Sprite icon, float iconSize, TMP_FontAsset font, string fallbackLabel)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = bgColor;

            if (icon != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(btnObj.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                var iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = icon;
                iconImg.color = Color.white;
            }
            else
            {
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(btnObj.transform, false);
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = fallbackLabel;
                tmp.fontSize = 26;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                if (font != null) tmp.font = font;
            }

            return btnObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color bgColor, TMP_FontAsset font, float fontSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = bgColor;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            if (font != null) tmp.font = font;

            return btnObj;
        }
    }
}
#endif
