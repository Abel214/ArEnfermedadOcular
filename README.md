# AR Enfermedad Ocular
**Universidad Nacional de Loja — Facultad de la Salud Humana**

Aplicación de Realidad Aumentada educativa para la visualización interactiva de enfermedades oculares, desarrollada en Unity 6 con AR Foundation y control por comandos de voz.

---

## Descripción General

AR Enfermedad Ocular permite a estudiantes y pacientes visualizar modelos 3D de enfermedades oculares en Realidad Aumentada, interactuar con ellos y consultar información médica mediante una IA conversacional. La interfaz sigue principios de HCI (Human-Computer Interaction), eliminando botones innecesarios y priorizando la interacción natural por voz.

---

## Tecnologías Utilizadas

| Tecnología | Versión | Uso |
|------------|---------|-----|
| Unity | 6000.4.9f1 | Motor de desarrollo |
| AR Foundation | 6.4.1 | Realidad Aumentada |
| ARCore | - | Tracking en Android |
| XR Interaction Toolkit | 3.3.0 | Interacción XR |
| Gemini API | 1.5 Flash | IA médica conversacional |
| DOTween | - | Animaciones UI |
| TextMeshPro | - | Textos UI |
| Android TTS | Nativo | Respuestas por voz |
| Android SpeechRecognizer | Nativo | Reconocimiento de voz |

---

## Requisitos del Sistema

### Desarrollo
- Unity 6000.4.9f1 o superior
- Android Build Support con IL2CPP
- .NET Standard 2.1
- Visual Studio Code con extensión C# Dev Kit

### Dispositivo
- Android 11.0 (API level 30) o superior
- Arquitectura ARM64
- Soporte ARCore
- Micrófono
- Conexión a Internet (para Gemini API)

---

## Estructura del Proyecto

```
ArEnfermedadOcular/
├── Assets/
│   ├── Scripts/
│   │   ├── GameManager.cs
│   │   ├── UIManager.cs
│   │   ├── DataManager.cs
│   │   ├── ARInteractionManager.cs
│   │   ├── ItemButtonManager.cs
│   │   ├── ARMedicalAI.cs
│   │   ├── AndroidVoiceRecognizer.cs
│   │   ├── VoiceComanderManage.cs
│   │   ├── UnityMainThreadDispatcher.cs
│   │   ├── SplashManager.cs
│   │   ├── InstruccionesManager.cs
│   │   └── DebugNaN.cs
│   ├── ScriptableObject/
│   │   ├── AnatomíaCatarata.asset
│   │   ├── AnatomiaConjuntivitis.asset
│   │   ├── AnatomiaGlaucoma.asset
│   │   ├── OjoCatarata.asset
│   │   ├── OjoConjuntivitis.asset
│   │   ├── OjoGlaucoma.asset
│   │   └── OjoSano.asset
│   ├── Models3D/
│   ├── Materials/
│   ├── MobileARTemplateAssets/
│   └── Plugins/
│       └── Android/
│           └── AndroidManifest.xml
└── Packages/
    └── manifest.json
```

---

## Arquitectura de la Escena

```
SampleScene
├── Directional Light
├── GameManager              ← Singleton, eventos globales
├── UIManager
│   ├── SplashCanvas         ← Pantalla de inicio (Sort Order: 99)
│   │   ├── Fondo
│   │   ├── LogoUNL
│   │   ├── LogoFacultad
│   │   ├── BtnInstrucciones
│   │   ├── BtnIniciarAR
│   │   └── PanelInstrucciones
│   │       ├── SlideContainer
│   │       ├── TituloSlide
│   │       ├── TextoSlide
│   │       ├── IconoSlide
│   │       ├── IndicadorPuntos (●●●●●)
│   │       ├── BtnAnterior
│   │       ├── BtnSiguiente
│   │       └── BtnCerrar
│   ├── MainMenuCanvas
│   │   ├── CloseApp
│   │   ├── ShowItems
│   │   ├── ScreenShot
│   │   └── Microfono (Botón Voz 🎤)
│   ├── ARPositionCanvas
│   └── ItemsMenuCanvas
│       ├── ItemsOpen
│       └── ItemsMenuPanel
│           └── Viewport → Content
│               ├── Ojo con Catarata
│               ├── Ojo con Conjuntivitis
│               ├── Ojo Glaucoma
│               └── Ojo Sano
├── EventSystem
├── AR Session
├── XR Interaction Manager
├── XR Origin (Mobile AR)
├── DataManager              ← Genera botones desde ScriptableObjects
├── ARInteractionManager     ← Colocación y manipulación de modelos
├── Debug                    ← DebugNaN
└── VoiceManager             ← AndroidVoiceRecognizer + VoiceComanderManage
```

