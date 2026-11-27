# Inventario (mecánica suelta para Unity 3D en primera persona)

Sistema de inventario completo y reutilizable. Va en su propia carpeta y no depende de ninguna otra mecánica ni de ningún proyecto: lo copias a cualquier juego de Unity y funciona.

---

## 1. Para qué sirve

Para que el jugador pueda **recoger objetos del mundo, guardarlos, verlos en una ventana, organizarlos, llevarlos en la mano, usarlos y tirarlos**. Es la base de muchas otras cosas:

- Llaves para abrir puertas (la puerta pregunta "¿tiene la llave?").
- Monedas o materiales para comprar o construir.
- Comida, botiquines, pilas para la linterna...
- Cofres y almacenes (otro `Inventory` en el cofre).
- Objetos de misión.

## 2. Qué incluye

| Función | Detalle |
|---|---|
| Objetos como archivos | Cada tipo de objeto (manzana, llave...) es un `ItemData` (ScriptableObject): nombre, descripción, icono, categoría, pila máxima, peso, modelo 3D, si se puede usar/gastar/tirar. |
| Casillas y pilas | Número de casillas configurable; los objetos apilables se juntan solos. |
| Peso máximo | Opcional. Si no cabe por peso, se queda en el suelo. |
| Barra rápida | Las primeras casillas (hasta 9). Se elige con 1-9 o la rueda del ratón y el objeto se ve **en la mano**. |
| Recoger | Mirando al objeto y pulsando **E**, o automáticamente al pasar por encima. |
| Usar | Clic derecho en la casilla, o clic izquierdo con el objeto en la mano. |
| Tirar | **G** (una unidad), **Mayús+G** (toda la pila), o arrastrar la casilla fuera de la ventana. El objeto cae al mundo con física. |
| Ventana del inventario | **Tab** o **I**. Arrastrar y soltar, dividir pilas, mover rápido, ordenar, tooltip con descripción, peso total. |
| Interfaz generada sola | No hay que montar Canvas a mano: se crea al darle a Play. Colores, tamaños, textos y fuente se cambian desde el Inspector. También admite tu propio prefab de casilla. |
| Guardar y cargar | A JSON (para tu sistema de partidas) o rápido a PlayerPrefs (F5 / F9). |
| Eventos | C# y UnityEvent (Inspector) para: añadido, quitado, usado, tirado, lleno, cambiado. |
| Input | Funciona con el **Input System nuevo** y con el **Input Manager antiguo** sin tocar nada. |
| Demo | Un menú crea una escena de prueba con objetos, jugador e interfaz. |
| Tests | 8 tests de la lógica (Window > General > Test Runner). |

## 3. Requisitos

- Unity **2022.3 LTS o Unity 6** (pensado para Unity 6).
- Paquete **Unity UI (uGUI)**, viene instalado por defecto.
- Opcional: paquete **Input System**. Si está instalado se usa; si no, se usa el Input Manager antiguo.
- Cualquier render pipeline (Built-in, URP o HDRP). La demo elige el shader adecuado.

## 4. Instalación

Elige **una** de estas dos formas:

**A) Copiar la carpeta (lo más sencillo).** Copia la carpeta `Mecanicas/Inventario` de este repositorio dentro de tu proyecto, en `Assets/Mecanicas/Inventario`. Ya está.

**B) Como paquete desde Git** (se actualiza solo y no ensucia tu carpeta Assets). En Unity: *Window > Package Manager > + > Add package from git URL...* y pega:

```
https://github.com/cazadoxs/UnityGames.git?path=Mecanicas/Inventario#claude/project-thread-jxrhxu
```

(Cuando la rama se fusione, cambia lo que hay después de `#` por el nombre de la rama principal o quítalo.) Con esta opción los archivos son de solo lectura; para modificarlos usa la opción A.

## 5. Probar la demo (1 minuto)

1. Menú **Herramientas > Mecánicas > Inventario > Crear escena demo**.
2. Se crea `Assets/Mecanicas/InventarioDemo/` con la escena, los objetos, prefabs y materiales. Se abre la escena.
3. Pulsa **Play**.

