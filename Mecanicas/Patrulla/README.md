# Patrulla — tráfico y patrullas por puntos

Mecánica suelta para Unity 3D que hace que cualquier objeto **recorra una ruta de puntos**: guardias que patrullan, coches que circulan por una ciudad, peatones que cruzan, trenes, animales, drones…

- Cada esquina de la ruta puede ser **suave** (una curva natural, como gira un coche) o **en pico** (giro seco en el punto exacto, como un guardia marcial o un robot). Se pueden mezclar en la misma ruta.
- Velocidad realista: **acelera, frena, reduce en las curvas** y se detiene suavemente donde tiene que esperar.
- Para tráfico: **cola automática** con el de delante (también en curvas), **semáforos**, **pasos de peatones**, detección de **obstáculos físicos** (el jugador, una caja) y un **generador de vehículos**.
- Para patrullas: **esperas** en los puntos (con margen aleatorio), **mirar en una dirección** mientras espera, **ida y vuelta**, eventos para enganchar animaciones y sonidos.
- No depende de ningún otro sistema ni paquete (ni NavMesh, ni Input System). Se copia una carpeta y listo.

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

Muchísimos juegos necesitan cosas que se mueven "por su cuenta" siguiendo un camino fijo. Esta mecánica lo resuelve con tres piezas:

| Pieza | Qué es |
|---|---|
| **Ruta de Puntos** | Un objeto vacío cuyos hijos son los puntos del camino. Dibuja la ruta y calcula las curvas. |
| **Punto de Ruta** *(opcional)* | Se pone en un punto concreto para darle reglas propias: esquina distinta, espera, límite de velocidad, bloqueo. |
| **Seguidor de Ruta** | Se pone en lo que se mueve (guardia, coche…). Lee la ruta y la recorre. |

Y dos extras para tráfico: **Semáforo de Ruta** (bloquea y desbloquea puntos por turnos) y **Generador de Tráfico** (crea vehículos en una ruta).

Ejemplos:

- Un guardia que va de la puerta a la torre, se para 3 s mirando al patio y vuelve.
- Coches que dan vueltas por una manzana, frenan en las curvas, hacen cola en el semáforo y paran si el jugador se cruza.
- Peatones que cruzan un paso de cebra solo cuando su semáforo está en verde.
- Un dron que vuela en una ruta 3D (con alturas distintas) inclinándose en las subidas.
- Un tren o vagoneta que hace un recorrido una vez y se destruye al final.

### Esquinas suaves vs. en pico

```
  En pico                      Suave (radio 3)
  ●─────────●                  ●───────╮
            │                           ╲
            │                            │
            ●                            ●
```

- **Pico**: el seguidor pisa exactamente el punto y gira ahí. Con `Velocidad Giro = 0` gira de golpe; con un valor (p. ej. 180 °/s) se ve como alguien que se para un instante y se gira.
- **Suave**: la ruta empieza a curvar `Radio` metros antes del punto y termina `Radio` metros después. Cuanto más grande el radio, más abierta y natural es la curva. El seguidor **no pasa por el punto exacto**, pasa por dentro de la esquina.

---

## 2. Instalación en un proyecto

Requisitos: Unity **2022.3 o superior** (pensado para Unity 6). Vale para Built-in, URP y HDRP.

### Opción A — copiar la carpeta (la más sencilla)

1. Copia la carpeta `Patrulla` entera dentro de tu proyecto, por ejemplo en `Assets/Mecanicas/Patrulla`.
2. Espera a que Unity compile. No hace falta nada más.

Si no quieres la demo en tu juego final, puedes borrar la carpeta `Demo`. La carpeta `Tests` también se puede borrar. Nada más depende de ellas.

### Opción B — `.unitypackage`

Importa `Mecanicas/Paquetes/Patrulla.unitypackage` con **Assets → Import Package → Custom Package…**. Se instala en `Assets/Mecanicas/Patrulla`.

Si cambias algo y quieres regenerar el paquete:

```bash
python3 Mecanicas/Herramientas/empaquetar_unitypackage.py Patrulla
```

### Opción C — como paquete del Package Manager (para compartirlo entre proyectos)

En Unity: **Window → Package Manager → + → Add package from git URL…** y pega:

```
https://github.com/cazadoxs/UnityGames.git?path=Mecanicas/Patrulla
```

(Si el repositorio es privado necesitas tener acceso con git en tu PC.) También puedes usar **Add package from disk…** y elegir el `package.json` de esta carpeta.

### Ensamblados (asmdef)

