# Sistema de eventos por trigger

Mecánica suelta para **Unity 6, 3D y primera persona**. Cuando el jugador pasa por una zona invisible (un trigger), se lanza una lista de acciones: una animación, un objeto que aparece, la cámara que mira hacia un sitio (de golpe o con recorrido), un temblor de cámara, un diálogo o un sonido. Las acciones se combinan y se ordenan como quieras, sin escribir código.

No depende de ningún otro asset ni paquete: funciona con Built-in, URP o HDRP, con el Input System nuevo o con el antiguo, y con tu propio controlador de jugador.

---

## Índice

1. [Para qué sirve](#1-para-qué-sirve)
2. [Instalarlo en un proyecto](#2-instalarlo-en-un-proyecto)
3. [Probar la demo](#3-probar-la-demo)
4. [Uso rápido: tu primer evento en 1 minuto](#4-uso-rápido-tu-primer-evento-en-1-minuto)
5. [La zona de evento, campo a campo](#5-la-zona-de-evento-campo-a-campo)
6. [Las acciones, una por una](#6-las-acciones-una-por-una)
7. [Cómo funciona por dentro](#7-cómo-funciona-por-dentro)
8. [Conectarlo con tu jugador](#8-conectarlo-con-tu-jugador)
9. [Conectar tu sistema de diálogos](#9-conectar-tu-sistema-de-diálogos)
10. [Usarlo desde código](#10-usarlo-desde-código)
11. [Modificarlo y crear tus propias acciones](#11-modificarlo-y-crear-tus-propias-acciones)
12. [Recetas](#12-recetas)
13. [Problemas frecuentes](#13-problemas-frecuentes)
14. [Lista de archivos](#14-lista-de-archivos)

---

## 1. Para qué sirve

Sirve para todo lo que en un juego "pasa cuando llegas a un sitio":

- Al cruzar una puerta, se cierra de golpe detrás de ti (animación + sonido + temblor).
- Al entrar en una sala, la cámara gira sola hacia un cuadro que se cae (mirada con recorrido + animación).
- Al acercarte a un NPC, te dice una frase (diálogo).
- Al pisar una baldosa, aparece un enemigo o un objeto (aparecer objeto).
- Un susto: luz que se apaga, golpe de sonido, la cámara mira de golpe hacia atrás.

Cada cosa que pasa es una **acción** (un componente). La **zona** decide cuándo se lanzan, en qué orden y cuántas veces.

## 2. Instalarlo en un proyecto

1. Copia la carpeta `Mecanicas/Eventos` de este repositorio dentro de la carpeta `Assets` de tu proyecto, para que quede `Assets/Mecanicas/Eventos`. (Puedes ponerla en otra ruta: todo busca sus archivos de forma relativa.)
2. Abre Unity y espera a que compile. No hace falta instalar nada más.
3. Si quieres la demo, sigue el apartado 3. Si no, puedes borrar la carpeta `Demo` y el archivo `Editor/CreadorEscenaDemoEventos.cs` (y quitar `Mecanicas.Eventos.Demo` de las referencias de `Editor/Mecanicas.Eventos.Editor.asmdef`).

La carpeta trae tres *Assembly Definitions*, así que compila aparte del resto de tu código:

| Ensamblado | Qué contiene | Se incluye en el juego final |
|---|---|---|
| `Mecanicas.Eventos` | Zona, acciones, cámara, subtítulos | Sí |
| `Mecanicas.Eventos.Demo` | Un controlador de primera persona sencillo para la demo | Solo si lo usas |
| `Mecanicas.Eventos.Editor` | Inspector de la zona, menús y generador de la demo | No (solo editor) |

> **Si tu código no usa Assembly Definitions** no tienes que hacer nada: `Mecanicas.Eventos` está marcado como *Auto Referenced* y tus scripts lo ven directamente.
> **Si tu código sí usa Assembly Definitions**, añade `Mecanicas.Eventos` a sus *Assembly Definition References* para poder usar las clases desde tus scripts.

**Para exportarlo como paquete** a otros proyectos: clic derecho sobre la carpeta `Eventos` > *Export Package…* y se genera un `.unitypackage` con todo.

## 3. Probar la demo

1. Menú **Herramientas > Mecánicas > Eventos > Crear escena de demo**.
2. Se crea y abre la escena `Demo/Generado/DemoEventos.unity` (con sus materiales, sonidos, animación de puerta y prefab, todo generado en esa carpeta).
3. Pulsa **Play**. Muévete con **WASD** y el ratón. **Esc** suelta el ratón; clic lo vuelve a capturar.

El pasillo tiene 8 estaciones, cada una marcada con una franja naranja en el suelo y un cartel:

| Estación | Qué enseña |
|---|---|
| 1 · Animación | Una puerta con Animator se abre (parámetro Trigger `Abrir`). Solo una vez. |
| 2 · Aparecer objeto | Se activa un objeto oculto con efecto de escala y, a la vez, se crea una bola desde un prefab. Ambos desaparecen solos. |
| 3 · Mirar con recorrido | La cámara gira despacio hacia la columna roja, espera y vuelve. |
| 4 · Mirar de golpe | La cámara mira de golpe a la lámpara y **se queda** mirando hacia allí (modo Mantener). |
| 5 · Temblor | Terremoto suave de 2 segundos. |
| 6 · Diálogo | Dos frases con los subtítulos incluidos. Espacio o clic para pasar. |
| 7 · Sonido | Un pitido en 3D que sale del altavoz verde. |
| 8 · Todo junto | Explosión: sonido + temblor + objeto + mirada + diálogo, coordinados con retrasos. |

Selecciona cualquier zona en la jerarquía para ver cómo está configurada. Con el juego en marcha, el Inspector de la zona muestra sus activaciones y botones para **Activar**, **Detener** y **Reiniciar**. Además las zonas de la demo tienen *Mostrar Mensajes* activado, así que la consola te dice cuándo entra el jugador y cuándo se activa y termina cada evento.

Puedes volver a generar la escena cuando quieras; se sobrescribe.

## 4. Uso rápido: tu primer evento en 1 minuto

1. En la jerarquía: clic derecho > **Mecánicas > Zona de evento (caja)**. Se crea un objeto con un `BoxCollider` en modo trigger y el componente **Zona de evento**.
2. Colócalo y escálalo donde quieras que se active (la caja naranja que ves en la escena es la zona).
3. En el Inspector de la zona pulsa **Añadir acción…** y elige, por ejemplo, **Temblor de cámara**. La acción se añade al mismo objeto y a la lista *Acciones* de la zona.
4. Asegúrate de que tu jugador tiene la etiqueta **Player** y un `CharacterController` o un `Rigidbody` (ver apartado 8).
5. Play y pasa por la zona.

Para añadir más cosas, repite el paso 3. Se ejecutan en el orden de la lista *Acciones* (puedes arrastrarlas para reordenarlas).

> Las acciones **no tienen por qué estar en el objeto de la zona**. Puedes poner la acción en el objeto que afecta (por ejemplo, la `AccionAnimacion` en la propia puerta) y arrastrarla a la lista de la zona. Una misma acción puede estar en la lista de varias zonas.

## 5. La zona de evento, campo a campo

Componente `ZonaEvento` (menú *Add Component > Mecánicas > Eventos > Zona de evento*). Necesita un Collider con **Is Trigger** marcado en el mismo objeto (el Inspector avisa si falta y tiene un botón para arreglarlo).

**Activación**

| Campo | Qué hace |
|---|---|
| Activacion | **Al Entrar**: en cuanto entra. **Al Salir**: al salir. **Al Permanecer**: tras estar dentro *Tiempo Permanencia* segundos seguidos. **Manual**: solo cuando algo llama a `Activar()` (un botón, una palanca, otra mecánica). |
| Tiempo Permanencia | Segundos dentro para el modo Al Permanecer. Si sale antes, se reinicia la cuenta. |

**Quién la activa**

| Campo | Qué hace |
|---|---|
| Filtrar Por Etiqueta / Etiqueta | Solo objetos con esa etiqueta (por defecto `Player`). Desmárcalo para que valga cualquiera (por ejemplo, una caja empujada). |
| Capas | Capas que pueden activarla. Se combina con la etiqueta. |

**Repetición**

| Campo | Qué hace |
|---|---|
| Repeticion | **Una Vez**, **Cada Vez** o **Numero De Veces** (con *Max Activaciones*). |
| Enfriamiento | Segundos mínimos entre activaciones. |
| Ignorar Si En Curso | Si el evento aún no ha terminado, no se vuelve a lanzar encima. |

**Acciones**

| Campo | Qué hace |
|---|---|
| Ejecucion | **Secuencia**: una detrás de otra. Cada acción tiene la casilla *Esperar A Que Termine*: si está marcada, la siguiente espera; si no, la siguiente arranca a la vez. **Paralelo**: todas a la vez (cada una con su *Retraso*). |
| Acciones | La lista, en orden. Botón **Añadir acción…** para crear una nueva; **Recoger de hijos** rellena la lista con las acciones de este objeto y sus hijos. Las acciones con el componente desactivado (casilla del componente) se saltan. |
| Desactivar Al Agotarse | Cuando ya no puede repetirse y ha terminado, apaga el objeto de la zona. |

**Eventos**: `Al Activar`, `Al Terminar` (cuando acaban todas las acciones), `Al Entrar` y `Al Salir`. Son UnityEvents normales: arrastra cualquier objeto y elige un método público. Sirven para conectar cosas que no son acciones (encender una luz con `Light.enabled`, abrir otra mecánica, etc.).

**Depuración**: *Mostrar Mensajes* escribe en la consola lo que pasa; *Color Gizmo* cambia el color de la caja en la escena. Al seleccionar la zona se dibujan líneas hacia las acciones que están en otros objetos.

## 6. Las acciones, una por una

Todas comparten tres campos, que vienen de la clase base `AccionEvento`:

- **Nota**: texto libre para recordar qué hace.
- **Retraso**: segundos de espera antes de ejecutarse.
- **Esperar A Que Termine**: solo en zonas en Secuencia (ver arriba).

Y todas tienen el método público `EjecutarSuelta()`, así que también puedes lanzar una acción sola desde cualquier UnityEvent (un botón de la UI, por ejemplo) sin zona.

### Animación (`AccionAnimacion`)

Controla un **Animator** (o un componente **Animation** antiguo).

| Tipo | Qué hace |
|---|---|
| Trigger / Bool / Entero / Decimal | Cambia el parámetro *Parametro* del Animator (con *Valor Bool*, *Valor Entero* o *Valor Decimal*). Si el parámetro no existe con ese nombre y tipo, avisa en la consola. |
| Reproducir Estado | Salta al estado *Estado* con una mezcla de *Transicion* segundos. *Capa* = índice de capa (-1 = la que tenga el estado). |
| Clip Legacy | Reproduce *Clip Legacy* en el componente *Animacion Legacy*. |

*Duracion*: segundos que la acción cuenta como "en marcha", para que la secuencia espere a la animación. Con **-1** usa la duración real del estado o del clip.

### Aparecer objeto (`AccionObjeto`)

| Modo | Qué hace |
|---|---|
| Activar Existente | Activa *Objetivo*, un objeto que ya está en la escena desactivado. Es lo más cómodo: lo colocas a mano donde quieras. |
| Instanciar Prefab | Crea una copia de *Prefab* en *Punto Aparicion* (o en la acción). *Como Hijo* la emparenta al punto. |
| Desactivar / Destruir | Quita *Objetivo*. |
| Alternar | Lo activa si está apagado y lo apaga si está encendido. |

*Animar Escala* hace que crezca desde 0 al aparecer (con la curva *Curva Escala*, que por defecto rebota un poco) y que encoja al desaparecer. *Efecto* crea un prefab de partículas o humo en el mismo sitio. *Quitar Tras* hace que lo que apareció se vaya solo al cabo de unos segundos (0 = se queda).

### Mirar con la cámara (`AccionMirarCamara`)

Gira la cámara del jugador hacia algo. Todo está en el bloque *Mirada*:

| Campo | Qué hace |
|---|---|
| Objetivo / Desplazamiento | A qué mirar y un ajuste (por ejemplo `Y = 1.6` para mirar a la cara de un NPC cuyo origen está en los pies). |
| Direccion | Si no hay objetivo, dirección del mundo hacia la que mirar. |
| Seguir Objetivo | Si el objetivo se mueve, la cámara lo sigue. |
| Duracion Ida | **0 = de golpe.** Más de 0 = segundos del giro (con recorrido). |
| Curva | Forma del giro (por defecto acelera y frena suave). |
| Tiempo Mirando | Segundos mirando. **-1** = hasta que algo llame a `TerminarMirada()` en la acción (útil para esperar a que acabe un diálogo). |
| Al Terminar | **Volver** (suave, en *Duracion Vuelta*), **Volver De Golpe** o **Mantener** (se queda mirando hacia allí; ver apartado 8). |
| Bloquear Control | Mientras dura, el jugador no puede mover la cámara ni andar (ver apartado 8). |
| Desactivar Durante | Componentes que se apagan mientras dura (por ejemplo tu controlador, si no usa el bloqueo). |

*Camara*: vacío = la cámara con etiqueta **MainCamera**.

### Temblor de cámara (`AccionTemblorCamara`)

| Campo | Qué hace |
|---|---|
| Intensidad | Multiplicador general. |
| Duracion | Segundos. |
| Frecuencia | Rapidez. 8-12 = terremoto lento; 20-30 = explosión o golpe. |
| Amplitud Posicion | Metros máximos que se desplaza en cada eje. |
| Amplitud Rotacion | Grados máximos de giro (X cabeceo, Y guiñada, Z alabeo). |
| Atenuacion | Curva de fuerza en el tiempo (por defecto, de fuerte a nada). |
| Atenuar Por Distancia | Más flojo cuanto más lejos esté la cámara de *Origen*: completo hasta *Radio Completo*, nada desde *Radio Maximo*. Se dibujan las dos esferas al seleccionarla. |
| Esperar Duracion | La acción no termina hasta que acaba el temblor. |

Varios temblores a la vez se suman. El componente `CamaraEventos` de la cámara tiene *Escala Temblores* (0 a 2), pensado para una opción de accesibilidad de tu menú.

### Diálogo (`AccionDialogo`)

| Modo | Qué hace |
|---|---|
| Automatico | Si hay un sistema de diálogos registrado (apartado 9), se lo pasa. Si no, usa los subtítulos incluidos. |
| Subtitulos | Siempre los subtítulos incluidos. |
| Evento Externo | No muestra nada: lanza *Al Iniciar* (para conectar cualquier sistema por UnityEvent) y espera a que alguien llame a `NotificarFin()`. *Tiempo Maximo* evita que se quede esperando para siempre. |

*Lineas*: frases con *Hablante*, *Texto*, *Duracion* (0 = automática según la longitud del texto o la duración de la voz) y *Voz* (un AudioClip opcional). *Dialogo Externo*: el asset de diálogo de tu sistema, que se le pasa tal cual. *Bloquear Control*, *Esperar Fin Dialogo*, y los eventos *Al Iniciar* / *Al Terminar*.

**Subtítulos incluidos** (`SubtitulosSimples`): se dibujan abajo en el centro, sin Canvas ni TextMeshPro. Se crean solos la primera vez; si quieres cambiar el tamaño de letra, los colores, el ancho, el margen o el ritmo, añade el componente *Mecánicas > Eventos > Subtítulos simples* a un objeto de tu escena y ajústalo ahí. *Permitir Saltar* deja pasar de frase con clic, Espacio o Intro.

### Sonido (`AccionSonido`)

| Campo | Qué hace |
|---|---|
| Clips | Uno o varios; se elige uno al azar cada vez. |
| Posicion | **En Fuente**: usa el AudioSource *Fuente* (con su configuración). **En Punto**: suena en 3D en *Punto*. **En Activador**: pegado a quien activó la zona. **Global 2D**: igual en los dos oídos (música, voz en off). |
| Volumen / Tono | El tono se elige al azar entre los dos valores (variación natural). |
| Grupo Mezclador | AudioMixerGroup de salida (música, efectos...). |
| Bucle / Fundido Entrada | Repetición y entrada suave. |
| Distancia Minima / Maxima | Alcance del sonido 3D. |
| Detener En Vez De Reproducir | Convierte la acción en "parar sonido": para el último sonido que lanzó esta acción (o la *Fuente*) con *Fundido Salida*. Útil con una zona Al Salir para apagar una música ambiente. |
| Esperar Fin Sonido | La acción no termina hasta que acaba el sonido. |

Los sonidos En Punto, En Activador y Global se reproducen en un objeto temporal que se borra solo al terminar.

### Esperar (`AccionEsperar`)

Pausa la secuencia *Segundos*. *Tiempo Real* sigue contando con el juego en pausa (`Time.timeScale = 0`).

### Llamar evento (`AccionUnityEvent`)

Un UnityEvent dentro de la secuencia, en su sitio y con su retraso. Es la forma de meter cualquier otra mecánica en el orden de un evento sin programar: abrir una puerta del sistema de puertas, dar un objeto del inventario, lanzar un `DialogueStarter`, encender una luz...

## 7. Cómo funciona por dentro

```
Jugador entra en el trigger
        │
        ▼
ZonaEvento.OnTriggerEnter ──► ¿etiqueta y capa válidas? ──► ¿modo Al Entrar?
        │                                                        │
        │                    ¿agotada? ¿enfriamiento? ¿en curso? ◄┘
        ▼
Activar(jugador) ──► Al Activar ──► corrutina RutinaEvento
        │
        ├── Secuencia: para cada acción
        │      espera Retraso → EjecutarAccion → (espera si "Esperar A Que Termine")
        └── Paralelo: lanza todas a la vez
        ▼
cuando todas han terminado ──► Al Terminar ──► (Desactivar Al Agotarse)
```

- **Detección.** `ZonaEvento` usa `OnTriggerEnter` / `OnTriggerExit`. Si un objeto tiene varios colliders o un Rigidbody, cuenta como uno solo (se identifica por su Rigidbody). Así no se activa dos veces por tener, por ejemplo, un collider en el cuerpo y otro en los pies.
- **Acciones.** Cada acción es un componente que hereda de `AccionEvento` y devuelve una corrutina (`IEnumerator`). La zona ejecuta esas corrutinas, por eso una acción puede durar lo que quiera (un giro de cámara, una animación) y la secuencia sabe cuándo ha terminado. El objeto *ContextoEvento* que recibe cada acción dice qué zona la lanzó y quién entró.
- **Cámara.** Las acciones de cámara no mueven la cámara directamente: se lo piden a `CamaraEventos`, un componente que se añade solo a la cámara principal. Se ejecuta el último del frame (`DefaultExecutionOrder(10000)`) y aplica la mirada y los temblores **encima** de lo que haya hecho tu controlador ese frame. Detecta si tu controlador ha escrito la rotación (y la toma como nueva base) o no la ha tocado (y conserva la anterior), por eso funciona con controladores que giran la cámara cada frame y con los que no. Al terminar, deja la cámara exactamente como estaba.
- **Bloqueo del jugador.** `BloqueoControlJugador` es un registro global: las acciones dicen "bloqueo" y "libero" con su propia llave. El control vuelve cuando todas lo han liberado, así que dos eventos solapados no se pisan.
- **Diálogos.** `AccionDialogo` no conoce ningún sistema de diálogos: habla con la interfaz `IProveedorDialogo`. Si nadie la ha registrado, usa `SubtitulosSimples`.
- **Detener.** `ZonaEvento.Detener()` (o desactivar el objeto de la zona) corta el evento y llama a `Detener()` en cada acción para que libere la cámara, pare sonidos en bucle, etc.

## 8. Conectarlo con tu jugador

Requisitos del jugador:

1. **Etiqueta `Player`** (o la que pongas en la zona).
2. **Un `CharacterController` o un `Rigidbody`** en el jugador. Unity solo lanza los triggers si uno de los dos objetos tiene uno de ellos. (Con Rigidbody, puede ser cinemático.)
3. **Cámara con etiqueta `MainCamera`** (o arrastra tu cámara al campo *Camara* de las acciones).

Para que **Bloquear Control** funcione, tu controlador tiene que mirar el bloqueo. Añade una línea al principio de tu código de movimiento y de giro de cámara:

```csharp
using Mecanicas.Eventos;

void Update()
{
    if (BloqueoControlJugador.Bloqueado) return; // un evento tiene el control
    // ... tu movimiento y tu giro de cámara de siempre ...
}
```

Si no puedes tocar el controlador (por ejemplo, uno de la Asset Store), arrastra el componente del controlador al campo **Desactivar Durante** de la acción de mirar: se apaga mientras dura y se vuelve a encender al terminar.

Para el modo **Mantener** (la cámara se queda mirando hacia donde la dejó el evento), tu controlador tiene que saber "adoptar" esa rotación, porque si no, al recuperar el control volvería a su ángulo anterior. Implementa `IControlCamaraJugador` en el mismo objeto de la cámara o en uno de sus padres:

```csharp
public class MiJugador : MonoBehaviour, IControlCamaraJugador
{
    float giroHorizontal, giroVertical; // los que ya uses para mirar con el ratón

    public void AplicarRotacionMundo(Quaternion rotacionCamara)
    {
        Vector3 e = rotacionCamara.eulerAngles;
        giroHorizontal = e.y;
        giroVertical = Mathf.DeltaAngle(0f, e.x);
    }
}
```

`Demo/JugadorDemoPrimeraPersona.cs` es un ejemplo completo y corto de las dos cosas, con soporte del Input System nuevo y del antiguo. Puedes usarlo como base o copiar solo esas partes.

**Con Cinemachine**: pon el campo *Camara* de las acciones en la cámara que tiene el `CinemachineBrain`. Como `CamaraEventos` se ejecuta después, se suma a lo que haga Cinemachine. Si prefieres los temblores de Cinemachine (Impulse), usa `AccionUnityEvent` para llamar a tu `CinemachineImpulseSource.GenerateImpulse()`.

## 9. Conectar tu sistema de diálogos

Hay tres formas, de la más sencilla a la más integrada. Ninguna obliga a este paquete a depender del sistema de diálogos.

**A) Por UnityEvent, sin código (recomendado con `Mecanicas/Dialogos`).**
Añade una **Llamar evento (UnityEvent)** a la zona y en su evento arrastra el objeto que tiene el `DialogueStarter` del sistema de diálogos, eligiendo `DialogueStarter.Play()`. Si quieres que la secuencia espere a que acabe la conversación, usa en su lugar una **Diálogo** en modo **Evento Externo**: conecta *Al Iniciar* a `DialogueStarter.Play()` y, en el sistema de diálogos, conecta su evento de fin a `AccionDialogo.NotificarFin()`.

**B) Por código, con un puente.** Crea en tu proyecto (fuera de esta carpeta) un script que implemente `IProveedorDialogo` y se registre. Con eso, todas las acciones **Diálogo** en modo Automático usan tu sistema. Ejemplo para el sistema de `Mecanicas/Dialogos` (`DialogueManager`):

```csharp
using System;
using System.Collections.Generic;
using Mecanicas.Dialogos;
using Mecanicas.Eventos;
using UnityEngine;

// Pon este componente en cualquier objeto de la escena.
public class PuenteDialogos : MonoBehaviour, IProveedorDialogo
{
    void OnEnable()  => ProveedorDialogos.Registrar(this);
    void OnDisable() => ProveedorDialogos.Quitar(this);

    public void Reproducir(UnityEngine.Object dialogo, IReadOnlyList<LineaDialogo> lineas, Action alTerminar)
    {
        if (dialogo is DialogueAsset conversacion)
        {
            DialogueManager.Play(conversacion, alTerminar);   // conversación completa, con elecciones
            return;
        }

        // Sin asset: frases sueltas que no bloquean.
        foreach (LineaDialogo l in lineas)
            DialogueManager.ShowLine(l.hablante, l.texto, l.duracion > 0f ? l.duracion : 3f);
        alTerminar?.Invoke();
    }
}
```

Luego, en la acción **Diálogo**, arrastra tu `DialogueAsset` al campo *Dialogo Externo*. Si tu código usa Assembly Definitions, el asmdef de este puente debe referenciar `Mecanicas.Eventos` y `Mecanicas.Dialogos`.

**C) Cualquier otro sistema** (Yarn Spinner, Dialogue System for Unity, Ink...): igual que B, cambiando las dos llamadas por las de ese sistema.

## 10. Usarlo desde código

```csharp
using Mecanicas.Eventos;

// Activar una zona (respeta repeticiones y enfriamiento) o forzarla
zona.Activar();
zona.ForzarActivacion();
zona.Detener();
zona.Reiniciar();          // vuelve a 0 activaciones

// Escuchar
zona.AlTerminar.AddListener(() => Debug.Log("Evento terminado"));

// Lanzar una acción suelta
miAccion.EjecutarSuelta();

// Efectos de cámara directos
CamaraEventos.TemblarPrincipal(intensidad: 1.5f, duracion: 0.4f);

CamaraEventos cam = CamaraEventos.Obtener();          // la de la cámara principal
int id = cam.Mirar(new ParametrosMirada { objetivo = puerta, duracionIda = 0.5f, tiempoMirando = 2f });
bool sigue = cam.MiradaActiva(id);
cam.CancelarMirada();

// Subtítulos
SubtitulosSimples.Obtener().Mostrar("Radio", "Aquí base, ¿me recibe?", 3f);

// Bloqueo del jugador para tus propias cinemáticas
BloqueoControlJugador.Bloquear(this);
BloqueoControlJugador.Liberar(this);
BloqueoControlJugador.Cambio += bloqueado => cursorUI.SetActive(!bloqueado);
```

## 11. Modificarlo y crear tus propias acciones

### Crear una acción nueva

Crea un script que herede de `AccionEvento` e implemente `EjecutarAccion`. Aparecerá automáticamente en el botón **Añadir acción…** de todas las zonas.

```csharp
using System.Collections;
using Mecanicas.Eventos;
using UnityEngine;

[AddComponentMenu("Mecánicas/Eventos/Acciones/Parpadeo de luz")]
public class AccionParpadeoLuz : AccionEvento
{
    [SerializeField] Light luz;
    [SerializeField] int parpadeos = 5;
    [SerializeField] float intervalo = 0.08f;

    protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
    {
        for (int i = 0; i < parpadeos; i++)
        {
            luz.enabled = !luz.enabled;
            yield return new WaitForSeconds(intervalo);
        }
        luz.enabled = true;
    }

    public override void Detener()
    {
        base.Detener();
        if (luz != null) luz.enabled = true;  // dejarlo limpio si cortan el evento
    }
}
```

Reglas:

- Lo que dure varios frames, con `yield return` (la secuencia esperará). Si es instantánea, termina con `yield break`.
- `contexto.Activador` es quien entró en la zona (puede ser null si se lanzó a mano); `contexto.Zona` es la zona (null si se usó `EjecutarSuelta`).
- Si algo debe seguir vivo después de que la acción termine (por ejemplo, borrar algo a los 5 segundos), lánzalo con `LanzarCorrutina(...)`, no con `StartCoroutine`: usa la zona como anfitriona y funciona aunque el objeto de la acción esté apagado.
- Sobrescribe `Detener()` si tu acción deja algo a medias (bloqueos, sonidos en bucle, luces...).
- El nombre que sale en el menú **Añadir acción…** es la última parte del `AddComponentMenu`.

### Cambiar lo que ya hay

- **Valores por defecto**: están en los campos de cada clase (por ejemplo, la curva de escala en `AccionObjeto.cs` o la amplitud del temblor en `ParametrosTemblor`, dentro de `Camara/CamaraEventos.cs`).
- **Aspecto de los subtítulos**: componente `SubtitulosSimples` en la escena, o su método `OnGUI` si quieres cambiar el diseño.
- **Forma del temblor**: `CamaraEventos.CalcularTemblor` (usa ruido Perlin; puedes cambiarlo por un seno o por una curva).
- **Nuevos modos de activación** (por ejemplo, "al pulsar E dentro"): añade el valor al enum `ModoActivacion` en `ZonaEvento.cs` y gestiónalo en `Update`, como se hace con *Al Permanecer*.
- **Otro filtro de quién entra** (por ejemplo, por componente): cambia `ZonaEvento.EsValido`.
- **Inspector**: `Editor/ZonaEventoEditor.cs`.

Mantén el código del juego en `Runtime` y lo que solo sirve en el editor en `Editor`; así el ensamblado del editor no entra en tu juego compilado.

## 12. Recetas

**Puerta que se cierra detrás del jugador.** Zona **Al Salir** en el umbral, en el lado de dentro. Acciones: Animación (Trigger `Cerrar`) → Sonido (portazo, En Punto en la puerta, *Esperar A Que Termine* desmarcado) → Temblor corto (Intensidad 0.6, Duración 0.25, Frecuencia 28). Repetición: Una Vez.

**NPC que te llama al acercarte.** Zona esfera alrededor del NPC. Mirar con la cámara (Objetivo el NPC, Desplazamiento Y 1.6, Duracion Ida 0.6, Tiempo Mirando -1, Al Terminar Volver) con *Esperar A Que Termine* desmarcado → Diálogo (Esperar Fin Dialogo). En el *Al Terminar* del diálogo, llama a `AccionMirarCamara.TerminarMirada()` para que la cámara vuelva cuando acabe la conversación.

**Susto.** Secuencia: Sonido Global 2D fuerte (sin esperar) → Mirar de golpe hacia atrás (Duracion Ida 0, Tiempo Mirando 0.8, Volver De Golpe) → Aparecer objeto (la figura, Quitar Tras 0.8, sin Animar Escala).

**Música de zona.** Pon un AudioSource con la música en un objeto de la sala. Zona A **Al Entrar** con Sonido en modo *En Fuente* (esa fuente, Bucle, Fundido Entrada 2). Zona B en el mismo sitio y con el mismo tamaño, **Al Salir**, con otro Sonido *En Fuente* (la misma fuente) y *Detener En Vez De Reproducir* marcado. Las dos zonas en Cada Vez.

**Evento que solo pasa tras otro.** Pon la zona B en modo **Manual** y en *Al Terminar* de la zona A llama a `ZonaEvento.Activar()` de B. O desactiva el objeto de B y actívalo desde A con Aparecer objeto.

## 13. Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| No pasa nada al entrar | El jugador no tiene etiqueta `Player`, o ni él ni la zona tienen `CharacterController`/`Rigidbody`, o el Collider de la zona no es trigger, o la capa del jugador no está en *Capas*. Activa *Mostrar Mensajes*: si no sale "entra ...", el problema es la detección. |
| Se activa solo una vez | *Repeticion* está en Una Vez (por defecto). Cámbialo a Cada Vez. |
| La cámara tiembla pero no mira | Tu controlador mueve la cámara cada frame y no respeta el bloqueo. Añade la comprobación de `BloqueoControlJugador.Bloqueado` o usa *Desactivar Durante*. |
| Con Mantener, la cámara vuelve a su sitio al recuperar el control | Tu controlador no implementa `IControlCamaraJugador` (apartado 8). |
| "No hay cámara principal" en la consola | Tu cámara no tiene la etiqueta `MainCamera`. Pónsela o asígnala en el campo *Camara*. |
| La animación no se reproduce | El nombre del parámetro no coincide (mayúsculas incluidas) o es de otro tipo: la consola lo dice. En Reproducir Estado, el nombre es el del estado, no el del clip. |
| El sonido no se oye | En 3D, puede que estés más lejos que *Distancia Maxima*. Prueba con Global 2D para descartar. Comprueba que hay un `AudioListener` (normalmente en la cámara). |
| El texto de la demo no se ve | En proyectos muy personalizados puede faltar la fuente integrada; no afecta al sistema, solo a los carteles. |

## 14. Lista de archivos

```
Eventos/
├── README.md                              este documento
├── Runtime/                               (ensamblado Mecanicas.Eventos)
│   ├── ZonaEvento.cs                      la zona trigger
│   ├── AccionEvento.cs                    clase base de las acciones + ContextoEvento
│   ├── Acciones/
│   │   ├── AccionAnimacion.cs
│   │   ├── AccionObjeto.cs                aparecer / desaparecer objetos
│   │   ├── AccionMirarCamara.cs
│   │   ├── AccionTemblorCamara.cs
│   │   ├── AccionDialogo.cs
│   │   ├── AccionSonido.cs
│   │   ├── AccionEsperar.cs
│   │   └── AccionUnityEvent.cs
│   ├── Camara/
│   │   ├── CamaraEventos.cs               aplica miradas y temblores sobre tu cámara
│   │   └── BloqueoControlJugador.cs       bloqueo global + interfaz IControlCamaraJugador
│   └── Dialogo/
│       ├── ProveedorDialogos.cs           LineaDialogo, IProveedorDialogo y su registro
│       └── SubtitulosSimples.cs           subtítulos de serie
├── Editor/                                (ensamblado Mecanicas.Eventos.Editor, solo editor)
│   ├── ZonaEventoEditor.cs                inspector + menú GameObject > Mecánicas
│   └── CreadorEscenaDemoEventos.cs        Herramientas > Mecánicas > Eventos > Crear escena de demo
└── Demo/                                  (ensamblado Mecanicas.Eventos.Demo)
    ├── JugadorDemoPrimeraPersona.cs       controlador de ejemplo
    └── Generado/                          se crea al generar la demo (escena, materiales, sonidos...)
```

## Estado

El código compila sin errores ni avisos contra las librerías de Unity (motor y editor), con el Input System nuevo y con el antiguo. **Todavía no se ha abierto en el editor de Unity real**: la primera vez que lo copies a un proyecto, genera la demo y recorre las 8 estaciones para confirmar que todo se comporta como se describe aquí.
