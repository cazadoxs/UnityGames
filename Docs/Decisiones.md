# Registro de decisiones técnicas

Cada decisión con su fecha, contexto y consecuencias. Las nuevas se añaden al final.

| # | Fecha | Decisión | Motivo | Consecuencias |
|---|---|---|---|---|
| D-001 | 2026-10-31 | La lógica de reglas e IA de alto nivel vive en **`Estandartes.Core`**, C# puro sin dependencias de Unity (`noEngineReferences`), netstandard2.1 y C# 9 | Se puede compilar y probar sin el editor (CI sin licencia), y las funciones estáticas sin tipos gestionados se pueden llamar desde Burst | Las partes masivas (hash espacial, flow fields, slots) tienen una implementación de referencia en Core y su versión con jobs en `Sim`; las pruebas de Core sirven de especificación |
| D-002 | 2026-10-31 | JSON con **Newtonsoft** (`com.unity.nuget.newtonsoft-json` en Unity, NuGet 13.0.2 fuera) | Mismo comportamiento dentro y fuera de Unity; soporta diccionarios, enums como texto y parches | Campos desconocidos son error (`MissingMemberHandling.Error`) para detectar erratas en mods |
| D-003 | 2026-10-31 | El **contenido base son archivos JSON** en `StreamingAssets/Mods/estandartes/`, no ScriptableObjects | Principio "el juego base es un mod más" (Vol. V §2): se carga con el mismo cargador que los mods | Sustituye a la idea de ScriptableObjects del paso 1.2 de la guía. Si hace falta edición visual, se harán editores que lean y escriban estos JSON |
| D-004 | 2026-10-31 | Simulación a **30 Hz fijos** en `FixedStepSimulationSystemGroup`, con posiciones propias (`SimPosition`) e interpolación en presentación | Coste predecible y determinismo (Vol. III §17) | `LocalTransform` solo lo escribe la presentación |
| D-005 | 2026-10-31 | **Versiones de paquetes** del `manifest.json` escritas sin poder comprobarlas | El PC estaba apagado | Revisar al abrir el proyecto (Docs/PendienteDeVerificar.md, paso 3) |
| D-006 | 2026-10-31 | El generador de slots existe dos veces: `Core.Formations.FormationSlots` (referencia, con pruebas) y `Sim.BurstFormation` (Burst) | El de Core usa `List<T>`, que Burst no admite | Cualquier cambio de algoritmo se hace en los dos y se cubre con las pruebas de Core |
| D-007 | 2026-10-31 | Esquemas JSON **generados desde el código** (`Tools/DotNet/SchemaGen`) | Nunca se desincronizan de las definiciones | La CI falla si los esquemas no están regenerados |
| D-008 | 2026-10-31 | Mapas de referencia por defecto: URP Forward+; la comparación con HDRP sigue pendiente (paso 1.13) | Escalabilidad con miles de instancias | Decisión final tras el prototipo |
