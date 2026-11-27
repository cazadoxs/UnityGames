# Tienda y compras · mecánica suelta para Unity 6 (3D, primera persona)

Sistema de compra reutilizable para meter en cualquier proyecto. Permite comprar:

- **Con un botón en el mundo**: el jugador lo mira, pulsa **E**, el botón se hunde, cambia de color y el objeto aparece.
- **Con una plataforma**: el jugador la pisa y compra (botón de suelo / zona de compra).
- **Con una tienda en menú**: lista con pestañas, saldo, precio y botón *Comprar*. Se abre con **Tab** o desde un mostrador del mundo (**E**).
- **Como tú quieras ("personalizable")**: cualquier botón de UI, animación, trigger, misión o código puede llamar a la compra.

Lo comprado puede **aparecer en el mundo**, ir a **un inventario** (opcional, sin depender de ninguno), las dos cosas según quepa, o **no ser un objeto** (mejoras y desbloqueos que solo lanzan un evento).

No depende de nada más que de Unity: funciona con **Built-in, URP y HDRP**, y con el **Input System nuevo, el clásico o los dos**.

---

## Índice

1. [Para qué sirve](#1-para-qué-sirve)
2. [Instalación](#2-instalación)
3. [Probar la demo en 1 minuto](#3-probar-la-demo-en-1-minuto)
4. [Cómo funciona por dentro](#4-cómo-funciona-por-dentro)
5. [Los componentes uno a uno](#5-los-componentes-uno-a-uno)
6. [Implementarlo en tu proyecto paso a paso](#6-implementarlo-en-tu-proyecto-paso-a-paso)
7. [Conectar un inventario](#7-conectar-un-inventario)
8. [Conectar tu propia economía (monedero)](#8-conectar-tu-propia-economía-monedero)
9. [Personalizar y modificar](#9-personalizar-y-modificar)
10. [Usarlo desde código](#10-usarlo-desde-código)
11. [Recetas rápidas](#11-recetas-rápidas)
12. [Problemas frecuentes](#12-problemas-frecuentes)
13. [Estructura de archivos](#13-estructura-de-archivos)

---

## 1. Para qué sirve

Cualquier juego en el que el jugador gaste algo para obtener algo: una máquina expendedora, una tienda de armas, un mostrador de restaurante donde compras ingredientes, un botón que hace aparecer una caja, una mejora de velocidad que se compra una sola vez, un objeto de edición limitada...

Todo el sistema gira alrededor de una idea: **todas las formas de comprar acaban en el mismo sitio** (`GestorTienda.Comprar`). Por eso el botón del mundo, la tienda en menú y tu código se comportan igual: cobran igual, respetan el stock igual y lanzan los mismos eventos.

## 2. Instalación

Tienes dos formas. Elige una.

**A) Copiar la carpeta (lo más sencillo).**
Copia la carpeta `Mecanicas/Tienda` de este repositorio dentro de tu proyecto, en `Assets/Mecanicas/Tienda`. Ya está.

**B) Como paquete de Unity (se actualiza solo).**
En Unity: *Window › Package Manager › + › Add package from git URL...* y pega:

```
https://github.com/cazadoxs/UnityGames.git?path=/Mecanicas/Tienda#NOMBRE_DE_LA_RAMA
```

Requisitos: **Unity 6** (6000.0 o superior) y el paquete **uGUI** (`com.unity.ugui`, viene instalado por defecto). El Input System nuevo es opcional.

> Si el proyecto usa el Input System nuevo y aparece un error de `StandaloneInputModule`, es que tienes un EventSystem antiguo en la escena: selecciónalo y pulsa *Replace with InputSystemUIInputModule*. El que crea la tienda ya viene bien.

## 3. Probar la demo en 1 minuto

1. Menú **Herramientas › Mecánicas › Tienda › Crear escena de demostración**. Se crea y se guarda `TiendaDemo.unity` junto a los scripts de la demo.
2. Dale a **Play**.

(También vale: escena vacía → objeto vacío → *Add Component › TiendaDemo* → Play.)

Qué hay en la demo:

| Qué | Cómo se usa | Qué enseña |
|---|---|---|
| 3 pedestales con botón | Míralos y pulsa **E** | Botón del mundo, animación, colores, objeto que aparece detrás. El tercero da **5 cajas** de una vez |
| Plataforma azul a la derecha | Písala | Compra al entrar en un trigger |
| Mostrador "BAZAR DE PRUEBAS" a la izquierda | Míralo y pulsa **E** (o pulsa **Tab** en cualquier sitio) | Tienda en menú con pestañas; lo que compras ahí sale delante del mostrador |
| Cofre dorado | **E** | Da 100 monedas y 1 gema para seguir probando |
| Pestaña *Premium* | — | Pago con **gemas**, compra única (*Lámpara*) y stock limitado a 2 (*Estatua*) |
| Pestaña *Inventario* | **U** gasta un objeto | *Botiquín* va solo al inventario (capacidad 5, si está lleno no te cobra). *Munición* va al inventario y, si no cabe, cae al suelo |
| Pestaña *Mejoras* | — | *Botas rápidas*: no crea objeto, solo un evento (corres más) |

Controles: WASD moverse · ratón mirar · E usar · Tab tienda · Esc cerrar · clic para capturar el ratón.

## 4. Cómo funciona por dentro

```
 BotonCompraMundo ─┐
 TiendaMenu (fila) ─┼──►  GestorTienda.Comprar(articulo, receptor)
 DisparadorCompra ─┤            │
 tu código ────────┘            ├─ 1. ¿Se puede? (ReglasCompra)  → límite, stock, dinero
                                ├─ 2. ¿Se puede entregar?          → inventario con hueco / punto de aparición libre
                                ├─ 3. Cobrar  (IMonedero.Gastar)
                                ├─ 4. Entregar (IInventarioCompras.Anadir  o  IReceptorCompra.Entregar)
                                │      └─ si falla → se DEVUELVE el dinero
                                ├─ 5. Apuntar la compra y bajar el stock
                                └─ 6. Sonido + eventos (alComprar / alFallar / CompraProcesada / CualquierCompra)
                                       y devuelve un ResultadoCompra con el mensaje ya escrito
```

Ideas clave:

- **El artículo es un asset (`ArticuloTienda`)** con nombre, precio, moneda, prefab, cantidad, límites... El asset **no se modifica al jugar**: el stock y cuántas veces se ha comprado los guarda el `GestorTienda`. Así puedes usar el mismo artículo en varias tiendas.
- **El dinero está detrás de una interfaz (`IMonedero`)**. Viene un `Monedero` con varias monedas, pero puedes enchufar el de tu juego.
- **La entrega está detrás de interfaces**: `IReceptorCompra` (el `PuntoAparicion` incluido, u otro tuyo) e `IInventarioCompras` (opcional). La tienda **no conoce ningún inventario concreto**.
- **Se comprueba la entrega antes de cobrar**, y si aun así falla, se devuelve el dinero. Nunca se cobra sin entregar.

## 5. Los componentes uno a uno

### Datos (assets)

**`ArticuloTienda`** — *Create › Mecánicas › Tienda › Artículo*

| Campo | Para qué |
|---|---|
| `id` | Identificador único (si lo dejas vacío, el nombre del asset). Se usa para el guardado y `ComprarPorId` |
| `nombre`, `descripcion`, `icono`, `categoria` | Lo que se ve en la tienda. `categoria` = pestaña |
| `moneda`, `precio` | Con qué se paga y cuánto. Precio 0 = gratis |
| `modoEntrega` | `Aparecer` · `Inventario` · `InventarioOAparecer` · `SoloEvento` |
| `prefab`, `cantidadPorCompra` | Qué aparece y cuántos por compra (packs) |
| `idInventario` | Id con el que llega al inventario (vacío = `id`) |
| `maxCompras` | 0 = sin límite; 1 = compra única (desbloqueos) |
| `stockInicial` | -1 = infinito; 2 = solo hay dos |
| `visibleEnTienda` | Desmárcalo para artículos que solo se compran con un botón del mundo |

**`CatalogoTienda`** — *Create › Mecánicas › Tienda › Catálogo*: nombre de la tienda y lista de artículos. Puedes tener uno por tienda.

### Núcleo

**`GestorTienda`** — el cerebro. Normalmente uno por escena (el primero activo es `GestorTienda.Principal` y lo usan los botones que no tienen tienda asignada).

| Campo | Para qué |
|---|---|
| `idTienda` | Prefijo del guardado. Cámbialo si hay varias tiendas con `guardarCompras` |
| `catalogo` | Lo que muestra la tienda en menú por defecto |
| `monedero` | Componente que implementa `IMonedero`. Vacío = busca un `Monedero` |
| `inventario` | **Opcional.** Componente que implementa `IInventarioCompras` |
| `buscarInventarioEnEscena` | Si no asignas inventario, busca uno solo |
| `receptorPorDefecto` | Dónde aparecen las cosas si quien compra no dice otro sitio (un `PuntoAparicion`) |
| `guardarCompras` | Recuerda entre partidas cuántas veces se compró cada cosa (PlayerPrefs) |
| `sonidoCompra`, `sonidoError` | Sonidos globales |
| `textos` | Todos los mensajes que ve el jugador (ver [Personalizar](#9-personalizar-y-modificar)) |
| `alComprar`, `alFallar` | UnityEvents para conectar cosas desde el inspector |

En Play, su inspector tiene botones para **dar dinero** y **reiniciar stock**.

**`Monedero`** — dinero en varias monedas (`monedas`, `gemas`, lo que quieras: es solo un texto). Lista `saldoInicial`, guardado opcional en PlayerPrefs y evento `alCambiarSaldo`.

### Entrega

**`PuntoAparicion`** — dónde sale lo comprado. La flecha azul (Z) es hacia dónde mira el objeto y hacia dónde se empuja.

| Campo | Para qué |
|---|---|
| `radioDispersion`, `separacionVertical` | Reparte los objetos de un pack para que no se metan unos en otros |
| `exigirHuecoLibre` + `radioComprobacion` | No vende si hay algo encima del punto (máquina expendedora con bandeja ocupada) |
| `impulsoLocal` | Velocidad inicial (que salga "disparado" de una máquina) |
| `duracionEscalado` | Animación de crecer al aparecer (0 = de golpe) |
| `efectoAparicion`, `sonidoAparicion` | Partículas y sonido |
| `maxObjetosVivos` | Borra los más antiguos si hay demasiados (rendimiento) |
| `alAparecer` | Evento con el objeto creado |

### En el mundo

**`InteractorTienda`** — va en la **cámara del jugador**. Lanza un rayo, muestra el aviso ("[E] Comprar Caja · 20 monedas") y pulsa lo que miras. Si tu juego ya tiene su propio sistema de interacción, no lo pongas: llama tú a `Interactuar()` (ver [Usarlo desde código](#10-usarlo-desde-código)).

**`BotonCompraMundo`** — botón físico. Necesita un Collider (en él o en un hijo).

| Campo | Para qué |
|---|---|
| `articulo`, `tienda`, `receptor` | Qué compra, con qué tienda y dónde aparece |
| `enfriamiento` | Tiempo mínimo entre pulsaciones |
| `comprarAlEntrarEnTrigger` + `etiquetaJugador` | Botón de suelo: compra al pisarlo (pon un Collider con *Is Trigger*) |
| `textoAviso` | Texto al mirarlo (`{nombre}`, `{precio}`, `{moneda}`, `{saldo}`) |
| `parteMovil`, `recorridoPulsado` | La tapa que se hunde |
| `animador`, `triggerAnimador` | Si prefieres una animación propia |
| `indicador` + colores | Verde si se puede comprar, gris si no, destello verde/rojo al pulsar |
| `etiqueta` | TextMesh 3D con nombre y precio |
| `alPulsar`, `alComprar`, `alFallar` | Eventos propios del botón |

**`TerminalTienda`** — mostrador, ordenador, NPC... que **abre la tienda en menú**. Puede tener su **propio catálogo** y su **propio punto de entrega**, así una misma escena puede tener una armería y una frutería con un solo menú. Opción de abrir al entrar en una zona y cerrar al salir.

**`DisparadorCompra`** — compra sin interfaz. Llama a `Comprar()` o `ComprarEste(articulo)` desde cualquier UnityEvent.

### Interfaz

**`TiendaMenu`** — la tienda en menú.

| Campo | Para qué |
|---|---|
| `tienda`, `catalogo`, `receptor` | De dónde saca datos y dónde entrega |
| `abrirConTecla`, `teclaAbrir`, `teclaCerrar` | Tab / Esc por defecto. Desactiva la tecla si solo se abre desde terminales |
| `cerrarTrasComprar` | Útil para máquinas de "una compra y fuera" |
| `gestionarCursor` | Suelta el ratón al abrir y lo vuelve a bloquear al cerrar |
| `pausarTiempo` | Pausa el juego mientras está abierta |
| `desactivarMientrasAbierta` | **Arrastra aquí el script de cámara/movimiento de tu jugador** para que no gire al mover el ratón |
| Interfaz propia (`panel`, `contenedorFilas`, `prefabFila`...) | Si las dejas vacías, la interfaz se genera sola |
| `estilo` | Colores, tamaños y fuente de la interfaz generada |

También existe `TiendaMenu.AlgunaAbierta` (estático) para que cualquier script sepa si hay una tienda abierta.

**`FilaArticuloUI`** — una fila (icono, nombre, descripción, estado, precio, botón). Úsala en tu prefab de fila.

**`ConstructorUITienda`** — genera la interfaz por defecto. No hace falta tocarlo.

## 6. Implementarlo en tu proyecto paso a paso

### Montaje rápido con menús

1. **GameObject › Mecánicas › Tienda › Sistema de tienda (gestor + monedero + menú)**. Crea "Sistema Tienda" con todo conectado y un "Punto de entrega" hijo. Mueve ese punto a donde quieras que aparezcan las compras del menú.
2. En `Monedero › Saldo inicial` pon el dinero de partida.
3. En la ventana *Project*: **Create › Mecánicas › Tienda › Artículo**. Rellena nombre, precio y arrastra tu prefab. Repite para cada artículo.
4. **Create › Mecánicas › Tienda › Catálogo**, mete los artículos y arrástralo al campo `Catálogo` del `GestorTienda`.
5. Añade **`InteractorTienda` a la cámara de tu jugador** (o usa tu propio sistema de interacción, ver sección 10).
6. En `TiendaMenu › Desactivar mientras abierta` arrastra el script que mueve la cámara de tu jugador.
7. Para botones del mundo: **GameObject › Mecánicas › Tienda › Botón de compra con pedestal** y asígnale un artículo. Mueve su "Punto de aparición" hijo.
8. Para un mostrador: **GameObject › Mecánicas › Tienda › Terminal de tienda**.
9. Play.

### Con tus propios modelos (sin los pedestales de ejemplo)

- **Botón:** a tu modelo de botón (con Collider) añádele `BotonCompraMundo`. Arrastra la pieza que se mueve a `parteMovil` y su Renderer a `indicador` (opcional). Crea un hijo vacío con `PuntoAparicion` y arrástralo a `receptor`.
- **Máquina expendedora:** `TerminalTienda` en la máquina + un `PuntoAparicion` en la bandeja con `exigirHuecoLibre` activado + un catálogo solo para esa máquina.
- **NPC tendero:** `TerminalTienda` en el NPC (en su collider). Si tu sistema de diálogos decide cuándo abrir, llama a `menu.Abrir(catalogo, punto)` desde el diálogo en vez de usar `InteractorTienda`.

### Jugador

El sistema **no trae un controlador obligatorio**. El `JugadorDemo` es solo para la demo. Con tu controlador solo necesitas:
- `InteractorTienda` en la cámara (o tu interacción llamando a `Interactuar`).
- Que tu cámara no se mueva con la tienda abierta: arrástrala a `desactivarMientrasAbierta` o comprueba `TiendaMenu.AlgunaAbierta` en tu script.
- Etiqueta `Player` en el jugador si usas plataformas/zonas con trigger.

## 7. Conectar un inventario

La tienda **no depende de ningún inventario**. Para conectar el tuyo, crea un adaptador pequeño:

```csharp
using UnityEngine;
using Mecanicas.Tienda;

public class AdaptadorInventario : MonoBehaviour, IInventarioCompras
{
    public MiInventario inventario;   // tu sistema de inventario

    public bool PuedeAnadir(ArticuloTienda articulo, int cantidad)
    {
        return inventario.TieneHueco(articulo.IdInventario, cantidad);
    }

    public bool Anadir(ArticuloTienda articulo, int cantidad)
    {
        return inventario.Anadir(articulo.IdInventario, cantidad);
    }
}
```

Ponlo en cualquier objeto y arrástralo al campo `Inventario` del `GestorTienda` (o deja `buscarInventarioEnEscena` activado). Después, en cada artículo elige `modoEntrega`:

- `Inventario`: va al inventario; si no cabe, **no se cobra** y sale el mensaje "No se puede entregar".
- `InventarioOAparecer`: al inventario si cabe; si no, aparece en el mundo.

`articulo.IdInventario` es el id que debe reconocer tu inventario (campo `idInventario` del artículo, o su `id`). `InventarioDemo.cs` es un ejemplo completo.

## 8. Conectar tu propia economía (monedero)

Si tu juego ya guarda el dinero en otro sitio, implementa `IMonedero`:

```csharp
using System;
using UnityEngine;
using Mecanicas.Tienda;

public class MonederoDeMiJuego : MonoBehaviour, IMonedero
{
    public event Action<string, int> SaldoCambiado;

    public int Saldo(string moneda) => MiPartida.Dinero;

    public bool Gastar(string moneda, int cantidad)
    {
        if (MiPartida.Dinero < cantidad) return false;
        MiPartida.Dinero -= cantidad;
        SaldoCambiado?.Invoke(moneda, MiPartida.Dinero);
        return true;
    }

    public void Anadir(string moneda, int cantidad)
    {
        MiPartida.Dinero += cantidad;
        SaldoCambiado?.Invoke(moneda, MiPartida.Dinero);
    }
}
```

Y arrástralo al campo `Monedero` del `GestorTienda`. Puedes borrar el `Monedero` incluido.

Para **dar dinero** con el incluido: `monedero.Anadir("monedas", 50)`, o desde un UnityEvent con `Monedero.AnadirMonedas(int)`.

## 9. Personalizar y modificar

**Textos y traducción.** En `GestorTienda › Textos` están todos los mensajes. Admiten `{nombre}`, `{precio}`, `{moneda}` y `{saldo}`. Ejemplo: `sinDinero = "Te faltan monedas, cariño"`. En los botones, `textoAviso` y `formatoEtiqueta` funcionan igual.

**Aspecto de la tienda generada.** `TiendaMenu › Estilo`: fuente, tamaño del panel, tamaño de texto, alto de fila y todos los colores.

**Tu propia interfaz (recomendado para el juego final).**
1. Dale a Play, abre la tienda, selecciona en la jerarquía el objeto `Canvas Tienda` (hijo del objeto con `TiendaMenu`), copia (Ctrl+C), sal de Play y pega (Ctrl+V). Ya tienes una copia editable de la interfaz.
2. Cámbiala a tu gusto (imágenes, fuentes, posiciones...). Convierte la "Fila artículo" en prefab.
3. En `TiendaMenu` asigna `panel` (el objeto "Tienda", que se enciende/apaga), `contenedorFilas` (el "Contenido" del scroll), `prefabFila`, y si quieres pestañas `contenedorCategorias` + `prefabPestana`, y los textos de título, saldo y mensaje. Todas son opcionales salvo `panel`, `contenedorFilas` y `prefabFila`.

**TextMeshPro.** Crea una clase que herede de `FilaArticuloUI`, añade tus campos `TMP_Text` y sobrescribe `Pintar()`:

```csharp
public class FilaTMP : FilaArticuloUI
{
    public TMPro.TMP_Text nombreTMP, precioTMP;
    public override void Pintar()
    {
        base.Pintar();
        nombreTMP.text = Articulo.NombreVisible;
        precioTMP.text = menu.TiendaActual.textos.Precio(Articulo);
    }
}
```

**Otra forma de entregar.** Implementa `IReceptorCompra` (por ejemplo, que el objeto llegue por una cinta transportadora, un dron o un buzón) y arrástralo al campo `receptor` del botón, de la terminal, del menú o al `receptorPorDefecto` del gestor:

```csharp
public class EntregaPorCinta : MonoBehaviour, IReceptorCompra
{
    public Transform inicioCinta;
    public bool PuedeEntregar(ArticuloTienda a, int cantidad) => a.prefab != null;
    public bool Entregar(ArticuloTienda a, int cantidad, List<GameObject> creados)
    {
        for (int i = 0; i < cantidad; i++)
            creados.Add(Instantiate(a.prefab, inicioCinta.position + Vector3.up * i * 0.3f, inicioCinta.rotation));
        return true;
    }
}
```

**Teclas.** `InteractorTienda.tecla` y `TiendaMenu.teclaAbrir/teclaCerrar` (lista de teclas comunes). Para mando o tu propio Input System, pon las teclas a `Ninguna` y llama tú a los métodos (`Interactuar`, `Abrir`, `Cerrar`, `Alternar`). Si necesitas más teclas, añádelas al enum `TeclaTienda` en `EntradaTienda.cs` (dos `case`, uno por cada sistema de entrada).

**Reglas de compra.** Las reglas (límite, stock, dinero) están en `ReglasCompra.Validar`. Si quieres por ejemplo descuentos o requisitos de nivel, el sitio es `GestorTienda.PuedeComprar` y `GestorTienda.Comprar`. Los tests de `Tests/EditMode` te avisan si rompes algo (*Window › General › Test Runner › EditMode › Run All*).

**Guardado.** `Monedero.guardarEnPlayerPrefs` y `GestorTienda.guardarCompras` usan PlayerPrefs. Si tu juego tiene su propio sistema de guardado, déjalos desactivados y guarda tú `Saldo(...)` y `ComprasHechas(...)`. Para borrar el guardado en pruebas: *Herramientas › Mecánicas › Tienda › Borrar compras y dinero guardados*.

## 10. Usarlo desde código

```csharp
using Mecanicas.Tienda;

// Comprar (con el punto de aparición por defecto)
ResultadoCompra r = GestorTienda.Principal.Comprar(miArticulo);
if (!r.Exito) Debug.Log(r.Mensaje);            // "No tienes suficientes monedas (...)"

// Comprar entregando en un sitio concreto
GestorTienda.Principal.Comprar(miArticulo, miPuntoAparicion);

// Saber si se puede antes de mostrar algo
if (GestorTienda.Principal.PuedeComprar(miArticulo) == MotivoCompra.Ok) { ... }

// Objetos creados por la compra
foreach (var go in r.ObjetosCreados) go.GetComponent<Rigidbody>().AddForce(...);

// Escuchar todas las compras (logros, misiones, analíticas)
GestorTienda.CualquierCompra += res => { if (res.Exito) Debug.Log("Comprado " + res.Articulo.NombreVisible); };

// Abrir la tienda desde un diálogo, con otro catálogo y otro punto de entrega
FindFirstObjectByType<TiendaMenu>().Abrir(catalogoArmeria, puntoMostrador);

// Pulsar un botón del mundo desde tu propio sistema de interacción
botonCompra.Interactuar(jugador);      // o miTerminal.Interactuar(jugador)

// Dinero
GestorTienda.Principal.Monedero.Anadir("monedas", 100);
int gemas = GestorTienda.Principal.Saldo("gemas");

// Stock
GestorTienda.Principal.Reponer(miArticulo, 5);
```

Cualquier objeto de tu juego que implemente `IInteractuableTienda` (texto de aviso + `Interactuar`) funcionará con `InteractorTienda`; así es como está hecho el cofre de la demo.

Por UnityEvent sin código: `GestorTienda.ComprarArticulo(ArticuloTienda)`, `GestorTienda.ComprarPorId(string)`, `BotonCompraMundo.Pulsar()`, `DisparadorCompra.Comprar()`, `TiendaMenu.Abrir()/Cerrar()/Alternar()`, `Monedero.AnadirMonedas(int)`.

## 11. Recetas rápidas

| Quiero... | Cómo |
|---|---|
| Una mejora que se compra una vez | `modoEntrega = SoloEvento`, `maxCompras = 1`, y en `GestorTienda.alComprar` (o `CompraProcesada`) aplica la mejora |
| Un pack de 10 balas | `cantidadPorCompra = 10` |
| Objeto limitado | `stockInicial = 3` |
| Que no salga en la tienda, solo en un botón | Desmarca `visibleEnTienda` |
| Pagar con otra moneda | Escribe `gemas` en `moneda` del artículo y añade `gemas` al `Monedero` |
| Tienda que solo se abre en el mostrador | `TiendaMenu.abrirConTecla = false` + `TerminalTienda` |
| Que el objeto salga disparado | `PuntoAparicion.impulsoLocal = (0, 2, 3)` |
| Máquina que no vende si la bandeja está ocupada | `PuntoAparicion.exigirHuecoLibre = true` |
| Un "pulsa para comprar" en el suelo | `BotonCompraMundo.comprarAlEntrarEnTrigger = true` + Collider *Is Trigger* + jugador con etiqueta `Player` |
| Cambiar sonidos | `GestorTienda.sonidoCompra/sonidoError`, `BotonCompraMundo.sonidoPulsar`, `PuntoAparicion.sonidoAparicion` |
| Comprar desde el sistema de eventos/triggers | `DisparadorCompra.Comprar()` en el UnityEvent del trigger |

## 12. Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| No sale el aviso "[E] Comprar" | Falta `InteractorTienda` en la cámara, el botón no tiene Collider, la distancia es corta o la capa no está en `capas`. Si el collider del botón es *trigger*, desmarca `ignorarTriggers` |
| "No se puede entregar" siempre | El artículo es `Aparecer` y no hay `PuntoAparicion` (ni en el botón ni como `receptorPorDefecto`), o no tiene `prefab`, o `exigirHuecoLibre` detecta algo encima (¡puede ser el propio pedestal! sube el punto o baja `radioComprobacion`) |
| La cámara gira con la tienda abierta | Arrastra tu script de cámara a `desactivarMientrasAbierta` |
| Los clics no funcionan en la tienda | Falta EventSystem o tienes uno con `StandaloneInputModule` y usas el Input System nuevo (ver Instalación) |
| La plataforma no compra al pisarla | El collider de la plataforma no es *Is Trigger*, el jugador no tiene la etiqueta `Player`, o el jugador no tiene Rigidbody ni CharacterController |
| El dinero no se reinicia entre pruebas | Tienes activado el guardado: *Herramientas › Mecánicas › Tienda › Borrar compras y dinero guardados* |
| Los objetos salen rosas | Material de otro render pipeline; usa materiales de tu pipeline en tus prefabs |

## 13. Estructura de archivos

```
Mecanicas/Tienda/
├─ README.md                    ← este documento
├─ package.json                 ← para instalarlo como paquete (opción B)
├─ Runtime/                     ← lo que necesitas en tu juego (ensamblado Mecanicas.Tienda)
│  ├─ Datos/        ArticuloTienda, CatalogoTienda
│  ├─ Economia/     IMonedero, Monedero
│  ├─ Nucleo/       GestorTienda (+ TextosTienda), ReglasCompra, ResultadoCompra
│  ├─ Entrega/      IReceptorCompra, IInventarioCompras, PuntoAparicion
│  ├─ Mundo/        InteractorTienda, IInteractuableTienda, BotonCompraMundo, TerminalTienda, DisparadorCompra
│  ├─ UI/           TiendaMenu, FilaArticuloUI, EstiloTiendaUI, ConstructorUITienda
│  └─ Utilidades/   EntradaTienda (teclado/ratón para los dos Input System), FabricaTienda (montajes por código)
├─ Demo/                        ← se puede borrar en tu juego final
│  ├─ TiendaDemo, JugadorDemo, InventarioDemo, CofreMonedasDemo
│  └─ Editor/   menú «Crear escena de demostración»
├─ Editor/                      ← menús del editor e inspector del gestor
└─ Tests/EditMode/              ← pruebas de las reglas y del flujo de compra
```

Todo el código está en el espacio de nombres `Mecanicas.Tienda`. Para quitar la demo de tu juego final basta con borrar la carpeta `Demo` (se lleva su menú con ella).

## Estado

Escrito sin abrir Unity: el código compila contra una réplica de la API de Unity, pero **todavía no se ha probado dentro del editor**. Lo primero al abrirlo: crear la escena de demostración, darle a Play y pasar los tests de EditMode.
