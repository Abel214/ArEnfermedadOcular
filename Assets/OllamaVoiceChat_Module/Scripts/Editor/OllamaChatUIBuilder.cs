#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OllamaIntegration;
using OllamaUI;
using OllamaVoice;
using Whisper;

namespace OllamaEditor
{
    [InitializeOnLoad]
    public static class OllamaChatUIBuilder
    {
        static OllamaChatUIBuilder()
        {
            EditorApplication.delayCall += EnsureChatUIInScene;
        }

        private static void EnsureChatUIInScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var existingManager = Object.FindFirstObjectByType<OllamaChatManager>();
            var existingPanel = GameObject.Find("Ollama_Chat_Panel");
            if (existingManager == null || existingPanel == null)
            {
                CreateChatUI();
            }
        }

        [MenuItem("Ollama/Create Chat UI Canvas (with Voice)", false, 10)]
        [MenuItem("GameObject/UI/Ollama Chat UI with Voice", false, 10)]
        public static void CreateChatUI()
        {
            // Limpiar versión anterior si existe
            var oldPanel = GameObject.Find("Ollama_Chat_Panel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel);

            var oldToggle = GameObject.Find("Ollama_Chat_Toggle_Button");
            if (oldToggle != null) Object.DestroyImmediate(oldToggle);

            var oldCanvas = GameObject.Find("Ollama_Chat_Canvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            var oldManager = GameObject.Find("Ollama_Manager");
            if (oldManager != null) Object.DestroyImmediate(oldManager);

            // 1. Crear Canvas dedicado e independiente para Ollama
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

            // Asegurar EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            // Obtener font por defecto de TMP
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            // 2. Crear Panel Principal de Chat (Diseño optimizado para móvil vertical 1080x1920)
            GameObject panelObj = new GameObject("Ollama_Chat_Panel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 130f);
            panelRect.sizeDelta = new Vector2(980f, 1320f);

            var panelImg = panelObj.GetComponent<Image>();
            panelImg.color = new Color(0.10f, 0.12f, 0.17f, 0.97f);

            // 3. Header del Chat
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(panelObj.transform, false);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 95f);
            headerObj.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 1f);

            // Título
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.70f, 1f);
            titleRect.offsetMin = new Vector2(25f, 0f);
            titleRect.offsetMax = new Vector2(-10f, 0f);
            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "<b>Ollama AI</b> <size=70%><color=#90CAF9>(Gemma 3:4B)</color></size>";
            titleTMP.fontSize = 32;
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (defaultFont != null) titleTMP.font = defaultFont;

            // Botón Limpiar (Clear)
            GameObject clearBtnObj = CreateButton(headerObj.transform, "ClearButton", "Borrar", new Color(0.25f, 0.28f, 0.35f, 1f), defaultFont, 22);
            var clearRect = clearBtnObj.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(1f, 0.5f);
            clearRect.anchorMax = new Vector2(1f, 0.5f);
            clearRect.pivot = new Vector2(1f, 0.5f);
            clearRect.anchoredPosition = new Vector2(-115f, 0f);
            clearRect.sizeDelta = new Vector2(100f, 65f);

            // Botón Cerrar (Close)
            GameObject closeBtnObj = CreateButton(headerObj.transform, "CloseButton", "X", new Color(0.85f, 0.25f, 0.25f, 1f), defaultFont, 32);
            var closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-15f, 0f);
            closeRect.sizeDelta = new Vector2(75f, 65f);