| Ensamblado | Contenido | Quién lo usa |
|---|---|---|
| `Mecanicas.Patrulla` | Todo lo necesario en juego (`Runtime/`) | Tu código. Se referencia solo (Auto Referenced). |
| `Mecanicas.Patrulla.Editor` | Inspector de la ruta y menús (`Editor/`) | Solo el editor. No entra en la build. |
| `Mecanicas.Patrulla.Demo` | Escena de prueba (`Demo/`) | Nadie. Se puede borrar. |
| `Mecanicas.Patrulla.Tests` | Tests de EditMode (`Tests/`) | Test Runner. Se puede borrar. |

Si tu código está en su propio asmdef, añade `Mecanicas.Patrulla` a sus referencias. Si no usas asmdef, no tienes que hacer nada.

---

## 3. Probar la demo

1. Abre `Demo/DemoPatrulla.unity`.
2. Dale a **Play**. La escena se monta sola (suelo, luz, cámara y todo lo demás).

Qué verás:

| Zona | Qué enseña |
|---|---|
| **Centro** | Circuito con esquinas suaves y una chicane. 6 coches (del generador) que frenan en curvas y hacen cola entre ellos. |
| **Abajo del circuito** | Semáforo de coches y paso de peatones. El peatón solo cruza en su verde; los coches paran en rojo y también si el peatón está en la calzada. |
| **Abajo a la derecha** | Guardia que patrulla en zigzag con **esquinas en pico**, se para en los extremos mirando hacia fuera y vuelve. |
| **Arriba a la izquierda** | Comparación: la **misma ruta** en pico (naranja) y en suave (verde), con un cubo cada una. |
| **Arriba a la derecha** | Dron en una ruta 3D (alturas distintas) que mezcla esquinas suaves y en pico. |

El panel de arriba a la izquierda te deja **cambiar las esquinas del circuito entre suave y pico**, mover el **radio de curva** en directo y pasar de fase el semáforo. En la vista de escena (Scene) verás las rutas como gizmos de colores: amarillo = primer punto, verde = esquina suave, naranja = esquina en pico, rojo = punto bloqueado.

---

## 4. Uso rápido (5 minutos)

### Paso 1 — crear la ruta

1. Clic derecho en la jerarquía → **Mecanicas → Patrulla → Ruta de puntos (circuito)** (o **(abierta)** si no vuelve al principio).
2. Se crea un objeto con 4 hijos `Punto 0…3`. **Esos hijos son la ruta, en ese orden.**
3. Con la ruta seleccionada verás un manejador de posición en cada punto: arrástralos a donde quieras.
4. En el inspector:
   - **Añadir punto al final / al principio** crea más puntos.
   - **Esquina Por Defecto**: `Suave` o `Pico`.
   - **Radio Por Defecto**: cuánto se abre la curva (en metros).
   - **Pegar puntos al suelo** baja cada punto hasta el suelo que tenga debajo (útil en terrenos).

> Truco: puedes duplicar un punto (Ctrl+D) y moverlo; el orden es el de la jerarquía, así que arrástralo para reordenar.

### Paso 2 — el que se mueve

1. Selecciona tu guardia/coche y añade el componente **Seguidor De Ruta** (*Add Component → Mecanicas → Patrulla → Seguidor De Ruta*).
2. Arrastra la ruta al campo **Ruta**.
3. Elige el **Modo**:
   - `Bucle`: da vueltas sin fin (lo normal en circuitos y tráfico).
   - `IdaYVuelta`: llega al final, da media vuelta y vuelve (patrulla clásica).
   - `UnaVez`: hace el recorrido y se para (o se destruye con *Destruir Al Terminar*).
4. Ajusta **Velocidad** (m/s): ~1.5 paseo, ~3 patrulla, ~5 corriendo, 8–14 coche en ciudad.

Atajo: selecciona varios objetos y usa **Herramientas → Mecánicas → Patrulla → Hacer que los seleccionados sigan la ruta más cercana**.

### Paso 3 — esperas y reglas por punto

Añade **Punto De Ruta** al punto que quieras y rellena lo que te interese:

- **Espera** (+ *Espera Aleatoria*): segundos que se para ahí.
- **Mirar En Direccion Del Punto**: mientras espera, mira hacia el eje Z (flecha azul) del punto. Gira el punto para elegir hacia dónde mira el guardia.
- **Usar Esquina Propia** + **Esquina** + **Radio**: esta esquina distinta del resto (p. ej. una curva cerrada en pico en una ruta suave).
- **Velocidad Maxima**: llega a ese punto a esa velocidad como mucho (curvas peligrosas, zona escolar).
- **Bloqueado**: los seguidores se paran antes de llegar (`Distancia Parada` metros antes) hasta que se desbloquee.

