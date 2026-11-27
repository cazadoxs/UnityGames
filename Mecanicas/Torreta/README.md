# Sistema de torretas

Torretas reutilizables para cualquier juego 3D de Unity 6: buscan objetivos, giran para apuntar, disparan balas reales (con o sin gravedad), retroceden, echan fogonazo y suenan. Funcionan con Built-in, URP o HDRP y no dependen de ningún otro paquete.

Tres formas de apuntar:

| Modo | Qué hace | Ejemplo típico |
|---|---|---|
| **Fija** | No gira nunca. Dispara recto cuando algo entra en su cono de visión. | Trampa en un pasillo, cañón de pared, lanzallamas fijo |
| **Un eje** | Gira solo alrededor de un eje (por defecto el vertical: izquierda y derecha). El cañón no sube ni baja. | Torreta de tower defense, ametralladora de búnker |
| **Todos los ángulos** | Gira y además eleva el cañón: apunta a cualquier dirección. Puede compensar la gravedad de las balas. | Antiaérea, cañón de tanque, mortero |

---

## Índice

1. [Instalación](#1-instalación)
2. [Prueba rápida: la escena de demostración](#2-prueba-rápida-la-escena-de-demostración)
3. [Qué hay en la carpeta](#3-qué-hay-en-la-carpeta)
4. [Cómo funciona por dentro](#4-cómo-funciona-por-dentro)
5. [Referencia de todos los campos](#5-referencia-de-todos-los-campos)
6. [Montar una torreta con tu propio modelo 3D](#6-montar-una-torreta-con-tu-propio-modelo-3d)
7. [Implementarla en tu juego](#7-implementarla-en-tu-juego)
8. [Cómo modificar el sistema](#8-cómo-modificar-el-sistema)
9. [Problemas frecuentes](#9-problemas-frecuentes)
10. [Estado](#10-estado)

---

## 1. Instalación

1. Copia la carpeta `Torreta` entera dentro de tu proyecto, por ejemplo en `Assets/Mecanicas/Torreta`. Puede ir en cualquier sitio de `Assets`: los menús encuentran solos su carpeta.
2. Espera a que Unity compile. No hace falta instalar nada más.

El código está separado en cuatro *assemblies* (archivos `.asmdef`) para que compile rápido y no se mezcle con tu código:

| Assembly | Carpeta | Para qué |
|---|---|---|
| `Mecanicas.Torretas` | `Runtime/` | El sistema en sí. Es lo único imprescindible. |
| `Mecanicas.Torretas.Demo` | `Demo/` | Dianas y montaje de la escena de prueba. Puedes borrarla en el juego final. |
| `Mecanicas.Torretas.Editor` | `Editor/` | Inspector personalizado y menús. Solo existe en el editor. |
| `Mecanicas.Torretas.Tests` | `Tests/EditMode/` | Pruebas de los cálculos (Window > General > Test Runner). |

Si tu código usa sus propios `.asmdef`, añade `Mecanicas.Torretas` a sus referencias para poder usar las clases. Si no usas `.asmdef`, no tienes que hacer nada.

## 2. Prueba rápida: la escena de demostración

Menú **Herramientas > Mecánicas > Torreta > Crear escena de demostración** y dale a **Play**.

Crea la escena `Demo/DemoTorretas.unity` con:

- Las tres torretas, una de cada modo, con una etiqueta encima.
- Un muro marrón para ver que no disparan a través de las paredes.
- Tres dianas: una que va y viene por un carril (pasa por delante de la torreta fija), una que da vueltas alrededor de la torreta de un eje y una voladora que sube y baja (para ver cómo la de todos los ángulos eleva el cañón y compensa la caída de las balas).

Las dianas parpadean en blanco al recibir un impacto y reaparecen tres segundos después de morir.

Otra forma: crea una escena vacía, añade un objeto vacío con el componente **Demo Torretas** y dale a Play.

Para meter una torreta suelta en cualquier escena: clic derecho en la jerarquía > **Mecánicas > Torreta > Fija / Giratoria (un eje) / Todos los ángulos**.

Para tener prefabs de ejemplo: **Herramientas > Mecánicas > Torreta > Guardar torretas de ejemplo como prefabs** (los deja en `Prefabs/`).

> Las torretas de ejemplo están hechas con cubos y cilindros. Los materiales que generan se guardan en `Generado/`. Puedes borrar esa carpeta cuando quieras: se vuelve a crear.

## 3. Qué hay en la carpeta

```
Torreta/
├── README.md                 este documento
├── Runtime/
│   ├── Torreta.cs            el cerebro: busca, apunta y dispara
│   ├── TorretaEfectos.cs     retroceso, fogonazo, luz, partículas, Animator y sonidos
│   ├── Proyectil.cs          la bala: vuela, choca, hace daño
│   ├── PoolProyectiles.cs    reutiliza las balas para que no haya tirones
│   ├── MatematicaTorreta.cs  cálculos: predicción, tiro parabólico, ángulos
│   ├── IDaniable.cs          interfaz "esto puede recibir daño" + InfoDanio
│   ├── Salud.cs              vida lista para usar (implementa IDaniable)
│   ├── SonidoProcedural.cs   sonidos generados por código (para prototipos)
│   ├── EfectoTemporal.cs     chispa de impacto sencilla
│   └── FabricaTorretas.cs    construye torretas de ejemplo con formas básicas
├── Demo/
│   ├── DemoTorretas.cs       monta la escena de prueba
│   └── ObjetivoMovil.cs      dianas que se mueven y reaparecen
├── Editor/
│   ├── TorretaEditor.cs      inspector que oculta lo que no sirve en cada modo
│   └── TorretaMenu.cs        menús de creación
└── Tests/EditMode/
    └── MatematicaTorretaTests.cs
```

## 4. Cómo funciona por dentro

### 4.1 Reparto de responsabilidades

```
            eventos (Disparo, ObjetivoCambiado, Recarga...)
 Torreta  ─────────────────────────────────────────────►  TorretaEfectos
   │                                                      (solo ve y suena)
   │ PoolProyectiles.Obtener()  +  Proyectil.Lanzar()
   ▼
 Proyectil ── al chocar ──►  IDaniable.RecibirDanio()  (Salud o tu script)
```

- **Torreta** toma todas las decisiones y no sabe nada de cómo se ve ni de cómo suena.
- **TorretaEfectos** escucha los eventos de la torreta y reproduce efectos. Puedes quitarlo, cambiarlo o escribir el tuyo sin tocar la lógica.
- **Proyectil** no sabe qué torreta lo disparó salvo para no darse a sí mismo. Hace daño a cualquier cosa que implemente `IDaniable`.

### 4.2 Lo que hace la torreta cada frame (`Torreta.Update`)

1. **Recarga**: si está recargando y ya pasó `Tiempo Recarga`, llena el cargador.
2. **Búsqueda** (cada `Intervalo Busqueda` segundos, no cada frame, porque es lo más caro): si no tiene objetivo (o si `Cambiar Objetivo Si Hay Mejor` está marcado), busca uno nuevo.
3. **Validación**: si el objetivo murió, se desactivó, salió del alcance, quedó fuera del ángulo que puede cubrir o lo tapa una pared, lo suelta.
4. **Velocidad del objetivo**: compara su posición con la del frame anterior (no necesita Rigidbody).
5. **Apuntado**: calcula la dirección ideal (con predicción y gravedad si toca) y gira las piezas hacia ella a la velocidad máxima configurada.
6. **Disparo**: si el cañón está dentro de `Tolerancia Apuntado` (o `Disparar Siempre` está marcado) y ha pasado el tiempo de cadencia, dispara.

### 4.3 Búsqueda y elección de objetivo

`Physics.OverlapSphereNonAlloc` recoge todos los colliders en las `Capas Objetivo` dentro de `Alcance`. Para cada uno:

1. Busca su "dueño": el objeto que tiene `IDaniable` (normalmente `Salud`); si no hay, el del Rigidbody; si no, el propio collider. Así un enemigo con diez colliders cuenta como uno solo.
2. Lo descarta si es parte de la propia torreta.
3. **Filtro**: si `Etiquetas Objetivo` tiene alguna etiqueta, el objeto debe tener una de ellas. Si está vacía, debe tener `IDaniable` (así nunca dispara al suelo o a las paredes).
4. Lo descarta si está muerto, fuera de `Alcance` / `Alcance Minimo`, fuera del ángulo que puede cubrir el modo, o sin línea de visión.
5. Le da una puntuación según `Prioridad` y se queda con la mejor.

| Prioridad | Elige |
|---|---|
| Mas Cercano | El más próximo |
| Mas Lejano | El más alejado (bueno para artillería con alcance mínimo) |
| Menos Vida | El que tiene menos `VidaActual` (para rematar) |
| Primero En Entrar | El que lleva más tiempo dentro del alcance (estilo tower defense) |
| Mas Alineado | El que necesita menos giro (reacciona más rápido) |

La torreta apunta al **centro del collider** del objetivo (no a sus pies), más `Altura Apuntado`.

### 4.4 Los tres modos de apuntado

Todas las rotaciones se guardan como **ángulos respecto a la posición de reposo** (la que tiene la pieza al empezar). Por eso puedes colocar la torreta en cualquier orientación (en el suelo, en una pared, boca abajo en el techo) y los límites siguen siendo relativos a ella.

**Fija.** No mueve nada. Comprueba si el ángulo entre el cañón (eje Z azul del primer punto de disparo) y el objetivo es menor que `Angulo Vision`. Si lo es, dispara.

**Un eje.**
1. Pasa la dirección al objetivo al espacio del padre del `Pivot Giro`.
2. La proyecta en el plano perpendicular a `Eje Giro` y mide el ángulo con signo respecto a la posición de reposo (`MatematicaTorreta.AnguloEnEje`).
3. Si `Limitar Giro` está marcado, recorta ese ángulo entre `Giro Minimo` y `Giro Maximo`.
4. Avanza hacia él como mucho `Velocidad Giro` grados por segundo.
5. Solo puede disparar a objetivos cuya altura no se desvíe del plano de giro más de `Angulo Vision` grados.

`Eje Giro` es (0, 1, 0) por defecto: gira a izquierda y derecha. Si quieres que gire en otro eje (por ejemplo un cañón que solo sube y baja, o una torreta de pared), cambia el vector: (1, 0, 0) gira en el eje X rojo.

**Todos los ángulos**, con dos piezas (lo normal):
- `Pivot Giro` hace exactamente lo del modo Un eje.
- `Pivot Elevacion` (hijo de `Pivot Giro`) gira sobre su eje X local para subir y bajar el cañón, entre `Elevacion Minima` y `Elevacion Maxima`, a `Velocidad Elevacion`.
- Dispara cuando el ángulo entre el cañón y la dirección ideal es menor que `Tolerancia Apuntado`.

**Todos los ángulos** sin `Pivot Elevacion`: el `Pivot Giro` entero mira directamente al objetivo con `Quaternion.RotateTowards`. Útil para cosas esféricas (un ojo, un dron).

### 4.5 Predicción del movimiento

Con `Predecir Movimiento`, la torreta no apunta adonde está el objetivo sino adonde **estará** cuando llegue la bala. `MatematicaTorreta.PuntoInterceptacion` resuelve la ecuación exacta (una ecuación de segundo grado) para un objetivo a velocidad constante. Si el objetivo huye más rápido que la bala, apunta a su posición actual.

### 4.6 Gravedad y tiro parabólico

`Gravedad Proyectil` multiplica la gravedad del proyecto (`Physics.gravity`). Con 0 las balas van rectas; con 1 caen como cualquier objeto; con 0.3 caen poco (bala pesada pero rápida).

En el modo **Todos los ángulos** con `Compensar Gravedad`, la torreta calcula el ángulo de lanzamiento exacto para que la parábola pase por el objetivo (`MatematicaTorreta.AnguloBalistico`). Con `Arco Alto` elige la parábola alta, como un mortero. Si el objetivo está fuera de alcance, apunta a 45° (máximo alcance posible). Predicción y gravedad se combinan: calcula el tiempo de vuelo, mira dónde estará el objetivo en ese tiempo, y repite tres veces.

### 4.7 Disparo, ráfagas, cañones y munición

- `Cadencia`: disparos por segundo.
- `Balas Por Rafaga` y `Pausa Entre Rafagas`: dispara N seguidas y descansa.
- `Proyectiles Por Disparo`: más de 1 = escopeta (cada bala con su propia `Dispersion`).
- Varios `Puntos Disparo`: con `Alternar` dispara por uno y luego por el siguiente; con `Simultaneo`, por todos a la vez.
- Munición: si `Municion Infinita` está desmarcado, al vaciar el `Tamano Cargador` espera `Tiempo Recarga`.

### 4.8 Las balas

`Proyectil` **no usa Rigidbody**. Cada frame calcula el tramo que va a recorrer y lanza un rayo (o una esfera si `Radio` > 0) a lo largo de ese tramo. Así no atraviesa paredes aunque vaya rapidísimo, que es el fallo típico de las balas con Rigidbody.

Al chocar: busca un `IDaniable` en el objeto golpeado (o en sus padres) y le hace `Danio`; empuja el Rigidbody si lo hay; crea `Efecto Impacto` mirando hacia fuera de la superficie; suena; lanza el evento `Impacto`; y vuelve al pool.

**Pool**: crear y destruir objetos durante el juego provoca tirones. `PoolProyectiles` guarda las balas usadas desactivadas y las reutiliza. Hay una reserva por cada prefab. Si quieres evitar el primer tirón, llama a `PoolProyectiles.Precalentar(prefab, 30)` al cargar el nivel.

### 4.9 Animación, sonido y efectos (`TorretaEfectos`)

- **Retroceso**: mueve cada pieza de `Piezas Retroceso` hacia `Direccion Retroceso` (local) `Distancia Retroceso` metros en `Tiempo Golpe` segundos y la devuelve suavemente en `Tiempo Recuperacion`. Si hay una pieza por cañón, retrocede la del cañón que disparó.
- **Fogonazo**: enciende el objeto del cañón que disparó durante `Duracion Fogonazo` y le da un giro al azar. También puede reproducir `Particulas Disparo` y encender `Luz Fogonazo`.
- **Animator** (opcional): si tienes animaciones hechas a mano, el componente envía `Disparar` (trigger), `TieneObjetivo` (bool), `Recargando` (bool) y `VelocidadGiro` (float). Los parámetros que no existan en tu Animator se ignoran sin errores. Puedes cambiar los nombres en el Inspector.
- **Sonidos**: disparo (con tono al azar dentro de `Rango Tono`), pitido al fijar objetivo, sonido de recarga y **motor de giro** en bucle cuyo volumen y tono suben con la velocidad de giro.
- `Generar Sonidos Si Faltan`: cualquier clip que no asignes se sustituye por uno generado por código (`SonidoProcedural`). Sirven para prototipar; en el juego final asigna tus propios clips.

## 5. Referencia de todos los campos

El Inspector oculta los campos que no se usan en el modo elegido y avisa de errores de montaje (falta la bala, el pivote de elevación no es hijo del de giro...). Pasa el ratón por encima de cada campo para ver su explicación. En Play enseña el objetivo actual, la munición y botones para **Disparar ahora**, **Recargar** y **Soltar objetivo**.

### Torreta

| Campo | Para qué sirve |
|---|---|
| Modo | Fija, Un eje o Todos los ángulos |
| Pivot Giro | Pieza que gira en el eje principal. Vacío = el propio objeto |
| Pivot Elevacion | Pieza que sube y baja el cañón (solo Todos los ángulos) |
| Puntos Disparo | Bocas de los cañones; las balas salen por su eje Z azul |
| Eje Giro | Eje de giro en coordenadas locales del pivote en reposo |
| Velocidad Giro / Velocidad Elevacion | Grados por segundo |
| Limitar Giro, Giro Minimo, Giro Maximo | Arco permitido (se dibuja en verde en la escena) |
| Elevacion Minima / Maxima | Cuánto puede bajar y subir el cañón |
| Volver A Reposo | Sin objetivo vuelve a su posición inicial |
| Vigilar Sin Objetivo, Angulo / Velocidad Vigilancia | Sin objetivo gira de lado a lado |
| Alcance / Alcance Minimo | Distancias de detección (esfera naranja y roja en la escena) |
| Capas Objetivo | Capas donde busca objetivos |
| Etiquetas Objetivo | Si tiene alguna, solo ataca objetos con esas etiquetas |
| Prioridad | A cuál dispara si hay varios |
| Angulo Vision | Fija: medio ángulo del cono. Un eje: desviación vertical permitida |
| Requiere Linea De Vision, Capas Obstaculo | No dispara a través de lo que esté en esas capas |
| Intervalo Busqueda | Cada cuánto busca (más alto = más barato) |
| Cambiar Objetivo Si Hay Mejor | Cambia aunque el actual siga siendo válido |
| Altura Apuntado | Desplazamiento vertical del punto de mira |
| Prefab Proyectil | La bala (debe tener `Proyectil`) |
| Cadencia | Disparos por segundo |
| Balas Por Rafaga, Pausa Entre Rafagas | Ráfagas |
| Proyectiles Por Disparo | Balas por disparo y cañón (escopeta) |
| Dispersion | Desvío aleatorio en grados |
| Modo Canones | Alternar o Simultáneo |
| Tolerancia Apuntado | Error máximo para empezar a disparar |
| Disparar Siempre | Dispara sin parar aunque no haya objetivo |
| Velocidad Proyectil, Danio | Velocidad (m/s) y daño de cada bala |
| Gravedad Proyectil | Multiplicador de `Physics.gravity` |
| Capas Impacto | Contra qué chocan las balas |
| Predecir Movimiento | Apunta adonde estará el objetivo |
| Compensar Gravedad, Arco Alto | Tiro parabólico (solo Todos los ángulos) |
| Municion Infinita, Tamano Cargador, Tiempo Recarga | Munición |
| Eventos | `Al Adquirir Objetivo`, `Al Perder Objetivo`, `Al Disparar`, `Al Empezar Recarga`, `Al Terminar Recarga` |

### Proyectil

| Campo | Para qué sirve |
|---|---|
| Vida Maxima | Segundos antes de desaparecer si no choca |
| Radio | 0 = rayo fino; mayor = bala gorda, más fácil de acertar |
| Fuerza Empuje | Impulso a los Rigidbody golpeados |
| Alinear Con Velocidad | La bala mira hacia donde vuela (importante con gravedad) |
| Efecto Impacto, Duracion Efecto Impacto | Lo que aparece al chocar |
| Sonidos Impacto, Volumen Impacto, Sonido Impacto Generado | Sonido al chocar |

La velocidad, el daño, la gravedad y las capas los decide la **torreta**, no la bala. Así el mismo prefab de bala sirve para torretas distintas.

### Salud

| Campo | Para qué sirve |
|---|---|
| Vida Maxima | Vida inicial |
| Destruir Al Morir / Desactivar Al Morir | Qué pasa con el objeto al morir |
| Efecto Muerte | Explosión o similar |
| Al Recibir Danio, Al Morir | Eventos |

Métodos: `RecibirDanio`, `Curar`, `Revivir`.

## 6. Montar una torreta con tu propio modelo 3D

La forma más fácil de entender la jerarquía es crear una torreta de ejemplo (clic derecho > Mecánicas > Torreta) y mirarla. La estructura es:

```
Mi Torreta                  ← Torreta + TorretaEfectos
├── Base                    (modelo de la base, no se mueve)
└── Pivot Giro              ← gira a izquierda y derecha
    ├── Cuerpo              (modelo)
    └── Pivot Elevacion     ← sube y baja (solo en Todos los ángulos)
        └── Cañón           ← pieza que retrocede
            ├── Tubo        (modelo)
            └── Boca        ← punto de disparo (Z azul hacia fuera)
                └── Fogonazo (desactivado)
```

Pasos:

1. **Separa el modelo en piezas** en tu programa 3D: base, parte que gira, parte que eleva (si la hay) y cañón. Exporta todo junto con la jerarquía.
2. **Comprueba los pivotes**. Selecciona cada pieza en Unity con la herramienta de mover en modo **Local** y **Pivot** (no Center):
   - La flecha azul (Z) de las piezas que giran debe apuntar hacia donde apunta el cañón.
   - El pivote debe estar en el centro de giro real (la bisagra).
   - Si tu modelo no cumple esto, no lo arregles en el modelo: crea un **objeto vacío** colocado y orientado correctamente, y mete la pieza del modelo dentro. Esos vacíos serán los pivotes.
3. **Crea un vacío por cada boca de cañón**, en la punta, con la Z azul hacia fuera.
4. Añade **Torreta** a la raíz y arrastra `Pivot Giro`, `Pivot Elevacion` y `Puntos Disparo`.
5. Asigna una **bala** en `Prefab Proyectil` (puedes usar la de los prefabs de ejemplo, o crear un objeto con `Proyectil` y un modelo, sin collider).
6. Añade **Torreta Efectos** y arrastra las piezas que retroceden (normalmente el vacío "Cañón" de cada boca), los fogonazos y la luz.
7. Colliders: déjalos en las piezas grandes para que otras cosas puedan chocar con la torreta. Sus propias balas los ignoran.

> El modelo debe tener la torreta **en reposo mirando al frente** cuando entra en la escena. Ese es el "ángulo 0" para los límites.

## 7. Implementarla en tu juego

### Que dispare a tus enemigos (o al jugador)

Opción A, con vida: añade `Salud` a tus enemigos (o haz que tu script de vida implemente `IDaniable`). Deja `Etiquetas Objetivo` vacío.

Opción B, por etiqueta: pon a tus enemigos la etiqueta `Enemy` y escríbela en `Etiquetas Objetivo`. Para recibir daño seguirán necesitando algo que implemente `IDaniable`.

Para que una torreta enemiga ataque al jugador: pon la etiqueta `Player` en `Etiquetas Objetivo` y haz que la vida del jugador implemente `IDaniable`.

### Tu propio script de vida

```csharp
using Mecanicas.Torretas;
using UnityEngine;

public class VidaEnemigo : MonoBehaviour, IDaniable
{
    public float vida = 50f;
    public bool EstaVivo => vida > 0f;
    public float VidaActual => vida;

    public void RecibirDanio(InfoDanio info)
    {
        vida -= info.cantidad;
        // info.punto, info.direccion e info.atacante sirven para efectos y para saber quién disparó
        if (vida <= 0f) Destroy(gameObject);
    }
}
```

### Capas recomendadas

Crea capas `Enemigos`, `Escenario` y `Proyectiles` y configura:

- `Capas Objetivo` = Enemigos (busca más rápido y no mira el resto)
- `Capas Obstaculo` = Escenario (lo que tapa la vista)
- `Capas Impacto` = Enemigos + Escenario (contra qué chocan las balas)

### Controlarla desde código

```csharp
using Mecanicas.Torretas;
using UnityEngine;

public class EjemploTorreta : MonoBehaviour
{
    public Torreta torreta;
    public Transform jefe;

    void Start()
    {
        torreta.Disparo += canon => Debug.Log("Pum por el cañón " + canon);
        torreta.ObjetivoCambiado += t => Debug.Log(t != null ? "Te veo, " + t.name : "Te perdí");
    }

    void ActivarModoJefe()
    {
        torreta.AsignarObjetivo(jefe);   // fuerza un objetivo (null = vuelve a buscar sola)
        torreta.cadencia = 10f;          // todos los campos públicos se pueden cambiar en marcha
    }

    void ApagarTorreta() => torreta.enabled = false;  // deja de buscar y de disparar
}
```

| API | Qué hace |
|---|---|
| `AsignarObjetivo(Transform)` | Fuerza un objetivo; `null` vuelve a la búsqueda automática |
| `DispararAhora()` | Dispara una vez sin esperar |
| `Recargar()` | Recarga aunque no esté vacío |
| `PuedeApuntarA(Vector3)` | ¿Puede cubrir ese punto con su modo y sus límites? |
| `Objetivo`, `TieneObjetivo`, `Alineada`, `Recargando`, `MunicionActual` | Estado |
| `VelocidadAngularActual`, `PuntoApuntado`, `DireccionCanon`, `OrigenApuntado` | Estado del apuntado |
| Eventos C#: `Disparo`, `ObjetivoCambiado`, `RecargaIniciada`, `RecargaTerminada` | Para scripts |
| `UnityEvent`s del Inspector | Lo mismo, para conectar sin código |
| `Proyectil.Impacto` | Evento de cada bala al chocar |
| `PoolProyectiles.Precalentar(prefab, n)` | Crea balas por adelantado |

### Usarla con tus animaciones

Si tu torreta tiene un Animator (por ejemplo una animación de "despliegue" o de disparo hecha en Blender), crea en el Animator los parámetros `Disparar` (Trigger), `TieneObjetivo` (Bool), `Recargando` (Bool) y/o `VelocidadGiro` (Float), y arrastra el Animator a `Torreta Efectos`. Ojo: si una animación mueve los mismos huesos que los pivotes, ganará la animación; usa animaciones solo para piezas que el código no gira, o capas con máscara.

## 8. Cómo modificar el sistema

| Quiero... | Dónde tocar |
|---|---|
| Otra regla para elegir objetivo | Añade un valor a `PrioridadObjetivo` y un `case` en `Torreta.Puntuar` (la puntuación más **baja** gana) |
| Filtrar objetivos de otra forma (equipo, invisibilidad...) | `Torreta.EsCandidato` |
| Otro modo de apuntado | Añade un valor a `ModoApuntado` y un `case` en `Torreta.Apuntar` y `Torreta.PuedeApuntarA` |
| Que dispare láser instantáneo (hitscan) | En `Torreta.CrearBalas`, en vez de pedir una bala al pool, haz `Physics.Raycast` y llama a `IDaniable.RecibirDanio` directamente |
| Misiles teledirigidos | Hereda de `Proyectil` o copia su `Update` y gira `velocidad` poco a poco hacia un objetivo |
| Balas que atraviesan | En `Proyectil.Chocar`, no llames a `Devolver()` hasta N impactos |
| Daño en área (explosión) | En `Proyectil.Chocar`, usa `Physics.OverlapSphere` en `hit.point` y daña a todos |
| Sobrecalentamiento en vez de munición | En `Torreta.EjecutarDisparo` suma calor; en `Update` réstalo; bloquea `IntentarDisparar` si pasa del límite |
| Otros efectos | Escribe tu propio componente que se suscriba a `torreta.Disparo` (mira `TorretaEfectos.OnEnable`) |
| Que la torreta tenga vida | Añade `Salud` a la torreta y conecta su evento `Al Morir` para desactivar el componente `Torreta` |

Consejos:

- Los cálculos puros están en `MatematicaTorreta`. Si los cambias, ejecuta las pruebas (Window > General > Test Runner > EditMode > Run All).
- `Torreta` no debe saber nada de sonido ni de gráficos; si añades algo visual, ponlo en `TorretaEfectos` o en un componente nuevo que escuche los eventos.
- Todos los nombres de campos están en español. Si renombras un campo público, Unity perderá su valor en las escenas ya guardadas; para evitarlo añade `[UnityEngine.Serialization.FormerlySerializedAs("nombreViejo")]` encima.

## 9. Problemas frecuentes

| Síntoma | Causa probable |
|---|---|
| No detecta a nadie | El objetivo no tiene `Salud`/`IDaniable` ni una de las `Etiquetas Objetivo`; o no tiene collider; o su capa no está en `Capas Objetivo` |
| Gira hacia el objetivo pero no dispara | Falta `Prefab Proyectil`; `Tolerancia Apuntado` muy baja; en Un eje, el objetivo está más alto o más bajo que `Angulo Vision` |
| Gira hacia el lado contrario o se pone de lado | La Z azul del pivote no mira hacia delante, o el pivote no está en la bisagra (ver apartado 6) |
| El cañón apunta al suelo o al cielo | `Pivot Elevacion` no es hijo de `Pivot Giro`, o su eje X no es horizontal |
| No dispara aunque se ve claramente | `Capas Obstaculo` incluye la capa del objetivo o de algo pegado a él (por ejemplo su arma) |
| Las balas no hacen daño | El objeto golpeado (o sus padres) no tiene `IDaniable`; o su capa no está en `Capas Impacto` |
| Las balas fallan objetivos rápidos | Activa `Predecir Movimiento` o sube `Velocidad Proyectil` |
| Las balas caen antes de llegar | Usa el modo Todos los ángulos con `Compensar Gravedad`, o baja `Gravedad Proyectil` |
| No suena | No hay `AudioListener` en la escena (va en la cámara) |
| Las estelas de las balas se ven rosas | El shader `Sprites/Default` no está en la build; asigna otro material a la estela de tu prefab de bala |
| Pequeño tirón al primer disparo | Llama a `PoolProyectiles.Precalentar` al cargar |

## 10. Estado

- Escrito para **Unity 6** (sin paquetes extra). El código compila fuera de Unity contra las librerías de Unity y las pruebas de `MatematicaTorreta` pasan, pero **todavía no se ha abierto en el editor de Unity**: lo primero en el PC es copiarlo a un proyecto, crear la escena de demostración y comprobar que todo se ve y suena bien.
- Las torretas de ejemplo son cubos y cilindros; los sonidos generados son provisionales.
