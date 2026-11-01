# Pendiente de verificar en el PC

Todo lo de esta lista se escribió **sin abrir Unity** (el PC estaba apagado). La lógica pura (`Estandartes.Core`) ya está compilada y probada fuera de Unity (80 pruebas en verde); el código que depende de Unity solo tiene la **sintaxis** comprobada. Al encender el PC, seguir estos pasos **en orden**.

## 1. Preparar el PC

Seguir el [Volumen IV, pasos 0.1 y 0.2](GDD/04_Guia_de_Construccion.md#fase-0-preparar-el-entorno): Unity Hub, la última Unity 6 LTS con *Windows Build Support (IL2CPP)*, IDE, Git + Git LFS, Blender, ComfyUI, FMOD, MCP de Unity y de Blender.

## 2. Clonar el repositorio

```bash
git clone https://github.com/cazadoxs/UnityGames.git
```

```bash
cd UnityGames && git lfs install && git lfs pull && git checkout claude/inspiring-sagan-3nhksi
```

## 3. Abrir el proyecto

1. Unity Hub → *Add* → *Add project from disk* → carpeta `UnityGames/Estandartes`.
2. Elegir la Unity 6 LTS instalada. Unity genera `ProjectSettings/` y resuelve los paquetes de `Packages/manifest.json`.
3. **Si algún paquete no resuelve** (las versiones del manifiesto se pusieron sin poder comprobarlas): *Window → Package Manager*, actualizar cada paquete a la versión que ofrezca el editor y guardar el `manifest.json` resultante. Paquetes: URP, Entities, Entities Graphics, Unity Physics, Burst, Collections, Mathematics, Input System, Cinemachine, AI Navigation, Animation Rigging, Addressables, Localization, Newtonsoft Json, Test Framework, Performance Testing, Profile Analyzer, Memory Profiler.
4. Al no partir de la plantilla URP, crear la configuración de render: *Assets → Create → Rendering → URP Asset (with Universal Renderer)* en `Assets/_Project/Settings/`, asignarlo en *Project Settings → Graphics* y en cada nivel de *Quality* (Bajo, Medio, Alto, Épico).
5. Aplicar el resto de la configuración del [paso 0.5](GDD/04_Guia_de_Construccion.md#paso-05--configuración-del-proyecto).

## 4. Compilar y corregir

Archivos escritos sin compilar (por orden de dependencia):

| Archivo | Qué hace | Cómo se valida |
|---|---|---|
| `Code/Sim/Components/SoldierComponents.cs` | Componentes ECS del soldado (Vol. III §3.2) | Compila |
| `Code/Sim/Components/SquadComponents.cs` | Escuadra, comandante, reloj de simulación | Compila |
| `Code/Sim/Components/SpatialHash.cs` | Hash espacial como singleton | Compila |
| `Code/Sim/Systems/SimulationGroups.cs` | Grupos por tick (Vol. III §4), tick fijo a 30 Hz, reloj | En *Play*, `FixedStepSimulationSystemGroup` a 1/30 s en *Systems* |
| `Code/Sim/Systems/SpatialHashSystem.cs` | Reconstrucción paralela del hash | Sin errores de seguridad de jobs en consola |
| `Code/Sim/Systems/BattleSpawnSystem.cs` | Genera dos ejércitos en escuadras | Aparecen los soldados (paso 6) |
| `Code/Sim/Systems/FormationSystems.cs` | Slots (versión Burst del generador de Core), ancla, guiado del soldado con separación | Las escuadras forman en línea |
| `Code/Sim.AI/AILodSystem.cs` | LOD de IA por distancia al foco con histéresis | Depurar `AILod.Level` en el inspector de entidades |
| `Code/Sim.AI/MoraleSystem.cs` | Moral continua y contagio (usa `Core.Morale.MoraleModel` desde Burst) | Sin errores de Burst; moral estable sin eventos |
| `Code/Presentation/InterpolationSystem.cs` | Interpolación entre ticks | Movimiento fluido a más de 60 FPS |
| `Code/Authoring/SoldierAuthoring.cs` | Prefab de soldado y generador de batalla | El *baking* no da errores |
| `Code/Game/ContentBootstrap.cs` | Carga juego base y mods al arrancar | Mensaje "Contenido cargado: …" sin errores |
| `Code/Tools/ContentMenu.cs` | Menú *Estandartes → Validar contenido base y mods* | Diálogo "Contenido correcto" |

Puntos con más riesgo de necesitar ajustes de API: `SystemAPI.QueryBuilder().WithDisabled<Dead>()`, `[WithDisabled]` en `IJobEntity`, `SetComponentEnabled` en *bakers*, la llamada a funciones de `Estandartes.Core` desde Burst y el uso de `FixedList512Bytes` en `BurstFormation.Rings`.

## 5. Pruebas en Unity

*Window → General → Test Runner → EditMode → Run All*. Son las mismas pruebas que ya pasan con `dotnet test`; deben pasar igual dentro de Unity.

## 6. Primera escena (paso 1.1 y 1.3 de la guía)

1. Crear `Assets/_Project/Maps/Bench_Llanura/Bench_Llanura.unity` con un Terrain de 2.048 × 2.048 m.
2. Añadir una SubScene `BenchContent`.
3. Prefab `Soldier_Prueba`: cápsula + `SoldierAuthoring`.
4. En la SubScene, un objeto con `BattleSpawnerAuthoring` (prefab `Soldier_Prueba`, 2.500 por bando).
5. En la escena principal, un objeto con `ContentBootstrap`.
6. *Play*: deben aparecer 5.000 cápsulas en dos ejércitos que forman en línea. Abrir el *Profiler* y anotar tiempos en `Docs/Benchmarks.md`.

## 7. Integración continua con Unity

Activar la licencia de Unity como indica la documentación de GameCI, guardar `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD` en los *secrets* del repositorio y lanzar a mano el flujo **Unity**. El flujo **Núcleo y contenido** ya funciona sin licencia.

## 8. Seguir con la guía

Continuar el [Volumen IV](GDD/04_Guia_de_Construccion.md) desde el paso 1.4 (lo hecho hasta aquí cubre parcialmente los pasos 1.2, 1.3, 1.5, 1.7 y 1.10).

## 9. Arte

- Generar el concept art de **Agincourt** con ComfyUI (se agotaron los créditos de Canva). Ver `Docs/Arte/Concept_Art.md`.
- Revisar los wireframes de Figma y pasar al diseño visual (paso 4.10).
