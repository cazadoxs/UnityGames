# Cinta transportadora (mecánica suelta para Unity)

Sistema de **cinta transportadora** para juegos 3D en primera persona: la banda **se ve moverse** (textura en movimiento y rodillos girando) y **arrastra de verdad** los objetos con física que caen encima, y también al jugador. Es un asset independiente: no depende de ningún otro sistema ni de paquetes externos, y lo puedes copiar a cualquier proyecto.

---

## Índice

1. [Para qué sirve](#1-para-qué-sirve)
2. [Qué hay en la carpeta](#2-qué-hay-en-la-carpeta)
3. [Instalación en un proyecto](#3-instalación-en-un-proyecto)
4. [Probar la demo](#4-probar-la-demo)
5. [Uso rápido: poner una cinta en tu escena](#5-uso-rápido-poner-una-cinta-en-tu-escena)
6. [Usar tu propio modelo 3D como cinta](#6-usar-tu-propio-modelo-3d-como-cinta)
7. [Cómo funciona por dentro](#7-cómo-funciona-por-dentro)
8. [Referencia de todos los parámetros](#8-referencia-de-todos-los-parámetros)
9. [Controlar la cinta desde otros sistemas (botones, palancas, eventos)](#9-controlar-la-cinta-desde-otros-sistemas)
10. [Controlar la cinta desde código](#10-controlar-la-cinta-desde-código)
11. [Cómo modificarla](#11-cómo-modificarla)
12. [Ejemplos de implementación en juegos](#12-ejemplos-de-implementación-en-juegos)
13. [Problemas frecuentes](#13-problemas-frecuentes)
14. [Estado y limitaciones](#14-estado-y-limitaciones)

---

## 1. Para qué sirve

- Mover objetos de un sitio a otro de forma automática: fábricas, cocinas (bandejas, hamburguesas que salen de la plancha), cajas de supermercado, aeropuertos (equipaje), minas, puzles.
- Pasillos rodantes que arrastran al jugador (como en un aeropuerto), o cintas que lo empujan como obstáculo.
- Cadenas de producción: un **generador** suelta objetos, varias cintas encadenadas los llevan y un **receptor** al final los cuenta, los destruye o lanza un evento (por ejemplo "pedido entregado").

Funciona con **cualquier objeto que tenga Rigidbody** y con **jugadores que usen CharacterController** (el controlador en primera persona estándar de Unity). Los objetos siguen siendo físicos: caen, ruedan, chocan entre sí y se pueden coger con un sistema de agarre.

## 2. Qué hay en la carpeta

```
CintaTransportadora/
├── Runtime/                         ← lo que necesita tu juego (obligatorio)
│   ├── CintaTransportadora.cs           La cinta: detecta y arrastra objetos
│   ├── CintaTransportadoraVisual.cs     Textura en movimiento, rodillos y sonido
│   ├── CintaZonaDeteccion.cs            Pieza interna: avisa a la cinta de lo que entra
│   ├── CintaGenerador.cs                Hace aparecer objetos (opcional)
│   ├── CintaReceptor.cs                 Recibe/cuenta/destruye objetos (opcional)
│   ├── CintaTransportadoraConstructor.cs Crea una cinta completa con primitivas
│   ├── CintaMateriales.cs               Materiales por defecto (Built-in, URP y HDRP)
│   ├── CintaTexturaProcedural.cs        Textura de goma con flechas generada por código
│   └── Mecanicas.Cintas.asmdef          Ensamblado propio (compila separado)
├── Editor/                          ← menús del editor (obligatorio para los menús)
│   ├── CintaTransportadoraMenu.cs
│   └── Mecanicas.Cintas.Editor.asmdef
├── Demo/                            ← se puede borrar cuando ya no la necesites
│   ├── CintaDemo.unity                  Escena de prueba
│   ├── DemoCintaTransportadora.cs       Monta la demo al dar a Play
│   ├── DemoJugadorPrimeraPersona.cs     Jugador FPS mínimo para probar
│   └── DemoEntrada.cs                   Teclado/ratón (Input System nuevo o antiguo)
└── README.md                        ← este archivo
```

Todo el código está en el espacio de nombres `Mecanicas.Cintas`.

## 3. Instalación en un proyecto

**Requisitos:** Unity 6 (6000.x) recomendado; también debería funcionar en Unity 2021.3 / 2022.3. Vale para Built-in, URP y HDRP. No necesita paquetes extra.

Hay dos formas:

**A. Copiar la carpeta (la más sencilla)**
1. Copia la carpeta `CintaTransportadora` completa (con sus archivos `.meta`) dentro de `Assets/` de tu proyecto. Recomendado: `Assets/Mecanicas/CintaTransportadora`.
2. Espera a que Unity compile. No debe salir ningún error en la consola.

**B. Importar el paquete**
1. En Unity: `Assets > Import Package > Custom Package...`
2. Elige `Mecanicas/Paquetes/CintaTransportadora.unitypackage` (está en este repositorio).
3. Pulsa *Import*. Se instala en `Assets/Mecanicas/CintaTransportadora`.

Si no quieres la demo, desmarca la carpeta `Demo` al importar o bórrala después.

## 4. Probar la demo

1. Abre `Demo/CintaDemo.unity` y dale a **Play**. La escena está casi vacía a propósito: el script `DemoCintaTransportadora` lo monta todo al empezar (suelo, luz, jugador, cintas, generador y contenedor).
2. Si la escena no abriera bien, créala de nuevo con `Tools > Mecanicas > Cinta transportadora > Crear escena demo`.

Qué verás:
- **Cinta 1:** un generador suelta cajas y esferas de colores; la cinta las lleva hacia la cinta 2.
- **Cinta 2:** más baja y en perpendicular, recoge lo que cae y lo lleva a un **contenedor verde** que cuenta lo que recibe.
- **Pasillo rodante** (a la izquierda): ancho y a ras de suelo; súbete para notar cómo te arrastra.

Controles de la demo:

| Tecla | Acción |
|---|---|
| WASD / Ratón | Moverse / mirar |
| Espacio / Shift | Saltar / correr |
| F | Encender o apagar todas las cintas |
| R | Invertir el sentido |
| Z / X | Bajar / subir la velocidad 0,5 m/s |
| G | Soltar una caja delante de ti |
| Esc | Soltar el ratón (clic para volver a capturarlo) |

## 5. Uso rápido: poner una cinta en tu escena

1. En la jerarquía: clic derecho (o menú `GameObject`) **> Mecanicas > Cinta transportadora**.
2. Aparece una cinta de 0,8 × 4 m con cuerpo, banda, rodillos, barandillas y zona de detección. La primera vez se crean los materiales en `CintaTransportadora/Generado/` (textura, materiales y material físico sin fricción).
3. Muévela y gírala como cualquier objeto. **El eje Z azul es el sentido de avance**: verás una flecha verde (encendida) o roja (apagada) en la vista de escena.
4. Pon encima objetos con `Rigidbody` y dale a Play.

Para cambiar las medidas de una cinta creada con el menú, lo más fácil es crear otra desde código con las medidas que quieras (ver sección 10) o adaptar las piezas a mano: si cambias el largo, ajusta también el `BoxCollider` de la raíz, el de `ZonaDeteccion` y el campo *Largo Banda* de `CintaTransportadoraVisual`.

## 6. Usar tu propio modelo 3D como cinta

No hace falta usar las primitivas. Con cualquier modelo:

1. Coloca tu modelo y asegúrate de que su **eje Z local apunte hacia donde debe avanzar** la cinta (si no, cambia *Direccion Local* en el componente).
2. Añade un **BoxCollider** (no Trigger) que cubra la cinta: es la superficie donde se apoyan los objetos.
3. Añade el componente **Cinta Transportadora** (`Add Component > Mecanicas > Cinta Transportadora > Cinta Transportadora`). Al dar a Play creará sola un Trigger encima del BoxCollider. Si prefieres hacerlo tú, crea un hijo con un BoxCollider marcado como *Is Trigger* que cubra la superficie y unos 30 cm por encima, y arrástralo al campo *Zona Deteccion*.
4. (Recomendado) Pon en el BoxCollider un **Physics Material sin fricción** (`Generado/CintaSinFriccion`). Así la cinta es la única que decide cómo se mueven los objetos. Con fricción también funciona, pero los objetos van algo más lentos.
5. Para la parte visual añade **Cinta Transportadora (Visual)**:
   - *Renderer Banda*: la malla de la banda.
   - *Eje UV*: `(0,1)` si la textura de la banda va a lo largo de la V, `(1,0)` si va a lo largo de la U.
   - *Metros Por Repeticion* y *Largo Banda*: para que la textura vaya **a la misma velocidad que los objetos** (ver 7.4).
   - Si la textura se mueve al revés, marca *Invertir Textura*.
   - *Rodillos*: arrastra los objetos que deben girar e indica su radio.

## 7. Cómo funciona por dentro

### 7.1 Detección de lo que hay encima
La cinta tiene una **zona de detección**: un `BoxCollider` en modo *Trigger* justo encima de la banda (35 cm de alto por defecto). Cuando un collider entra, Unity llama a `OnTriggerEnter` en `CintaZonaDeteccion`, que se lo pasa a la cinta. La cinta guarda:
- los **Rigidbody** que hay dentro (contando cuántos colliders de cada uno, porque un objeto puede tener varios), y
- los **CharacterController** que hay dentro.

Los objetos destruidos o desactivados se limpian solos. Se ignoran los colliders Trigger y las capas que no estén en *Capas*.

### 7.2 Cómo arrastra los objetos con física
En cada `FixedUpdate`, para cada Rigidbody:
1. Calcula la **velocidad de la superficie**: `dirección × velocidad actual`.
2. Toma la velocidad del objeto **solo en el plano de la cinta** (quita la componente vertical, así la gravedad y los saltos no se tocan).
3. Le aplica un cambio de velocidad (`ForceMode.VelocityChange`) hacia la de la cinta, limitado a `agarre × deltaTime`.

Resultado: el objeto acelera con suavidad hasta ir a la velocidad de la banda, se frena si la cinta se para y puede chocar o rodar con normalidad. Si se activa *Centrar Objetos*, se añade una pequeña velocidad lateral hacia el centro de la cinta.

Los Rigidbody **cinemáticos** se ignoran por defecto (por ejemplo, un objeto que el jugador tiene agarrado). Si desmarcas *Ignorar Cinematicos*, se mueven con `MovePosition`.

### 7.3 Cómo arrastra al jugador
En cada `Update`, a cada CharacterController dentro de la zona le hace `Move(velocidad de la superficie × deltaTime)`. Se suma a tu propio movimiento, así que el jugador puede andar a favor o en contra. Un empuje mínimo hacia abajo mantiene `isGrounded` para que el jugador pueda saltar estando encima.

Si tu jugador usa **Rigidbody** en lugar de CharacterController, se arrastra como cualquier objeto físico.

### 7.4 La banda que se mueve
`CintaTransportadoraVisual` desplaza el *offset* de la textura de la banda en cada frame:

```
offset += ejeUV × (velocidad actual / metrosPorRepeticion) × deltaTime
```

y ajusta el *tiling* a `largoBanda / metrosPorRepeticion`. Como una repetición de textura ocupa exactamente `metrosPorRepeticion` metros, **la textura avanza a la misma velocidad que los objetos**. Usa una copia del material (no modifica tu material original).

Si el material no tiene textura, se genera una de goma oscura con flechas amarillas (`CintaTexturaProcedural`). Las flechas apuntan hacia donde va la cinta.

### 7.5 Rodillos y sonido
Los rodillos giran `velocidad / radio` radianes por segundo alrededor de *Eje Giro Rodillos*, así su superficie va a la misma velocidad que la banda. Si asignas un `AudioSource` con un sonido de motor en bucle, el volumen y el tono suben con la velocidad y se callan al parar.

### 7.6 Encender, apagar y aceleración
La cinta no cambia de velocidad de golpe: la **velocidad actual** se acerca a la objetivo a razón de *Aceleracion* m/s². Al apagarla, la banda y los objetos frenan poco a poco; al invertirla, frena, se para y arranca en sentido contrario. Con *Aceleracion* = 0 todo es instantáneo.

## 8. Referencia de todos los parámetros

### Cinta Transportadora (`CintaTransportadora`)

| Parámetro | Por defecto | Qué hace |
|---|---|---|
| Encendida | sí | Estado inicial. |
| Velocidad | 1,5 m/s | Velocidad objetivo de la banda. |
| Direccion Local | (0,0,1) | Sentido de avance en ejes locales (Z azul). |
| Aceleracion | 3 m/s² | Lo rápido que cambia de velocidad. 0 = instantáneo. |
| Agarre | 25 m/s² | Lo rápido que los objetos alcanzan la velocidad de la cinta. Bájalo (5–10) para que patinen; súbelo (50+) para que "se peguen". |
| Centrar Objetos / Fuerza Centrado | no / 2 | Empuja los objetos hacia el centro de la banda. |
| Capas | todas | Qué capas puede arrastrar. |
| Mover Character Controllers | sí | Arrastrar al jugador si usa CharacterController. |
| Ignorar Cinematicos | sí | No tocar Rigidbody cinemáticos (objetos agarrados). |
| Zona Deteccion | vacío | Trigger de la superficie. Vacío = se crea solo. |
| Altura Zona Automatica | 0,35 m | Alto del Trigger creado automáticamente. |
| Eventos | | `Al Encender`, `Al Apagar`, `Al Entrar Objeto (GameObject)`, `Al Salir Objeto (GameObject)`. |

En el menú contextual del componente (los tres puntos) tienes **Encender o apagar** e **Invertir sentido** para probar en Play.

### Cinta Transportadora Visual (`CintaTransportadoraVisual`)

| Parámetro | Qué hace |
|---|---|
| Cinta | Cinta que manda (vacío = la busca en el objeto o en sus padres). |
| Renderer Banda / Indice Material | Malla y material que se animan. |
| Propiedad Textura | Vacío = textura principal del shader (`_BaseMap` en URP/HDRP, `_MainTex` en Built-in). Pon otro nombre si tu shader usa otro. |
| Eje UV | Eje de la UV a lo largo de la cinta. |
| Metros Por Repeticion | Metros que ocupa una repetición de la textura. |
| Ajustar Tiling / Largo Banda | Calcula el tiling solo (desmárcalo si ya lo tienes bien en tu material). |
| Invertir Textura | Si la textura va al revés. |
| Generar Textura Si Falta | Textura de goma con flechas si el material no tiene. |
| Rodillos / Radio / Eje Giro | Transforms que giran con la cinta. |
| Sonido / Volumen / Tono / Velocidad Tono Maximo | Sonido en bucle que sigue a la velocidad. |

### Generador (`CintaGenerador`)

| Parámetro | Qué hace |
|---|---|
| Prefabs | Qué generar. Vacío = cajas y esferas de prueba. Los prefabs deben tener **Rigidbody** y collider. |
| Aleatorio | Al azar o en orden. |
| Automatico / Intervalo | Generar uno cada X segundos. Desmárcalo para generar solo con `Generar()`. |
| Maximo Vivos | Límite de objetos a la vez (0 = sin límite). |
| Dispersion | Variación aleatoria de posición en X/Z locales. |
| Rotacion Aleatoria | Giro aleatorio en vertical. |
| Contenedor | Padre de los objetos creados. |
| Al Generar | Evento con el objeto nuevo. |

### Receptor (`CintaReceptor`)

Necesita un collider (se pone en Trigger solo).

| Parámetro | Qué hace |
|---|---|
| Etiqueta | Solo acepta objetos con ese Tag (vacío = todos). El Tag debe existir en el proyecto. |
| Capas / Solo Rigidbody | Filtros. |
| Destruir Al Recibir / Retraso | Borra el objeto al llegar. |
| Al Recibir | Evento con el objeto recibido. |
| `Contador` (código) | Total recibido. `ReiniciarContador()` lo pone a 0. |

## 9. Controlar la cinta desde otros sistemas

Todas las acciones son métodos públicos sin parámetros o con un número, así que se pueden enlazar **sin programar** desde cualquier `UnityEvent` (un botón de UI, un trigger, una palanca, un panel numérico, el sistema de eventos…):

| Método | Uso |
|---|---|
| `Encender()` / `Apagar()` / `Alternar()` | Interruptor. |
| `InvertirSentido()` | Cambiar el sentido. |
| `EstablecerSentido(bool)` | true = normal, false = inverso. |
| `EstablecerVelocidad(float)` | Fijar la velocidad. |
| `SumarVelocidad(float)` | Sumar o restar (con negativo). |
| `CintaGenerador.GenerarUno()` | Soltar un objeto ahora. |
| `CintaGenerador.DestruirTodos()` | Limpiar lo generado. |

Ejemplo con un botón de UI: en el `Button`, en *On Click ()* pulsa `+`, arrastra la cinta y elige `CintaTransportadora > Alternar`.

Al revés, la cinta avisa con sus eventos: por ejemplo, en *Al Encender* puedes reproducir un sonido o encender una luz, y en el *Al Recibir* del receptor sumar puntos o completar un pedido.

## 10. Controlar la cinta desde código

```csharp
using Mecanicas.Cintas;
using UnityEngine;

public class EjemploCinta : MonoBehaviour
{
    public CintaTransportadora cinta;
    public CintaReceptor receptor;

    void Start()
    {
        cinta.EstablecerVelocidad(2f);
        cinta.alEntrarObjeto.AddListener(obj => Debug.Log($"{obj.name} sube a la cinta"));
        receptor.alRecibir.AddListener(obj => Debug.Log($"Recibidos: {receptor.Contador}"));
    }

    void Update()
    {
        // Velocidad real en este instante (negativa si va al revés):
        float v = cinta.VelocidadActual;
        // Objetos que está arrastrando ahora:
        int cuantos = cinta.ObjetosEncima.Count;
    }
}
```

Crear una cinta entera en tiempo de juego (por ejemplo, desde un sistema de construcción):

```csharp
var opciones = new CintaTransportadoraConstructor.Opciones
{
    ancho = 1f, largo = 6f, alto = 0.9f, velocidad = 1.2f, barandillas = true
};
CintaTransportadora nueva = CintaTransportadoraConstructor.Crear(
    opciones, new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0));
```

El pivote de la cinta creada está **en el suelo, en el centro**; la superficie queda a la altura `alto`.

## 11. Cómo modificarla

**Cambios sin tocar código**
- *Más rápida / más lenta:* `Velocidad`. *Arranque más suave:* baja `Aceleracion`.
- *Que los objetos resbalen (cinta de hielo, grasa):* baja `Agarre` a 3–8.
- *Que no se caigan por los lados:* activa `Centrar Objetos` o usa barandillas.
- *Cinta inclinada (rampa):* simplemente gira la cinta en X. La dirección y la normal se calculan con la rotación, y la gravedad sigue actuando, así que en pendientes muy fuertes sube `Agarre`.
- *Curvas:* encadena varias cintas rectas con ángulos (como en la demo), o pon cintas cortas giradas unos grados cada una.
- *Textura propia:* pon tu textura en el material de la banda. Ajusta `Eje UV`, `Metros Por Repeticion` y, si va al revés, `Invertir Textura`. Si tu shader usa otra propiedad (p. ej. un Shader Graph con `_Textura`), escríbela en `Propiedad Textura`.
- *Colores:* edita los materiales de `Generado/` (se reutilizan en todas las cintas creadas con el menú).

**Cambios en el código** (todo comentado en español)
- El arrastre está en `CintaTransportadora.FixedUpdate()`. Ahí puedes, por ejemplo, multiplicar el agarre por la masa (`maxCambio / rb.mass`) para que lo pesado cueste más, o añadir una fuerza que haga girar los objetos.
- El arrastre del jugador está en `CintaTransportadora.Update()`.
- El aspecto de la textura por defecto está en `CintaTexturaProcedural.Crear()` (colores, número de flechas, listones).
- Las piezas y medidas de la cinta prefabricada están en `CintaTransportadoraConstructor.Crear()`.

**Hacer un prefab:** crea la cinta con el menú, ajústala y arrástrala a la ventana *Project*. Después úsala como cualquier prefab.

## 12. Ejemplos de implementación en juegos

- **Hamburguesería:** una cinta lleva las bandejas desde la cocina al mostrador. Pon un `CintaGenerador` con *Automatico* desmarcado y llama a `GenerarUno()` cuando el pedido esté listo; al final un `CintaReceptor` sin destruir, con `Al Recibir` conectado a tu lógica de "pedido servido".
- **Fábrica / puzle:** un botón o palanca llama a `Alternar()` o `InvertirSentido()` para llevar cajas a un sitio u otro; un receptor con *Etiqueta* solo acepta las piezas correctas.
- **Aeropuerto o tienda:** cinta de caja registradora que el jugador enciende para que los productos lleguen a la caja; usa `alEntrarObjeto` para sumar el precio.
- **Obstáculo:** un pasillo rodante a contracorriente que el jugador debe cruzar corriendo (`Velocidad` alta, sin barandillas).
- **Con un sistema de agarre:** los objetos agarrados suelen pasar a cinemáticos y la cinta los ignora; al soltarlos vuelven a ser arrastrados solos.

## 13. Problemas frecuentes

| Problema | Solución |
|---|---|
| Los objetos no se mueven | Deben tener **Rigidbody** (no cinemático) y un collider no Trigger. Mira que su capa esté en *Capas* y que la zona de detección cubra la superficie (selecciona `ZonaDeteccion` y mira el cubo verde). |
| El jugador no se mueve | Debe usar CharacterController (o Rigidbody) y *Mover Character Controllers* debe estar marcado. |
| La textura va al revés | Marca *Invertir Textura*. |
| La textura va más rápida o lenta que los objetos | Revisa *Metros Por Repeticion* y *Largo Banda* (o desmarca *Ajustar Tiling* y pon `Metros Por Repeticion = largo / tiling` de tu material). |
| La banda no se mueve pero los objetos sí | Asigna *Renderer Banda* en el componente Visual y comprueba que el shader tenga textura principal (o rellena *Propiedad Textura*). |
| Materiales rosas | El shader no corresponde a tu pipeline. Borra la carpeta `Generado/` y vuelve a crear la cinta, o asigna tus propios materiales. |
| Los objetos van más lentos que la banda | Pon el material físico sin fricción en el BoxCollider de la cinta o sube *Agarre*. |
| Objetos rápidos atraviesan la cinta | Pon su Rigidbody en *Collision Detection: Continuous*. |
| Error de Tag en el receptor | La etiqueta escrita en *Etiqueta* no existe: créala en `Tags and Layers` o deja el campo vacío. |

## 14. Estado y limitaciones

- El código se ha compilado contra las librerías de Unity 2021.3 fuera del editor, pero **aún no se ha abierto en un editor de Unity real**. Lo primero al copiarlo es comprobar que la consola no muestra errores y probar la escena de demo. Las partes específicas de Unity 6 (`linearVelocity`, `PhysicsMaterial`, `FindFirstObjectByType`) van con `#if UNITY_6000_0_OR_NEWER` / `UNITY_2023_1_OR_NEWER`.
- La cinta es **recta**. Las curvas se hacen encadenando tramos.
- La banda es una textura que se desplaza, no una malla que da la vuelta a los rodillos.
- Si construyes cintas desde código **en una build** (no en el editor), los materiales por defecto usan `Shader.Find`: incluye el shader Lit de tu pipeline en *Always Included Shaders* o pasa tus propios materiales en `Opciones`.
