# GDD de Estandartes

Documento de Diseño del Juego de **Estandartes**, una saga de batallas históricas masivas para **PC** hecha en **Unity 6**. El primer juego es ***Estandartes: Edad Media***.

**Versión:** 0.3 (30-10-2026) · **Estado:** pre-producción

## Volúmenes

| Volumen | Contenido | Para quién |
|---|---|---|
| [**I. Diseño general**](01_Diseno_General.md) | Visión de la saga, modos de juego, zonas y objetivos, el héroe, unidades y épocas, IA (visión de jugador), optimización, mapas, interfaz, arte, audio, tecnología, pipeline con IA, hoja de ruta, riesgos y anexos | Todo el equipo |
| [**II. Niveles de la Edad Media**](02_Niveles_Edad_Media.md) | Los 5 niveles del Modo Historia del primer juego: Hastings, Château Gaillard, Las Navas de Tolosa, Puente de Stirling y Agincourt | Diseño de niveles, narrativa, arte |
| [**III. Arquitectura de la IA**](03_Arquitectura_IA.md) | Datos ECS, orden de ejecución, general (HTN + Monte Carlo), mapas de influencia, comandantes (utility AI), escuadras, soldados, moral, cadena de mando, Director, LOD de IA, navegación, modelos estadísticos, hilos y determinismo, extensión a otras épocas | Programación |
| [**IV. Guía de construcción**](04_Guia_de_Construccion.md) | Cómo construir el juego paso a paso, de la preparación del PC al lanzamiento en Steam y a la fase de mods, con pruebas de aceptación por paso | Quien construya el juego (persona o Claude) |
| [**V. Constructor de mapas y mods**](05_Mapas_y_Mods.md) | Constructor de mapas para la comunidad dentro del juego, soporte para cargar mods (datos, assets, mapas, escenarios, campañas), Steam Workshop, reglas, límites, seguridad y plan de implementación | Diseño, programación, comunidad |

## Orden de lectura recomendado

1. Volumen I, secciones 0–7 (qué juego es).
2. Volumen II (cómo se juegan los niveles).
3. Volumen I, secciones 8–9, y después el Volumen III (cómo piensa la IA y cómo escala).
4. Volumen IV (cómo se construye) junto con las secciones 18–20 del Volumen I.
5. Volumen V (constructor de mapas y mods, después del lanzamiento).

## Decisiones tomadas

- PC (Windows, Steam), Unity 6 LTS con DOTS.
- Tercera persona con primera persona opcional.
- Un jugador contra la IA; simulación preparada para un cooperativo futuro.
- Estilo realista histórico.
- Saga: Edad Media → Japón feudal → era de la pólvora → era del rifle (alcance principal hasta el siglo XIX); Antigüedad como expansión aparte; una posible ampliación hasta la actualidad queda fuera del juego base.
- Primer juego: Modo Historia con 5 niveles medievales y Modo Libre completo.
- Después del lanzamiento: constructor de mapas para la comunidad y soporte para implementar mods (sin herramienta propia de creación de mods), con Steam Workshop.