Controles de la demo:

| Tecla | Acción |
|---|---|
| WASD / ratón / Espacio / Mayús | Moverse, mirar, saltar, correr |
| **E** | Recoger el objeto al que miras |
| **Tab** o **I** | Abrir/cerrar inventario (Esc también cierra) |
| **1-6** o rueda | Elegir casilla de la barra rápida |
| **Clic izquierdo** | Usar lo que tienes en la mano (la linterna se enciende; la manzana se come) |
| **G** / **Mayús+G** | Tirar una unidad / la pila entera |
| **F5** / **F9** | Guardar / cargar el inventario |
| **H** | Ocultar la ayuda |

Dentro de la ventana del inventario:

| Acción | Resultado |
|---|---|
| Arrastrar una casilla a otra | Mover; si es el mismo objeto se juntan; si no, se intercambian |
| **Ctrl** + arrastrar | Mover solo una unidad |
| Arrastrar fuera de la ventana | Tirarlo al suelo |
| Clic derecho | Usar |
| Clic con la rueda | Dividir la pila por la mitad |
| **Mayús** + clic | Pasar de la barra rápida a la mochila o al revés |
| Clic izquierdo (fila de la barra) | Seleccionarla |
| Botón **Ordenar** | Junta pilas y ordena la mochila por categoría y nombre (no toca la barra rápida) |

En la demo hay: manzanas (comida apilable que quita hambre, ejemplo de objeto con código propio), piedras (pesan 1, el límite es 30), monedas (pila de 99; las del fondo se recogen solas al pasar por encima), una llave, una botella y la linterna con la que empiezas.

## 6. Implementarlo en tu juego, paso a paso

### 6.1 Preparar el jugador

En el GameObject de tu jugador añade (botón *Add Component*, todos están en **Mecánicas > Inventario**):

1. **Inventario** (`Inventory`): número de casillas, tamaño de la barra rápida, peso máximo (0 = sin límite), contenido inicial y la base de datos (solo hace falta para cargar partidas). En *Drop Origin* arrastra la cámara.
2. **Recogedor (primera persona)** (`InventoryInteractor`): arrastra tu cámara a *View Camera*. Ajusta el alcance y la tecla.
3. **Barra rápida y mano** (`InventoryHotbar`): crea un hijo vacío de la cámara llamado `Mano`, colócalo abajo a la derecha (por ejemplo `0.32, -0.28, 0.55`) y arrástralo a *Hand Anchor*. Si no quieres objeto en la mano, déjalo vacío.
4. Opcional: **Guardado rápido** (`InventorySaver`).

### 6.2 Poner la interfaz

Crea un GameObject vacío en la escena (por ejemplo `InventarioUI`) y añádele **Interfaz del inventario** (`InventoryUI`). Arrastra el jugador a los campos *Inventory*, *Hotbar* e *Interactor* (si no lo haces, los busca solo). Si la escena no tiene EventSystem, lo crea.

### 6.3 Que tu controlador no gire la cámara con el inventario abierto

Es la única línea que tienes que añadir a tu propio controlador de primera persona:

```csharp
if (Mecanicas.Inventario.InventoryUI.IsAnyOpen) return; // no mover cámara ni personaje
```

(La interfaz ya desbloquea y muestra el cursor al abrir, y lo bloquea al cerrar. Si tu controlador también gestiona el cursor, desactiva *Lock Cursor When Closed* en `InventoryUI`.)

### 6.4 Crear objetos

