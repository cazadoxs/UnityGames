# Mecánicas · Puertas, llaves e interruptores

Asset suelto para **Unity 3D en primera persona** con un sistema universal de interacción: puertas, llaves, candados, candados de combinación, pestillo corredero realista, panel numérico, interruptores de luz, palancas y botones. Está pensado para copiarlo a cualquier proyecto y montarlo sobre tus propios modelos.

---

## 1. Para qué sirve

Con este asset puedes hacer, sin programar:

- **Puertas** de bisagra o correderas (también rejas que suben), puertas de vaivén que se abren hacia el lado contrario al jugador, puertas dobles y puertas que se cierran solas.
- Abrirlas con **un clic** o **a mano**: mantienes pulsado y mueves el ratón para empujar o tirar, como en los juegos de terror.
- **Bloquearlas** con cualquier combinación de: cerradura con llave, candado con llave, candado de combinación, pestillo corredero, panel numérico o una señal (palanca, botón, interruptor...).
- **Llaves** que se recogen del suelo y se guardan en un llavero (o en tu propio inventario).
- **Luces** con interruptores conmutados (dos o más interruptores para la misma luz) y corte de corriente desde una palanca o un cuadro eléctrico.
- **Palancas** de 2 o más posiciones y **botones** pulsadores o conmutadores, que lanzan eventos a cualquier cosa (abrir una reja, encender una máquina, activar una trampa...).
- **Puzles** combinando señales: "la reja se abre cuando las tres palancas están arriba".

Todo se configura en el Inspector y se conecta con **UnityEvents**, así que también sirve para disparar el resto de tus sistemas (sonidos, animaciones, diálogos, cámaras).

---

## 2. Requisitos

- Unity **6** (también debería funcionar en 2021.3 o posterior).
- Cualquier render pipeline: Built-in, URP o HDRP.
- Input System nuevo **o** Input Manager clásico: el asset detecta cuál tienes. No hace falta instalar nada más.
- Sin dependencias de otros paquetes (ni TextMeshPro, ni Cinemachine).

---

## 3. Instalación

1. Copia la carpeta `Mecanicas/Puertas` de este repositorio dentro de tu proyecto, en `Assets/Mecanicas/Puertas`.
2. Espera a que Unity compile. Si sale algún error, mira la sección 11.
3. (Opcional) Para tenerlo listo para otros proyectos: clic derecho en la carpeta `Assets/Mecanicas/Puertas` > **Export Package...** y guarda el `.unitypackage`. Luego en cualquier proyecto: **Assets > Import Package > Custom Package**.

Estructura:

```
Puertas/
├─ README.md                    ← este archivo
├─ Runtime/                     ← todo el código del juego (ensamblado Mecanicas.Puertas)
│  ├─ Nucleo/                   ← Interactor, Interactuable, HUD, entrada, interfaces
│  ├─ Puertas/                  ← Puerta, CerraduraLlave, BloqueoPorSenal, IBloqueo
│  ├─ Llaves/                   ← Llave (recogible) y Llavero
│  ├─ Candados/                 ← Candado, CandadoCombinacion, RuedaCombinacion
│  ├─ Pestillo/                 ← PestilloCorredero
│  ├─ Panel/                    ← PanelNumerico y TeclaPanel
│  ├─ Luces/                    ← CircuitoLuz e Interruptor
│  ├─ Controles/                ← Palanca, Boton, CombinadorSenales
│  └─ Demo/                     ← controlador de jugador sencillo para la demo
├─ Editor/                      ← generador de la escena demo (ensamblado Mecanicas.Puertas.Editor)
└─ Demo/                        ← aquí se crea la escena demo y sus materiales
```

---

## 4. Probar la demo

1. Menú **Herramientas > Mecánicas > Puertas > Crear escena demo**.
2. Se crea `Demo/DemoPuertas.unity` con un pasillo y 10 estaciones hechas con cubos. Pulsa **Play**.

Controles: **WASD** moverse, **ratón** mirar, **Shift** correr, **Espacio** saltar, **E o clic izquierdo** usar, **mantener pulsado y mover el ratón** para arrastrar puertas, palancas y pestillos, **Esc** soltar el cursor.