---

## Flujo de la Aplicación

```
1. SPLASH SCREEN
   App abre → SplashCanvas visible
   Usuario toca "INSTRUCCIONES" → slides animados con voz
   Usuario toca "INICIAR AR" → entra a la escena AR

2. PANTALLA PRINCIPAL AR
   Cámara activa detectando superficies
   MainMenuCanvas visible con botones y micrófono

3. SELECCIÓN DE MODELO (por voz o botón)
   Usuario dice "colocar objeto" → ItemsMenuCanvas aparece
   Usuario dice "catarata/conjuntivitis/glaucoma/sano"
   O toca el botón del modelo → modelo 3D se instancia en AR

4. INTERACCIÓN CON MODELO
   1 dedo → mover en plano XZ + altura
   2 dedos → zoom (pinch) + rotación (twist)
   Modelo se coloca sobre superficies detectadas por ARCore

5. CONSULTA IA (por voz)
   "explica"      → Gemini describe la enfermedad
   "síntomas"     → Gemini lista los síntomas
   "tratamiento"  → Gemini explica el tratamiento
   Respuesta por TTS (voz sintetizada en español)

6. COMANDOS DE VOZ DISPONIBLES
   "colocar objeto"  → abre menú de modelos
   "catarata"        → coloca modelo de catarata
   "conjuntivitis"   → coloca modelo de conjuntivitis
   "glaucoma"        → coloca modelo de glaucoma
   "ojo sano"        → coloca modelo de ojo sano
   "explica"         → IA explica la enfermedad
   "síntomas"        → IA lista síntomas
   "tratamiento"     → IA explica tratamiento
   "eliminar"        → borra el modelo actual
   "volver"          → regresa al menú principal
```

---

## Scripts — Descripción

### GameManager.cs
Singleton que gestiona los eventos globales de la aplicación mediante el patrón Observer.

**Eventos:**
- `OnSplash` — activa la pantalla de inicio
- `OnMainMenu` — activa el menú principal AR
- `OnItemsMenu` — activa el menú de modelos
- `OnArPosition` — activa el canvas de posicionamiento
- `OnIAMenu` — activa el panel de IA

---

### UIManager.cs
Controla las animaciones de los canvas usando DOTween. Cada estado de la app activa/desactiva elementos con animaciones de escala y posición.

---

### DataManager.cs
Lee los ScriptableObjects de tipo `Item` y genera dinámicamente los botones del menú de modelos al primer uso.

---

### ARInteractionManager.cs
Maneja toda la interacción táctil con los modelos 3D en AR:
- Modo colocación inicial (raycast al centro de pantalla)
- Movimiento con 1 dedo (plano XZ + altura)
- Zoom con 2 dedos (pinch)
- Rotación con 2 dedos (twist)
- Selección por toque (raycast con tag "Item")

**Métodos públicos:**
```csharp
Item3DModel         // setter — asigna modelo y activa modo colocación
SetItemPosition()   // confirma posición del modelo
DeleteItem()        // elimina el modelo actual
ColocarPorNombre(string nombre)  // busca y coloca modelo por nombre
EliminarModelo()    // alias de DeleteItem
```

---

### ItemButtonManager.cs
Gestiona cada botón del menú de modelos. Al hacer clic instancia el prefab 3D y notifica a ARMedicalAI.

**Propiedades:**
```csharp
ItemName        // nombre del item
ItemDescription // descripción
ItemImage       // sprite del item
Item3DModel     // prefab 3D
ItemData        // referencia al ScriptableObject
```

---

### ARMedicalAI.cs
Integración con Gemini 1.5 Flash API para respuestas médicas por voz.

