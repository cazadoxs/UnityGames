# Mirada NPC — seguimiento de cabeza y ojos

Mecánica suelta para Unity (3D, pensada para primera persona) que hace que los NPC **sigan al jugador con la cabeza y con los ojos**, sin pasarse nunca de lo que puede girar un cuello o un ojo de verdad.

- Funciona con modelos **Humanoid**, **Generic** (Mixamo, Blender…) y con jerarquías hechas a mano con primitivas.
- Funciona **con o sin Animator**: se aplica encima de la animación, así que un NPC puede estar andando, hablando o en reposo y además mirarte.
- No depende de ningún otro sistema ni paquete. Se copia una carpeta y listo.

---

## Índice

1. [Para qué sirve](#1-para-qué-sirve)
2. [Instalación en un proyecto](#2-instalación-en-un-proyecto)
3. [Probar la demo](#3-probar-la-demo)
4. [Uso rápido (5 minutos)](#4-uso-rápido-5-minutos)
5. [Cómo funciona por dentro](#5-cómo-funciona-por-dentro)
6. [Referencia de todos los parámetros](#6-referencia-de-todos-los-parámetros)
7. [Usarlo desde código](#7-usarlo-desde-código)
8. [Recetas (casos típicos)](#8-recetas-casos-típicos)
9. [Cómo modificarlo y ampliarlo](#9-cómo-modificarlo-y-ampliarlo)
10. [Problemas frecuentes](#10-problemas-frecuentes)
11. [Archivos incluidos](#11-archivos-incluidos)

---

## 1. Para qué sirve

Da vida a los NPC: cuando el jugador se acerca, el NPC gira la cabeza hacia él y le mira a los ojos; si el jugador se mueve, le sigue; si se va detrás de él, el NPC llega hasta el tope del cuello (o deja de mirar, según lo configures) y solo los ojos le siguen de reojo.

Ejemplos de uso:

- Tenderos, recepcionistas y clientes que te miran al acercarte al mostrador.
- Guardias que te siguen con la mirada solo si te ven (con línea de visión, paredes incluidas).
- Personajes que te miran mientras hablan contigo en un diálogo.
- Criaturas con cuellos raros (un búho que gira 170°, un robot con un cuello lento y rígido).
- Hacer que un NPC mire a otro NPC, a un objeto que cae o a un punto concreto durante una cinemática.

---

## 2. Instalación en un proyecto

Requisitos: Unity **2021.3 o superior** (pensado para Unity 6). Vale para Built-in, URP y HDRP.

### Opción A — copiar la carpeta (la más sencilla)

1. Copia la carpeta `MiradaNPC` entera dentro de tu proyecto, por ejemplo en `Assets/Mecanicas/MiradaNPC`.
2. Espera a que Unity compile. No hace falta nada más.

Si no quieres la demo en tu juego final, puedes borrar la carpeta `Demo` (y en ese caso borra también `Editor/CreadorEscenaDemoMirada.cs` y quita `Mecanicas.MiradaNPC.Demo` de la lista de referencias de `Editor/Mecanicas.MiradaNPC.Editor.asmdef`). La carpeta `Tests` también se puede borrar.

### Opción B — como paquete (para compartirlo entre varios proyectos)

En Unity: **Window → Package Manager → + → Add package from git URL…** y pega:

```
https://github.com/cazadoxs/UnityGames.git?path=Mecanicas/MiradaNPC
```

(Si el repositorio es privado necesitas tener acceso con git en tu PC.) También puedes usar **Add package from disk…** y elegir el `package.json` de esta carpeta.

### Ensamblados (asmdef)

La mecánica trae sus propios ensamblados para que compile rápido y no choque con tu código:

| Ensamblado | Qué contiene |
|---|---|
| `Mecanicas.MiradaNPC` | La mecánica en sí (lo único imprescindible) |
| `Mecanicas.MiradaNPC.Editor` | Inspector con botones, arcos de límites y menús |
| `Mecanicas.MiradaNPC.Demo` | Jugador de prueba y constructor de la escena demo |
| `Mecanicas.MiradaNPC.Tests` | Pruebas de la matemática (Test Runner) |

Si tu código está en un asmdef propio y quieres llamar a la mecánica desde él, añade `Mecanicas.MiradaNPC` a sus referencias. Si tu código no usa asmdef (está en `Assembly-CSharp`), no tienes que hacer nada.

---

## 3. Probar la demo

1. Menú **Herramientas → Mecánicas → Mirada NPC → Crear escena demo**.
2. Se crea y se guarda `Demo/MiradaNPC_Demo.unity`. Dale a **Play**.
3. Controles: **WASD** andar, **ratón** mirar, **Shift** correr, **Espacio** saltar, **C** agacharse, **Esc** soltar el ratón (clic para volver).

Cada NPC tiene un cartel con lo que demuestra:

| NPC | Qué enseña |
|---|---|
| **Normal** | Cuello humano (±70°). Si te pones detrás, se queda en el tope y los ojos te siguen de reojo. |
| **Búho** | Límites enormes (±170°) y campo de visión de 360°. |
| **Tímido** | Modo *Volver al frente*: si te sales de ±45°, deja de mirarte. |
| **Vigilante** | Necesita **línea de visión**: escóndete tras el muro y deja de mirarte. Campo de visión de 120°. |
| **Gira el cuerpo** | Cuando la cabeza pasa de 50°, gira el cuerpo entero hacia ti. |
| **Solo ojos** | Sin cadena de cuello: solo se mueven los ojos. |
| **Robot lento** | Cabeza muy lenta (45°/s) y sin ojos. |

Sube por la rampa a la plataforma alta, o agáchate delante de ellos, para ver los límites de arriba y abajo.

Alternativa sin menú: crea una escena vacía, añade un objeto vacío con el componente **Constructor de Demo** y dale a Play.

---

## 4. Uso rápido (5 minutos)

### Paso 1 — el NPC

1. Selecciona la **raíz** de tu NPC (el objeto que tiene el Animator, o el padre de todo el modelo).
2. **Add Component → Mecánicas → Mirada NPC → Seguimiento de Mirada NPC**
   (o menú **Herramientas → Mecánicas → Mirada NPC → Añadir a los objetos seleccionados**).
3. Al añadirlo se **autoconfigura**:
   - Si el modelo es **Humanoid**, usa los huesos del Avatar: Spine, Chest, Neck, Head, LeftEye y RightEye.
   - Si es **Generic** o hecho a mano, busca por nombre: `head`/`cabeza`, `neck`/`cuello`, `spine`/`chest`/`columna`/`pecho` y `eye`/`ojo` (sin distinguir mayúsculas; por ejemplo `mixamorig:Head` funciona).
4. Revisa en el inspector que la lista **Cadena** y la lista **Ojos** tengan los huesos correctos. Si falta alguno, arrástralo a mano.

> **Importante (ojos):** la mecánica necesita saber hacia dónde miran los ojos en reposo. Se calibra sola al empezar, suponiendo que en ese momento el modelo mira al frente del cuerpo. Si tu modelo empieza en una pose rara, pon el modelo en su pose de reposo en el editor y pulsa **Calibrar ojos**.

### Paso 2 — el jugador

No hace falta hacer nada: si no hay objetivo, el NPC mira a la **cámara principal** (etiqueta `MainCamera`), que en primera persona son los ojos del jugador.

Recomendado: añade el componente **Objetivo de Mirada** a la cámara del jugador (o usa el menú **Marcar cámara principal como objetivo**). Así puedes tener más objetivos en la escena y elegir prioridades.

### Paso 3 — ajustar

Selecciona el NPC y mira la vista de escena:

- **Arco verde horizontal**: lo que puede girar el cuello a izquierda y derecha.
- **Arco verde claro vertical**: lo que puede mirar hacia arriba y hacia abajo.
- **Arco blanco**: el campo de visión (dónde "te ve").
- **Esfera blanca**: la distancia máxima.
- **Arcos azules pequeños**: los límites de cada ojo.

Ajusta **Limites Cuello**, **Limites Ojos**, **Distancia Maxima** y **Campo Vision** hasta que te guste. Dale a Play y prueba.

---

## 5. Cómo funciona por dentro

El componente trabaja en `LateUpdate`, es decir **después** de que el Animator haya puesto la pose de la animación de ese frame. Cada frame hace esto:

1. **Recupera la pose base.** Si un Animator ha movido los huesos este frame, parte de esa pose. Si nadie los ha movido (no hay animación), deshace el giro que aplicó el frame anterior. Así nunca se acumulan rotaciones y funciona igual con o sin Animator.
2. **Elige objetivo.** Por orden: el objetivo fijo que le hayas asignado → el mejor `ObjetivoMirada` de la escena (mayor prioridad y, si empatan, el más cercano) → la cámara principal.
3. **Comprueba si lo ve:** distancia máxima, campo de visión horizontal medido desde el frente del cuerpo y, si lo activas, un rayo desde los ojos hasta el objetivo (ignora el propio cuerpo del NPC y el del jugador).
4. **Interés:** solo empieza a mirar cuando lleva *Tiempo Reaccion* segundos viéndote, y sigue mirando *Tiempo Memoria* segundos después de perderte. Esto evita tics.
5. **Calcula los ángulos** (giro horizontal y vertical) desde los ojos del NPC hasta el objetivo, en el espacio del **cuerpo** (`Referencia Cuerpo`). 0° es mirar al frente del cuerpo.
6. **Aplica los límites del cuello:**
   - *Limitar al borde*: recorta el ángulo al máximo permitido (se queda en el tope).
   - *Volver al frente*: si te sales del límite (más el *Margen Limite*), deja de mirar y vuelve al frente. Para volver a mirarte tienes que entrar otra vez dentro del límite (sin margen), así no parpadea en el borde.
7. **Suaviza** el giro con `SmoothDampAngle` (*Suavizado Cabeza* y *Velocidad Maxima Cabeza*), para que gire como una persona y no de golpe.
8. **Reparte el giro por la cadena.** Cada hueso de la cadena (columna, pecho, cuello, cabeza…) gira una fracción del total según su peso. Los pesos se normalizan: por ejemplo, con Spine 0.1, Chest 0.15, Neck 0.3 y Head 0.45, la cabeza aporta el 45% del giro. Como cada hueso es hijo del anterior, la suma final es el giro total.
9. **Ojos.** Después de girar la cabeza, cada ojo calcula su propio ángulo hacia el objetivo **respecto a la cabeza**, lo limita con *Limites Ojos* y lo suaviza (más rápido que la cabeza). Así los ojos compensan lo que el cuello no llega a girar.
10. Si **Girar Cuerpo** está activo y la cabeza tendría que girar más de *Umbral Giro Cuerpo*, gira la raíz del NPC hacia el objetivo.

Todo se multiplica por **Peso** (0 a 1), que sirve para apagar o encender el efecto de forma suave.

### Por qué los límites se miden desde el cuerpo

Los límites son "lo que el cuello puede girar respecto al torso". Por eso se miden desde `Referencia Cuerpo` (normalmente la raíz del NPC) y no desde la cabeza. Si tu personaje se inclina mucho con la columna en la animación y quieres que los límites sigan al torso, pon el hueso del pecho como **Referencia Cuerpo** y deja en la cadena solo cuello y cabeza.

---

## 6. Referencia de todos los parámetros

### Seguimiento de Mirada NPC

**Objetivo**

| Campo | Qué hace | Valor por defecto |
|---|---|---|
| Objetivo | Transform al que mirar siempre. Vacío = buscar solo. | vacío |
| Buscar Automaticamente | Si no hay objetivo fijo, busca `ObjetivoMirada` en la escena o usa la cámara principal. | sí |
| Intervalo Busqueda | Cada cuántos segundos vuelve a elegir el mejor objetivo. 0 = cada frame. | 0.25 |
| Desplazamiento Objetivo | Punto que se mira, relativo al objetivo, cuando este no tiene `ObjetivoMirada` (p. ej. `(0, 1.6, 0)` si el objetivo está en los pies). | 0,0,0 |

**Detección**

| Campo | Qué hace | Valor por defecto |
|---|---|---|
| Distancia Maxima | Metros a los que se fija en el objetivo. | 8 |
| Campo Vision | Grados totales de visión horizontal, desde el frente del cuerpo. 360 = ve en todas direcciones. | 220 |
| Requiere Linea De Vision | Lanza un rayo: si hay una pared en medio, no te ve. | no |
| Capas Que Bloquean | Capas que tapan la vista (quita, por ejemplo, la capa de cristales). | todas |
| Tiempo Reaccion | Segundos viéndote antes de empezar a mirar. | 0.15 |
| Tiempo Memoria | Segundos que sigue mirando después de perderte. | 0.75 |

**Cabeza y cuello**

| Campo | Qué hace | Valor por defecto |
|---|---|---|
| Referencia Cuerpo | Desde dónde se miden los ángulos (su +Z es "al frente" y su +Y "arriba"). | la raíz |
| Origen Mirada | Punto desde el que mira. Vacío = punto medio de los ojos (o la cabeza). | vacío |
| Cadena | Huesos que giran, cada uno con su peso. El orden da igual: se ordenan de padre a hijo solos. | autodetectado |
| Limites Cuello | Izquierda, derecha, arriba y abajo, en grados. | 70 / 70 / 40 / 45 |
| Fuera De Limite | *Limitar al borde* o *Volver al frente*. | Limitar al borde |
| Margen Limite | Tolerancia extra antes de considerar que te has salido (solo en *Volver al frente*). | 15 |
| Suavizado Cabeza | Segundos que tarda en llegar. Más = más perezoso. | 0.25 |
| Velocidad Maxima Cabeza | Tope de velocidad en grados por segundo. | 360 |

**Ojos**

| Campo | Qué hace | Valor por defecto |
|---|---|---|
| Ojos | Huesos de los ojos (opcional). | autodetectado |
| Limites Ojos | Cuánto giran los ojos respecto a la cabeza. | 30 / 30 / 20 / 25 |
| Suavizado Ojos | Segundos que tardan en llegar (los ojos son rápidos). | 0.06 |
| Velocidad Maxima Ojos | Grados por segundo. | 900 |
| Ojos Siguen Fuera De Limite | En *Volver al frente*, si los ojos te siguen de reojo aunque la cabeza haya vuelto. | sí |

**Girar el cuerpo**

| Campo | Qué hace | Valor por defecto |
|---|---|---|
| Girar Cuerpo | Gira todo el NPC cuando la cabeza no da más de sí. Desactívalo si una IA o un NavMeshAgent controla la rotación. | no |
| Cuerpo A Girar | Qué se gira. Vacío = la referencia del cuerpo. | vacío |
| Umbral Giro Cuerpo | A partir de cuántos grados de cabeza empieza a girar el cuerpo. | 50 |
| Velocidad Giro Cuerpo | Grados por segundo. | 90 |

**General y eventos**

| Campo | Qué hace |
|---|---|
| Peso | Intensidad global (0–1). Anímalo para encender/apagar suave. |
| Depuracion | Dibuja una línea al objetivo en la vista de escena mientras juegas (verde = mira, amarilla = fuera de límite, roja = no le ve). |
| Al Empezar A Mirar | Evento cuando empieza a mirar (para sonidos, saludos, una animación…). |
| Al Dejar De Mirar | Evento cuando deja de mirar. |

Botones del inspector: **Autoconfigurar huesos** y **Calibrar ojos**. En Play, el inspector muestra a quién mira y con qué ángulos.

### Objetivo de Mirada

| Campo | Qué hace |
|---|---|
| Desplazamiento | Punto exacto que se mira, relativo a este objeto (p. ej. si lo pones en la raíz del jugador, `(0, 1.6, 0)` para los ojos). |
| Prioridad | Si un NPC ve varios objetivos, elige el de mayor prioridad; si empatan, el más cercano. |

---

## 7. Usarlo desde código

```csharp
using Mecanicas.MiradaNPC;
using UnityEngine;

public class EjemploMirada : MonoBehaviour
{
    public SeguimientoMiradaNPC npc;
    public Transform jugador;
    public Transform cuadro;

    void EmpezarDialogo()
    {
        // Mirar al jugador aunque esté fuera de su campo de visión o detrás de algo
        // (los límites del cuello se siguen respetando).
        npc.MirarA(jugador, forzar: true);
    }

    void SenalarElCuadro()
    {
        npc.MirarAPunto(cuadro.position);   // mirar a un punto fijo
    }

    void TerminarDialogo()
    {
        npc.DejarDeMirar();                 // vuelve al modo automático
    }

    void Update()
    {
        if (npc.EstaMirando)
            Debug.Log($"{npc.name} mira a {npc.ObjetivoActual?.name} ({npc.GiroHorizontal:0}°, {npc.GiroVertical:0}°)");
    }
}
```

API pública de `SeguimientoMiradaNPC`:

| Miembro | Para qué |
|---|---|
| `MirarA(Transform t, bool forzar = false)` | Fija un objetivo. Con `forzar` ignora distancia, campo de visión y línea de visión. |
| `MirarAPunto(Vector3 p)` | Mira a un punto del mundo (siempre forzado). |
| `DejarDeMirar()` | Quita el objetivo fijo/punto y vuelve a buscar solo. |
| `Objetivo` (propiedad) | Lee o cambia el objetivo fijo (sin forzar). |
| `peso` | Intensidad 0–1. |
| `EstaMirando`, `ObjetivoActual` | Estado actual. |
| `GiroHorizontal`, `GiroVertical` | Ángulos actuales de la cabeza respecto al cuerpo. |
| `AutoConfigurar()` | Vuelve a detectar huesos (también en el menú contextual ⋮ del componente). |
| `CalibrarOjos()` | Guarda la orientación de reposo de los ojos. |
| `alEmpezarAMirar`, `alDejarDeMirar` | `UnityEvent` para enganchar otras cosas. |

Encender y apagar suave (por ejemplo, durante una animación en la que no debe mirar):

```csharp
IEnumerator FundirPeso(SeguimientoMiradaNPC npc, float destino, float duracion)
{
    float inicio = npc.peso;
    for (float t = 0; t < duracion; t += Time.deltaTime)
    {
        npc.peso = Mathf.Lerp(inicio, destino, t / duracion);
        yield return null;
    }
    npc.peso = destino;
}
```

---

## 8. Recetas (casos típicos)

**Tendero que solo te mira cuando estás en el mostrador**
Distancia Maxima 3, Campo Vision 160, Fuera De Limite = *Volver al frente*. En *Al Empezar A Mirar* conecta un sonido de "¡Hola!".

**Guardia que te vigila**
Requiere Linea De Vision activo, Campo Vision 110, Distancia Maxima 15, Tiempo Reaccion 0.5, Tiempo Memoria 2. Engancha *Al Empezar A Mirar* a tu IA para que dé la alarma.

**NPC que te mira durante un diálogo**
Al empezar el diálogo `npc.MirarA(camaraJugador, true)`, al terminar `npc.DejarDeMirar()`. Si además quieres que se gire hacia ti, activa *Girar Cuerpo* solo durante el diálogo.

**Varios NPC que se miran entre ellos**
Pon un `ObjetivoMirada` en la cabeza de cada NPC (un NPC nunca se mira a sí mismo). Dale al jugador más prioridad (por ejemplo 10) para que, si está cerca, le miren a él primero.

**Monstruo / criatura**
Cuello largo: Limites Cuello 120/120/60/60, Suavizado Cabeza 0.5. Sin ojos o con ojos con límites pequeños.

**Animación que ya mueve mucho la cabeza** (por ejemplo, gritar)
Baja *Peso* a 0.3–0.5 durante esa animación, o pon el hueso del pecho como *Referencia Cuerpo*.

**Con NavMeshAgent**
Deja *Girar Cuerpo* desactivado (el agente ya rota el cuerpo). La cabeza seguirá al jugador mientras el NPC camina.

---

## 9. Cómo modificarlo y ampliarlo

Toda la lógica está en tres archivos de `Runtime/`, comentados en español:

- `SeguimientoMiradaNPC.cs`: el componente. Está dividido en bloques (objetivo y detección, aplicación a los huesos, utilidades).
- `MiradaMatematica.cs`: cálculos sin estado (dirección → ángulos, límites). Si cambias algo aquí, pasa las pruebas en **Window → General → Test Runner → EditMode**.
- `ObjetivoMirada.cs`: el marcador de objetivos.

Ideas de cambios y dónde tocar:

| Quiero… | Dónde |
|---|---|
| Cambiar los límites humanos por defecto | `LimitesMirada.CuelloHumano` y `LimitesMirada.OjosHumanos` en `MiradaMatematica.cs`. |
| Cambiar cómo se elige el objetivo (p. ej. el que más se mueve, o solo por etiqueta) | `BuscarMejorObjetivo()` en `SeguimientoMiradaNPC.cs`. |
| Que el campo de visión tenga también límite vertical | `EsVisible()`: calcula el pitch igual que el yaw y compáralo. |
| Añadir miradas "distraídas" al azar cuando no hay objetivo | En `LateUpdate`, cuando `mirando` es falso, en vez de `yawDeseado = pitchDeseado = 0` usa unos ángulos aleatorios que cambien cada pocos segundos. |
| Añadir parpadeo | Crea un componente aparte que escale/rote los párpados; no hace falta tocar este. |
| Añadir más nombres de huesos para la autodetección | `AutoConfigurar()`: las palabras que se buscan están en las llamadas a `BuscarPorNombre`. |
| Usar límites distintos para cada hueso | Añade un `LimitesMirada` a la clase `HuesoMirada` y recorta en `AplicarCadena()` el ángulo de cada hueso. |
| Que vaya en tiempo sin escala (menús en pausa) | Cambia `Time.deltaTime` por `Time.unscaledDeltaTime` en `LateUpdate`. |

Consejos:

- El componente usa `[DefaultExecutionOrder(100)]` para ejecutarse después de la mayoría de scripts. Si tienes otro script que también modifica la cabeza en `LateUpdate` (por ejemplo, un IK), decide quién va primero cambiando ese número.
- Si mueves la mecánica a otra carpeta, no pasa nada: la demo localiza su carpeta sola.
- Si cambias el namespace, actualiza también el `rootNamespace` de los asmdef.

---

## 10. Problemas frecuentes

**El NPC no mira.**
Comprueba: que la lista **Cadena** u **Ojos** tenga huesos; que haya una cámara con la etiqueta `MainCamera` o un `ObjetivoMirada` activo; que estés dentro de *Distancia Maxima* y del *Campo Vision*; que *Peso* no sea 0. Activa **Depuracion** para ver la línea al objetivo.

**La cabeza gira hacia un lado raro o se tuerce.**
Los ejes de la *Referencia Cuerpo* deben ser: +Z al frente del personaje y +Y arriba. Si tu modelo viene girado (típico de algunos FBX), usa como referencia un objeto vacío hijo de la raíz orientado correctamente.

**Los ojos miran bizcos o hacia otro sitio.**
Pon el modelo en pose de reposo mirando al frente y pulsa **Calibrar ojos**. Si los ojos del modelo no son huesos sino parte de la malla de la cabeza, no se pueden girar: quita la lista de ojos.

**Con Animator, la cabeza no se mueve.**
Asegúrate de que el componente está activo y de que el Animator no está en *Animate Physics* con un orden raro. Si usas Humanoid con IK de la animación ("IK Pass") que también controla la cabeza, puede pisar el giro; desactiva el *Look At* de ese IK.

**Con el cuello a tope, la cabeza "parpadea" entre mirar y no mirar.**
Estás en *Volver al frente*: sube *Margen Limite*.

**El vigilante me ve a través de las paredes.**
Activa *Requiere Linea De Vision* y comprueba que la pared tiene Collider y está en una capa incluida en *Capas Que Bloquean*.

**Error de compilación por `Unity.InputSystem` en la demo.**
La demo usa el Input System nuevo si está instalado y, si no, el Input Manager clásico. Si en *Project Settings → Player → Active Input Handling* tienes solo "Input System Package (New)" pero no tienes el paquete, instálalo o cambia a "Both".

---

## 11. Archivos incluidos

```
MiradaNPC/
├── README.md                     ← este documento
├── package.json                  ← para instalarlo como paquete
├── Runtime/                      ← LA MECÁNICA (lo único imprescindible)
│   ├── Mecanicas.MiradaNPC.asmdef
│   ├── SeguimientoMiradaNPC.cs   ← componente principal
│   ├── ObjetivoMirada.cs         ← marcador de "esto se puede mirar"
│   └── MiradaMatematica.cs       ← ángulos y límites (LimitesMirada)
├── Editor/
│   ├── Mecanicas.MiradaNPC.Editor.asmdef
│   ├── SeguimientoMiradaNPCEditor.cs  ← inspector, botones y arcos de límites
│   └── CreadorEscenaDemoMirada.cs     ← menús Herramientas → Mecánicas → Mirada NPC
├── Demo/
│   └── Scripts/
│       ├── Mecanicas.MiradaNPC.Demo.asmdef
│       ├── ConstructorDemoMirada.cs   ← construye la escena demo con primitivas
│       └── JugadorDemoMirada.cs       ← jugador en primera persona mínimo (solo demo)
└── Tests/
    └── EditMode/                 ← pruebas de MiradaMatematica
```

La escena `Demo/MiradaNPC_Demo.unity` y sus materiales se generan con el menú (no van en el repositorio para evitar problemas de versión entre editores).

### Estado

El código compila contra las librerías de Unity 2021.3 (comprobado fuera del editor), pero **todavía no se ha abierto en el editor de Unity**. La primera vez que lo pruebes en tu PC, si aparece algún error o algo se ve raro en la demo, avísame.