---

## 5. Cómo funciona por dentro

### 5.1 La ruta se "hornea"

`RutaDePuntos` no guarda una curva matemática que haya que resolver cada frame. Cuando algo cambia, calcula una **polilínea** (una lista de puntos muy juntos, las *muestras*) y la distancia acumulada desde el inicio hasta cada muestra. Esto se llama hornear.

Para cada esquina:

1. Mira el punto anterior y el siguiente.
2. Si la esquina es **Pico**, la muestra es el propio punto.
3. Si es **Suave**, calcula un punto de **entrada** (`radio` metros antes, sobre el tramo de llegada) y uno de **salida** (`radio` metros después, sobre el tramo de salida) y une ambos con una **curva de Bézier cuadrática** cuyo punto de control es la esquina. Esa curva es tangente a los dos tramos, así que no hay saltos de dirección.
4. El radio se recorta a la mitad del tramo más corto para que dos curvas seguidas no se pisen. Por eso un radio enorme no rompe nada: simplemente la curva se abre todo lo que cabe.
5. Si el giro es casi nulo (puntos alineados) no se crea curva.

En un **circuito** (ruta cerrada) también se redondean el primer y el último punto, y la ruta empieza en la *salida* del punto 0. En una ruta **abierta** el primer y el último punto nunca tienen curva.

Además se guarda, para cada punto, la **distancia a la que "se pasa" por él**: el punto exacto en esquinas en pico, el centro de la curva en esquinas suaves. Así el seguidor sabe cuándo ha llegado a un punto aunque no lo pise.

Con esto, preguntar "¿dónde estoy a 37,5 m del inicio?" es una búsqueda binaria en la tabla de distancias más una interpolación: barato aunque haya cientos de seguidores.

**Cuándo se recalcula:** en el editor, siempre que mueves un punto o cambias un ajuste. En juego, solo si activas *Ruta Dinamica* (por ejemplo, puntos que se mueven con una plataforma) o si llamas a `Reconstruir()`.

### 5.2 El seguidor recorre distancias, no puntos

`SeguidorDeRuta` guarda **una distancia** a lo largo de la ruta y el **sentido** (+1 en el orden de los puntos, −1 al revés). Cada frame:

1. **Calcula la velocidad objetivo**, que es la más pequeña de:
   - la velocidad de crucero × `Multiplicador Velocidad`;
   - la reducción por **curva**: compara la dirección actual con la de `Distancia Anticipacion` metros más adelante; cuanto mayor el ángulo, más frena (hasta `Factor Velocidad En Curva` en giros de `Angulo Curva Completa` o más);
   - la necesaria para **llegar a un punto con espera** casi parado, o a su **velocidad máxima**;
   - la necesaria para **pararse antes de un punto bloqueado** (`Distancia Parada` metros antes);
   - la necesaria para **no comerse al de delante** en la misma ruta (`Separacion Minima`);
   - la necesaria para **no chocar con un obstáculo físico** (si *Detectar Obstaculos* está activo).

   Todas las frenadas usan la fórmula física `v = √(v_final² + 2·frenada·distancia)`: la velocidad máxima a la que puedes ir para, frenando con `Frenada`, llegar a esa distancia a la velocidad final. Por eso los seguidores **empiezan a frenar a tiempo** y no de golpe.

2. **Acelera o frena** hacia la velocidad objetivo con `Aceleracion` o `Frenada` (m/s²).
3. **Avanza** la distancia. Si cruza el próximo punto, se dispara `Al Llegar A Punto`, se aplica la espera, y si es el final de la ruta se aplica el modo (dar la vuelta, parar…).
4. **Coloca y gira** el objeto: posición de la ruta en esa distancia + `Desplazamiento Lateral` (carril) + ajuste al suelo opcional. Para girar mira a un punto `Adelanto Mirada` metros más adelante y rota como mucho `Velocidad Giro` grados por segundo.

### 5.3 Cola de tráfico

Todos los seguidores activos están en una lista estática (`SeguidorDeRuta.Activos`). Para la cola, cada uno busca al seguidor **de la misma ruta, mismo sentido y mismo carril** (desplazamiento lateral a menos de 1 m) que tiene más cerca por delante, midiendo **a lo largo de la ruta**. Por eso funciona en curvas, donde un rayo hacia delante no vería al coche que va girando.

