# Sistema de diálogos y conversaciones

Mecánica suelta para **Unity 6**, pensada para juegos **3D en primera persona**. Sirve para:

- **Frases sueltas**: un NPC comenta algo al pasar a su lado, el jugador piensa en voz alta, suena una radio. Salen como subtítulos y **no bloquean** al jugador.
- **Conversaciones largas**: varias líneas seguidas entre uno o varios personajes, **con o sin elección de respuesta**, con ramas, saltos, variables (lo que el jugador ya ha hecho o dicho) y eventos que activan cosas en la escena.
- **Conversaciones de fondo**: una conversación entera que avanza sola como subtítulos mientras el jugador sigue moviéndose.

No depende de ninguna otra mecánica. Funciona con el Input System nuevo y con el Input Manager antiguo, con Built-in y con URP, y no necesita TextMeshPro (aunque se puede cambiar a TextMeshPro, ver [Modificarlo](#6-modificarlo)).

---

## Índice

1. [Instalación en tu proyecto](#1-instalación-en-tu-proyecto)
2. [Probar la demo](#2-probar-la-demo)
3. [Uso rápido (5 minutos)](#3-uso-rápido-5-minutos)
4. [Escribir conversaciones](#4-escribir-conversaciones)
5. [Cómo funciona por dentro](#5-cómo-funciona-por-dentro)
6. [Modificarlo](#6-modificarlo)
7. [Implementarlo con otros sistemas (API)](#7-implementarlo-con-otros-sistemas-api)
8. [Referencia de componentes](#8-referencia-de-componentes)
9. [Problemas frecuentes](#9-problemas-frecuentes)

---

## 1. Instalación en tu proyecto

1. Copia la carpeta `Mecanicas/Dialogos` de este repositorio dentro de la carpeta `Assets` de tu proyecto. Debe quedar así: `Assets/Mecanicas/Dialogos/`. Copia también los archivos `.meta` (sobre todo el de `Demo/DialogosDemoBootstrap.cs`, que la escena demo necesita).
2. Abre Unity y espera a que compile. No hay que instalar nada más: **uGUI** (`com.unity.ugui`) viene ya en todos los proyectos de Unity 6.
3. Si tu proyecto usa el **Input System nuevo**, el sistema lo detecta solo. Si usa el **Input Manager antiguo**, también. Si tienes los dos activos (*Both*), usa el nuevo.

Estructura de la carpeta:

| Carpeta | Qué hay | ¿Necesaria? |
|---|---|---|
| `Runtime/Data` | Los datos: `DialogueAsset` (conversación), `DialogueNode` (línea), `DialogueActor` (personaje) | Sí |
| `Runtime/Core` | `DialogueManager` (el cerebro), el lector del formato de texto, las variables, la lectura de teclado | Sí |
| `Runtime/UI` | La vista: `DialogueViewBase` (la "plantilla") y `DialogueViewUGUI` (la caja por defecto) | Sí |
| `Runtime/Componentes` | Lo que pones en los objetos: iniciar diálogo, NPC hablable, frase suelta, eventos, variables | Sí |
| `Editor` | Inspector especial para escribir diálogos como texto y el menú `Herramientas > Mecánicas > Diálogos` | Recomendada |
| `Demo` | Escena de ejemplo y un controlador en primera persona muy simple | No (bórrala cuando ya no la necesites) |

Cada carpeta tiene su propio *Assembly Definition* (`Mecanicas.Dialogos`, `Mecanicas.Dialogos.Editor`, `Mecanicas.Dialogos.Demo`), así que el sistema compila aparte y no ralentiza el resto de tu código.

---

## 2. Probar la demo

1. Abre `Assets/Mecanicas/Dialogos/Demo/DialogosDemo.unity` (o usa el menú `Herramientas > Mecánicas > Diálogos > Abrir escena demo`).
2. Dale a **Play**. La escena se monta sola por código (no hay nada en la jerarquía hasta que pulsas Play).
3. Controles: **WASD** para moverte, **ratón** para mirar, **E** para hablar, **Espacio / E / clic** para avanzar, **ratón o teclas 1-9** para elegir respuesta, **Esc** libera el cursor.

Qué enseña cada cosa de la demo:

| Elemento | Qué demuestra |
|---|---|
| **Cartel** | Texto de narrador (sin nombre) y "diálogo de repetición": la segunda vez dice otra cosa |
| **Mercader** | Conversación larga con menú de respuestas que vuelve al menú, respuestas que aparecen o desaparecen según variables, rich text y un **evento** (`regalo`) que hace aparecer una caja en la mesa |
| **Anciana** | Conversación sin respuestas, que cambia según lo que hayas hablado con el mercader, con líneas que avanzan solas |
| **Guardia** (zona azul) | Frases sueltas al pasar por una zona, una distinta cada vez |
| **Radio** (zona gris) | Conversación de fondo que no bloquea al jugador |

Todo el código de la demo está en `Demo/DialogosDemoBootstrap.cs` y sirve de ejemplo de uso desde código.

---

## 3. Uso rápido (5 minutos)

### 3.1. Un NPC con el que se puede hablar

1. **Crea el diálogo**: en la ventana Project, clic derecho → `Create > Mecánicas > Diálogos > Diálogo`. Llámalo por ejemplo `Dialogo_Juan`.
2. **Escríbelo**: selecciónalo; en el Inspector, en "Escribir como texto", borra el ejemplo y escribe:
   ```
   Juan: ¡Hola! ¿Qué te trae por aquí?
   > Busco trabajo -> trabajo
   > Nada, adiós -> fin
   [trabajo]
   Juan: Pues has llegado en buen momento.
   ```
   Pulsa **Importar texto → líneas**. Abajo verás que se han creado las líneas.
3. **Prepara al NPC**: selecciónalo en la escena y usa `Herramientas > Mecánicas > Diálogos > Hacer hablable el objeto seleccionado`. Esto añade `DialogueInteractable` + `DialogueStarter` (y un `CapsuleCollider` si no tenía collider). Arrastra `Dialogo_Juan` al campo **Dialogue** del `DialogueStarter`.
4. **Prepara al jugador**: `Herramientas > Mecánicas > Diálogos > Añadir Dialogue Interactor a la cámara principal`. La cámara del jugador lanza un rayo hacia delante y, si mira a un NPC hablable de cerca, sale "[E] Hablar con Juan".
5. **Que el jugador no se mueva mientras habla** (recomendado): `Herramientas > Mecánicas > Diálogos > Añadir Dialogue Manager a la escena`, y en el campo **Disable While Talking** arrastra tu controlador en primera persona (y el script de disparo, el de agarrar objetos, etc.). Se desactivan al empezar a hablar y se vuelven a activar al terminar.

Dale a Play, mira a Juan y pulsa E.

> Si no añades un `DialogueManager`, se crea solo la primera vez que hace falta. Solo lo necesitas en la escena para configurarlo.

### 3.2. Una frase suelta al pasar por un sitio

1. Crea un objeto vacío, añádele un `BoxCollider` con **Is Trigger** activado y dale el tamaño de la zona.
2. Añade el componente `Dialogue Line (frase suelta)`. Escribe el nombre en **Speaker Name** (o deja vacío para un pensamiento) y las frases en **Lines**. Marca **Say On Trigger Enter**.
3. Tu jugador debe tener la etiqueta (*tag*) `Player` y un `CharacterController` o `Rigidbody`.

### 3.3. Lanzar un diálogo desde cualquier cosa

`DialogueStarter` tiene un método público **`Play()`**. Cualquier `UnityEvent` puede llamarlo: el `onClick` de un botón, una animación, una palanca, el sistema de eventos por trigger... Arrastra el objeto con el `DialogueStarter` al evento y elige `DialogueStarter > Play ()`.

---

## 4. Escribir conversaciones

Hay dos formas, y se pueden mezclar:

- **Como texto** (recomendado para conversaciones largas): en el Inspector del diálogo, sección "Escribir como texto", y botón **Importar texto → líneas**. El texto se guarda dentro del diálogo para poder seguir editándolo (botón "Recuperar texto guardado").
- **A mano en el Inspector**: en la sección "Datos", lista **Nodes**. Cada elemento es una línea. Útil para retocar cosas que el texto no cubre, como asignar un audio de voz o un `DialogueActor` concreto a una línea.

> Cuidado: **Importar texto** reemplaza todas las líneas. Si has asignado voces a mano, hazlo después de la última importación (o vuelve a asignarlas).

### 4.1. Formato de texto

Una instrucción por renglón. Los espacios al principio y al final no importan.

| Escribes | Significa |
|---|---|
| `Juan: Hola, ¿qué tal?` | Línea dicha por Juan |
| `- Empieza a llover.` | Línea de narrador (sin nombre) |
| `Hola, ¿qué tal?` | También narrador, si no hay "Nombre:" delante |
| `[tienda]` | Etiqueta: le pone el id `tienda` a la **siguiente** línea, para poder saltar a ella |
| `> Quiero comprar -> tienda` | Respuesta de la línea anterior; al elegirla, salta a `tienda` |
| `> Seguir` | Respuesta sin destino: continúa con la línea siguiente de la lista |
| `> Adiós -> fin` | Respuesta que termina la conversación |
| `-> tienda` | Después de la línea anterior, salta a `tienda` (sin preguntar) |
| `-> fin` | Después de la línea anterior, termina la conversación |
| `! evento abrir_puerta` | Al mostrar la línea anterior, lanza el evento `abrir_puerta` |
| `! activar tiene_llave` | Al mostrar la línea anterior, activa la variable `tiene_llave` |
| `! desactivar tiene_llave` | Al mostrar la línea anterior, desactiva la variable |
| `! espera 2.5` | La línea anterior avanza sola a los 2,5 s (no hay que pulsar nada) |
| `! inicio saludo` | La conversación empieza en la etiqueta `saludo` en vez de en la primera línea |
| `# lo que sea` o `// lo que sea` | Comentario, se ignora |
| `\n` dentro de un texto | Salto de línea |
| `{jugador}` dentro de un texto | Se cambia por el valor de `DialogueManager.TextTokens["jugador"]` |

**Condiciones y efectos**, al final de una línea o de una respuesta, entre corchetes:

| Etiqueta | En una línea | En una respuesta |
|---|---|---|
| `[si:variable]` | Solo se muestra si la variable está activada; si no, **se salta a la línea siguiente de la lista** | Solo aparece si la variable está activada |
| `[no:variable]` | Solo se muestra si la variable NO está activada | Solo aparece si la variable NO está activada |
| `[activa:variable]` | Activa la variable al mostrarse | Activa la variable al elegirla |
| `[desactiva:variable]` | Desactiva la variable | Desactiva la variable al elegirla |
| `[evento:nombre]` | Lanza el evento | Lanza el evento al elegirla |

En las respuestas, las etiquetas pueden ir antes o después de la flecha: `> Abrir [si:tiene_llave] -> abrir` y `> Abrir -> abrir [si:tiene_llave]` hacen lo mismo.

El texto admite **rich text** de Unity: `<b>negrita</b>`, `<i>cursiva</i>`, `<color=#ff8800>color</color>`, `<size=40>tamaño</size>`.

### 4.2. Cómo se recorre una conversación

- Se empieza por la línea de `! inicio` (o la primera de la lista).
- Después de cada línea se va a **la siguiente de la lista**, salvo que haya un salto (`->`), respuestas (`>`) o sea la última (entonces termina).
- Si una línea tiene una condición que no se cumple, se salta y se pasa a la siguiente de la lista (sin seguir su salto).

### 4.3. Ejemplos

**Conversación lineal, sin respuestas:**
```
Ana: ¿Has visto eso?
Luis: ¿El qué?
Ana: Nada, déjalo.
```

**Menú de preguntas que vuelve siempre al menú:**
```
[menu]
Guía: ¿Qué quieres saber?
> ¿Dónde estoy? -> donde
> ¿Qué hago ahora? -> que
> Nada, gracias -> fin
[donde]
Guía: En el puerto viejo.
-> menu
[que]
Guía: Busca al capitán.
-> menu
```

**Saludo distinto la segunda vez (con variables):**
```
Juan: ¡Otra vez tú! [si:conoce_juan]
-> menu
Juan: Encantado, soy Juan.
! activar conoce_juan
[menu]
Juan: ¿Qué necesitas?
...
```

**Respuesta que solo sale si tienes un objeto** (la variable la activa otro sistema, ver [7.3](#73-variables-compartidas)):
```
Guardia: No puedes pasar.
> Tengo un pase. -> pasa [si:tiene_pase]
> Vale, me voy. -> fin
[pasa]
Guardia: Adelante.
! evento abrir_porton
```

### 4.4. Personajes (`DialogueActor`)

Opcional. `Create > Mecánicas > Diálogos > Personaje`. Define **nombre**, **color del nombre**, **retrato** y un **blip** (sonido por letra, estilo Animal Crossing/Undertale). Para usarlo, añádelo a la lista **Actors** del diálogo: todas las líneas cuyo nombre coincida (sin importar mayúsculas) usarán su color, retrato y blip. También puedes asignar un actor directamente a una línea en el campo **Actor**.

### 4.5. Validar

El botón **Validar saltos y etiquetas** del Inspector avisa de saltos a etiquetas que no existen y de ids repetidos. En Play Mode, **Probar (Play Mode)** lanza el diálogo directamente.

---

## 5. Cómo funciona por dentro

```
                        ┌──────────────────────┐
 DialogueStarter.Play() │                      │  view.ShowLine / SetText / ShowChoices
 DialogueManager.Play() ├─►  DialogueManager   ├──────────────────────────────►  DialogueViewBase
 DialogueLine.Say()     │   (uno por escena)   │◄──────────────────────────────  (DialogueViewUGUI
                        │                      │   callback con la respuesta       u otra tuya)
                        └───┬─────────┬────────┘
                            │         │
                 DialogueVariables   eventos: NodeEvent, DialogueStarted, DialogueEnded...
                 (flags sí/no)        └─► DialogueEventListener → UnityEvents de la escena
```

**Datos.** Un `DialogueAsset` (ScriptableObject) es una lista de `DialogueNode`. Cada nodo es una línea: quién habla, texto, voz, respuestas (`DialogueChoice`), salto, condiciones y efectos. El formato de texto lo convierte `DialogueScriptParser` en esa lista; da igual cómo se haya creado, el manager solo ve nodos.

**El manager** (`DialogueManager`) es un *singleton*: hay uno por escena y, si no lo hay, se crea solo. Cuando empieza una conversación:

1. Guarda el estado y, si el diálogo bloquea (`Blocks Player`), abre la caja y desactiva los componentes de **Disable While Talking**.
2. Espera un frame para que la tecla con la que se empezó a hablar no cuente como "avanzar".
3. Para cada línea: comprueba sus condiciones (si fallan, pasa a la siguiente), aplica sus efectos (variables y evento), reproduce la voz, y escribe el texto letra a letra (con pausas en `.`, `!`, `?`). Pulsar avanzar mientras se escribe muestra la línea entera de golpe.
4. Si la línea tiene respuestas visibles, libera el cursor, las muestra y espera a que se elija una (con el ratón o 1-9). Si no tiene, espera a que se pulse avanzar (o el tiempo de `! espera`).
5. Decide la siguiente línea (salto, destino de la respuesta, o la siguiente de la lista) y repite.
6. Al terminar: cierra la caja, restaura el cursor y los componentes, lanza `onConversationEnded` / `DialogueEnded` y el callback `alTerminar`.

Todo usa **tiempo no escalado**, así que funciona aunque pongas `Time.timeScale = 0` para pausar el juego durante la conversación.

**Frases sueltas.** `ShowLine` mete la frase en una cola y la muestra como subtítulo durante un tiempo (automático según la longitud, o el que digas). Si hay una conversación que bloquea en curso, la frase espera a que termine.

**Conversaciones de fondo.** Si un diálogo tiene **Blocks Player** desactivado, sus líneas salen como subtítulos y avanzan solas; el jugador no pierde el control. Las respuestas no tienen sentido aquí: si hay alguna, se elige la primera y sale un aviso en consola.

**La vista** está separada de la lógica. El manager solo llama a métodos de `DialogueViewBase` (abrir, cerrar, mostrar línea, poner texto, mostrar respuestas, subtítulo, aviso). `DialogueViewUGUI` es la implementación por defecto: si no le asignas nada, construye sola un Canvas con la caja abajo, las respuestas encima, subtítulos y el aviso de interacción.

**Entrada.** `DialogueInput` traduce los `KeyCode` configurados al Input System nuevo o al antiguo, según lo que tenga activo el proyecto. Por eso las teclas se configuran siempre como `KeyCode`.

---

## 6. Modificarlo

### 6.1. Sin tocar código (Inspector)

En el **DialogueManager**:

| Campo | Para qué |
|---|---|
| `Characters Per Second` | Velocidad del efecto máquina de escribir (0 = texto de golpe) |
| `Punctuation Pause` | Pausa extra en `.` `!` `?` |
| `Advance Keys` | Teclas para avanzar (por defecto Espacio, Enter, E y clic izquierdo) |
| `Number Keys For Choices` | Elegir respuestas con 1-9 |
| `Allow Skip Typing` | Poder mostrar la línea entera pulsando avanzar |
| `Input Cooldown` | Tiempo tras cada línea en el que se ignora avanzar (evita saltarse líneas por pulsar dos veces) |
| `Manage Cursor` | Liberar el cursor al elegir respuesta y restaurarlo después |
| `Disable While Talking` | Componentes que se apagan mientras se habla |
| `Subtitle Base Seconds` / `Per Char` | Duración automática de las frases sueltas |
| `Voice Source` | Tu propio `AudioSource` para las voces (por ejemplo, conectado a un *Audio Mixer*) |
| `Dont Destroy On Load` | Mantener el manager entre escenas |
| `On Conversation Started/Ended`, `On Dialogue Event` | Eventos para conectar cosas sin código |

En la **vista** (`DialogueViewUGUI`, objeto hijo `VistaDialogos` del manager): fuente, tamaños de letra, colores de la caja y de las respuestas, y si las respuestas llevan número.

En el **DialogueInteractor** (cámara): tecla de hablar, alcance, grosor del rayo, capas y si se muestra el aviso.

En el **DialogueInteractable** (NPC): texto del aviso (`{tecla}` y `{nombre}` se sustituyen) y distancia máxima.

### 6.2. Usar tu propio diseño de caja (sin programar)

1. Crea tu Canvas con tu panel, textos, imagen de retrato, un indicador de "continuar", un contenedor con `VerticalLayoutGroup` para las respuestas y un botón de respuesta de plantilla (con un `Text` hijo).
2. Añade `DialogueViewUGUI` a tu Canvas (o a un padre) y arrastra cada pieza a su campo (**Panel**, **Name Text**, **Body Text**, **Portrait Image**, **Continue Hint**, **Choices Container**, **Choice Button Template**, **Subtitle Text**, **Prompt Text**). Si asignas **Panel**, ya no se construye la caja automática.
3. Arrastra esa vista al campo **View** del `DialogueManager`.

Conviértelo en *prefab* para reutilizarlo en todos tus juegos.

### 6.3. Usar TextMeshPro, UI Toolkit o bocadillos 3D

Crea un script que herede de `DialogueViewBase` e implemente sus métodos. Ejemplo mínimo con TextMeshPro:

```csharp
using System;
using System.Collections.Generic;
using Mecanicas.Dialogos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiVistaTMP : DialogueViewBase
{
    public GameObject panel, hint, choices;
    public TMP_Text nombre, texto, subtitulo, aviso;
    public Button plantilla;
    readonly List<Button> botones = new List<Button>();

    public override void Open() => panel.SetActive(true);
    public override void Close() { HideChoices(); panel.SetActive(false); }
    public override void ShowLine(DialogueLineInfo l) { nombre.text = l.speaker; nombre.color = l.speakerColor; texto.text = ""; }
    public override void SetText(string t) => texto.text = t;
    public override void SetContinueHint(bool v) => hint.SetActive(v);
    public override void ShowChoices(IReadOnlyList<string> o, Action<int> elegir)
    {
        HideChoices(); choices.SetActive(true);
        for (int i = 0; i < o.Count; i++)
        {
            int idx = i;
            var b = Instantiate(plantilla, choices.transform);
            b.gameObject.SetActive(true);
            b.GetComponentInChildren<TMP_Text>().text = o[i];
            b.onClick.AddListener(() => elegir(idx));
            botones.Add(b);
        }
    }
    public override void HideChoices() { foreach (var b in botones) Destroy(b.gameObject); botones.Clear(); choices.SetActive(false); }
    public override void ShowSubtitle(string s, Color c, string t) { subtitulo.gameObject.SetActive(true); subtitulo.text = string.IsNullOrEmpty(s) ? t : $"{s}: {t}"; }
    public override void HideSubtitle() => subtitulo.gameObject.SetActive(false);
    public override void ShowPrompt(string t) { aviso.gameObject.SetActive(true); aviso.text = t; }
    public override void HidePrompt() => aviso.gameObject.SetActive(false);
}
```

(Ponlo fuera de la carpeta `Mecanicas/Dialogos` o añade `Unity.TextMeshPro` a las referencias de `Mecanicas.Dialogos.asmdef`.) El efecto máquina de escribir sigue funcionando: el manager te pasa el texto ya cortado en `SetText`.

### 6.4. Cambios en el código más habituales

| Quiero... | Dónde |
|---|---|
| Añadir un comando nuevo al formato de texto (`! camara x`) | `DialogueScriptParser.ParseCommand`: añade un `case`. Lo más sencillo es convertirlo en un evento: `last.eventKey = "camara_" + arg;` |
| Una etiqueta nueva (`[voz:...]`) | `DialogueScriptParser`: añádela a la expresión `TrailingTag` y trátala en `Parse` / `ParseChoice` |
| Un campo nuevo por línea (expresión facial, animación...) | Añádelo a `DialogueNode`; léelo en `DialogueManager.Run` o escucha `DialogueManager.LineShown` desde otro script, sin tocar el manager |
| Variables numéricas (dinero, reputación) | `DialogueVariables` solo guarda sí/no. Añade un `Dictionary<string,int>` allí y nuevas etiquetas en el parser, o usa eventos para que tu sistema de economía decida |
| Que avanzar sea con el mando | Añade `KeyCode.JoystickButton0` a `Advance Keys` (Input Manager antiguo) o amplía `DialogueInput.GetKeyDown` para leer `Gamepad.current` |
| Que el NPC mire al jugador al hablar | Escucha `DialogueManager.DialogueStarted` / `onStarted` del `DialogueStarter` y activa tu sistema de mirada |

---

## 7. Implementarlo con otros sistemas (API)

Todo está en el *namespace* `Mecanicas.Dialogos`. Si tu código tiene su propio *Assembly Definition*, añade `Mecanicas.Dialogos` a sus referencias. Si no usas asmdef en tu código, no hace falta nada.

### 7.1. Sin código, con UnityEvents (lo más desacoplado)

| Desde otro sistema quiero... | Llama a... |
|---|---|
| Empezar una conversación | `DialogueStarter.Play()` |
| Empezar en una etiqueta | `DialogueStarter.PlayFrom(string)` |
| Decir una frase suelta | `DialogueLine.Say()` |
| Activar / desactivar una variable | `DialogueFlagSetter.SetFlag(string)` / `ClearFlag(string)` |
| Cortar la conversación | (por código) `DialogueManager.Stop()` |

Y en sentido contrario, para que el diálogo active cosas de la escena:

- **Eventos con nombre**: pon `! evento abrir_puerta` en el diálogo y, en la escena, un `DialogueEventListener` con una entrada `abrir_puerta` cuyo UnityEvent llame a lo que quieras (abrir una puerta, dar un objeto, mover la cámara, reproducir un sonido...).
- **Al terminar**: `DialogueStarter.onFinished`, o `DialogueManager.onConversationEnded`.
- **Cuando cambia una variable**: `DialogueFlagSetter` con `onTrue` / `onFalse`.

Así este asset y los demás (eventos por trigger, puertas, inventario...) no necesitan conocerse entre sí.

### 7.2. Por código

```csharp
using Mecanicas.Dialogos;

// Conversaciones
DialogueManager.Play(miDialogo);
DialogueManager.Play(miDialogo, () => Debug.Log("terminó"));
DialogueManager.Play(miDialogo, "tienda", alTerminar: null, interrupt: true);
DialogueManager.Stop();
bool hablando = DialogueManager.IsRunning;
DialogueAsset actual = DialogueManager.Current;

// Frases sueltas (no bloquean). duración 0 = automática
DialogueManager.ShowLine("Guardia", "¡Alto!", 2f);
DialogueManager.ShowLine(actorGuardia, "¡Alto!");
DialogueManager.ShowLine("", "Creo que he oído algo...");   // pensamiento, sin nombre

// Crear un diálogo por código desde texto
var d = DialogueAsset.FromScript("Juan: Hola\n> Adiós -> fin");

// Sustituciones de texto: {jugador} en los diálogos
DialogueManager.TextTokens["jugador"] = nombreDelJugador;

// Eventos (suscríbete en OnEnable y date de baja en OnDisable)
DialogueManager.DialogueStarted += asset => { };
DialogueManager.DialogueEnded   += asset => { };
DialogueManager.LineShown       += (asset, nodo) => { };
DialogueManager.ChoiceMade      += (asset, respuesta) => { };
DialogueManager.NodeEvent       += nombre => { if (nombre == "abrir_puerta") puerta.Abrir(); };
```

### 7.3. Variables compartidas

`DialogueVariables` es un almacén global de variables sí/no que cualquier sistema puede usar:

```csharp
DialogueVariables.Set("tiene_llave");          // al recoger la llave (inventario)
DialogueVariables.Clear("tiene_llave");        // al usarla
bool ok = DialogueVariables.Get("tiene_llave");
DialogueVariables.Changed += (nombre, valor) => { };
```

**Guardar partida**: las variables solo viven mientras el juego está abierto. Para guardarlas:

```csharp
string[] guardar = DialogueVariables.Export();   // mételo en tu archivo de guardado
DialogueVariables.Import(guardadas);             // al cargar
DialogueVariables.ResetAll();                    // partida nueva
```

### 7.4. Si ya tienes tu propio sistema de interacción

No uses `DialogueInteractor`. Cuando tu sistema detecte que el jugador interactúa con el NPC, llama a `GetComponent<DialogueInteractable>().Interact()` o directamente a `DialogueStarter.Play()`. Para no interactuar con otras cosas mientras se habla, comprueba `DialogueManager.IsRunning`.

---

## 8. Referencia de componentes

| Componente | Dónde va | Qué hace |
|---|---|---|
| `DialogueManager` | Un objeto de la escena (se crea solo si falta) | Ejecuta las conversaciones y las frases sueltas |
| `DialogueViewUGUI` | Hijo del manager o tu Canvas | Dibuja la caja, respuestas, subtítulos y aviso |
| `DialogueStarter` | NPC, zona, botón... | Lanza un diálogo con `Play()`; diálogo de repetición, "solo una vez", inicio automático o por trigger, eventos `onStarted`/`onFinished` |
| `DialogueInteractable` | NPC u objeto (con collider) | Lo hace hablable mirando + E. Requiere `DialogueStarter` |
| `DialogueInteractor` | Cámara del jugador | Detecta el NPC al que miras, muestra el aviso y empieza a hablar |
| `DialogueLine` | NPC, zona... | Frases sueltas sin crear un diálogo: todas en orden, una cada vez o al azar; con trigger, enfriamiento y "solo una vez" |
| `DialogueEventListener` | Cualquiera | Convierte los eventos con nombre del diálogo en UnityEvents |
| `DialogueFlagSetter` | Cualquiera | Lee/escribe variables desde UnityEvents y reacciona cuando cambian |

| Asset | Crear con | Qué es |
|---|---|---|
| `DialogueAsset` | `Create > Mecánicas > Diálogos > Diálogo` | Una conversación (o una frase) |
| `DialogueActor` | `Create > Mecánicas > Diálogos > Personaje` | Nombre, color, retrato y blip de un personaje |

Opciones de `DialogueAsset`: **Start Node** (etiqueta de inicio), **Actors**, **Nodes** (las líneas), **Blocks Player** (desactívalo para conversaciones de fondo).

---

## 9. Problemas frecuentes

| Problema | Solución |
|---|---|
| No sale "[E] Hablar" al mirar al NPC | El NPC necesita un **Collider que no sea trigger** (en él o en un hijo). La cámara necesita `DialogueInteractor`. Revisa **Reach** del interactor, **Max Distance** del interactable y las **Layers** |
| No puedo pulsar las respuestas con el ratón | Tiene que haber un `EventSystem` en la escena (la vista crea uno si no existe). Si tu controlador vuelve a bloquear el cursor cada frame, añádelo a **Disable While Talking** |
| El jugador se mueve o dispara mientras habla | Añade esos scripts a **Disable While Talking** del `DialogueManager` |
| El trigger de `DialogueLine` / `DialogueStarter` no hace nada | El jugador debe tener el tag `Player` (o cambia **Player Tag**) y un `CharacterController` o `Rigidbody`. El collider de la zona debe tener **Is Trigger** |
| Se salta líneas al pulsar | Sube **Input Cooldown** |
| Las letras no salen (cuadraditos) o quiero otra fuente | Cambia **Font** en la vista, o usa tu propia vista (6.2 / 6.3) |
| Aviso "la etiqueta X no existe" | Hay un salto a una etiqueta mal escrita. Usa **Validar saltos y etiquetas** |
| La escena demo sale vacía | Es normal antes de Play: se monta al darle a Play. Si sale "Missing script", no copiaste `DialogosDemoBootstrap.cs.meta`; arrastra el script `DialogosDemoBootstrap` al objeto de la escena |
| Errores de `UnityEngine.InputSystem` | Instala el paquete **Input System** o pon **Active Input Handling** en *Input Manager (Old)*: el sistema se adapta a lo que haya |

---

**Estado**: código revisado y compilado fuera de Unity (contra una imitación de la API de Unity) con las dos configuraciones de entrada, y el lector del formato de texto probado. **Falta abrirlo en el editor de Unity** para confirmar que compila y que la demo se ve bien; si sale algún error al importarlo, es lo primero que hay que corregir.
