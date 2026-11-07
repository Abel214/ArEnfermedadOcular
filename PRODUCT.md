# Product

<!-- impeccable:product-schema 1 -->

## Platform

android

## Users

Primary: health-science students (Universidad Nacional de Loja, Facultad de la Salud Humana) in classroom, lab, or self-study, using personal Android phones to study ocular pathology.

Job: see eye diseases spatially — select a condition, place its 3D model in the real environment, inspect anatomy/external views, and get short spoken/written explanations.

Other audiences (unconfirmed): professors demonstrating in class, patients/general public. Do not design for them until confirmed.

## Product Purpose

ARVISION (project name `EnfermedadesOcularesAR`, on-screen title `AR ENFERMEDAD OCULAR`) is an educational AR visualizer for ocular diseases. It exists to make eye pathology tangible where textbooks and slides stay flat.

Success means a student can, unaided: open the app, start AR, place a disease model, distinguish healthy vs. pathological structures, and recall symptoms/causes/treatment context afterward.

## Positioning

ScriptableObject-driven catalog (`Medical AR/Eye Disease` items) of healthy eye plus catarata / conjuntivitis / glaucoma in external and anatomy views, placed via AR Foundation with explode-view inspection, bilingual UI, and voice + Gemini AI short explanations. A neighboring study app could not truthfully copy the AR placement + anatomy explode + voice Q&A loop bound to curated medical context per item.

## Operating Context

Workflow: `MenuPrincipal.unity` (Tutorial / Empezar / Salida, Settings, Info) → `InterfazModelosOjosCompletos.unity` (static presentation → items menu/scrollbar → AR placement → confirm/position controls → AI/voice query).

Environments: indoor classrooms/labs in Loja - Ecuador, 2026; handheld Android ARCore devices (min SDK 30), camera + microphone required, variable lighting and space.

Rituals/tools: Unity 6000.4.9f1, URP, AR Foundation 6.4.3 (ARCore + ARKit packages present), XR Interaction Toolkit 3.4.1, DOTween-animated menus, TextMesh Pro UI, Android TTS / `AndroidVoiceRecognizer`, volume control.

## Capabilities and Constraints

Confirmed:

- Catalog: `Assets/ScripteableObject/` — `OjoSano`, `OjoCatarata`, `OjoConjuntivitis`, `OjoGlaucoma`, `AnatomiaCatarata`, `AnatomiaConjuntivitis`, `AnatomiaGlaucoma` (each: `itemName`, `itemDescription`, `item3DModel`, `medicalContext`, `isAnatomyView`).
- AR placement and positioning via `ARInteractionManager`, `ARHUDManager`, `OjoExplodeView`; `GameManager` states: MainMenu / ItemsMenu / ArPosition / AIMenu.
- AI explanation via `ARMedicalAI` → Gemini 1.5 Flash (`gemini-1.5-flash:generateContent`), 2–3 sentence patient-facing answers from `medicalContext`; TTS in Spanish.
- Bilingual UI (ES/EN) via `LocalizationManager` + `LocalizedUIText`; persisted `tts_idioma`; keys include `titulo_app`, `btn_empezar`, `btn_tutorial`, `btn_salida`, `labeldescripcion`.
- Main menu, tutorial/instructions panel, settings (voice volume, language), info/about panel, exit.

Constraints:

- Target: Android ARCore first; iOS/ARKit packages installed but not confirmed as ship target — treat as undecided.
- Online Gemini dependency for AI answers; no API key bundled in repo; offline fallback behavior undecided.
- Medical accuracy must come from curated `medicalContext` per item; no invented conditions, testimonials, or efficacy claims.
- Terminology: Spanish source (`Enfermedad Ocular`, `Empezar`, `Salida`, `Tutorial`); product names `ARVISION` / `AR ENFERMEDAD OCULAR`.

Explicitly undecided: clinical validation standard, target Android device list, minimum AR space/lighting guidance, offline content scope, data privacy handling for voice/AI queries.

## Brand Commitments

- Names: `ARVISION` (PlayerSettings `productName`), `EnfermedadesOcularesAR` (project), on-screen `AR ENFERMEDAD OCULAR` (`TituloAPP.png`).
- Owner voice: `Universidad Nacional de Loja`, `Facultad de la Salud Humana`, `Loja - Ecuador, 2026` (localization keys `labeluniversidad`, `labelfacultad`, `labelanio`).
- Assets: `Assets/Sprite/TituloAPP.png`, `EyeImage.jpg`; no binding aesthetic direction beyond existing Unity UI.
- Tone: educational, patient-facing, Spanish-first.

## Evidence on Hand

- Runnable Unity scenes: `Assets/Scenes/MenuPrincipal.unity`, `Assets/Scenes/InterfazModelosOjosCompletos.unity`, `Assets/Scenes/Interfaz.unity`.
- Disease definitions: 7 `.asset` files in `Assets/ScripteableObject/` listed above.
- Core scripts: `Assets/Scripts/GameManager.cs`, `UIManager.cs`, `MenuPrincipalManager.cs`, `ARMedicalAI.cs`, `ARInteractionManager.cs`, `Item.cs`, `LocalizationManager.cs`, `AndroidVoiceRecognizer.cs`, `VoiceComanderManage.cs`.
- Config: `ProjectSettings/ProjectSettings.asset` (`productName: ARVISION`, Android min SDK 30), `Packages/manifest.json` (AR Foundation 6.4.3, URP 17.4.0).
- Absences future work must not fabricate: no clinical trial data, no testimonials, no benchmark efficacy, no pricing/licensing, no production icon/splash finalization.

## Product Principles

1. Spatial first: every disease must be placeable and inspectable in AR, not just readable.
2. Curated truth over generated breadth: AI answers stay inside per-item `medicalContext` and stay short.
3. Bilingual by default: no Spanish-only or English-only dead ends.
4. Classroom-robust: large touch targets, high-contrast AR HUD, tolerant voice input, clear tutorial.
5. University-grade provenance: Loja authorship, condition scope, and evidence gaps stay visible.

## Accessibility & Inclusion

- Spanish/English UI toggle required; TTS voice output with volume control (`ControlVolumen`, `labelvoz`).
- Touch + voice redundant paths for core actions (select, place, ask).
- No WCAG or clinical accessibility standard confirmed — do not claim compliance.