Para cosas que no son de la misma ruta (el jugador, otro coche que se cruza, una caja) está la **detección física**: un `SphereCast` hacia delante desde el morro.

### 5.4 Semáforos y bloqueo por sentido

Un `PuntoDeRuta` bloqueado detiene a quien vaya hacia él. Si cuando se bloquea el seguidor ya ha pasado la línea de stop, **sigue** (como un coche que pilla el ámbar encima del paso).

`Sentido Bloqueo` permite que el bloqueo afecte solo a quien va en un sentido. En un paso de peatones de ida y vuelta (`acera → bordillo A → bordillo B → acera`): el bordillo A se bloquea *SoloIda* y el bordillo B *SoloVuelta*. Así nadie se queda parado en mitad de la calzada.

`SemaforoDeRuta` alterna dos grupos de puntos: `VerdeA → AmbarA → VerdeB → AmbarB`. En ámbar están los dos grupos bloqueados para vaciar el cruce.

---

## 6. Referencia de todos los parámetros

### Ruta De Puntos

| Parámetro | Qué hace |
|---|---|
| **Cerrada** | El último punto se une con el primero (circuito). Necesita 3 puntos o más; con 2 se trata como abierta. |
| **Esquina Por Defecto** | `Suave` o `Pico` para los puntos que no tienen esquina propia. |
| **Radio Por Defecto** | Metros antes y después del punto donde empieza y acaba la curva. Se recorta solo si el tramo es corto. |
| **Resolucion Curva** | Segmentos con los que se dibuja cada curva. 8–16 es suficiente; más solo si las curvas son enormes. |
| **Usar Hijos Como Puntos** | Los puntos son los hijos directos (en orden). Desactívalo para usar la lista **Puntos** con Transforms de cualquier parte de la escena. Los hijos desactivados no cuentan. |
| **Puntos** | Lista manual (solo si lo anterior está desactivado). |
| **Ruta Dinamica** | Recalcula en juego si algún punto se mueve. Desactivado por rendimiento. |
| **Color Ruta / Mostrar Siempre** | Color del gizmo y si se ve aunque no esté seleccionada. |

Botones del inspector: añadir punto al final/al principio, invertir orden, pegar puntos al suelo, todas las esquinas en pico/suaves (y quitar las esquinas propias), recalcular, y activar/desactivar los manejadores en escena.

### Punto De Ruta

| Parámetro | Qué hace |
|---|---|
| **Usar Esquina Propia / Esquina / Radio** | Esquina y radio de este punto, ignorando los de la ruta. |
| **Espera** | Segundos que se para aquí. 0 = no se para. |
| **Espera Aleatoria** | Segundos extra al azar (0..valor) sumados a la espera. |
| **Mirar En Direccion Del Punto** | Durante la espera mira hacia el eje Z del punto. |
| **Velocidad Maxima** | Velocidad máxima al pasar por aquí (m/s). 0 = sin límite. |
| **Bloqueado** | Los seguidores se paran antes de llegar. Se cambia desde código o con un semáforo. |
| **Sentido Bloqueo** | `Ambos`, `SoloIda` (solo a quien va en el orden de los puntos) o `SoloVuelta`. |

### Seguidor De Ruta

**Ruta**

| Parámetro | Qué hace |
|---|---|
| **Ruta** | La `RutaDePuntos` que recorre. |
| **Modo** | `Bucle`, `IdaYVuelta` o `UnaVez`. En una ruta abierta, `Bucle` vuelve de golpe al primer punto (para tráfico en rutas abiertas usa mejor el generador). En un circuito, `IdaYVuelta` da media vuelta cada vez que completa una vuelta. |
| **Punto Inicial** | Índice del punto donde empieza. |
| **Empezar En Lugar Mas Cercano** | Ignora lo anterior y empieza en el sitio de la ruta más cercano a donde está colocado. Cómodo para colocar coches a mano. |
| **Sentido Inverso** | Recorre la ruta al revés. |
| **Empezar Automaticamente** | Arranca en `Start`. Desactívalo para llamar a `Iniciar()` cuando quieras. |
| **Destruir Al Terminar** | En `UnaVez`, destruye el objeto al acabar. |

**Velocidad**

| Parámetro | Qué hace |
|---|---|
| **Velocidad** | Velocidad de crucero (m/s). |
| **Aceleracion** | m/s que gana por segundo. |
| **Frenada** | m/s que pierde por segundo. **También decide cuánto antes empieza a frenar**: con poca frenada, frena desde más lejos. |
| **Multiplicador Velocidad** | Para cambiar la velocidad desde código sin tocar la de crucero (alarma = 2, cansado = 0.6). |