1. En la ventana *Project*: clic derecho > **Create > Mecánicas > Inventario > Objeto**.
2. Rellena: *Id* (único y sin cambiarlo después, se usa al guardar), nombre, descripción, categoría, icono (Sprite; si no hay se muestra un cuadrado de color con las iniciales), *Max Stack*, peso, si se puede usar/gastar/tirar.
3. *World Prefab*: el modelo que aparece en el suelo al tirarlo. Necesita un Collider; el Rigidbody y el `WorldItem` se añaden solos si faltan. Si lo dejas vacío, aparece un cubo del color del icono.
4. *Held Prefab*: el modelo que se ve en la mano (si está vacío se usa el de suelo) y *Held Scale* para su tamaño.
5. Añádelo a tu **Base de datos de objetos** (Create > Mecánicas > Inventario > Base de datos de objetos) si vas a guardar partidas.

### 6.5 Poner objetos en el mundo

Cualquier modelo con Collider + componente **Objeto en el mundo (recogible)** (`WorldItem`): elige el `ItemData` y la cantidad. Activa *Pickup On Touch* si quieres que se recoja al pasar por encima (monedas, munición). Lo más cómodo es usar el *World Prefab* del objeto y arrastrarlo a la escena.

## 7. Cómo funciona por dentro

```
ItemData (archivo)  ──┐
                      │   WorldItem (en el suelo) ──E──> InventoryInteractor ──Add()──┐
                      │                                                                ▼
                      └──────────────────────────────────────────────────────> Inventory (componente)
                                                                                       │  envuelve
                                                                                       ▼
                                                                               InventoryModel (lógica pura)
                                                                                       │  eventos
                                ┌──────────────────────────┬───────────────────────────┤
                                ▼                          ▼                           ▼
                         InventoryUI (ventana,     InventoryHotbar (mano,       Tu juego (puertas, tiendas,
                         barra, tooltip)           usar, tirar)                 misiones, ItemUseListener)
```

- **`ItemData`**: qué es un objeto. No guarda cantidades; es la "ficha" del objeto. Se puede heredar para darle comportamiento (ver 8.4).
- **`ItemStack`**: una casilla = objeto + cantidad.
- **`InventoryModel`**: toda la lógica, sin nada de Unity: añadir (primero rellena pilas existentes, después casillas vacías, y respeta el peso), quitar, mover, intercambiar, dividir, ordenar, redimensionar. Se puede usar suelta (`new InventoryModel(20)`) para cofres invisibles, tiendas, etc. Está cubierta por los tests.
- **`Inventory`**: el componente que pones en el jugador. Llama al modelo y lanza los eventos (`ItemAdded`, `ItemRemoved`, `ItemUsed`, `ItemDropped`, `InventoryFull`, `SlotChanged`, `Changed` y sus versiones UnityEvent). También tira objetos al mundo y guarda/carga.
- **`WorldItem`**: un objeto en el suelo. `TryPickup()` mete lo que quepa; si no cabe todo, el resto se queda en el suelo con la cantidad que sobra. `WorldItem.Spawn()` crea uno nuevo (lo usa "tirar").
- **`InventoryInteractor`**: cada frame lanza un rayo desde la cámara. Si toca un `WorldItem`, guarda el texto del aviso (`PromptText`) y, al pulsar E, lo recoge.
- **`InventoryHotbar`**: la casilla seleccionada de la barra. Instancia una copia del modelo en `Mano` quitándole física y colisiones. Usa y tira la casilla seleccionada.
- **`InventoryUI`** + **`InventorySlotUI`**: al empezar genera un Canvas con la barra inferior, la ventana (rejilla de casillas), el tooltip, el punto de mira, el aviso de recoger y los mensajes. Escucha los eventos del inventario y solo redibuja la casilla que cambia. Mientras está abierta desactiva la barra rápida y el recogedor.
- **`InventoryInput`**: traduce las teclas (`KeyCode`) al Input System nuevo o al antiguo, según lo que tenga el proyecto.
- **`ItemUseListener`**: conecta "al usar este objeto" con cualquier cosa del Inspector, sin programar.
- **`InventorySaver`**: guardado rápido en PlayerPrefs.

**Usar un objeto** (`Inventory.UseItem(casilla)`): comprueba `CanUse()`, llama a `Use()`, si devuelve `true` y el objeto tiene *Consume On Use* gasta una unidad y lanza `ItemUsed`.

