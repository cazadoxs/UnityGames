# Imprenta Infinita: prototipo gris del acto 1

Proyecto Unity 6 (6000.1.4f1, URP) en `Documents\Claudeproyect\ImprentaInfinita`.

## Cómo jugar

- **Ejecutable:** `Builds\Windows\ImprentaInfinita.exe` (ventana 1600×900).
- **En el editor:** abre `Assets/Scenes/Imprenta.unity` y pulsa Play. Toda la nave se construye por código al arrancar.
- **Empezar de cero:** menú de pausa (Esc) → «Borrar partida», o lanza el ejecutable con `-nuevo`.
- La partida se guarda sola cada 15 s en `%USERPROFILE%\AppData\LocalLow\Imprenta Infinita\Imprenta Infinita\imprenta_save.json`.

| Tecla | Acción |
| --- | --- |
| WASD / Shift | Andar / correr |
| Clic izquierdo o E | Coger hoja (mantener para seguir), usar máquina, ducto, bandejas y tarjetas del tablón |
| Teclado | Escribir la frase en la máquina; Espacio o Enter al final de cada línea |
| Enter | Sacar la hoja terminada (se carga la siguiente sola si tienes hojas) |
| Retroceso | Corrector líquido (cuando lo compras) |
| Clic con la máquina atascada | Soltar las varillas |
| Esc | Levantarse de la máquina / pausa |

## Qué incluye (doc 4.2 a 4.7 y 6.1, acto 1)

- Bucle básico: coger hoja de la montaña → máquina de escribir → ducto de succión → dinero en el contador mecánico.
- Máquina de escribir con 44 varillas animadas, carro con escape, campanilla a 5 letras del final, retorno de carro, atascos si tecleas más rápido que la mecánica y fallos impresos en rojo.
- Calidad de página (impecable ×2), racha de 10 impecables = inspiración +50 % durante 60 s, frases doradas 1 de cada 500.
- Base de frases: 98 curadas + 9 plantillas (11.888 frases en total), orden por permutación de Feistel con semilla, sin repetir nunca.
- Montaña de 25 m dibujada con instancias en GPU que se vacía en semicírculo a medida que sacas hojas (100 → 400 m² cierra el acto).
- 15 mejoras en un tablón de corcho que se revela poco a poco: dedil, carpeta, bandeja, caja, pistola de succión (doc 4.6), bandeja de la máquina, cinta corta, muelles, corrector, 4 máquinas de margarita con sus mejoras.
- Producción mientras no juegas (margaritas, hasta 8 h), sonidos sintetizados, post-procesado y HUD mínimo.
- Sin reloj de jornada: según el doc 4.17, la hora de cierre empieza en el acto 2.

## Código (`Assets/Scripts/Imprenta`)

| Archivo | Qué hace |
| --- | --- |
| `Game.cs` | Arranque, modos, tutorial, economía, guardado |
| `Upgrades.cs` | Todas las cifras de equilibrio y las mejoras |
| `Phrases.cs` | Frases curadas, plantillas y permutación sin repetición |
| `Typewriter.cs` | Máquina de escribir y validación de lo tecleado |
| `Mountain.cs` | Campo de alturas de la montaña, instancias y raycast |
| `Stations.cs` | Ducto, cintas, bandeja, margaritas y tarjetas de mejora |
| `WorldBuilder.cs` | Nave, luces, post-procesado y escritorio |
| `PlayerController.cs`, `Hud.cs`, `Sfx.cs` | Jugador, interfaz y sonido |
| `AutoTest.cs` | Partida automática con capturas (`-autotest -autotestOut carpeta`) |
| `Editor/ImprentaSetup.cs` | Menú Imprenta → crear escena y compilar para Windows |

## Comprobado

Compila sin errores y la partida automática recorre el acto entero sin errores en consola, a 144 FPS: 0 repeticiones en 3.000 frases, fallo marcado y calidad ×0,91, atasco al teclear rápido, venta en el ducto, compra de las 15 mejoras, 4 margaritas produciendo y cierre del acto a 400 m².