| Nº | Estación | Qué enseña |
|---|---|---|
| 1 | Puerta roja con llave | `CerraduraLlave` + `Llave` en la mesa. Dentro está la llave azul. |
| 2 | Candado con llave | `Candado` que cae al suelo al abrirlo. |
| 3 | Candado de combinación | `CandadoCombinacion` con 3 `RuedaCombinacion` (clic o rueda del ratón). Pista en la nota. |
| 4 | Panel numérico | `PanelNumerico` (código 4321, 3 fallos lo bloquean 5 s). |
| 5 | Pestillo corredero | `PestilloCorredero` en una puerta de vaivén. Empieza echado. |
| 6 | Puerta corredera con botón | `Boton` → `Puerta.Abrir`, cierre automático a los 4 s. |
| 7 | Reja con dos palancas | 2 `Palanca` → `CombinadorSenales` (Todas) → reja corredera vertical. |
| 8 | Luces conmutadas | `CircuitoLuz` con 2 `Interruptor` (fuera y dentro) y una `Palanca` de corriente. |
| 9 | Puerta doble con seguro | 2 `Puerta` vinculadas + `BloqueoPorSenal` de un `Boton` conmutador. |
| 10 | Puerta libre | `Puerta` sin bloqueos para probar el arrastre. |

---

## 5. Montar el jugador en tu proyecto

El asset no impone un controlador. Solo necesita tres cosas:

1. **`Interactor`** en la **cámara** del jugador. Lanza un rayo hacia delante y detecta lo que miras.
   - `Distancia`: alcance (2,5 m por defecto).
   - `Capas`: quita la capa del jugador si tu cámara está fuera de su collider.
2. **`Llavero`** en el objeto del jugador (o tu propio inventario, ver 9.3).
3. **`InteraccionHUD`** en la cámara (opcional): punto de mira, texto "[E] Abrir" y mensajes. Si tienes tu propia UI, ver 9.4.

**Muy importante:** mientras el jugador arrastra algo, tu controlador de cámara **no debe girar**. Añade esto a tu script de cámara:

```csharp
using Mecanicas.Puertas;

if (interactor != null && interactor.CamaraBloqueada) return; // no girar la cámara este frame
```

Si no tienes controlador, usa `JugadorPrimeraPersonaDemo` (necesita un `CharacterController`).

---

## 6. Cómo funciona por dentro

Todo se apoya en cuatro piezas pequeñas:

| Pieza | Qué es | Quién la usa |
|---|---|---|
| `Interactuable` (clase base) | Algo que se puede usar mirándolo. Tiene texto de acción, `Habilitado` y el evento `alInteractuar`. | Puerta, Llave, Candado, Rueda, Pestillo, Tecla, Interruptor, Palanca, Botón, InteractuableSimple |
| `IArrastrable` (interfaz) | Algo que se mueve a mano con el ratón. | Puerta, Palanca, PestilloCorredero |
| `IBloqueo` (interfaz) | Algo que impide abrir una puerta. | CerraduraLlave, Candado(s), PestilloCorredero, PanelNumerico, BloqueoPorSenal |
| `ISenal` (interfaz) | Algo que está activo o inactivo. | Palanca, Boton, Interruptor, CircuitoLuz, PanelNumerico, CombinadorSenales |

Flujo de una pulsación:

```
Interactor (rayo desde la cámara)
   └─ encuentra un Interactuable en el collider (o en sus padres)
        ├─ si es IArrastrable → empieza el arrastre
        │     ├─ suelta rápido sin mover → cuenta como clic → Interactuar()
        │     └─ mueve el ratón → Arrastrar(movimiento en metros) y la cámara se bloquea
        └─ si no → Interactuar()

Puerta.Interactuar()
   ├─ abierta → Cerrar()  (si un pestillo sobresale, choca con el marco y se queda entornada)
   ├─ bloqueada → pregunta a cada IBloqueo si se puede abrir desde la puerta (llave)
   │     └─ sigue bloqueada → mensaje del bloqueo + sonido + pequeña sacudida
   └─ libre → AbrirDesde(posición del jugador)
```

Detalles útiles:

- La **puerta** guarda un valor `Apertura` de 0 (cerrada) a 1 (abierta); en vaivén va de −1 a 1. Se mueve con suavizado, y al arrastrar se calcula cuánto empuja tu mano alrededor de la bisagra.
- Los **bloqueos** se recogen solos si están en la puerta o en sus hijos (`Buscar bloqueos en hijos`). Si están en otro sitio (un panel en la pared), arrástralos a la lista `Bloqueos`.
- La **luz conmutada** funciona porque todos los interruptores cambian el mismo `CircuitoLuz`; cada tecla basculante cambia de posición como en una casa real.

---