**Guardado**: se guarda solo `Id` + cantidad + número de casilla en JSON. Al cargar, la base de datos convierte cada `Id` en su `ItemData`. Por eso el *Id* no se debe cambiar una vez publicado el juego.

## 8. Usarlo desde código

Todo está en el espacio de nombres `Mecanicas.Inventario`.

### 8.1 Una puerta que necesita llave

```csharp
using Mecanicas.Inventario;
using UnityEngine;

public class PuertaConLlave : MonoBehaviour
{
    public ItemData llave;
    public bool gastarLlave = false;

    public bool IntentarAbrir(Inventory inv)
    {
        if (gastarLlave ? !inv.TryConsume(llave) : !inv.Contains(llave)) return false;
        // ... abrir la puerta
        return true;
    }
}
```

### 8.2 Comprar algo (tienda)

```csharp
if (inv.CanAdd(espada) && inv.TryConsume(moneda, 50))
    inv.Add(espada);
```

### 8.3 Dar objetos y reaccionar a eventos

```csharp
inv.Add(manzana, 3);                       // devuelve lo que no ha cabido
int cuantas = inv.Count(moneda);
inv.ItemAdded += (item, n) => Debug.Log("+" + n + " " + item.DisplayName);
inv.InventoryFull += item => Debug.Log("No cabe " + item.DisplayName);
```

Otros métodos útiles: `Remove`, `RemoveFromSlot`, `GetSlot`, `MoveOrSwap`, `Split`, `SortBackpack`, `UseItem`, `DropFromSlot`, `TransferTo(otroInventario, casilla)` (cofres), `SetCapacity` (mochilas más grandes), `ToJson` / `LoadFromJson`.

### 8.4 Objetos con comportamiento propio

Hereda de `ItemData` y sobrescribe `Use` (y si quieres `CanUse` y `GetTooltipExtra`). Ejemplo real en `Runtime/Demo/FoodItemData.cs`:

```csharp
[CreateAssetMenu(menuName = "Mecánicas/Inventario/Botiquín")]
public class BotiquinData : ItemData
{
    public int curacion = 50;
    public override bool Use(Inventory owner, int slot)
    {
        var vida = owner.GetComponent<MiVida>();
        if (vida == null || vida.EstaLlena) return false; // false = no se gasta
        vida.Curar(curacion);
        return true;                                       // true = usado (se gasta si Consume On Use)
    }
    public override string GetTooltipExtra() => "Cura " + curacion;
}
```

### 8.5 Sin programar: `ItemUseListener`

Crea un GameObject, añade **Al usar objeto (evento)**, arrastra el `ItemData` y en *On Used* añade lo que quieras (encender una luz, reproducir un sonido, activar una animación, `InventoryUI.ShowMessage`...). Así está hecha la linterna de la demo.

### 8.6 Guardar dentro de tu sistema de partidas

```csharp
string json = inv.ToJson();      // guárdalo donde quieras
inv.LoadFromJson(json);          // necesita la base de datos asignada en el Inventory
```

## 9. Cómo modificarlo