            // 4. Barra de Estado
            GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(panelObj.transform, false);
            var statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -100f);
            statusRect.sizeDelta = new Vector2(-40f, 36f);
            var statusTMP = statusObj.GetComponent<TextMeshProUGUI>();
            statusTMP.text = "Listo para conversar (Texto o Voz)";
            statusTMP.fontSize = 22;
            statusTMP.color = new Color(0.7f, 0.78f, 0.9f, 1f);
            statusTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (defaultFont != null) statusTMP.font = defaultFont;

            // 5. Scroll View para Mensajes
            GameObject scrollObj = new GameObject("ChatScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(panelObj.transform, false);
            var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(20f, 135f);
            scrollRectTransform.offsetMax = new Vector2(-20f, -145f);
            scrollObj.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.11f, 0.90f);

            var scrollRectComponent = scrollObj.GetComponent<ScrollRect>();
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            scrollRectComponent.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scrollRectComponent.viewport = viewportRect;

            // Content
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
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = contentObj.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRectComponent.content = contentRect;

            // Texto de registro de Chat
            GameObject chatLogObj = new GameObject("ChatLogText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            chatLogObj.transform.SetParent(contentObj.transform, false);
            var chatLogRect = chatLogObj.GetComponent<RectTransform>();
            chatLogRect.anchorMin = Vector2.zero;
            chatLogRect.anchorMax = Vector2.one;
            chatLogRect.sizeDelta = Vector2.zero;

            var chatLogTMP = chatLogObj.GetComponent<TextMeshProUGUI>();
            chatLogTMP.fontSize = 28;
            chatLogTMP.lineSpacing = 10f;
            chatLogTMP.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            chatLogTMP.enableWordWrapping = true;
            chatLogTMP.richText = true;
            chatLogTMP.text = "";
            if (defaultFont != null) chatLogTMP.font = defaultFont;

            var textFitter = chatLogObj.GetComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 6. Indicador de Carga
            GameObject loadingObj = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
            loadingObj.transform.SetParent(panelObj.transform, false);
            var loadingRect = loadingObj.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0f, 0f);
            loadingRect.anchorMax = new Vector2(1f, 0f);
            loadingRect.pivot = new Vector2(0.5f, 0f);
            loadingRect.anchoredPosition = new Vector2(0f, 128f);
            loadingRect.sizeDelta = new Vector2(-50f, 35f);
            var loadingTMP = loadingObj.GetComponent<TextMeshProUGUI>();
            loadingTMP.text = "<i>Gemma está respondiendo...</i>";
            loadingTMP.fontSize = 24;
            loadingTMP.color = new Color(0.4f, 0.8f, 1f, 1f);
            loadingTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (defaultFont != null) loadingTMP.font = defaultFont;
            loadingObj.SetActive(false);

            // 7. Área Inferior: InputField + Botón Micrófono + Botón Enviar
            GameObject inputAreaObj = new GameObject("InputArea", typeof(RectTransform));
            inputAreaObj.transform.SetParent(panelObj.transform, false);
            var inputAreaRect = inputAreaObj.GetComponent<RectTransform>();
            inputAreaRect.anchorMin = new Vector2(0f, 0f);
            inputAreaRect.anchorMax = new Vector2(1f, 0f);
            inputAreaRect.pivot = new Vector2(0.5f, 0f);
            inputAreaRect.anchoredPosition = new Vector2(0f, 20f);
            inputAreaRect.sizeDelta = new Vector2(-40f, 95f);

            // InputField
            GameObject inputFieldObj = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputFieldObj.transform.SetParent(inputAreaObj.transform, false);
            var inputRect = inputFieldObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = new Vector2(-260f, 0f); // Espacio para Mic (95px) y Enviar (145px) + gap
            inputFieldObj.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.27f, 1f);

            var tmpInput = inputFieldObj.GetComponent<TMP_InputField>();

            // Text Area
            GameObject textAreaObj = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaObj.transform.SetParent(inputFieldObj.transform, false);
            var textAreaRect = textAreaObj.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(15f, 8f);
            textAreaRect.offsetMax = new Vector2(-15f, -8f);
            tmpInput.textViewport = textAreaRect;

            // Placeholder Text
            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(textAreaObj.transform, false);
            var phRect = placeholderObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;
            var phTMP = placeholderObj.GetComponent<TextMeshProUGUI>();
            phTMP.text = "Escribe o habla por voz...";
            phTMP.fontSize = 26;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.color = new Color(0.55f, 0.60f, 0.70f, 0.8f);
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (defaultFont != null) phTMP.font = defaultFont;
            tmpInput.placeholder = phTMP;

            // Input Text
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
            if (defaultFont != null) textTMP.font = defaultFont;
            tmpInput.textComponent = textTMP;

            // Botón Micrófono
            GameObject micBtnObj = CreateButton(inputAreaObj.transform, "MicButton", "Voz", new Color(0.23f, 0.49f, 0.95f, 1f), defaultFont, 26);
            var micRect = micBtnObj.GetComponent<RectTransform>();
            micRect.anchorMin = new Vector2(1f, 0f);
            micRect.anchorMax = new Vector2(1f, 1f);
            micRect.pivot = new Vector2(1f, 0.5f);
            micRect.anchoredPosition = new Vector2(-150f, 0f);
            micRect.sizeDelta = new Vector2(95f, 0f);

            // Botón Enviar
            GameObject sendBtnObj = CreateButton(inputAreaObj.transform, "SendButton", "Enviar", new Color(0.18f, 0.70f, 0.45f, 1f), defaultFont, 26);
            var sendRect = sendBtnObj.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(1f, 0f);
            sendRect.anchorMax = new Vector2(1f, 1f);
            sendRect.pivot = new Vector2(1f, 0.5f);
            sendRect.anchoredPosition = Vector2.zero;
            sendRect.sizeDelta = new Vector2(140f, 0f);

            // 8. Botón Flotante para Abrir/Cerrar Chat (Floating Action Button)
            GameObject toggleBtnObj = CreateButton(canvasObj.transform, "Ollama_Chat_Toggle_Button", "Chat Bot", new Color(0.18f, 0.45f, 0.85f, 0.95f), defaultFont, 28);
            var toggleRect = toggleBtnObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(1f, 0f);
            toggleRect.anchorMax = new Vector2(1f, 0f);
            toggleRect.pivot = new Vector2(1f, 0f);
            toggleRect.anchoredPosition = new Vector2(-30f, 30f);
            toggleRect.sizeDelta = new Vector2(250f, 85f);

            // 9. Crear u Obtener GameObject dedicado Ollama_Manager
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

            // 10. Configurar UI Controller en el Canvas (para que no se desactive cuando se cierra el panel)
            var uiController = canvasObj.GetComponent<ChatUIController>() ?? canvasObj.AddComponent<ChatUIController>();
            uiController.SetupReferences(
                chatManager,
                panelObj,
                toggleBtnObj.GetComponent<Button>(),
                closeBtnObj.GetComponent<Button>(),
                clearBtnObj.GetComponent<Button>(),
                tmpInput,
                sendBtnObj.GetComponent<Button>(),
                chatLogTMP,
                scrollRectComponent,
                statusTMP,
                loadingObj
            );

            voiceController.SetupReferences(
                micRecorder,
                sttService,
                ttsService,
                chatManager,
                uiController,
                micBtnObj.GetComponent<Button>(),
                micBtnObj.GetComponent<Image>(),
                micBtnObj.GetComponentInChildren<TextMeshProUGUI>()
            );

            // Registrar Undo y guardar cambios en la escena
            Undo.RegisterCreatedObjectUndo(panelObj, "Create Ollama Chat Panel with Voice");
            Undo.RegisterCreatedObjectUndo(toggleBtnObj, "Create Ollama Toggle Button");
            Selection.activeGameObject = panelObj;
            EditorUtility.SetDirty(panelObj);
            EditorUtility.SetDirty(managerObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelObj.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(panelObj.scene);

            Debug.Log("[OllamaChatUIBuilder] ¡Interfaz de Chat con soporte de Voz (STT Whisper + TTS) creada y configurada!");
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