## 7. Guía de cada componente

Todos están en **Add Component > Mecánicas > Puertas**. Cada campo tiene un tooltip en el Inspector.

### 7.1 Puerta

**Para qué:** cualquier cosa que se abre y se cierra: puertas, armarios, portones, rejas, trampillas, compuertas.

**Montaje paso a paso (bisagra):**
1. Crea un objeto vacío llamado `Puerta` y colócalo **justo en la bisagra** (el borde sobre el que gira).
2. Mete tu modelo de puerta como hijo, desplazado para que el borde quede en el origen del padre. Asegúrate de que tiene collider.
3. Añade `Puerta` al objeto vacío.
4. `Eje local` (0,1,0) para una puerta normal. `Ángulo de apertura` 95; si abre hacia el lado que no quieres, ponlo en −95. Selecciona el objeto: el **gizmo azul** dibuja el arco de apertura.

**Corredera:** `Tipo = Corredera`, `Dirección local` hacia donde se desliza (por ejemplo (−1,0,0) o (0,1,0) para una reja que sube) y `Distancia` en metros.

**Campos importantes:**
- `Sentido = AlejandoseDelJugador`: puerta de vaivén, siempre se abre hacia el otro lado.
- `Duración`, `Empieza abierta`, `Cierre automático` (segundos; 0 = nunca).
- `Permitir arrastre`: si se puede mover a mano.
- `Bloqueos` y `Buscar bloqueos en hijos`.
- `Abrir tras desbloquear`: si al usar la llave se abre en la misma pulsación.
- `Vinculadas`: otras puertas que se abren y cierran a la vez (puerta doble).
- `Habilitado` (de `Interactuable`): desmárcalo para que el jugador no pueda usarla y solo se abra por eventos (botón, palanca...).
- Sonidos: abrir, cerrar, bloqueada, golpe contra el marco.

**Eventos:** `alAbrir`, `alCerrar` (cuando queda cerrada del todo), `alIntentarAbrirBloqueada`.

**Métodos para UnityEvents o código:** `Abrir()`, `Cerrar()`, `Alternar()`, `AbrirIgnorandoBloqueos()`, `EstablecerAbierta(bool)`, `EstablecerAperturaInstantanea(float)`, `AbrirDesde(Vector3)`. Propiedades: `EstaCerrada`, `EstaBloqueada`, `Apertura`.

### 7.2 Llave y Llavero

- **`Llave`**: ponlo en el modelo de la llave (con collider). `Id llave` es el identificador interno (por ejemplo `llave_sotano`) y `Nombre visible` lo que lee el jugador ("la llave del sótano"). Al cogerla se añade al llavero y desaparece.
- **`Llavero`**: en el jugador. `Llaves iniciales` para empezar con alguna. Evento C# `AlCambiar` para refrescar tu UI.
- La llave con id **`maestra`** abre todas las cerraduras y candados que tengan marcado `Acepta llave maestra`.

### 7.3 Cerradura con llave

**Para qué:** la cerradura de la propia puerta.

1. Añade `CerraduraLlave` a la puerta (o a un hijo).
2. Pon el mismo `Id llave` que la `Llave` que la abre y un `Nombre llave` para el mensaje.
3. Opciones: `Gastar llave` (desaparece al usarla), `Rebloquear al cerrar` (vuelve a cerrarse con llave cada vez), `Empieza bloqueada`.

El jugador solo tiene que usar la puerta: si lleva la llave, se desbloquea (y se abre si `Abrir tras desbloquear` está marcado). Métodos: `Desbloquear()`, `Bloquear()`. Eventos: `alDesbloquear`, `alBloquear`.

### 7.4 Candado con llave

**Para qué:** un candado físico colgado de una aldaba.

1. Crea el candado como **hijo de la puerta** (así la bloquea solo). Necesita collider.
2. Añade `Candado`. Arrastra el arco (la U) a `Arco` para que suba al abrirse.
3. Configura `Id llave` y `Nombre llave`.
4. `Caer al abrir`: se suelta con física y cae al suelo. `Destruir tras`: segundos para borrarlo (0 = se queda).

El jugador apunta al candado (no a la puerta) y lo usa. Método `AbrirCandado()`, evento `alAbrir`.

### 7.5 Candado de combinación