**Curvas**

| Parámetro | Qué hace |
|---|---|
| **Reducir En Curvas** | Activa la frenada en curvas. |
| **Factor Velocidad En Curva** | Fracción de la velocidad en la curva más cerrada (0.4 = 40 %). |
| **Angulo Curva Completa** | A partir de este ángulo se aplica la reducción completa. |
| **Distancia Anticipacion** | Metros por delante en los que busca curvas. Más = frena antes. |

**Rotación**

| Parámetro | Qué hace |
|---|---|
| **Rotar** | Si gira para mirar hacia donde va. Desactívalo para una plataforma o un ascensor. |
| **Solo Eje Y** | Solo gira en horizontal. Desactívalo para que se incline en cuestas (drones, montañas rusas). |
| **Velocidad Giro** | Grados por segundo. 0 = instantáneo. |
| **Adelanto Mirada** | Mira a un punto de la ruta X metros por delante. Suaviza el giro como al conducir. 0 = mira justo hacia donde va. |

**Carril**

| Parámetro | Qué hace |
|---|---|
| **Desplazamiento Lateral** | Metros a la derecha de la ruta (negativo = izquierda). Como se mide respecto al sentido de marcha, con una sola ruta puedes tener los dos sentidos: un coche con `+1.75` y otro con `+1.75` y *Sentido Inverso* van cada uno por su carril. |

**Suelo**

| Parámetro | Qué hace |
|---|---|
| **Ajustar Al Suelo** | Lanza un rayo hacia abajo y pega el pivote al suelo. |
| **Capas Suelo** | Capas que cuentan como suelo. Quita la capa del propio personaje. |
| **Altura Sobre Suelo** | Separación entre el suelo y el pivote. |
| **Altura Rayo Suelo** | Desde cuánto más arriba se lanza el rayo. |

**Tráfico**

| Parámetro | Qué hace |
|---|---|
| **Respetar Cola En Ruta** | Guarda distancia con el de delante en la misma ruta, sentido y carril. |
| **Separacion Minima** | Distancia de pivote a pivote con el de delante. Para coches: largo del coche + hueco. |
| **Detectar Obstaculos** | Activa el SphereCast hacia delante. |
| **Capas Obstaculos** | Capas que le hacen frenar. Quita el suelo si tu suelo es irregular. |
| **Distancia Deteccion / Radio Deteccion** | Alcance y grosor del rayo. |
| **Altura Deteccion / Inicio Deteccion** | Desde dónde sale el rayo (altura y metros por delante del pivote, p. ej. el morro). |
| **Distancia Seguridad** | Hueco que deja con el obstáculo. |
| **Distancia Parada** | Metros antes de un punto bloqueado donde se detiene. |

**Movimiento y animación**

| Parámetro | Qué hace |
|---|---|
| **Modo Movimiento** | `Transform` (mueve el Transform en `Update`) o `RigidbodyCinematico` (usa `MovePosition`/`MoveRotation` en `FixedUpdate`; el Rigidbody debe ser *Is Kinematic*; empuja a otros cuerpos y dispara colisiones). |
| **Cuerpo** | El Rigidbody. Vacío = lo busca en el objeto. |
| **Animator / Parametro Velocidad** | Si el Animator tiene un parámetro **Float** con ese nombre, recibe la velocidad actual en m/s cada frame. Úsalo en un Blend Tree (0 = quieto, 1.5 = andar, 4 = correr). |

**Eventos**

| Evento | Cuándo |
|---|---|
| **Al Llegar A Punto (int)** | Al pasar por cualquier punto. Recibe el índice. |
| **Al Empezar Espera (int)** / **Al Terminar Espera (int)** | Al pararse y al reanudar en un punto con espera. |
| **Al Completar Vuelta** | Al completar un circuito o llegar a un extremo (en IdaYVuelta, cada extremo). |
| **Al Terminar Ruta** | Al acabar en modo UnaVez. |

### Semaforo De Ruta

| Parámetro | Qué hace |
|---|---|
| **Grupo A / Grupo B** | Puntos que se bloquean cuando su grupo está en rojo. B puede quedar vacío (una barrera que se abre y se cierra). |
| **Tiempo Verde A / Verde B / Ambar** | Duración de cada fase. Ámbar 0 = sin ámbar. |
| **Fase Inicial** | Con qué fase empieza. |
| **Automatico** | Si cambia solo. Desactívalo para controlarlo con `CambiarFase()` / `AplicarFase()` (un botón, un evento de misión…). |
| **Luces A / Luces B** | Renderers que se pintan del color de su grupo (verde, ámbar o rojo). |
| **Al Cambiar Fase** | Evento en cada cambio (para un sonido, por ejemplo). |

