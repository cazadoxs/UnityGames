# Guía rápida de modding

> Borrador. La documentación completa para modders llega con la fase 8 (Vol. V §9 y §10).

## Estructura de un mod

```text
mi_mod/
├── mod.json
├── data/units, data/weapons, data/siege, data/factions, data/doctrines,
│   data/personalities, data/formations, data/morale, data/curves, data/htn
├── scenarios/
└── localization/es.json, en.json
```

El juego base tiene la misma estructura: `Estandartes/Assets/StreamingAssets/Mods/estandartes/` es el mejor ejemplo.

## Esquemas

Los esquemas de `schemas/` se generan desde el código del juego. En VS Code, añade al principio de cada archivo:

```json
{ "$schema": "../../schemas/unit.schema.json" }
```

(el campo `$schema` se ignora al cargar; ajusta la ruta relativa).

## Reglas básicas

- IDs con la forma `espacio:tipo/nombre`, en minúsculas; el espacio es el `id` de tu mod.
- Para cambiar algo del juego base sin reemplazarlo entero, usa un parche: `data/weapons/arco.patch.json` con `{ "patch": "estandartes:weapon/arco_largo", "set": { "baseDamage": 50 } }`.
- Un campo mal escrito es un error: el mod se desactiva con un mensaje que lo indica.
- Solo se pueden usar capacidades, tareas primitivas de IA y hechos del mundo existentes.

## Validar un mod

```bash
dotnet run --project Tools/DotNet/SchemaGen -- validate Docs/Modding/schemas ruta/a/mi_mod
```
