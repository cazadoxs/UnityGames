# Inspección, agarre y lanzamiento de objetos (primera persona, Unity 3D)

Mecánica suelta y reutilizable para coger objetos con la mirada, llevarlos delante de la cámara, soltarlos, lanzarlos con fuerza cargada e inspeccionarlos de cerca girándolos con el ratón. **La rotación solo existe mientras inspeccionas**: con el objeto en la mano no se puede girar.

Está pensada para copiarla tal cual a cualquier proyecto en primera persona. No depende de ninguna otra mecánica, no necesita prefabs, Canvas ni TextMeshPro, y funciona con el Input System nuevo o con el Input Manager clásico sin configurar nada.

---

## Índice

1. [Para qué sirve](#1-para-qué-sirve)
2. [Requisitos](#2-requisitos)
3. [Instalación](#3-instalación)
4. [Prueba rápida con la escena de demostración](#4-prueba-rápida-con-la-escena-de-demostración)
5. [Controles](#5-controles)
6. [Cómo funciona por dentro](#6-cómo-funciona-por-dentro)
7. [Implementarlo en tu proyecto paso a paso](#7-implementarlo-en-tu-proyecto-paso-a-paso)
8. [Referencia de componentes](#8-referencia-de-componentes)
9. [Uso desde código: API y eventos](#9-uso-desde-código-api-y-eventos)
10. [Cómo modificarlo](#10-cómo-modificarlo)
11. [Solución de problemas](#11-solución-de-problemas)
12. [Estructura de archivos](#12-estructura-de-archivos)
13. [Estado y limitaciones](#13-estado-y-limitaciones)

---

## 1. Para qué sirve

| Acción | Qué pasa |
|---|---|
| **Apuntar** | El objeto al que miras se resalta y aparece su nombre con los controles disponibles. |
| **Coger** | El objeto flota delante de la cámara **con física real**: choca con las paredes, empuja otras cosas y, si se queda atascado, se suelta solo. Mantiene su orientación respecto a ti, pero no se puede girar. |
| **Soltar** | El objeto cae con su física normal. |
| **Lanzar** | Mantén el botón para cargar la fuerza (barra en pantalla) y suéltalo para lanzar. Los objetos pesados vuelan menos. |
| **Inspeccionar** | La cámara se queda quieta, el objeto se coloca en el centro de la pantalla, el ratón lo gira y la rueda acerca o aleja. Se muestra un panel con su nombre y descripción. |
| **Terminar la inspección** | Si el objeto estaba en la mano, vuelve a la mano (con la orientación nueva que le hayas dado). Si lo inspeccionaste sin cogerlo, vuelve suavemente a su sitio (o se queda en la mano, o cae delante: lo eliges tú). |

Ejemplos de uso: notas y diarios que se leen, llaves y pistas en juegos de misterio, objetos que se tiran para distraer o romper cosas, cajas para apilar, cuadros de la pared que se miran de cerca sin descolgarlos, botellas que no se pueden lanzar, cajas fuertes demasiado pesadas para levantar…

## 2. Requisitos

| | |
|---|---|
| **Unity** | Unity 6 (6000.0 o posterior) recomendado. También compila en 2021.3 y 2022.3 (el código elige `linearVelocity` / `velocity` y `PhysicsMaterial` / `PhysicMaterial` según la versión). |
| **Render pipeline** | URP, HDRP o Built-in. La escena de demostración crea materiales para el pipeline activo. |
| **Entrada** | Input System (el de Unity 6 por defecto) o Input Manager clásico. Si en *Project Settings > Player > Active Input Handling* tienes **Both**, se usa el Input System. |
| **Física** | La física 3D normal de Unity (PhysX). No usa DOTS. |

## 3. Instalación

1. Copia la carpeta `Mecanicas/Inspeccion` de este repositorio dentro de la carpeta `Assets` de tu proyecto. La ruta recomendada es `Assets/Mecanicas/Inspeccion`, pero puede ir en cualquier sitio.
2. Espera a que Unity compile. Se crean dos ensamblados:
   - `Mecanicas.Inspeccion`: todo lo que va en el juego.
   - `Mecanicas.Inspeccion.Editor`: los menús del editor (no se incluye en las compilaciones del juego).
3. Aparece el menú **Herramientas > Mecánicas > Inspección**.

> Si tu proyecto **no** usa archivos `.asmdef` y tus scripts necesitan usar estas clases, no hay que hacer nada: el ensamblado tiene *Auto Referenced* activado. Si tus scripts **sí** están en un `.asmdef`, añade `Mecanicas.Inspeccion` a sus referencias.

## 4. Prueba rápida con la escena de demostración

1. Menú **Herramientas > Mecánicas > Inspección > Crear escena de demostración**.
2. Se crea y se guarda `Demo/DemoInspeccion.unity` (dentro de la carpeta de la mecánica), con sus materiales en `Demo/Materiales`.
3. Dale a **Play**.

Qué hay en la sala y qué demuestra cada cosa:

| Objeto | Qué demuestra |
|---|---|
| **Libro antiguo** (sobre la mesa) | `Rotación inicial de inspección`: al inspeccionarlo siempre aparece con la tapa hacia ti. Descripción de varias líneas. |
| **Taza**, **Manzana** | Objetos normales: coger, soltar, lanzar, inspeccionar. |
| **Llave del sótano** | Objeto diminuto con `Distancia Inspeccion` propia (se ve muy de cerca). |
| **Estatuilla** | Objeto compuesto: `Rigidbody` y `ObjetoAgarrable` en el padre, colliders en los hijos. |
| **Pelota** | Material físico con rebote y `Multiplicador Lanzamiento` de 1,5. |
| **Botella de ácido** | `Se Puede Lanzar` desactivado: se coge y se inspecciona, pero no se lanza. |
| **Caja fuerte** | Pesa 120 kg (más que `Masa Maxima`): aparece «Demasiado pesado», pero se puede inspeccionar con R. |
| **Cuadro** (pared del fondo) | Sin `Rigidbody`: solo se inspecciona, vuelve a la pared y solo gira en horizontal. |
| **Torre de cajas** | Diana para lanzar cosas. Las cajas también se pueden coger. |

## 5. Controles

| Acción | Teclado y ratón | Mando |
|---|---|---|
| Coger / soltar | **E** | X (Xbox) / □ (PlayStation) |
| Inspeccionar / terminar | **R** | Y / △ |
| Lanzar (mantener para cargar) | **Clic izquierdo** | Gatillo derecho |
| Salir de la inspección | **Q** (o R otra vez) | B / ○ |
| Girar el objeto inspeccionado | **Mover el ratón** | Stick derecho |
| Acercar / alejar | **Rueda del ratón** | Cruceta arriba / abajo |
| Girar solo con botón (opcional) | Clic derecho | Gatillo izquierdo |

Solo en la escena de demostración (controlador de ejemplo): WASD mover, ratón mirar, Shift correr, Espacio saltar, Esc liberar el ratón, clic para volver a capturarlo.

Todos los controles se cambian en el componente **Entrada Inspeccion** (ver [10.1](#101-cambiar-las-teclas)).

## 6. Cómo funciona por dentro

### 6.1 Las piezas

```
Jugador (tu controlador)
└── Cámara
    ├── EntradaInspeccion        ← lee teclado, ratón y mando
    ├── AgarradorPrimeraPersona  ← toda la lógica (estados, física, inspección)
    └── InterfazInspeccion       ← punto de mira, avisos, barra de carga, panel (opcional)

Cualquier objeto de la escena
├── Collider(s)                  ← en el objeto o en sus hijos
├── Rigidbody                    ← opcional: sin él solo se puede inspeccionar
└── ObjetoAgarrable              ← permisos, textos, ajustes propios, sonidos y eventos
```

### 6.2 Los estados del agarrador

```
            E (coger)                       R (inspeccionar)
  LIBRE ───────────────▶ SUJETANDO ───────────────────────▶ INSPECCIONANDO
    ▲  ◀─── E (soltar) ───┘  │   ◀──── R / Q (vuelve a la mano) ───┘    │
    │  ◀── clic (lanzar) ────┘                                          │
    │                                                                   │
    │  R (inspeccionar sin coger) ─────────────────────────────────────▶│
    │                                                                   │
    └──── DEVOLVIENDO ◀──── R / Q (si no estaba en la mano) ────────────┘
          (vuelve a su sitio)
```

- **Libre**: cada frame lanza un rayo (un *SphereCast* fino) desde el centro de la cámara hasta `Alcance`. Si lo primero que toca es un `ObjetoAgarrable`, ese es el **objetivo**: se resalta y se lanza `alApuntar`. Si lo primero que toca es una pared, no hay objetivo (no se coge nada a través de las paredes).
- **Sujetando**: el `Rigidbody` sigue siendo dinámico pero sin gravedad. En `FixedUpdate` se calcula el **punto de sujeción** (delante de la cámara, a `Distancia Sujecion`, desplazado por `Desplazamiento Sujecion`) y se le da al objeto la velocidad necesaria para llegar a él (`Rapidez Seguimiento`, limitada por `Velocidad Maxima Sujeto`). Como se mueve por velocidad y no teletransportándolo, choca con todo de forma natural. La orientación se mantiene respecto al giro horizontal del jugador con velocidad angular. Si el objeto se aleja más de `Distancia Rotura` del punto (atascado tras una pared), se suelta solo.
- **Inspeccionando**: el objeto pasa a ser cinemático y sus colliders se desactivan (así no atraviesa ni empuja nada mientras lo miras). Se coloca en el centro de la pantalla, girando alrededor de su **centro visual** (el centro de sus renderers, no su pivote). La distancia se calcula según su tamaño. El ratón lo gira respecto a los ejes de la cámara (izquierda/derecha y arriba/abajo, como en los juegos de misterio). Los componentes de `Componentes A Bloquear` (tu controlador, tu cámara) se desactivan para que la vista no se mueva.
- **Devolviendo**: si inspeccionaste el objeto sin cogerlo y el modo es `Volver A Su Sitio`, vuelve suavemente a su posición y rotación originales y luego recupera su física.

### 6.3 Detalles que conviene saber

- **La física original se guarda y se restaura**: gravedad, cinemático, interpolación, detección de colisiones y velocidad angular máxima. Un objeto cinemático (quieto en una estantería) pasa a ser dinámico al soltarlo si `Volverse Fisico Al Soltar` está activo.
- **El objeto no choca con el jugador** mientras lo sujetas (`Ignorar Colision Con Jugador`), así no te empuja ni puedes subirte encima para volar. Al soltarlo, la colisión se recupera cuando deja de tocarte (máximo 3 segundos).
- **Al soltarlo, su velocidad se limita** a `Velocidad Maxima Al Soltar` para que no salga disparado si estabas girando rápido.
- **Lanzar**: se pone la velocidad a cero y se aplica `fuerza = Lerp(Fuerza Minima, Fuerza Maxima, carga) × Multiplicador Lanzamiento` en la dirección de la cámara, con `Modo Fuerza` (*Impulse* tiene en cuenta la masa). Se añade un giro aleatorio pequeño para que parezca natural.
- **El resaltado** cambia el color de los materiales con un `MaterialPropertyBlock` (no crea copias de materiales).
- `AgarradorPrimeraPersona.HayInspeccionActiva` es estático: cualquier script puede comprobarlo para pausar cosas (enemigos, temporizadores, menús…).

## 7. Implementarlo en tu proyecto paso a paso

### 7.1 Montar el jugador

**Opción A, automática:**

1. Selecciona la **cámara** de tu jugador en la jerarquía.
2. Menú **Herramientas > Mecánicas > Inspección > Añadir agarrador a la cámara seleccionada**. Se añaden `EntradaInspeccion`, `AgarradorPrimeraPersona` e `InterfazInspeccion`.
3. En el **Agarrador**, arrastra a `Componentes A Bloquear` el script de tu controlador de jugador (y el de la cámara, si es otro). Así la vista se queda quieta al inspeccionar.

**Opción B, a mano:**

1. Añade a la cámara `Agarrador Primera Persona` (*Add Component > Mecánicas > Inspección*). `Entrada Inspeccion` se añade sola si falta.
2. Opcional: añade `Interfaz Inspeccion` para tener punto de mira y textos.
3. Rellena `Componentes A Bloquear` como en el paso 3 anterior.
4. Comprueba que `Raiz Jugador` es el objeto raíz del jugador (si se deja vacío se usa `transform.root`, que vale en la mayoría de casos).

> **¿No tienes controlador?** Menú **Añadir jugador de ejemplo a la escena** crea un jugador con `CharacterController`, cámara y el controlador sencillo de la demo ya conectado.

> **Cinemachine**: pon el agarrador en la cámara real (la que tiene `CinemachineBrain`) y en `Componentes A Bloquear` pon el componente que mueve la cámara virtual (por ejemplo `CinemachineInputAxisController` en Cinemachine 3 o `CinemachineInputProvider` en Cinemachine 2) además de tu controlador.

### 7.2 Hacer objetos agarrables

**Opción A, automática:** selecciona uno o varios objetos y usa **Herramientas > Mecánicas > Inspección > Hacer agarrables los objetos seleccionados**. Se añade lo que falte: collider (`MeshCollider` convexo si hay malla, o `BoxCollider`), `Rigidbody` y `ObjetoAgarrable`.

**Opción B, a mano:**

1. El objeto necesita al menos un **collider** (en él o en sus hijos). Si es un `MeshCollider` con `Rigidbody`, márcalo como **Convex**.
2. Añade un **Rigidbody** en la raíz del objeto y ponle una **masa** realista (kg). Sin `Rigidbody` el objeto solo se podrá inspeccionar.
3. Añade **Objeto Agarrable** en la raíz (la misma que tiene el `Rigidbody`).
4. Escribe su **Nombre** y su **Descripcion** si quieres que aparezcan al inspeccionarlo.
5. Activa o desactiva **Se Puede Agarrar / Lanzar / Inspeccionar** según el objeto.

### 7.3 Ajustar la sensación

Empieza con estos valores del agarrador y cámbialos con el juego en marcha (los cambios en Play se pierden al parar: apúntalos o copia el componente con *Copy Component*):

| Quieres… | Cambia |
|---|---|
| Coger cosas desde más lejos | `Alcance` |
| El objeto más cerca o más a un lado | `Distancia Sujecion`, `Desplazamiento Sujecion` |
| Que el objeto vaya más «pegado» o más «pesado» | `Rapidez Seguimiento` (alto = pegado), `Rapidez Giro` |
| Lanzar más fuerte | `Fuerza Maxima` (y `Modo Fuerza = VelocityChange` si quieres que la masa no importe) |
| Lanzar al instante sin cargar | `Tiempo Carga Maxima = 0` |
| Objetos inspeccionados más grandes en pantalla | `Distancia Inspeccion` o `Factor Distancia Por Tamano` más bajos |
| Girar más rápido | `Velocidad Rotacion` |
| Girar solo manteniendo clic derecho | `Rotar Solo Con Boton` |
| Que lo inspeccionado se quede en la mano | `Al Terminar Sin Agarrar = Quedarse En La Mano` |

## 8. Referencia de componentes

### 8.1 `AgarradorPrimeraPersona` (en la cámara)

**Referencias**

| Campo | Para qué sirve |
|---|---|
| `Camara` | Cámara desde la que se apunta. Vacío = la de este objeto, la de un padre o `Camera.main`. |
| `Entrada` | El lector de controles. Vacío = el de este objeto (se crea si no hay). |
| `Raiz Jugador` | Raíz del jugador; sus colliders no chocan con lo que sujetas. Vacío = `transform.root`. |

**Detección**

| Campo | Por defecto | Para qué sirve |
|---|---|---|
| `Alcance` | 2,5 m | Distancia máxima para coger o inspeccionar. |
| `Radio Deteccion` | 0,05 m | Grosor del rayo. Más grande = más fácil apuntar a objetos pequeños. 0 = rayo fino. |
| `Capas Detectables` | Todas | Capas que el rayo tiene en cuenta (las paredes deben estar incluidas para que bloqueen). |
| `Detectar Triggers` | Ignore | Si el rayo debe chocar con triggers. |

**Sujetar**

| Campo | Por defecto | Para qué sirve |
|---|---|---|
| `Distancia Sujecion` | 1,3 m | Distancia del objeto a la cámara. |
| `Desplazamiento Sujecion` | (0, −0,15, 0) | Desplazamiento del punto de sujeción (x derecha, y arriba). |
| `Rapidez Seguimiento` | 14 | Lo rápido que el objeto llega al punto de sujeción. |
| `Velocidad Maxima Sujeto` | 18 m/s | Límite de velocidad mientras se sujeta. |
| `Rapidez Giro` | 12 | Lo rápido que el objeto recupera su orientación. |
| `Distancia Rotura` | 1,2 m | Si el objeto se queda más lejos que esto del punto, se suelta. |
| `Masa Maxima` | 30 kg | Lo más pesado que se puede coger. |
| `Velocidad Maxima Al Soltar` | 4 m/s | Velocidad máxima que conserva el objeto al soltarlo. |
| `Volverse Fisico Al Soltar` | Sí | Los objetos cinemáticos pasan a tener física al soltarlos. |
| `Ignorar Colision Con Jugador` | Sí | El objeto sujeto no choca con el jugador. |

**Lanzar**

| Campo | Por defecto | Para qué sirve |
|---|---|---|
| `Fuerza Minima` / `Fuerza Maxima` | 3 / 14 | Fuerza sin cargar y con la carga completa. |
| `Tiempo Carga Maxima` | 0,8 s | Tiempo manteniendo el botón hasta la fuerza máxima. 0 = siempre la máxima. |
| `Modo Fuerza` | Impulse | *Impulse*: la masa importa. *VelocityChange*: todos salen igual de rápido. |
| `Giro Aleatorio Al Lanzar` | 2 rad/s | Giro que se le da al objeto al lanzarlo. |

**Inspeccionar**

| Campo | Por defecto | Para qué sirve |
|---|---|---|
| `Distancia Inspeccion` | 0,55 m | Distancia mínima del objeto a la cámara. |
| `Factor Distancia Por Tamano` | 2,2 | Los objetos grandes se colocan a `radio × factor`. |
| `Desplazamiento Inspeccion` | (0, 0, 0) | Mueve el objeto respecto al centro de la pantalla (por ejemplo, a la derecha para dejar sitio al panel). |
| `Zoom Minimo` / `Zoom Maximo` | 0,6 / 1,6 | Cuánto se puede acercar y alejar (multiplica la distancia). |
| `Velocidad Zoom` | 1 | Lo rápido que hace zoom la rueda. |
| `Velocidad Rotacion` | 1 | Multiplicador del giro. |
| `Invertir X` / `Invertir Y` | No | Invierte los ejes de giro. |
| `Rotar Solo Con Boton` | No | Solo gira manteniendo la tecla *Rotar*. |
| `Suavizado` | 14 | Suavidad de los movimientos (más alto = más rápido). |
| `Permitir Inspeccion Sin Agarrar` | Sí | Inspeccionar algo con R sin cogerlo antes. |
| `Al Terminar Sin Agarrar` | Volver A Su Sitio | Qué pasa al terminar de inspeccionar algo que no estaba en la mano: *Volver A Su Sitio*, *Quedarse En La Mano* o *Soltar Delante*. |
| `Componentes A Bloquear` | — | Scripts que se desactivan al inspeccionar (tu controlador, tu cámara…). |
| `Mostrar Cursor Al Inspeccionar` | No | Libera y muestra el cursor durante la inspección. |

**Eventos** (todos reciben el `ObjetoAgarrable` afectado): `Al Cambiar Objetivo` (recibe `null` al dejar de apuntar), `Al Agarrar`, `Al Soltar`, `Al Lanzar`, `Al Empezar Inspeccion`, `Al Terminar Inspeccion`.

### 8.2 `ObjetoAgarrable` (en cada objeto)

| Campo | Para qué sirve |
|---|---|
| `Nombre` | Nombre en pantalla. Vacío = nombre del GameObject. |
| `Descripcion` | Texto del panel de inspección. |
| `Se Puede Agarrar` / `Se Puede Lanzar` / `Se Puede Inspeccionar` | Permisos del objeto. |
| `Multiplicador Lanzamiento` | Multiplica la fuerza de lanzamiento para este objeto. |
| `Distancia Sujecion` | Si es mayor que 0, sustituye la del agarrador. |
| `Distancia Inspeccion` | Si es mayor que 0, sustituye la calculada por tamaño. |
| `Usar Rotacion Inicial Inspeccion` + `Rotacion Inicial Inspeccion` | Orientación (en grados, relativa a la cámara) con la que aparece al inspeccionarlo. (0, 0, 0) = su eje Z apunta hacia delante, lejos de la cámara. |
| `Rotar Horizontal` / `Rotar Vertical` | Bloquea ejes de giro al inspeccionar. |
| `Resaltar Al Apuntar`, `Color Resaltado`, `Intensidad Resaltado` | Resaltado de color al apuntar. |
| `Renderers` | Qué renderers se resaltan y se usan para calcular el tamaño. Vacío = todos los hijos. |
| `Sonido Agarrar` / `Soltar` / `Lanzar` / `Golpe` | Sonidos opcionales. El golpe suena al chocar más rápido que `Velocidad Minima Golpe`, con volumen según la fuerza. |
| `Volumen` | Volumen de esos sonidos. |
| Eventos | `Al Apuntar`, `Al Dejar De Apuntar`, `Al Agarrar`, `Al Soltar`, `Al Lanzar`, `Al Empezar Inspeccion`, `Al Terminar Inspeccion`. |

### 8.3 `EntradaInspeccion` (en la cámara)

| Campo | Para qué sirve |
|---|---|
| `Tecla Agarrar`, `Tecla Inspeccionar`, `Tecla Lanzar`, `Tecla Salir`, `Tecla Rotar` | Teclas para el **Input Manager clásico**. |
| `Sensibilidad Raton Clasica` | Grados por unidad de los ejes *Mouse X / Mouse Y* (clásico). |
| `Rutas Agarrar`, `Rutas Inspeccionar`, `Rutas Lanzar`, `Rutas Salir`, `Rutas Rotar` | Controles para el **Input System** (puedes poner varios por acción). |
| `Sensibilidad Raton` | Grados por píxel (Input System). |
| `Velocidad Stick` | Grados por segundo con el stick derecho. |
| `Velocidad Zoom Mando` | Zoom por segundo con la cruceta. |
| `Entrada Externa` | Si está activo, no lee dispositivos: otro script rellena la entrada (ver [10.2](#102-usar-tu-propio-sistema-de-entrada)). |

### 8.4 `InterfazInspeccion` (opcional, en la cámara)

Dibuja con `OnGUI` el punto de mira (cambia de color al apuntar a algo), el aviso de controles, la barra de carga y el panel de nombre y descripción. Puedes cambiar todos los **textos** (`{0}` se sustituye por el nombre del objeto), los **colores**, el **tamaño de letra** (relativo a una pantalla de 1080 px) y ocultar cada parte por separado.

### 8.5 `ControladorJugadorDemo` (solo demostración)

Controlador en primera persona mínimo con `CharacterController`. Está en `Runtime/Demo` y se puede borrar si usas tu propio controlador.

## 9. Uso desde código: API y eventos

Espacio de nombres: `Mecanicas.Inspeccion`.

```csharp
using Mecanicas.Inspeccion;
using UnityEngine;

public class EjemploUso : MonoBehaviour
{
    public AgarradorPrimeraPersona agarrador;
    public ObjetoAgarrable llave;

    void Start()
    {
        // Reaccionar a lo que pasa
        agarrador.alEmpezarInspeccion.AddListener(obj => Debug.Log("Mirando " + obj.NombreVisible));
        agarrador.alLanzar.AddListener(obj => Debug.Log("Lanzado " + obj.name));
        llave.alTerminarInspeccion.AddListener(() => Debug.Log("El jugador ya ha visto la llave"));
    }

    void Update()
    {
        // Pausar a los enemigos mientras se inspecciona
        if (AgarradorPrimeraPersona.HayInspeccionActiva) return;

        // Consultar el estado
        if (agarrador.Estado == EstadoAgarre.Sujetando)
            Debug.Log("En la mano: " + agarrador.Sujeto.NombreVisible);
    }
}
```

| Miembro | Qué hace |
|---|---|
| `bool Agarrar(ObjetoAgarrable obj)` | Pone el objeto en la mano (devuelve `false` si no se puede). Útil para sacar algo de un inventario. |
| `void Soltar()` | Suelta lo que haya en la mano. |
| `void Lanzar(float carga)` | Lanza lo que haya en la mano con una carga de 0 a 1. |
| `bool EmpezarInspeccion(ObjetoAgarrable obj)` | Inspecciona un objeto (el de la mano o uno del mundo). Útil para mostrar un objeto al recogerlo o en una cinemática. |
| `void TerminarInspeccion()` | Termina la inspección actual. |
| `bool PuedeAgarrar(ObjetoAgarrable obj)` | Comprueba permisos, `Rigidbody` y masa. |
| `void RecargarCollidersJugador()` | Vuelve a leer los colliders del jugador si cambian. |
| `Estado`, `Objetivo`, `Sujeto`, `CargaLanzamiento`, `Cargando`, `InspeccionDesdeMano` | Estado actual, para tu propia interfaz. |
| `static bool HayInspeccionActiva` | `true` mientras alguien inspecciona. |
| `ObjetoAgarrable.ObtenerLimites()` | Caja que envuelve el objeto. |
| `ObjetoAgarrable.Resaltar(bool)` | Resalta o quita el resaltado a mano. |

**Ejemplo: guardar en un inventario al inspeccionar una llave** (sin tocar el código de la mecánica): en el `ObjetoAgarrable` de la llave, en `Al Terminar Inspeccion`, añade tu método `Inventario.Anadir(...)` y `GameObject.SetActive(false)`. Si el objeto se desactiva mientras está en la mano, el agarrador lo suelta solo.

## 10. Cómo modificarlo

### 10.1 Cambiar las teclas

- **Input System:** en `Entrada Inspeccion`, cambia las rutas. Ejemplos: `<Keyboard>/f`, `<Keyboard>/space`, `<Mouse>/middleButton`, `<Gamepad>/buttonSouth`, `<Gamepad>/rightShoulder`. Puedes poner varias por acción.
- **Input Manager clásico:** cambia los campos `Tecla …` (`KeyCode`).

### 10.2 Usar tu propio sistema de entrada

Si ya tienes tu propio *Input Actions* o usas Rewired, activa `Entrada Externa` y rellena la entrada cada frame desde tu script (con un orden de ejecución anterior al del agarrador, por ejemplo `[DefaultExecutionOrder(-100)]`):

```csharp
entrada.EstablecerExterna(
    agarrarPulsado: misAcciones.Coger.WasPressedThisFrame(),
    inspeccionarPulsado: misAcciones.Inspeccionar.WasPressedThisFrame(),
    lanzarPulsado: misAcciones.Lanzar.WasPressedThisFrame(),
    lanzarMantenido: misAcciones.Lanzar.IsPressed(),
    lanzarSoltado: misAcciones.Lanzar.WasReleasedThisFrame(),
    salirPulsado: misAcciones.Cancelar.WasPressedThisFrame(),
    rotarMantenido: false,
    rotacionGrados: misAcciones.Mirar.ReadValue<Vector2>() * 0.3f,
    zoom: 0f);
```

### 10.3 Usar tu propia interfaz

No añadas `InterfazInspeccion` (o desactiva las partes que no quieras) y haz la tuya con uGUI o UI Toolkit leyendo `Estado`, `Objetivo`, `Sujeto` y `CargaLanzamiento`, o escuchando los eventos. Por ejemplo, en `Al Empezar Inspeccion` muestras tu panel y en `Al Terminar Inspeccion` lo ocultas.

### 10.4 Desenfocar el fondo o que el objeto no atraviese nada al inspeccionar

- **Fondo desenfocado (URP):** crea un *Volume* con *Depth Of Field* desactivado y actívalo desde `Al Empezar Inspeccion` / desactívalo en `Al Terminar Inspeccion`.
- **Que el objeto se vea siempre por encima de las paredes:** crea una capa `Inspeccion`, una segunda cámara *Overlay* (URP, *Camera Stack*) que solo dibuje esa capa, quítasela a la cámara principal, y en los eventos de inspección cambia la capa del objeto (`gameObject.layer`) y de sus hijos.

### 10.5 Cambiar el resaltado

El resaltado está en `ObjetoAgarrable.Resaltar`. Para usar un contorno (*outline*) de otro asset, desactiva `Resaltar Al Apuntar` y usa los eventos `Al Apuntar` / `Al Dejar De Apuntar` para activar y desactivar tu contorno.

### 10.6 Añadir comportamientos nuevos

- **Por objeto, sin programar:** usa los eventos de `ObjetoAgarrable` (abrir una puerta al inspeccionar una nota, sonar algo al coger, activar un diálogo…).
- **Por objeto, programando:** crea un script en el mismo objeto que escuche sus eventos. Por ejemplo, un objeto que se rompe al lanzarlo:

```csharp
using Mecanicas.Inspeccion;
using UnityEngine;

[RequireComponent(typeof(ObjetoAgarrable))]
public class RomperAlChocar : MonoBehaviour
{
    public GameObject piezasRotas;
    public float velocidadParaRomper = 6f;
    bool _lanzado;

    void Awake() => GetComponent<ObjetoAgarrable>().alLanzar.AddListener(() => _lanzado = true);

    void OnCollisionEnter(Collision c)
    {
        if (!_lanzado || c.relativeVelocity.magnitude < velocidadParaRomper) return;
        if (piezasRotas != null) Instantiate(piezasRotas, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
```

- **Lógica general:** todo está en `AgarradorPrimeraPersona.cs`, dividido por regiones (estados, detección, agarrar y soltar, inspección, física). Las funciones de cada estado son `ActualizarLibre`, `ActualizarSujetando` y `ActualizarInspeccionEntrada`; el movimiento físico está en `MoverSujeto` y la colocación de la inspección en `ColocarInspeccion`.

## 11. Solución de problemas

| Problema | Causa y solución |
|---|---|
| No aparece nada al mirar un objeto | El objeto no tiene collider, `ObjetoAgarrable` no está en él ni en un padre, está fuera de `Alcance`, o su capa no está en `Capas Detectables`. |
| La cámara se sigue moviendo al inspeccionar | Falta tu controlador o tu script de cámara en `Componentes A Bloquear`. |
| El objeto atraviesa el suelo al lanzarlo rápido | En su `Rigidbody`, pon `Collision Detection` en *Continuous Dynamic*. |
| El objeto tiembla en la mano | Baja `Rapidez Seguimiento` o revisa que su `Rigidbody` no tenga una masa minúscula (menos de 0,05 kg). |
| Al soltarlo, el objeto me empuja | Normal si estaba dentro del jugador: la colisión se recupera sola cuando deja de tocarte. Si molesta, sube `Distancia Sujecion`. |
| El objeto gira alrededor de un punto raro al inspeccionarlo | Revisa `Renderers`: el centro se calcula con ellos. Si incluye partículas o un renderer muy grande, pon solo los que importan. |
| Mi `MeshCollider` da error con el `Rigidbody` | Márcalo como *Convex*. |
| Las teclas no responden | Comprueba *Active Input Handling* y que las rutas o teclas de `Entrada Inspeccion` son las que esperas. |
| El resaltado no se ve | Tu shader no tiene `_BaseColor` ni `_Color`. Usa los eventos `Al Apuntar` con tu propio efecto. |

## 12. Estructura de archivos

```
Inspeccion/
├── README.md                                   ← este archivo
├── Runtime/
│   ├── Mecanicas.Inspeccion.asmdef
│   ├── AgarradorPrimeraPersona.cs              ← lógica principal (estados, física, inspección)
│   ├── ObjetoAgarrable.cs                      ← componente de cada objeto
│   ├── EntradaInspeccion.cs                    ← controles (Input System o clásico)
│   ├── InterfazInspeccion.cs                   ← interfaz opcional con OnGUI
│   └── Demo/
│       └── ControladorJugadorDemo.cs           ← controlador de ejemplo (borrable)
├── Editor/
│   ├── Mecanicas.Inspeccion.Editor.asmdef
│   └── HerramientasInspeccion.cs               ← menús: demo, jugador, hacer agarrables
└── Demo/                                       ← se crea al usar «Crear escena de demostración»
    ├── DemoInspeccion.unity
    └── Materiales/
```

## 13. Estado y limitaciones

- El código compila contra la API de Unity 2021, con el Input System y con el Input Manager clásico. **Falta abrirlo en el editor de Unity 6 y probarlo jugando**: si al importarlo aparece algún error o algún valor por defecto no se siente bien, se corrige en ese momento.
- La escena de demostración no viene hecha en el repositorio: se genera con el menú (así funciona con cualquier render pipeline y no depende de archivos `.meta`).
- Pensado para un jugador local. Para multijugador habría que sincronizar el estado del objeto sujeto por red.
- La interfaz incluida usa `OnGUI`, que vale para prototipos y juegos pequeños. Para un juego final, lo normal es hacer tu propia interfaz con los eventos (ver [10.3](#103-usar-tu-propia-interfaz)).