1. Crea el candado como hijo de la puerta y añade `CandadoCombinacion`.
2. Crea una rueda por dígito (cilindros con collider) como hijos y añade `RuedaCombinacion` a cada una. Ajusta `Eje local` al eje del cilindro.
3. Opcional: un `TextMesh` por rueda en `Etiqueta` para ver el número. Ponlo como **hermano** de la rueda (no hijo), si no girará con ella.
4. En el candado escribe la `Combinación` (un dígito por rueda, en el orden de la lista `Ruedas`; si la lista está vacía se usan los hijos en orden).
5. `Abrir automáticamente`: se abre al acertar. Si lo desmarcas, el jugador debe usar el cuerpo del candado para "tirar".

Las ruedas giran con clic (+1) o con la **rueda del ratón** (±1) mientras las miras.

### 7.6 Pestillo corredero (realista)

**Para qué:** el cerrojo de pasador de toda la vida. Se maneja como uno real:

1. La manilla está bajada dentro de una muesca: el pasador no se puede deslizar.
2. **Sube el ratón** para levantar la manilla.
3. **Mueve el ratón hacia los lados** para deslizar el pasador.
4. En el extremo, **baja el ratón**: la manilla entra en la muesca y queda fijado.

Un clic corto hace toda la secuencia automáticamente. En `Modo = Simple` solo funciona con clic.

**Montaje:**
1. Crea un objeto `Pestillo` como **hijo de la puerta** (en la cara desde la que se maneja) y añade `PestilloCorredero`.
2. Crea dentro un hijo `Pasador` con la barra y la manilla. El origen del pasador debe estar **en el eje de la barra** y la manilla desplazada hacia un lado (colgando), para que al girar se levante.
3. `Eje local`: dirección hacia donde se echa (hacia el marco). Gizmo amarillo = recorrido.
4. `Recorrido` (metros), `Ángulo manilla` (90), `Recorrido manilla` (sensibilidad al subir el ratón), `Empieza echado`.

**Comportamiento con la puerta:**
- Puerta cerrada + pasador echado → la puerta no se abre ("Tiene el pestillo echado").
- Puerta abierta + pasador echado → el pasador sobresale y **choca con el marco**: la puerta se queda entornada hasta que lo quites.
- Como el pestillo está en una cara de la puerta, desde el otro lado no se puede tocar (el rayo da en la puerta).

Métodos: `Echar()`, `Quitar()`. Eventos: `alEchar`, `alQuitar`. Se puede poner en cualquier `Puerta` (también correderas).

### 7.7 Panel numérico

1. Crea el panel (en la pared o en la puerta) y añade `PanelNumerico`.
2. Crea las teclas como hijos. Cada tecla: un objeto con collider + `TeclaPanel` (`Tipo` Dígito/Borrar/Aceptar y `Dígito`). Consejo: pon el modelo y el texto de la tecla como hijos de ese objeto; así se hunden juntos.
3. Opcional: un `TextMesh` en `Pantalla` y un `Renderer` en `Luz estado` (cambia de color: espera, correcto, error).
4. Escribe el `Código`. `Confirmar automático` comprueba en cuanto se teclean todos los dígitos; si no, se pulsa la tecla Aceptar.
5. Seguridad: `Intentos máximos` y `Segundos bloqueo`; `Rebloquear tras` para que se cierre otra vez al rato.
6. **Arrastra el panel a la lista `Bloqueos` de la puerta.**

Eventos: `alCodigoCorrecto`, `alCodigoIncorrecto`, `alRebloquear`. Métodos: `PulsarDigito(int)`, `Borrar()`, `Confirmar()`, `Rebloquear()`, `EstablecerCodigo(string)`. También es una señal (`Activa` = desbloqueado), así que puede activar cualquier otra cosa.

### 7.8 Luces: Circuito e Interruptor

1. Crea un objeto `Circuito` y añade `CircuitoLuz`. Arrastra tus `Light` a `Luces` y las mallas de las bombillas a `Lámparas` (se les cambia la emisión; el material debe tener la emisión activada).
2. Pon un `Interruptor` en cada interruptor de la pared y arrastra el mismo circuito a todos. Opcional: `Tecla` (la pieza que bascula).
3. Opciones del circuito: `Encendido inicial`, `Fundido`, `Parpadeo al encender` (fluorescente) y `Con corriente`.

**Corriente:** `EstablecerCorriente(bool)` corta o devuelve la luz sin tocar los interruptores. Conéctalo a una palanca (`alCambiarEstado`) para hacer un cuadro eléctrico, o a un evento de tu juego para un apagón.

