# Estandartes (título provisional)

Saga de batallas históricas masivas para **PC**, hecha en **Unity 6**. El primer juego es la **Edad Media**; después vendrán Japón feudal (samuráis), la era de la pólvora, la era del rifle (siglo XIX) y la Antigüedad.

- **Un héroe, todos los roles:** juegas en tercera persona (con primera persona opcional) como un soldado que puede usar espada y escudo, arco, ballesta, caballo o máquinas de asedio.
- **Miles de soldados con IA viva:** cada soldado de la IA cumple su papel, con moral, fatiga, cadena de mando y generales que planifican.
- **Modo Historia:** batallas reales reconstruidas. El primer juego arranca con 5 niveles: Hastings, Château Gaillard, Las Navas de Tolosa, Puente de Stirling y Agincourt.
- **Modo Libre:** eliges el tamaño de los ejércitos y el juego los reparte, o defines cuántas unidades de cada tipo quieres, o planificas tú las escuadras y las zonas del mapa.
- **Comunidad:** tras el lanzamiento, constructor de mapas dentro del juego y soporte de mods con Steam Workshop.
- **Zonas y objetivos combinables:** capturar, mantener, destruir, escoltar, abrir puertas, infiltrarse…, en cualquier combinación (por ejemplo, un asedio por fases: puerta → patio → torretas → torre del homenaje).

## Documentación

- [**GDD — Documento de Diseño del Juego**](Docs/GDD/README.md), en cinco volúmenes:
  - [I. Diseño general](Docs/GDD/01_Diseno_General.md)
  - [II. Niveles de la Edad Media](Docs/GDD/02_Niveles_Edad_Media.md)
  - [III. Arquitectura de la IA](Docs/GDD/03_Arquitectura_IA.md)
  - [IV. Guía de construcción paso a paso](Docs/GDD/04_Guia_de_Construccion.md)
  - [V. Constructor de mapas y mods](Docs/GDD/05_Mapas_y_Mods.md)

## Estructura del repositorio

| Carpeta | Contenido |
|---|---|
| `Estandartes/` | Proyecto de Unity 6 (código en `Assets/_Project/Code`, contenido base en `Assets/StreamingAssets/Mods/estandartes`) |
| `Docs/GDD/` | Documento de diseño en cinco volúmenes |
| `Docs/` | Decisiones técnicas, lista de pendientes en el PC, biblia de arte, producción y modding |
| `Tools/DotNet/` | Compilación y pruebas del núcleo fuera de Unity, generador de esquemas JSON y comprobador de sintaxis |

## Probar el núcleo sin Unity

```bash
dotnet test Tools/DotNet/Tests/Estandartes.Core.Tests.csproj
```

## Estado

Fase 0 y comienzo de la fase 1. El núcleo de reglas e IA (`Estandartes.Core`) está escrito y probado; el contenido base (unidades, facciones, doctrinas, IA y los 5 niveles) está en JSON; el código de Unity está escrito pero **pendiente de compilar en el PC**: ver [Docs/PendienteDeVerificar.md](Docs/PendienteDeVerificar.md).