### Generador De Trafico

| Parámetro | Qué hace |
|---|---|
| **Ruta / Prefabs** | Dónde y qué crear (elige al azar entre los prefabs). Si un prefab no tiene `SeguidorDeRuta`, se le añade uno con valores por defecto; lo normal es que el prefab ya lo traiga configurado. |
| **Punto De Aparicion / Sentido Inverso / Desplazamiento Lateral** | Dónde aparecen, en qué sentido y en qué carril. |
| **Maximo A La Vez** | Límite de vehículos vivos. |
| **Intervalo / Intervalo Aleatorio** | Cada cuánto aparece uno nuevo. |
| **Repartir Al Empezar** | En circuitos, crea N vehículos repartidos por toda la ruta al empezar (así no empiezan todos en fila). |
| **Espacio Libre Necesario** | No crea uno si hay otro a menos de esa distancia del punto de aparición. |
| **Rango Velocidad** | Velocidad aleatoria para cada vehículo. |

En rutas **abiertas** los vehículos creados se ponen en `UnaVez` + *Destruir Al Terminar*; en **circuitos**, en `Bucle`.

---

## 7. Usarlo desde código

```csharp
using Mecanicas.Patrulla;
using UnityEngine;

public class Guardia : MonoBehaviour
{
    public SeguidorDeRuta seguidor;
    public RutaDePuntos rutaDeAlarma;

    void Start()
    {
        // Reaccionar al llegar a cada punto.
        seguidor.AlLlegarAPunto += (s, punto) => Debug.Log($"{name} ha llegado al punto {punto}");
    }

    public void DarLaAlarma()
    {
        seguidor.multiplicadorVelocidad = 2f;              // corre
        seguidor.AsignarRuta(rutaDeAlarma, -1);            // cambia de ruta, empezando en el lugar más cercano
    }

    public void VerAlJugador()  { seguidor.Pausar(); }     // frena hasta pararse (tu IA toma el control)
    public void PerderAlJugador() { seguidor.Reanudar(); }
}
```

API principal de `SeguidorDeRuta`:

| Miembro | Para qué |
|---|---|
| `Iniciar()` | Coloca el seguidor en la ruta según sus ajustes y arranca. |
| `AsignarRuta(ruta, punto = -1, inverso = false)` | Cambia de ruta. `punto = -1` = el lugar más cercano. |
| `Pausar()` / `Reanudar()` | Frena hasta parar / sigue. |
| `SaltarEspera()` | Corta la espera actual. |
| `InvertirSentido()` | Da media vuelta en el sitio. |
| `Distancia`, `Sentido`, `VelocidadActual`, `SiguientePunto`, `Esperando`, `Terminado`, `Pausado` | Estado (solo lectura). |
| `SeguidorDeRuta.Activos` | Lista de todos los seguidores activos. |

API principal de `RutaDePuntos`:

| Miembro | Para qué |
|---|---|
| `Longitud`, `NumeroDePuntos`, `EsValida` | Información de la ruta. |
| `PosicionEn(d)`, `DireccionEn(d)`, `Evaluar(d, out pos, out dir)` | Posición/dirección a `d` metros del inicio (da la vuelta en circuitos). |
| `DistanciaMasCercana(pos)`, `PuntoMasCercano(pos)` | Para colocar algo en la ruta o saber dónde está. |
| `DistanciaDePunto(i)`, `ObtenerPunto(i)`, `ObtenerAjustes(i)` | Datos de cada punto. |
| `Reconstruir()` / `MarcarSucia()` | Recalcular ahora / en el próximo uso (tras mover puntos por código). |
| `AlReconstruir` | Evento al recalcular (la demo lo usa para dibujar la línea en el Game view). |

Bloquear un punto desde código (una barrera, un puente levadizo):

```csharp
puntoDelPuente.bloqueado = true;   // todos se paran antes
puntoDelPuente.bloqueado = false;  // siguen
```

---

## 8. Recetas (casos típicos)

**Guardia de castillo, rígido y marcial**
Ruta abierta, *Esquina Por Defecto* = Pico. Seguidor en `IdaYVuelta`, velocidad 1.8, *Reducir En Curvas* desactivado, *Adelanto Mirada* = 0, *Velocidad Giro* = 200. En los extremos, `PuntoDeRuta` con *Espera* 3, *Espera Aleatoria* 2 y *Mirar En Direccion Del Punto*.