Métodos: `Encender()`, `Apagar()`, `Alternar()`, `EstablecerEncendido(bool)`, `AlternarCorriente()`. Eventos: `alEncender`, `alApagar`.

### 7.9 Palanca

1. Crea la palanca y, dentro, un objeto `Mango` con el **origen en el punto de giro**; el modelo del mango va como hijo.
2. Añade `Palanca` y arrastra el mango. `Eje local` es el eje de giro; `Ángulo mínimo/máximo` los topes.
3. `Posiciones` (2 = arriba/abajo; 3 o más = selector). `Posición inicial`.
4. `Retorno automático`: vuelve sola a la posición 0 (palanca de hombre muerto).

Clic = siguiente posición (va y vuelve). Arrastrando la mueves a mano y al soltar encaja en la posición más cercana.

Eventos: `alCambiarPosicion(int)`, `alActivar` (llega a la última posición), `alDesactivar`, `alCambiarEstado(bool)`. Métodos: `EstablecerPosicion(int)`, `Activar()`, `Desactivar()`.

### 7.10 Botón

1. Añade `Boton` al botón. `Pieza` es lo que se hunde y `Dirección pulsado` hacia dónde (local).
2. `Modo = Pulsador` (se activa un momento: `Duración pulso`) o `Conmutador` (cambia y se queda hundido).
3. `Enfriamiento`, `Usos máximos` (0 = sin límite), `Indicador` (renderer que cambia de color).

Eventos: `alPulsar`, `alActivar`, `alDesactivar`. Método `Pulsar()`.

### 7.11 Combinador de señales

Une varias señales en una. `Fuentes`: palancas, botones, interruptores, paneles u otros combinadores. `Modo`: `Todas`, `Alguna` o `Impar`. `Mantener al activar`: una vez resuelto el puzle, se queda resuelto. Eventos `alActivar`, `alDesactivar`, `alCambiar(bool)`.

### 7.12 Bloqueo por señal

Bloquea una puerta mientras una señal esté inactiva (o activa, con `Invertir`). Pon la señal en `Fuente`, escribe el `Motivo` y añade el componente a la lista `Bloqueos` de la puerta. La misma instancia puede bloquear varias puertas.

### 7.13 Interactuable simple

Para cualquier objeto que solo tenga que lanzar un evento o mostrar un mensaje: notas, carteles, timbres, ordenadores... `Mensaje`, `Sonido`, `Un solo uso` y el evento `alInteractuar`.

---

## 8. Recetas rápidas

- **Puerta con llave y panel a la vez:** `CerraduraLlave` en la puerta + `PanelNumerico` en su lista de bloqueos. Hay que cumplir los dos.
- **Reja que se abre con 3 palancas:** reja = `Puerta` corredera con `Habilitado` desmarcado. `CombinadorSenales` (Todas) con las 3 palancas; `alActivar → Puerta.Abrir`, `alDesactivar → Puerta.Cerrar`.
- **Puerta que se cierra con llave tras pasar** (con el sistema de eventos o un trigger): llama a `Puerta.Cerrar()` y luego a `CerraduraLlave.Bloquear()`.
- **Apagón de película:** cualquier evento → `CircuitoLuz.EstablecerCorriente(false)` en todos los circuitos; luego una palanca del cuadro los vuelve a encender.
- **Puerta de ascensor:** `Puerta` corredera + `Boton` pulsador → `Abrir`, `Cierre automático` 5 s.
- **Puerta que se abre al acertar el código sin que el jugador la toque:** `PanelNumerico.alCodigoCorrecto → Puerta.Abrir`.
- **Combinar con el resto de tus sistemas:** todos los eventos son UnityEvents, así que puedes llamar a tu cámara, diálogos, sonidos o animaciones desde el Inspector.

---

## 9. Cómo modificarlo

### 9.1 Cambiar las teclas
Todo el teclado y ratón se lee en `Runtime/Nucleo/Entrada.cs`. Hay dos bloques: uno para el Input System nuevo y otro para el clásico. Cambia `eKey`/`KeyCode.E` por la tecla que quieras y actualiza `NombreTeclaInteractuar` para que el HUD lo muestre bien. Para mando, añade `Gamepad.current.buttonSouth` en el bloque del Input System.