**Flujo:**
1. Recibe pregunta del comando de voz
2. Construye prompt con contexto del ScriptableObject
3. Llama a Gemini API via UnityWebRequest
4. Parsea la respuesta JSON
5. Reproduce por TTS Android

**Método principal:**
```csharp
AskAboutCurrentItem(string pregunta = "")
```

---

### AndroidVoiceRecognizer.cs
Usa el SpeechRecognizer nativo de Android sin abrir actividades externas. Funciona en segundo plano sin interrumpir ARCore.

**Flujo:**
1. `StartListening()` → inicia SpeechRecognizer en hilo UI de Android
2. `SpeechListener.onResults()` → captura resultado
3. `UnityMainThreadDispatcher` → regresa al hilo de Unity
4. `OnCommandRecognized` → evento disparado con el texto

---

### VoiceComanderManage.cs
Procesa los comandos de voz reconocidos y ejecuta las acciones correspondientes.

**Comandos registrados:**
```
"colocar/modelo/objeto/mostrar" → GameManager.ItemsMenu()
"catarata/conjuntivitis/glaucoma/sano" → ColocarModeloPorNombre()
"eliminar/borrar/quitar" → ARInteractionManager.DeleteItem()
"volver/inicio/menu principal" → GameManager.MainMenu()
"explica/información/que es" → ARMedicalAI.AskAboutCurrentItem()
"síntomas" → ARMedicalAI.AskAboutCurrentItem("síntomas")
"tratamiento" → ARMedicalAI.AskAboutCurrentItem("tratamiento")
```

---

### SplashManager.cs
Controla la pantalla de inicio con animaciones DOTween de entrada para logos, títulos y botones.

---

### InstruccionesManager.cs
Panel de instrucciones interactivo con 5 slides animados. Cada slide se lee automáticamente por TTS al mostrarse.

**Slides:**
1. Bienvenido — introducción a la app
2. Comando de Voz — cómo activar el micrófono
3. Seleccionar Modelo — elegir enfermedad
4. Interactuar — gestos táctiles
5. Consultar IA — comandos de información médica

---

## Configuración Android

### AndroidManifest.xml — Permisos requeridos
```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
```

### Player Settings
```
Scripting Backend: IL2CPP
Target Architecture: ARM64
Minimum API Level: Android 11.0 (API 30)
Internet Access: Required
```

---

## API Keys

### Gemini API
- Proveedor: Google AI Studio
- Modelo: gemini-1.5-flash
- Plan: Gratuito (1500 requests/día)
- Configurar en: Inspector de ARMedicalAI → Gemini Api Key

---

## Enfermedades Oculares Incluidas

| Enfermedad | Modelo | Vista Anatomía |
|------------|--------|----------------|
| Catarata | OjoCatarata + AnatomíaCatarata | Interna |
| Conjuntivitis | OjoConjuntivitis + AnatomiaConjuntivitis | Externa |
| Glaucoma | OjoGlaucoma + AnatomiaGlaucoma | Interna |
| Ojo Sano | OjoSano | Externa |

---

## Problemas Conocidos

| Problema | Causa | Estado |
|----------|-------|--------|
| `ApiNoLongerSupported` en consola | com.unity.ai.toolkit obsoleto | Ignorable — no afecta build |
| `Invalid worldAABB` en Editor | Simulación sin cámara real | Solo en Editor |
| `UInt64 cannot convert Int32` | Bug Unity 6 Editor drag&drop | Solo en Editor |
| ARCore tracking lost | Superficie sin textura o poca luz | Apuntar a superficie con textura |

---

## Instalación y Build

```bash
# 1. Clonar o abrir el proyecto en Unity 6000.4.9f1

# 2. Configurar API Key de Gemini
#    Inspector → ARMedicalAI → Gemini Api Key → pegar key

# 3. Build Settings
#    File → Build Settings → Android → Switch Platform

# 4. Player Settings
#    IL2CPP, ARM64, Internet Required

# 5. Conectar dispositivo Android con depuración USB

# 6. Build and Run
```

---

## Autores

Desarrollado en la **Universidad Nacional de Loja**
Abel Mora
Steven Luna
Brian Aguinsaca
Santiago Guachizaca
2026