**Paseante natural**
Ruta cerrada, Suave, radio 2–4. Velocidad 1.3, *Adelanto Mirada* 1, *Velocidad Giro* 120. Algunos puntos con *Espera* 0 + *Espera Aleatoria* 4 para que a veces se pare y otras no.

**Tráfico de ciudad en una manzana**
Ruta cerrada, Suave, radio 6–8, por el centro de la calle. Prefab de coche con `SeguidorDeRuta` (*Separacion Minima* = largo + 2.5, *Detectar Obstaculos* con *Inicio Deteccion* = medio largo, *Desplazamiento Lateral* = 1.75). `GeneradorDeTrafico` con *Repartir Al Empezar* y *Maximo A La Vez*. Para el carril contrario: otro generador con *Sentido Inverso* en la misma ruta.

**Cruce con semáforo**
Pon un `PuntoDeRuta` en cada ruta justo en la entrada al cruce (en un tramo recto: si el punto es una esquina suave, el seguidor se para antes del centro de la curva). Un `SemaforoDeRuta` con los de una calle en el grupo A y los de la otra en el grupo B. Los coches que ya están dentro terminan de cruzar; los que llegan se paran a *Distancia Parada* del punto.

**Paso de peatones**
Ruta abierta `acera → bordillo → bordillo → acera`, peatón en `IdaYVuelta`. Bordillo 1 con *Sentido Bloqueo* = SoloIda, bordillo 2 con SoloVuelta; los dos en el grupo B del semáforo (y los coches en el A). Pon *Detectar Obstaculos* a los coches por si el peatón se queda en medio.

**Tren o vagoneta que hace un viaje**
Ruta abierta, Suave con radio grande. Seguidor en `UnaVez`, *Empezar Automaticamente* desactivado. Llama a `Iniciar()` cuando el jugador suba. Usa `Al Terminar Ruta` para abrir las puertas.

**Coche con Rigidbody que empuja cajas**
*Modo Movimiento* = RigidbodyCinematico y un Rigidbody con *Is Kinematic*. Así las cajas salen despedidas y se disparan `OnCollisionEnter`.

**Dron o pájaro en 3D**
Puntos a distintas alturas, *Solo Eje Y* desactivado para que se incline, *Adelanto Mirada* 2–3 para que el morro anticipe la curva.

**Animación de andar/correr**
En el Animator crea un parámetro Float `Velocidad` y un Blend Tree (0 quieto, 1.5 andar, 4 correr). El seguidor lo rellena solo. Usa *Al Empezar Espera* para disparar una animación de "mirar alrededor".

---

## 9. Cómo modificarlo y ampliarlo

Todo el código está comentado en español. Dónde tocar según lo que quieras:

| Quiero… | Dónde |
|---|---|
| Otro tipo de curva (por ejemplo Catmull-Rom que pase por los puntos, o arcos de circunferencia) | `RutaDePuntos.Hornear()` y `RutaDePuntos.Esquina()`. Añade un valor a `TipoEsquina` y genera las muestras que quieras entre *entrada* y *salida*. Mientras rellenes `muestras`, `distancias` y `distanciaDePunto`, el resto sigue funcionando. |
| Que frene de otra manera en curvas (por curvatura real, por ejemplo) | `SeguidorDeRuta.VelocidadObjetivo()`, bloque `1) Curvas`. |
| Nuevas reglas por punto (por ejemplo "aquí toca el claxon", "aquí abre una puerta") | Añade el campo a `PuntoDeRuta` y léelo en `SeguidorDeRuta.Llegar()`. Para cosas puntuales, mejor usa el evento `Al Llegar A Punto` sin tocar el código. |
| Elegir entre varios caminos en un cruce (red de rutas) | Escucha `AlLlegarAPunto` y, en el punto del cruce, llama a `AsignarRuta(otraRuta, puntoDeEntrada)`. Es la forma más simple de hacer una red: cada calle es una ruta abierta y los cruces reparten. |
| Que se mueva con NavMesh en vez de en línea | En `AplicarPose()`, en vez de colocar el Transform, pasa la posición objetivo a `NavMeshAgent.SetDestination()` (o `Move`). |
| Que se mueva con CharacterController | En `AplicarPose()`, sustituye `SetPositionAndRotation` por `controller.Move(pos - transform.position)`. |
| Más fases en el semáforo (giro a la izquierda, peatones aparte) | `SemaforoDeRuta`: convierte los dos grupos en una lista de grupos y recorre las fases en `CambiarFase()`. |
| Tráfico en ambos sentidos en dos rutas distintas | Simplemente dos rutas; la cola solo mira la misma ruta, y la detección física cubre lo demás. |
| Más rendimiento con cientos de seguidores | El coste está en `HuecoConElDeDelante()` (recorre todos los activos) y en `DistanciaAObstaculo()` (un SphereCast). Desactiva lo que no necesites, o agrupa los seguidores por ruta en un diccionario. |