### 9.2 Crear tu propio objeto interactuable
```csharp
using Mecanicas.Puertas;
using UnityEngine;

public class Grifo : Interactuable
{
    [SerializeField] ParticleSystem agua;

    public override string ObtenerTexto(Interactor i) => agua.isPlaying ? "Cerrar grifo" : "Abrir grifo";

    protected override void AlInteractuar(Interactor i)
    {
        if (agua.isPlaying) agua.Stop(); else agua.Play();
    }
}
```
Para que además se maneje con el ratón, implementa también `IArrastrable` (mira `Palanca.cs` como ejemplo).

### 9.3 Crear tu propio bloqueo o usar tu inventario
- **Bloqueo nuevo** (por ejemplo, "necesita un fusible"): implementa `IBloqueo` en un MonoBehaviour y añádelo a la puerta. `Bloqueado`, `Motivo`, `ImpideCerrar` (normalmente `false`) e `IntentarDesbloquearDesdePuerta` (devuelve `true` si al usar la puerta se puede quitar).
- **Tu inventario:** implementa `IPortadorLlaves` (`TieneLlave`, `AnadirLlave`, `QuitarLlave`) en un componente del jugador y quita el `Llavero`. O asígnalo en el campo `Portador llaves` del `Interactor`, o por código con `AsignarPortadorLlaves`.

### 9.4 Usar tu propia interfaz
Quita `InteraccionHUD` y escucha al `Interactor`:
- `AlCambiarObjetivo(Interactuable)`: cambia lo que se mira (para resaltar el objeto o mostrar un icono).
- `TextoObjetivo`: texto de la acción actual.
- `AlMostrarMensaje(string, float)`: mensajes como "Necesitas la llave roja".
- `Arrastrando` y `CamaraBloqueada`.

### 9.5 Textos e idioma
Todos los textos visibles son campos del Inspector (`Texto abrir`, `Motivo`, etc.). Para varios idiomas, rellénalos desde tu sistema de localización al iniciar la escena.

### 9.6 Sonidos
Cada componente tiene campos `AudioClip` opcionales. Si pones un `AudioSource` en `Fuente audio`, suenan por él (útil para usar tu mezclador); si no, se reproducen en 3D en la posición del objeto.

---

## 10. Implementarlo en tus proyectos

1. Copia `Assets/Mecanicas/Puertas` (o importa el `.unitypackage`).
2. Monta el jugador (sección 5): `Interactor` en la cámara, `Llavero` en el jugador, `InteraccionHUD` y el bloqueo de cámara en tu controlador.
3. Sustituye los cubos de la demo por tus modelos siguiendo la guía del componente (sección 7). Lo único que importa de tus modelos es el **pivote** (bisagra, eje del pasador, punto de giro de la palanca) y que tengan **collider**.
4. Conecta los mecanismos entre sí con la lista `Bloqueos` y con los UnityEvents.
5. Si tu proyecto usa ensamblados (`.asmdef`), añade la referencia a `Mecanicas.Puertas`. Si no, no hace falta nada.
6. Todo el código está en el espacio de nombres `Mecanicas.Puertas`.

Para reutilizar una puerta ya montada, conviértela en **prefab** y úsala en todas tus escenas; cambia en cada copia solo el id de la llave o el código.

---

## 11. Problemas frecuentes

| Problema | Solución |
|---|---|
| No aparece "[E] Abrir" | El objeto necesita collider; revisa la `Distancia` y las `Capas` del `Interactor`; comprueba que `Habilitado` está marcado. |
| La puerta gira por el lado equivocado o desde el centro | El pivote no está en la bisagra. Pon el modelo como hijo de un vacío colocado en la bisagra. Cambia el signo del ángulo para el sentido. |
| La cámara se mueve mientras arrastro | Tu controlador no respeta `Interactor.CamaraBloqueada` (sección 5). |
| El panel o el bloqueo no bloquea la puerta | Si no es hijo de la puerta, arrástralo a su lista `Bloqueos`. |
| Las lámparas no brillan | El material necesita la emisión activada (casilla *Emission*). |
| Error con `Unity.InputSystem` | El ensamblado lo referencia de forma opcional; si no tienes el paquete se usa el Input Manager clásico. Si Unity se queja, borra `Unity.InputSystem` de `Runtime/Mecanicas.Puertas.asmdef`. |
| Texto rosa o invisible en la demo | Asigna una fuente a los `TextMesh` o regenera la escena. |

---

## 12. Estado

- El código compila contra las librerías de referencia de Unity (runtime y editor) con los dos sistemas de entrada.
- **Pendiente de verificar en el editor real de Unity:** abrir el proyecto, generar la escena demo y probar cada estación. Puede hacer falta ajustar algún tamaño o posición de la demo.
