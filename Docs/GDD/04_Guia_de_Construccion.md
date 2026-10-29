# ESTANDARTES — GDD · Volumen IV: Guía de construcción paso a paso

> Parte del [GDD de Estandartes](README.md). Versión 0.2 (29-10-2026).
> Cómo construir el juego desde un repositorio vacío hasta el lanzamiento en Steam. Está escrita para que la pueda seguir **una persona** o **Claude con control del PC de desarrollo** (con los MCP de Unity y Blender). Cada paso dice qué hacer, con qué herramientas, qué archivos se crean y cómo se comprueba que está terminado.

---

## Índice

- [1. Cómo usar esta guía](#1-cómo-usar-esta-guía)
- [Fase 0. Preparar el entorno](#fase-0-preparar-el-entorno)
- [Fase 1. Prototipo técnico de escala](#fase-1-prototipo-técnico-de-escala)
- [Fase 2. El héroe](#fase-2-el-héroe)
- [Fase 3. IA jerárquica, objetivos y Modo Libre rápido](#fase-3-ia-jerárquica-objetivos-y-modo-libre-rápido)
- [Fase 4. Vertical slice: Hastings y Château Gaillard](#fase-4-vertical-slice-hastings-y-château-gaillard)
- [Fase 5. Producción: niveles 3 a 5 y Modo Libre completo](#fase-5-producción-niveles-3-a-5-y-modo-libre-completo)
- [Fase 6. Alpha, Beta y optimización final](#fase-6-alpha-beta-y-optimización-final)
- [Fase 7. Lanzamiento](#fase-7-lanzamiento)
- [Después del lanzamiento: añadir una época](#después-del-lanzamiento-añadir-una-época)
- [Anexo. Convenciones del proyecto](#anexo-convenciones-del-proyecto)

---

## 1. Cómo usar esta guía

### 1.1 Formato de cada paso

| Campo | Significado |
|---|---|
| **Objetivo** | Qué existe al terminar el paso |
| **Herramientas** | Programas, MCP y modelo de Claude recomendado |
| **Instrucciones** | Acciones concretas, en orden |
| **Archivos** | Qué se crea o modifica |
| **Aceptación** | Pruebas que deben pasar para dar el paso por terminado |

Las secciones del GDD que define cada paso se citan como *Vol. III §9.2* (volumen y sección).

### 1.2 Definición de "terminado"

Un paso solo está terminado cuando: compila sin avisos nuevos, sus pruebas pasan (locales y en CI), las escenas de rendimiento no empeoran más de un 5 %, el código está revisado y la rama está integrada en `main`.

### 1.3 Flujo de trabajo con Claude

1. Crear una rama por paso: `feature/f1-07-formaciones`.
2. Pedir el paso a Claude con la plantilla de abajo, eligiendo el modelo según la tabla.
3. Claude implementa, escribe las pruebas y las ejecuta (con el MCP de Unity o en modo *batch* desde la línea de comandos).
4. Revisión del código (otra pasada de Claude con Opus 5.5 en esfuerzo alto para los sistemas críticos).
5. Integrar en `main` cuando se cumpla la aceptación.

**Plantilla de petición:**

```text
Proyecto Estandartes (Unity 6 LTS, DOTS). Lee:
- Docs/GDD/04_Guia_de_Construccion.md, paso <X.Y>
- Docs/GDD/03_Arquitectura_IA.md §<secciones>
Tarea: implementar el paso <X.Y> tal como está descrito.
Restricciones: Burst y jobs en lo masivo, sin memoria gestionada en bucles,
assembly <Estandartes.Sim | ...>, pruebas EditMode/PlayMode incluidas.
Terminado cuando: se cumplan todas las pruebas de aceptación del paso.
```

| Tipo de paso | Modelo recomendado |
|---|---|
| Arquitectura, sistemas DOTS críticos, IA de alto nivel, revisiones | **Opus 5.5** (esfuerzo alto) |
| Funcionalidades de juego, UI, herramientas de editor, escenas con el MCP de Unity, scripts de Blender | **Sonnet 5.5** |
| Datos en masa, tablas de stats, textos del códice, localización, organización de assets, análisis de registros | **Haiku 5.5** |

Si un MCP no está disponible, las mismas acciones se hacen a mano siguiendo las instrucciones de cada paso.

---

## Fase 0. Preparar el entorno

Duración orientativa: 1 mes (junto con la pre-producción). **Aceptación de la fase:** el proyecto abre sin errores, los paquetes están instalados, una prueba vacía pasa en local y en CI, y todo está en el repositorio.

### Paso 0.1 — Equipo de desarrollo

- **Objetivo:** un PC capaz de ejecutar Unity, Blender y ComfyUI a la vez.
- **Recomendado:** CPU de 8 núcleos o más, **32 GB de RAM** (64 GB si se usa ComfyUI con vídeo e imagen→3D), GPU NVIDIA con **12–24 GB de VRAM** (ComfyUI la necesita), SSD NVMe de 1–2 TB.

### Paso 0.2 — Instalar el software

| Programa | Notas |
|---|---|
| **Unity Hub** | Desde unity.com. Iniciar sesión con la cuenta de Unity |
| **Unity 6 LTS** | En Unity Hub → *Installs* → *Install Editor*: la última versión **LTS** de Unity 6. Módulos: *Windows Build Support (IL2CPP)* y *Documentation*. Comprobar que la versión es compatible con el paquete Entities (ver su documentación) |
| **IDE** | Visual Studio 2022 con la carga de trabajo *Desarrollo de juegos con Unity*, o JetBrains Rider |
| **Git + Git LFS** | Git para Windows; después `git lfs install` |
| **Blender** | Última versión LTS |
| **ComfyUI** | Versión portable para Windows + ComfyUI-Manager. Modelos de imagen, imagen→3D y vídeo **a evaluar**, comprobando su licencia comercial (ver Vol. I §19.4) |
| **FMOD Studio** | La misma versión que el plugin *FMOD for Unity* |
| **Python 3.11** | Para scripts de herramientas (DEM, conversión de datos) |
| **QGIS o GDAL** | Para convertir modelos de elevación en mapas de alturas |
| **Claude** | Claude Code o Claude Desktop con los **MCP de Unity y de Blender** configurados |
| **Mixamo** | Cuenta de Adobe (se usa desde el navegador) |

### Paso 0.3 — Repositorio

- **Instrucciones:**
  1. Clonar `cazadoxs/UnityGames`.
  2. Crear `.gitignore` con la plantilla de Unity (ignorar `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`, `*.user`).
  3. Crear `.gitattributes` para Git LFS:
     ```text
     *.psd        filter=lfs diff=lfs merge=lfs -text
     *.png        filter=lfs diff=lfs merge=lfs -text
     *.jpg        filter=lfs diff=lfs merge=lfs -text
     *.tga        filter=lfs diff=lfs merge=lfs -text
     *.tif        filter=lfs diff=lfs merge=lfs -text
     *.exr        filter=lfs diff=lfs merge=lfs -text
     *.hdr        filter=lfs diff=lfs merge=lfs -text
     *.fbx        filter=lfs diff=lfs merge=lfs -text
     *.obj        filter=lfs diff=lfs merge=lfs -text
     *.blend      filter=lfs diff=lfs merge=lfs -text
     *.glb        filter=lfs diff=lfs merge=lfs -text
     *.wav        filter=lfs diff=lfs merge=lfs -text
     *.ogg        filter=lfs diff=lfs merge=lfs -text
     *.mp3        filter=lfs diff=lfs merge=lfs -text
     *.bank       filter=lfs diff=lfs merge=lfs -text
     *.mp4        filter=lfs diff=lfs merge=lfs -text
     *.mov        filter=lfs diff=lfs merge=lfs -text
     *.raw        filter=lfs diff=lfs merge=lfs -text
     *.r16        filter=lfs diff=lfs merge=lfs -text
     *.unity      merge=unityyamlmerge eol=lf
     *.prefab     merge=unityyamlmerge eol=lf
     *.asset      merge=unityyamlmerge eol=lf
     *.mat        merge=unityyamlmerge eol=lf
     *.anim       merge=unityyamlmerge eol=lf
     *.controller merge=unityyamlmerge eol=lf
     ```
  4. Configurar **UnityYAMLMerge** como herramienta de fusión para escenas y prefabs.
- **Aceptación:** `git lfs ls-files` funciona; un PNG de prueba se sube por LFS.

### Paso 0.4 — Crear el proyecto de Unity

- **Instrucciones:** Unity Hub → *New project* → versión Unity 6 LTS → plantilla **Universal 3D** (URP) → nombre `Estandartes` → ubicación: la carpeta del repositorio. Resultado: `UnityGames/Estandartes/`.
- **Aceptación:** el proyecto abre y la escena de ejemplo se reproduce.

### Paso 0.5 — Configuración del proyecto

| Dónde | Ajuste |
|---|---|
| *Project Settings → Player → Other Settings* | Color Space **Linear**; Scripting Backend **IL2CPP** para compilaciones de lanzamiento (Mono para iterar); Api Compatibility **.NET Standard 2.1**; Active Input Handling **Input System Package** |
| *Player → Other Settings → Rendering* | Desactivar *Auto Graphics API for Windows* y poner **Direct3D12** primero |
| *Project Settings → Editor* | *Enter Play Mode Settings*: no recargar dominio ni escena (recomendado por Entities para iterar rápido) |
| *URP Asset* | Rendering Path **Forward+**; SRP Batcher activo; activar **GPU Resident Drawer** (requiere la opción de variantes de BatchRendererGroup en *Graphics*) |
| *Project Settings → Quality* | Crear los niveles **Bajo, Medio, Alto, Épico**, cada uno con su URP Asset |
| *Project Settings → Time* | Fixed Timestep de física para el héroe: 0,02 s (la simulación ECS usa su propio tick de 1/30 s, paso 1.4) |

- **Aceptación:** la configuración está en `ProjectSettings/` y en el repositorio.

### Paso 0.6 — Paquetes

- **Instrucciones:** *Window → Package Manager → + → Install package by name…* e instalar:
  `com.unity.entities`, `com.unity.entities.graphics`, `com.unity.physics`, `com.unity.collections`, `com.unity.mathematics`, `com.unity.burst`, `com.unity.inputsystem`, `com.unity.cinemachine`, `com.unity.ai.navigation`, `com.unity.animation.rigging`, `com.unity.addressables`, `com.unity.localization`, `com.unity.test-framework`, `com.unity.test-framework.performance`, `com.unity.performance.profile-analyzer`, `com.unity.memoryprofiler`.
  Después, importar **FMOD for Unity** (desde la web de FMOD o la Asset Store).
- **Aceptación:** `Packages/manifest.json` contiene los paquetes; el proyecto compila sin errores.

### Paso 0.7 — Carpetas y *assemblies*

- **Instrucciones:** crear la estructura del Vol. I §18.3 y un `.asmdef` por capa. Ejemplo:
  ```json
  {
    "name": "Estandartes.Sim",
    "rootNamespace": "Estandartes.Sim",
    "references": [ "Unity.Entities", "Unity.Transforms", "Unity.Collections",
                    "Unity.Mathematics", "Unity.Burst" ],
    "allowUnsafeCode": true,
    "autoReferenced": false
  }
  ```

| Assembly | Referencia a | Plataformas |
|---|---|---|
| `Estandartes.Sim` | Paquetes DOTS | Todas |
| `Estandartes.Sim.AI` | `Estandartes.Sim` + DOTS | Todas |
| `Estandartes.Presentation` | `Sim`, `Sim.AI`, Entities Graphics, URP, FMOD | Todas |
| `Estandartes.Game` | `Sim`, `Sim.AI`, `Presentation`, Input System, Cinemachine, Localization, Addressables | Todas |
| `Estandartes.Authoring` | `Sim`, `Sim.AI` (componentes *authoring* y *bakers*) | Todas |
| `Estandartes.Tools` | Todo lo anterior | Solo Editor |
| `Estandartes.Tests.EditMode` / `.PlayMode` | Lo que prueben + Test Framework | Editor / Todas |

- **Aceptación:** cada assembly compila por separado; `Sim` no referencia a `Presentation` ni a `Game`.

### Paso 0.8 — Escena de arranque y SubScene

- **Instrucciones:** crear `Assets/_Project/Maps/_Bootstrap/Bootstrap.unity`; añadir una **SubScene** (*GameObject → New Sub Scene → Empty Scene*). Las SubScenes contienen los objetos *authoring* que se convierten (*bake*) en entidades.
- **Aceptación:** al pulsar *Play*, la ventana *Entities Hierarchy* muestra las entidades de la SubScene.

### Paso 0.9 — Integración continua

- **Instrucciones:** crear `.github/workflows/tests.yml` con las acciones de **GameCI** (`unity-test-runner`, más adelante `unity-builder`), *checkout* con LFS y caché de `Library/`. Activar la licencia de Unity como indica la documentación de GameCI y guardarla en los *secrets* del repositorio.
- **Archivos:** `.github/workflows/tests.yml`, `Assets/_Project/Code/Tests/EditMode/SmokeTests.cs` (una prueba vacía).
- **Aceptación:** la prueba vacía pasa en GitHub Actions.

---

## Fase 1. Prototipo técnico de escala

Duración orientativa: 2–3 meses. **Es la fase más importante:** demuestra que la escala es posible antes de producir contenido. Soldados provisionales (cápsulas y después un maniquí animado).

**Puerta de salida:** **5.000 soldados luchando a ≥ 60 FPS** en el equipo recomendado, simulación ≤ 8 ms de tiempo real por tick, sin picos de GC. Si no se cumple, se revisa el alcance (cupos, LOD, escala objetivo) antes de seguir.

### Paso 1.1 — Escena de pruebas

- **Instrucciones:** crear `Maps/Bench_Llanura/Bench_Llanura.unity` con un Terrain de 2.048 × 2.048 m, algunas colinas suaves, un lago y rocas como obstáculos; una SubScene `BenchContent`.
- **Aceptación:** la escena abre y se reproduce.

### Paso 1.2 — Datos mínimos

- **Objetivo:** primeras definiciones de unidades y armas como datos.
- **Instrucciones:** crear `UnitDefinition` y `WeaponDefinition` (ScriptableObject) con los campos de Vol. III §3.5 y §18; sus *bakers* a `UnitBlob` y `WeaponBlob`. Crear tres assets de prueba: `Lancero_Prueba`, `Espadachin_Prueba`, `Arquero_Prueba` con valores del Vol. I, Anexo A.
- **Archivos:** `Code/Sim/Data/UnitDefinition.cs`, `WeaponDefinition.cs`, `Code/Authoring/UnitDefinitionBaker.cs`, `Data/Units/*.asset`.
- **Aceptación:** prueba EditMode que hornea un `UnitDefinition` y lee sus valores desde el blob.

### Paso 1.3 — Componentes y generación de ejércitos

- **Objetivo:** 5.000 soldados en dos ejércitos, organizados en escuadras.
- **Instrucciones:**
  1. Implementar los componentes de Vol. III §3.2–§3.3 (`SoldierComponents.cs`, `SquadComponents.cs`).
  2. Prefab `Soldier_Prueba`: cápsula + `SoldierAuthoring` (añade los componentes con sus valores iniciales).
  3. `BattleSpawnerAuthoring` (soldados por bando, tamaño de escuadra, unidad) y `BattleSpawnSystem` (`ISystem`) que crea las escuadras e instancia los soldados **en lote** (`EntityManager.Instantiate(prefab, NativeArray<Entity>)`), asignando `SquadMember` y la personalidad aleatoria con semilla.
- **Aceptación:** al pulsar *Play* aparecen 5.000 cápsulas en dos ejércitos y la generación tarda < 1 s.

### Paso 1.4 — Tick fijo e interpolación

- **Instrucciones:**
  1. Fijar `FixedStepSimulationSystemGroup.Timestep = 1/30` en un sistema de arranque.
  2. La simulación guarda la posición en sus propios componentes (`SimPosition`, `SimRotation`) y conserva la del tick anterior.
  3. `InterpolationSystem` (en `PresentationSystemGroup`) escribe `LocalTransform` interpolando entre el tick anterior y el actual según el tiempo transcurrido.
- **Aceptación:** con la simulación a 30 Hz, el movimiento se ve fluido a 144 Hz; prueba de que la simulación no depende de los FPS.

### Paso 1.5 — Hash espacial

- **Instrucciones:** `SpatialHashSystem` (Vol. III §2.3 y Vol. I §9.6): cálculo de la celda de cada soldado, ordenación por conteo en paralelo, tablas de inicio y tamaño por celda. API de consulta `SpatialQuery` utilizable desde *jobs*.
- **Aceptación:** pruebas EditMode que comparan sus resultados con una búsqueda por fuerza bruta en conjuntos aleatorios; < 0,5 ms con 10.000 soldados.

### Paso 1.6 — Flow fields

- **Instrucciones:** implementar Vol. III §15.1–§15.3: rejilla de costes generada a partir del Terrain al cargar (alturas y pendientes muestreadas a `NativeArray`), sectores de 64 × 64, grafo de portales, *job* de integración, *job* de direcciones, caché LRU y cola con presupuesto. Dibujo de depuración de las flechas.
- **Aceptación:** 5.000 soldados cruzan el mapa rodeando el lago y las rocas; navegación ≤ 2 ms por tick; recalcular un sector < 0,1 ms.

### Paso 1.7 — Formaciones y slots

- **Instrucciones:** implementar Vol. III §9.2–§9.5: `FormationDefinition`, generadores (línea, columna, cuña, erizo, muro de escudos, orden abierto), `FormationSlotSystem`, `SlotAssignmentSystem`, `SquadMovementSystem` (ancla por flow field) y búsqueda de slot del soldado con controlador de llegada.
- **Aceptación:** pruebas EditMode de los generadores y de la asignación; en la escena, una escuadra marcha en columna, despliega en línea y gira 90° sin cruces caóticos; la cohesión se ve en la depuración.

### Paso 1.8 — Combate básico

- **Instrucciones:** línea de contacto, elección de rival (Vol. III §10.2), fases de ataque y bloqueo (§10.3, versión inicial), modelo de daño (Vol. I §7.7), muerte (desactivar el soldado e instanciar un cadáver barato).
- **Aceptación:** dos líneas de 1.000 soldados chocan; solo combate la primera fila, el resto espera o empuja; la batalla termina; sin picos de frame.

### Paso 1.9 — Proyectiles

- **Instrucciones:** `ProjectileSystem` con balística propia en Burst, colisión contra el hash espacial y las alturas del terreno, andanadas lejanas como "nubes" estadísticas, avisos de impacto para que los soldados levanten el escudo.
- **Aceptación:** 2.000 flechas en vuelo simultáneo en < 1 ms; las escuadras de arqueros disparan andanadas a otra escuadra.

### Paso 1.10 — LOD de IA y planificador

- **Instrucciones:** `AILodAssignSystem` (Vol. III §14), cubos de actualización escalonada y `Wake` (§17.2); superposición de depuración con un color por nivel.
- **Aceptación:** con 10.000 soldados, simulación ≤ 8 ms por tick; desde la cámara del jugador no se perciben los cambios de nivel.

### Paso 1.11 — Animación por GPU

- **Objetivo:** miles de personajes animados con coste de CPU casi nulo.
- **Instrucciones:**
  1. Evaluar en una rama aparte un paquete de animación para ECS de la Asset Store frente a una solución propia; decidir con datos.
  2. Solución propia: descargar de **Mixamo** un personaje y clips (*idle*, andar, correr, 4 ataques, bloqueo, golpe recibido, 3 muertes) en FBX a 30 fps; **hornear** las matrices de huesos de cada clip en texturas (RGBAHalf) con un script de editor o de Blender (MCP); *shader* de URP (Shader Graph con nodo *Custom Function*) que aplica el *skinning* en el vértice leyendo esas texturas y mezcla dos clips.
  3. Parámetros por instancia (clip, tiempo, mezcla) como componentes con `[MaterialProperty]` para Entities Graphics.
  4. Desfase y velocidad aleatorios por soldado.
- **Aceptación:** 5.000 personajes animados a ≥ 60 FPS en GPU en el equipo recomendado.

### Paso 1.12 — Escenas de rendimiento y perfilado

- **Instrucciones:** marcadores de perfilado (`ProfilerMarker`) en cada sistema; pruebas de rendimiento (`Measure.Frames()`) para 1.000 / 4.000 / 10.000 soldados; registrar resultados en `Docs/Benchmarks.md`; integrarlas en CI (en una máquina con GPU o en modo *headless* para la parte de simulación).
- **Aceptación:** informe de tiempos por sistema; la CI avisa si algo empeora más de un 5 %.

### Paso 1.13 — URP frente a HDRP

- **Instrucciones:** en la rama `spike/hdrp`, duplicar la escena de rendimiento con HDRP; comparar tiempos de GPU y calidad visual con iluminación de batalla (amanecer, niebla, lluvia).
- **Aceptación:** decisión documentada en `Docs/Decisiones.md`.

---

## Fase 2. El héroe

Duración orientativa: 2 meses. **Puerta:** el combate es divertido contra 1 y contra 10 enemigos de IA (*playtest*).

### Paso 2.1 — Entrada

- **Instrucciones:** asset `EstandartesInput.inputactions` con mapas `Hero`, `Orders` y `UI` según Vol. I §13 (teclado+ratón y mando); generar la clase C#.
- **Aceptación:** todas las acciones se leen en una escena de prueba; reasignación de teclas funcional.

### Paso 2.2 — Controlador del héroe

- **Instrucciones:** controlador cinemático (basado en `CharacterController`): andar, correr, saltar o esquivar, estamina, peso del equipo, pendiente, barro.
- **Aceptación:** el héroe recorre el terreno de la escena de pruebas sin atascarse; la estamina responde.

### Paso 2.3 — Cámaras

- **Instrucciones:** Cinemachine: cámara de tercera persona sobre el hombro (cambio de lado), cámara de primera persona y alternancia con `V`; cámara de apuntado para arco y ballesta.
- **Aceptación:** transiciones suaves; sin atravesar paredes.

### Paso 2.4 — Animación del héroe

- **Instrucciones:** personaje humanoide (Mixamo para empezar), Animator con *blend tree* de locomoción, capa superior para combate, IK de pies y manos con Animation Rigging.
- **Aceptación:** locomoción fluida en pendientes; las manos sujetan el escudo y el arma.

### Paso 2.5 — Combate cuerpo a cuerpo

- **Instrucciones:** dirección del ataque según el movimiento del ratón al pulsar; fases de preparación, golpe y recuperación **con los mismos tiempos que la IA** (Vol. III §10.3); bloqueo direccional, parada perfecta, golpe de escudo y patada. Detección de impacto con cápsulas a lo largo del recorrido del arma, consultando el **hash espacial** (los soldados no tienen colisionadores de PhysX).
- **Aceptación:** pruebas PlayMode de cada caso (bloqueo correcto, incorrecto, parada, golpe cargado); el jugador gana y pierde duelos de forma justa.

### Paso 2.6 — Armas a distancia

- **Instrucciones:** arco (tensado, temblor, balística, viento), ballesta (recarga con vulnerabilidad), jabalinas; los proyectiles del héroe se crean en el **mismo sistema de proyectiles** ECS que los de la IA; munición limitada.
- **Aceptación:** el héroe abate soldados de IA a distancia; la flecha cae según la física.

### Paso 2.7 — Puente héroe ↔ ECS

- **Instrucciones:** entidad espejo del héroe (`HeroProxy`) con los componentes de soldado para que la IA lo vea, lo elija como rival y le haga daño; los eventos de daño de ECS se aplican a la salud del héroe.
- **Aceptación:** una escuadra de IA rodea y ataca al héroe con las mismas reglas que a cualquier soldado.

### Paso 2.8 — Equipo y cambio de arma

- **Instrucciones:** 4 ranuras + escudo (Vol. I §6.3), cambio con `1`–`4` y rueda, tiempos reales de cambio, recoger armas del suelo.
- **Aceptación:** el héroe pasa de espada y escudo a arco y de vuelta sin errores de animación.

### Paso 2.9 — Órdenes a la escuadra

- **Instrucciones:** menú radial (UI Toolkit) y atajos `F1`–`F6`; las órdenes se envían por la cadena de mando (medio "voz", Vol. III §12).
- **Aceptación:** la escuadra del héroe obedece "seguidme", "mantened posición", "carga" y "formación".

### Paso 2.10 — Muerte y relevo

- **Instrucciones:** al morir, elegir un soldado aliado cercano; convertirlo en el nuevo héroe (crear el héroe en su posición con su equipo y retirar la entidad del soldado); modo "Clásico" con puntos de control.
- **Aceptación:** el relevo tarda < 1 s y no rompe la escuadra.

---

## Fase 3. IA jerárquica, objetivos y Modo Libre rápido

Duración orientativa: 3 meses. **Puerta:** batallas IA contra IA creíbles en una llanura y en un castillo de pruebas; métricas de Vol. III §19.3 dentro de los umbrales.

| Paso | Contenido | Referencia |
|---|---|---|
| **3.1** | Máquina de estados de escuadra completa | Vol. III §9.1 |
| **3.2** | Tácticas de formación: erizo, muro de escudos, rotación de filas, reformar | Vol. III §9.4–§9.7 |
| **3.3** | Caballería: carga, impacto, reagrupar y volver a cargar | Vol. III §9.7 |
| **3.4** | Tiradores: hostigar, andanadas, disciplina de fuego, cambio a melé | Vol. III §10.4, §9.7 |
| **3.5** | Moral, contagio, huida, rendición y reagrupación | Vol. III §11 |
| **3.6** | Cadena de mando: medios, retrasos, músico, estandarte, mensajeros, promociones | Vol. III §12 |
| **3.7** | Mapas de influencia y su superposición de depuración | Vol. III §7 |
| **3.8** | Comandantes: tareas, curvas de utilidad (assets), asignación, sincronización, apoyo | Vol. III §8 |
| **3.9** | Modelo agregado L2/L3 y calibración *headless* | Vol. III §16 |
| **3.10** | General: instantánea, creencias, dominio HTN, Monte Carlo, personalidades | Vol. III §6 |
| **3.11** | Zonas y objetivos genéricos: `ObjectiveGraph`, volúmenes de zona, captura, efectos y condiciones de victoria | Vol. I §4.4–§5 |
| **3.12** | Director de batalla | Vol. III §13 |
| **3.13** | Modo Libre rápido: algoritmo de reparto y pantalla mínima | Vol. I §4.3, Anexo B |
| **3.14** | Herramientas de depuración y escenarios de regresión | Vol. III §19 |
| **3.15** | Navegación multicapa para murallas y torres | Vol. III §15.4 |

**Instrucciones clave:**

- **3.9 Calibración headless:** crear `Estandartes.Tools.Headless.RunCalibration` y ejecutarlo en modo *batch*:
  ```text
  Unity.exe -batchmode -nographics -projectPath <ruta>\Estandartes ^
            -executeMethod Estandartes.Tools.Headless.RunCalibration -quit -logFile calib.log
  ```
  El método recorre los pares de arquetipos, simula en L1 con varias semillas, ajusta los coeficientes y guarda `CombatCalibration.asset`.
- **3.10 General:** el cerebro vive en un hilo propio (`Thread` con prioridad baja) iniciado por `StrategicHostSystem`; las simulaciones Monte Carlo son métodos estáticos `[BurstCompile]` llamados directamente; los resultados vuelven por una cola concurrente y se aplican en `StrategicSyncSystem`.
- **3.11 Objetivos genéricos:** el grafo admite cualquier combinación de tipos de zona y de objetivo (Vol. I §5.1); prueba con al menos cuatro mapas de prueba distintos (llanura con 3 puntos, puente, castillo de 3 recintos, colina).
- **3.13 Modo Libre rápido:** implementar el algoritmo del Anexo B como función pura con pruebas EditMode (incluido el ejemplo resuelto del anexo); pantalla con UI Toolkit: mapa, dos cantidades, facciones y botón de batalla.

---

## Fase 4. Vertical slice: Hastings y Château Gaillard

Duración orientativa: 3 meses. **Puerta:** los niveles 1 y 2 (Vol. II) completos con calidad final; representan el juego terminado.

### Paso 4.1 — Biblia de arte

- **Instrucciones:** `Docs/Arte/Biblia_Edad_Media.md` con referencias por cultura y siglo (anglosajones y normandos del s. XI; franceses y angevinos de 1204), paleta por bando, heráldica, materiales, presupuesto de polígonos y texturas por LOD.
- **Herramientas:** ComfyUI (tableros de concept), Figma o Canva (paneles de referencia).

### Paso 4.2 — Pipeline de assets

- **Instrucciones:**
  1. Guardar en el repositorio los flujos de ComfyUI (`Tools/ComfyUI/*.json`): concept de personajes, texturas tileables, heráldica, imagen→3D de props.
  2. Scripts de Blender (`Tools/Blender/`): limpieza y retopología, UV, generación de LOD0–3, impostores, horneado de animaciones a texturas, pre-fractura, exportación por lotes a FBX.
  3. *Presets* de importación de Unity por tipo de asset y convenciones de nombres (ver el anexo).
- **Aceptación:** un prop (el carro de suministros del Vol. I §19.3) recorre todo el pipeline y entra en el juego.

### Paso 4.3 — Personajes modulares

- **Instrucciones:** cuerpo base humanoide + kits modulares por cultura (cabeza, torso, brazos, piernas, escudo, sobreveste). Partir de paquetes de la Asset Store compatibles y completar con piezas propias. Variación por *shader* (colores heráldicos, suciedad). Hornear LOD e impostores.
- **Aceptación:** 4 culturas listas (anglosajones, normandos, franceses e ingleses de 1204); 500 soldados distintos en pantalla sin repetición evidente.

### Paso 4.4 — Animaciones de combate

- **Instrucciones:** clips de Mixamo reorientados en Blender al esqueleto del juego; completar con lo que falta para la época (postura de muro de escudos, estocada de lanza, recarga de ballesta, empujar escalas) mediante paquetes de *mocap* o captura propia; hornear a texturas para multitudes.
- **Aceptación:** la tabla de animaciones necesarias de los niveles 1 y 2 está completa.

### Paso 4.5 — Caballos y montura del héroe

- **Instrucciones:** modelos y animaciones de caballo, caballería de IA renderizada por GPU, controlador de montura del héroe (Vol. I §6.6).
- **Aceptación:** el héroe monta, carga y desmonta; una escuadra de caballería de IA carga y se reagrupa.

### Paso 4.6 — Terreno de Hastings

- **Instrucciones:**
  1. Descargar el modelo de elevación de la zona (Copernicus DEM GLO-30 u OpenTopography).
  2. Recortar y convertir a mapa de alturas de 16 bits (por ejemplo, con GDAL: `gdal_translate -ot UInt16 -scale <min> <max> 0 65535 -of ENVI entrada.tif hastings.r16`).
  3. Importar en un Terrain de Unity, retocar la cresta de Senlac y el cerrillo, eliminar elementos modernos.
  4. Texturas del terreno (tileables de ComfyUI convertidas a PBR), vegetación instanciada, cielo y niebla de otoño.
- **Aceptación:** la cresta y la ladera se corresponden con el mapa del Vol. II; rendimiento dentro del presupuesto con 4.000 soldados.

### Paso 4.7 — Nivel 1: Hastings

- **Instrucciones:** asset `BattleScenario_Hastings`: ejércitos (Vol. II, ficha), comandantes y personalidades, grafo de objetivos, eventos guionizados (huida bretona, rumor de la muerte de Guillermo, anochecer), briefing y epílogo, objetivos secundarios. Orden "Retirada fingida" disponible para la escuadra del héroe.
- **Aceptación:** el nivel se juega de principio a fin; las cinco fases funcionan con distintas semillas; la IA anglosajona persigue o no según su disciplina.

### Paso 4.8 — Castillo y nivel 2: Château Gaillard

- **Instrucciones:** kit modular de castillo; los tres recintos con fosos y puentes; navegación multicapa (adarves, torres, escaleras, escalas); torre del recinto exterior y tramos pre-fracturados (fractura de celdas en Blender → variantes de prefab); mecánica de zapa; ruta de infiltración por la capilla; tronco de asalto; asset `BattleScenario_ChateauGaillard` con sus fases y "días".
- **Aceptación:** la cadena completa de objetivos funciona por las dos rutas de la fase 3; la guarnición se repliega con orden y se rinde al final.

### Paso 4.9 — Audio

- **Instrucciones:** proyecto de FMOD con música adaptativa, audio de multitudes por capas (Vol. I §9.15), efectos de armas, *barks* por facción con prioridades, narración del cronista.
- **Aceptación:** la batalla "suena" según la distancia al choque; no se superan las voces simultáneas del presupuesto.

### Paso 4.10 — Interfaz

- **Instrucciones:** diseñar en **Figma** el HUD, el menú radial, el mapa táctico, los menús y el briefing (Vol. I §12); implementarlos con **UI Toolkit** (UXML y USS).
- **Aceptación:** flujo completo de pantallas (Vol. I §11) para los niveles 1 y 2; modo inmersivo sin HUD.

### Paso 4.11 — Pulido y *playtest*

- **Instrucciones:** sesiones de prueba con jugadores; medir credibilidad de la IA, dificultad y rendimiento; corregir.
- **Aceptación:** los criterios de la puerta de la fase.

---

## Fase 5. Producción: niveles 3 a 5 y Modo Libre completo

Duración orientativa: 8–12 meses.

| Paso | Contenido | Necesidades específicas |
|---|---|---|
| **5.1** | Culturas restantes: castellanos, aragoneses, navarros, almohades, escoceses, ingleses y franceses del s. XV | Kits modulares, heráldica, voces |
| **5.2** | Nivel 3: Las Navas de Tolosa | Terreno de Sierra Morena, fatiga por calor, fase de marcha con patrullas, coalición de tres alas con personalidades, palenque fortificado |
| **5.3** | Nivel 4: Puente de Stirling | Puente con capacidad limitada (enlace de navegación), calzada, terreno blando, espera oculta con impaciencia, cálculo del momento del ataque |
| **5.4** | Nivel 5: Agincourt | Mecánica de estacas, barro progresivo, ajuste fino de la presión de masa y la aglomeración, andanadas de arco largo, ataque al campamento |
| **5.5** | Modo Libre detallado (cantidad por tipo, puntos) | Vol. I §4.3 nivel 2 |
| **5.6** | Planificador del Modo Libre | Colocar escuadras en zonas, papeles y órdenes iniciales, plantillas de zonas y zonas personalizadas, guardar y compartir escenarios |
| **5.7** | Mapas genéricos para el Modo Libre | Llanura, colinas, río con puente, bosque, pueblo, castillo |
| **5.8** | Códice, progresión, desbloqueos y estrellas | Vol. I §16–§17 |
| **5.9** | Guardado, opciones, accesibilidad, localización ES/EN | Vol. I §12.3, §18.5 |

**Aceptación de la fase:** los 5 niveles y el Modo Libre completos; todas las facciones jugables.

---

## Fase 6. Alpha, Beta y optimización final

Duración orientativa: 3–4 meses.

| Paso | Contenido |
|---|---|
| **6.1** | Pase de rendimiento por preset y por nivel (Profiler, Memory Profiler, GPU) hasta cumplir Vol. I §9.1 |
| **6.2** | Carga y memoria: Addressables por mapa y cultura, tiempos de carga < 30 s en SSD |
| **6.3** | Balance con torneos *headless* (miles de batallas) y recalibración de L2/L3 |
| **6.4** | Integración con Steam (Steamworks.NET): logros, nube de guardado, superposición |
| **6.5** | *Playtests* externos (Steam Playtest) y gestión de errores |
| **6.6** | Página de la tienda, cápsulas y tráiler (Canva/Adobe; capturas con Unity Recorder; borradores de vídeo con ComfyUI) |

**Aceptación:** sin errores críticos ni bloqueantes; objetivos de rendimiento cumplidos en todos los presets.

---

## Fase 7. Lanzamiento

- Compilación IL2CPP x64 de lanzamiento desde la CI.
- Informe de cierres inesperados (*crash reporting*) activo.
- Subida a Steam con SteamPipe (`steamcmd`), ramas *beta* y *default*.
- Cuestionario de contenido de Steam con la **declaración de contenido generado con IA**.
- Lista de comprobación de lanzamiento: requisitos de sistema publicados (Vol. I §23), idiomas, logros, guardado en la nube, accesibilidad.
- Opción de **Acceso Anticipado** tras la fase 4 o 5 si se decide así.

---

## Después del lanzamiento: añadir una época

Lista de comprobación para cada expansión (Japón feudal, era de la pólvora, era del rifle, Antigüedad):

1. **Investigación:** batallas candidatas (Vol. I §21), fuentes, mapas, uniformes y armas.
2. **Datos:** nuevos `UnitDefinition` y `WeaponDefinition` con sus arquetipos y capacidades (Vol. III §20).
3. **Conductas nuevas** solo para las capacidades nuevas (recarga, fuego por filas, humo, cobertura, duelos…), con sus pruebas.
4. **Doctrinas y personalidades** de las facciones de la época.
5. **Culturas visuales** (biblia de arte de la época, kits modulares, animaciones).
6. **Mapas y campaña** (un volumen de niveles nuevo, como el Vol. II).
7. **Calibración** de L2/L3 con los nuevos arquetipos.
8. **Modo Libre:** las unidades de la nueva época se añaden al selector; modo "¿Y si…?" entre épocas.

---

## Anexo. Convenciones del proyecto

### Nombres

| Tipo | Convención | Ejemplo |
|---|---|---|
| Código | Inglés, PascalCase, un tipo por archivo | `SquadMovementSystem.cs` |
| Assets de datos | `Tipo_Nombre` | `Unit_LanceroCastellano.asset` |
| Modelos | `SM_` (estático), `SK_` (con esqueleto) + cultura + pieza + LOD | `SK_Normando_Torso_A_LOD0.fbx` |
| Texturas | `T_` + nombre + sufijo de mapa | `T_Malla_Hierro_N.png` |
| Materiales | `M_` + nombre | `M_Escudo_Normando.mat` |
| Escenas | `Mapa_Nombre` / `Bench_Nombre` | `Mapa_Hastings.unity` |

### Ramas y *commits*

- `main` siempre compila y pasa las pruebas.
- Ramas `feature/fX-YY-nombre`, `fix/…`, `spike/…`.
- Mensajes de *commit* en imperativo y en una línea de resumen, con el paso de la guía cuando aplique (`F1.07: generadores de slots y asignación`).

### Revisión de código (lista)

- ¿Respeta las capas (Sim no depende de Presentation)?
- ¿Lo masivo está en *jobs* con Burst? ¿Sin memoria gestionada en bucles?
- ¿Usa RNG con semilla por entidad?
- ¿Tiene pruebas y marcadores de perfilado?
- ¿Respeta el presupuesto del sistema (Vol. I, Anexo D)?
EOF
wc -l Docs/GDD/04_Guia_de_Construccion.md