| Quiero... | Dónde |
|---|---|
| Cambiar colores, tamaño de casillas, columnas, fuente, textos | `InventoryUI` > **Theme** en el Inspector. Para fondos con bordes redondeados asigna *Panel Sprite* y *Slot Sprite* (sprites 9-slice). |
| Casillas con mi propio diseño | Haz un prefab de UI con `InventorySlotUI` y rellena sus referencias (fondo, icono, cantidad...). Asígnalo en `InventoryUI` > *Slot Prefab*. |
| Cambiar teclas | Cada componente tiene sus teclas: `InventoryUI` (abrir/cerrar), `InventoryInteractor` (recoger), `InventoryHotbar` (usar/tirar), `InventorySaver`. |
| Usar acciones del Input System, mando o reasignación | Cambia las funciones de `Runtime/Input/InventoryInput.cs`; todo el sistema lee la entrada solo desde ahí. |
| Pausar el juego al abrir | `InventoryUI` > *Pause When Open*. |
| Sin barra rápida | `Inventory` > *Hotbar Size* = 0 y quita `InventoryHotbar`. |
| Sin límite de peso | `Inventory` > *Max Weight* = 0. |
| Más casillas en mitad de la partida | `inventory.SetCapacity(36)`; la interfaz se regenera sola. |
| Una interfaz totalmente distinta (UI Toolkit, TextMeshPro...) | No uses `InventoryUI`: escucha `inventory.SlotChanged` / `Changed` y lee `GetSlot(i)`. Toda la lógica está en `Inventory`. |
| Un cofre | Pon otro `Inventory` en el cofre (sin barra rápida) y mueve cosas con `TransferTo`. |
| Que recoger no sea con la vista | No uses `InventoryInteractor`: llama a `worldItem.TryPickup(inventory)` desde tu propio sistema de interacción. |
| Mensajes en otro idioma | Textos en `Theme` (título, botón, ayuda, formato del peso) y en `WorldItem` / `InventoryInteractor` (aviso y "Inventario lleno"). |

## 10. Encaje con las otras mecánicas

Esta mecánica no depende de ninguna otra. Para combinarlas basta con llamar a su API: las **puertas y candados** preguntan `Contains(llave)`, la **tienda** usa `TryConsume(moneda, precio)` + `Add(objeto)`, el **sistema de eventos** puede escuchar `onItemAdded` / `onItemUsed` desde el Inspector y el de **inspección y agarre** puede convertir un objeto agarrado en uno de inventario con `WorldItem.TryPickup`.

## 11. Problemas frecuentes

- **No puedo hacer clic en la interfaz**: falta un EventSystem compatible. `InventoryUI` crea uno si no hay ninguno; si ya tienes uno con `StandaloneInputModule` y tu proyecto usa solo el Input System nuevo, cambia el módulo por `InputSystemUIInputModule` (Unity ofrece un botón para hacerlo en el Inspector del EventSystem).
- **La cámara gira con el inventario abierto**: falta la línea del apartado 6.3 en tu controlador.
- **El aviso "Recoger" no sale**: el objeto necesita un Collider y el `WorldItem` (en él o en un padre); revisa el alcance y las *Layers* del `InventoryInteractor`.
- **Materiales rosas en la demo**: vuelve a crear la demo después de configurar el render pipeline (URP/HDRP) del proyecto.
- **Al cargar faltan objetos**: el objeto no está en la Base de datos, o se le cambió el *Id*.
- **Los objetos tirados atraviesan el suelo**: el prefab necesita un Collider no-trigger.

## 12. Archivos

```
Inventario/
├── package.json                    (para instalarlo como paquete)
├── README.md                       (este archivo)
├── Runtime/
│   ├── Data/       ItemData, ItemStack, ItemDatabase
│   ├── Core/       InventoryModel (lógica), Inventory (componente + guardado)
│   ├── World/      WorldItem, InventoryInteractor, InventoryHotbar, ItemUseListener, InventorySaver
│   ├── UI/         InventoryUI, InventorySlotUI, InventoryUITheme
│   ├── Input/      InventoryInput (Input System nuevo y antiguo)
│   └── Demo/       Controlador FPS sencillo, comida de ejemplo, hambre, ayuda (solo para la demo)
├── Editor/         Generador de la demo, Inspector del inventario (en Play muestra el contenido y botones de prueba)
└── Tests/Editor/   Tests de la lógica
```

La carpeta `Runtime/Demo` se puede borrar si no la quieres en tu juego (y entonces también `Editor/InventoryDemoBuilder.cs`).

## 13. Estado

Escrito y comprobado fuera de Unity: compila contra las librerías de UnityEngine (con Input System nuevo y con el antiguo) y los 8 tests de lógica pasan. **Todavía no se ha abierto en el editor de Unity**: lo primero en el PC es importarlo, crear la demo y probarla; si sale algún error de compilación o de interfaz, se corrige en un momento.
