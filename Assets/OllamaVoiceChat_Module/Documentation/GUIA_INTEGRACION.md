# 📦 Módulo Ollama Voice Chat (Gemma 3:4b + STT Whisper + TTS) para Unity

Este módulo contiene toda la lógica, cliente HTTP, gestión de conversación, grabación de micrófono, reconocimiento de voz (STT con Whisper local), síntesis de voz (TTS nativo) y la interfaz de usuario completa (Canvas responsivo, panel de chat y botón flotante) lista para exportar e integrar en cualquier proyecto de Unity.

---

## 📁 Estructura del Módulo

```text
Assets/
└── OllamaVoiceChat_Module/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── OllamaModels.cs        # DTOs y modelos serializables (JSON)
    │   │   ├── OllamaClient.cs        # Cliente HTTP REST asíncrono (UnityWebRequest)
    │   │   └── OllamaChatManager.cs   # Orquestador del historial y conexión con Ollama
    │   ├── Voice/
    │   │   ├── MicRecorder.cs         # Grabación de audio desde el micrófono (16kHz)
    │   │   ├── SpeechToTextService.cs # Transcripción local offline con Whisper Unity
    │   │   ├── TextToSpeechService.cs # Síntesis de voz nativa (Windows SAPI / Android)
    │   │   └── VoiceChatController.cs # Controlador que conecta Mic -> STT -> Ollama -> TTS
    │   ├── UI/
    │   │   └── ChatUIController.cs    # Controlador de la interfaz (TMP Input, Scroll, Botones)
    │   └── Editor/
    │       └── OllamaChatUIBuilder.cs # Herramienta de 1-Click en el menú de Unity
    └── Documentation/
        ├── GUIA_INTEGRACION.md                            # Guía paso a paso para exportar e integrar
        └── ARQUITECTURA_Y_FUNCIONAMIENTO_OLLAMA_UNITY.md # Documento técnico detallado de arquitectura
```

---

## 🚀 Pasos para Integrar en tu Proyecto Real

### 1. Copiar la carpeta del módulo
Copia toda la carpeta `Assets/OllamaVoiceChat_Module/` dentro de la carpeta `Assets/` de tu proyecto destino.

---

### 2. Dependencias de Packages (Package Manager)

En tu nuevo proyecto de Unity:

1. **Newtonsoft.Json:**
   - Abre **Window > Package Manager**.
   - Presiona el botón `+` (esquina superior izquierda) y selecciona **Add package by name...**
   - Escribe: `com.unity.nuget.newtonsoft-json` y presiona **Add**.

2. **Whisper Unity (Reconocimiento de Voz Local):**
   - Opción A: Copia la carpeta `Packages/com.whisper.unity` de este proyecto a la carpeta `Packages/` de tu proyecto destino.
   - Opción B: En Package Manager, selecciona **Add package from git URL...** e ingresa:
     `https://github.com/Macoron/whisper.unity.git#1.4.0`

---

### 3. Modelo de Whisper (StreamingAssets)

Copia el archivo del modelo de voz:
- Origen: `Assets/StreamingAssets/whisper/ggml-tiny.bin`
- Destino: `Assets/StreamingAssets/whisper/ggml-tiny.bin` en tu nuevo proyecto.

*(Nota: Unity requiere que esté exactamente en esa ruta para que Whisper lo cargue tanto en Editor como en Builds finales).*

---

### 4. TextMesh Pro
Asegúrate de tener importado TextMesh Pro:
- Si no está activo: **Window > TextMeshPro > Import TMP Essential Resources**.

---

### 5. Generar la UI con 1 Clic (Automático)

1. Abre tu escena donde quieras el chat.
2. En la barra superior de Unity haz clic en:
   **Ollama > Create Chat UI Canvas (with Voice)**
3. ¡Listo! El script creará automáticamente en la escena:
   - El GameObject **`OllamaManager`** con todos los componentes de Ollama, Whisper, Mic y TTS configurados.
   - El Canvas **`Ollama_Chat_Panel`** con el historial de mensajes, caja de texto, botón de micrófono `🎙️`, botón de envío y el botón flotante `💬 Chat IA`.

---

## ⚙️ Configuración y Personalización

### Cambiar Parámetros de Ollama:
En el Inspector del objeto `OllamaManager`:
- **Base Url:** `http://localhost:11434` (por defecto).
- **Model Name:** `gemma3:4b` (o cualquier modelo instalado en tu Ollama, ej: `llama3`, `mistral`, etc.).
- **Temperature:** Controla la creatividad (0.7 recomendado).
- **Context Length:** Tamaño de tokens de contexto (4096 recomendado).

### Cambiar Ajustes de Voz (TTS):
En el componente `TextToSpeechService`:
- **Speech Rate:** Velocidad de reproducción (-10 a 10).
- **Speech Volume:** Volumen (0 a 100).
- **Enable Voice Output:** Activar o silenciar la voz si solo deseas texto.

---

## 📱 Uso en la Experiencia:
- **Escribir:** Escribe en la caja de texto y presiona `Enter` o el botón verde `Enviar`.
- **Hablar:** Haz clic en el botón `🎙️` azul (se pondrá rojo mientras hablas). Haz clic nuevamente para enviar y transcribir automáticamente con Whisper.
- **Voz de respuesta:** Gemma responderá en texto y se escuchará la voz hablada en español simultáneamente.
