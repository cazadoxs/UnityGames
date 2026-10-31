# Plantillas de prompts para ComfyUI

> Antes de usar un modelo, comprobar que su licencia permite el uso comercial de los resultados (Vol. I §19.4) y anotar modelo, versión y semilla de cada imagen que entre en el juego.

## Concept de personajes

```text
Positivo: historical concept art, full body, {cultura} {tipo de soldado}, {año}, {armadura}, {arma},
muddy and worn equipment, natural overcast light, neutral grey background, front and side view,
realistic proportions, museum-accurate details
Negativo: fantasy, anime, cartoon, plastic, clean, modern, glowing, extra limbs, text, watermark
```

Ejemplo: `{cultura}` = Norman, `{tipo}` = knight, `{año}` = 1066, `{armadura}` = knee-length mail hauberk and conical nasal helmet, `{arma}` = kite shield and lance.

## Texturas tileables de terreno

```text
Positivo: seamless tileable texture, top-down photo, {material}, overcast diffuse light, no shadows, 4k
Negativo: perspective, objects, text, borders, vignette
```

Materiales: *ploughed muddy field after rain*, *trampled wet grass*, *dry Mediterranean soil with stones*, *frozen mud with snow patches*, *dirt road with cart tracks*.

## Heráldica y estandartes

```text
Positivo: flat medieval heraldic banner design, {blasón}, simple shapes, two colors, front view, plain background
Negativo: gradients, photo, perspective, text
```

## Ilustraciones de briefing y códice

```text
Positivo: medieval manuscript illumination style, {escena}, gold leaf details, parchment texture
Negativo: modern, photo, text
```

## Imagen → 3D (props)

1. Concept del objeto aislado sobre fondo neutro, vista de tres cuartos.
2. Nodo de imagen→3D (modelo a evaluar).
3. Blender: retopología, UV, horneado de texturas, LOD0–3 (paso 4.2 de la guía).