Consejos:

- Si añades campos a la ruta que cambien la forma, inclúyelos en `CalcularFirmaAjustes()` para que el editor redibuje al cambiarlos.
- Los tests (`Tests/EditMode/RutaDePuntosTests.cs`) comprueban la geometría de la ruta. Ejecútalos en **Window → General → Test Runner → EditMode** después de tocar el horneado.

---

## 10. Problemas frecuentes

| Problema | Causa / solución |
|---|---|
| El seguidor no se mueve | Falta la ruta, la ruta tiene menos de 2 puntos, *Velocidad* es 0, o tiene a alguien parado delante (cola). Mira si hay un punto rojo (bloqueado) en la ruta. |
| Se queda parado detrás de otro que va en sentido contrario | No debería: la cola solo cuenta el mismo sentido y carril. Si es la detección física, ajusta *Capas Obstaculos* o *Radio Deteccion*. |
| Frena para siempre sin nada delante | La detección física está chocando con el suelo o con su propio modelo de otra capa. Sube *Altura Deteccion*, baja *Radio Deteccion* o quita esas capas. |
| La curva no aparece | Los dos tramos son casi rectos, el punto es el primero/último de una ruta abierta, o el radio es 0. |
| La curva es demasiado pequeña aunque el radio sea grande | El radio se recorta a la mitad del tramo más corto. Separa los puntos. |
| Gira de golpe en las esquinas en pico | Es lo esperado con *Velocidad Giro* = 0. Ponle un valor (180–360) o usa *Adelanto Mirada*. |
| Atraviesa paredes | La mecánica sigue la ruta tal cual. Coloca los puntos para que la ruta no las cruce (con esquinas suaves la ruta va por dentro de la esquina). |
| Flota o se hunde en un terreno | Activa *Ajustar Al Suelo* y elige bien *Capas Suelo*, o usa **Pegar puntos al suelo** en la ruta. |
| La animación no cambia | El parámetro del Animator debe ser **Float** y llamarse igual que *Parametro Velocidad*. |
| En una ruta abierta en Bucle "teletransporta" | Es así a propósito: vuelve al primer punto. Para tráfico en rutas abiertas usa el generador (destruye al final y crea al principio). |

---

## 11. Archivos incluidos

```
Patrulla/
├── README.md                       ← este documento
├── package.json                    ← para instalar como paquete
├── Runtime/                        ← lo que va al juego (asmdef Mecanicas.Patrulla)
│   ├── RutaDePuntos.cs             ← la ruta: horneado de curvas, consultas, gizmos
│   ├── PuntoDeRuta.cs              ← ajustes por punto (esquina, espera, límite, bloqueo)
│   ├── SeguidorDeRuta.cs           ← recorre la ruta: velocidad, curvas, cola, obstáculos
│   ├── SemaforoDeRuta.cs           ← semáforo de dos fases
│   └── GeneradorDeTrafico.cs       ← crea vehículos en una ruta
├── Editor/                         ← solo editor (asmdef Mecanicas.Patrulla.Editor)
│   ├── RutaDePuntosEditor.cs       ← inspector con botones y manejadores en escena
│   └── MenuPatrulla.cs             ← menús de GameObject y Herramientas
├── Demo/                           ← se puede borrar
│   ├── DemoPatrulla.unity          ← escena de prueba
│   └── Scripts/                    ← montaje de la demo (DemoPatrulla.cs y ayudantes)
└── Tests/EditMode/                 ← tests de la geometría de la ruta
```

### Estado

Escrito y probado fuera de Unity: el código compila contra una réplica mínima de la API de Unity y la lógica (horneado de la ruta, bucle, ida y vuelta, una vez, sentido inverso, bloqueo y desbloqueo, cola) se ha simulado con éxito, y los tests de EditMode pasan en esa simulación. **Falta abrirlo en el editor de Unity** para confirmar que compila allí y ver la demo en marcha.
