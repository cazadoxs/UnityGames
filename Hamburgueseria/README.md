# Hamburguesería 3D: mecánicas de cocina (prototipo para Unity)

Sistema completo y ampliable para un juego de hamburguesería **en primera persona**: preparar, cocinar y servir hamburguesas, patatas, batidos, refrescos y helados de máquina, con clientes que hacen cola, piden y esperan. Está pensado para **montarlo encima de tu propio escenario**: todo funciona con bloques provisionales y luego cambias cada bloque por tu modelo.

- **Sin arte obligatorio:** cada ingrediente se dibuja con una primitiva de color hasta que le pongas su modelo.
- **Basado en datos:** ingredientes, recetas, productos de la carta y reglas de la jornada son *ScriptableObjects*. Añadir un plato nuevo no requiere programar.
- **Estaciones genéricas:** una misma clase sirve para plancha, freidora, tabla de cortar, batidora, grifo de refrescos o máquina de helados; solo cambian las recetas y el modo.
- **Cocina de prueba en un clic:** un menú del editor crea todos los datos y monta una cocina gris jugable.

---

## Índice

1. [Requisitos](#1-requisitos)
2. [Prueba rápida (5 minutos)](#2-prueba-rápida-5-minutos)
3. [Controles](#3-controles)
4. [Cómo se prepara cada cosa](#4-cómo-se-prepara-cada-cosa)
5. [Montarlo en tu escenario](#5-montarlo-en-tu-escenario)
6. [Referencia de componentes](#6-referencia-de-componentes)
7. [Cómo ampliarlo](#7-cómo-ampliarlo)
8. [Arquitectura del código](#8-arquitectura-del-código)
9. [Pruebas](#9-pruebas)
10. [Limitaciones y siguientes pasos](#10-limitaciones-y-siguientes-pasos)

---

## 1. Requisitos

| | |
|---|---|
| **Unity** | Unity 6 (6000.0 o posterior). Usa `Rigidbody.linearVelocity`, que no existe en versiones anteriores. |
| **Render pipeline** | URP (recomendado) o Built-in. Los colores provisionales funcionan en ambos. |
| **Entrada** | Funciona con el paquete **Input System** (lo trae Unity 6 por defecto) o con el **Input Manager** clásico. No hace falta crear acciones. |
| **Paquetes opcionales** | *AI Navigation* si quieres que los clientes usen NavMesh (sin él andan en línea recta entre puntos). |

**Instalación:** copia la carpeta `Assets/BurgerKitchen` de este directorio dentro de la carpeta `Assets` de tu proyecto de Unity. Unity compilará tres ensamblados: `BurgerKitchen.Runtime`, `BurgerKitchen.Editor` y `BurgerKitchen.Tests.EditMode`.

## 2. Prueba rápida (5 minutos)

1. Crea una escena vacía (`File > New Scene > Basic (URP)` o la que quieras).
2. Menú **`Tools > Burger Kitchen > 1. Crear datos de ejemplo`**. Se crea `Assets/BurgerKitchenData` con 34 objetos, 15 recetas, 14 productos, la carta y la jornada.
3. Menú **`Tools > Burger Kitchen > 2. Montar cocina de prueba en la escena`**. Aparece `BurgerKitchen_Greybox` con todas las estaciones, el jugador, los clientes y la interfaz. (Desactiva la *Main Camera* que hubiera.)
4. Dale a **Play**, pulsa **Enter** para abrir el local y **F1** para ver los controles.

Distribución de la cocina de prueba:

```
                 (calle: entran los clientes, hacen cola y esperan)
   ─────────────────────────────────────────────────────────────────────────
   Vasos·Batidora·Vasos·Refrescos·CAJA·Bandejas·Encimera·ENTREGA·Cucur.·Tarr.·Helados·Sirope·Galleta   ← mostrador
                                      (jugador)
   Lechugas·Tomates·Tabla·Encimera·Encimera·Pan base·Pan tapa·Queso·Salsa·Papel                        ← isla
   Carne·Bacon·PLANCHA·Congelador·FREIDORA·Reparto patatas·Cajas patatas·Basura                         ← fondo
```

## 3. Controles

| Acción | Teclado y ratón | Mando |
|---|---|---|
| Moverse / correr | WASD / Shift | Stick izquierdo / L3 |
| Mirar | Ratón | Stick derecho |
| **Acción**: coger, dejar, montar, entregar, tomar nota | **E** o clic izquierdo | A |
| **Trabajar** (mantener): cortar, servir, empezar a batir, dar la vuelta, limpiar, reparar | **F** o clic derecho | X |
| Cambiar opción (sabor, tipo de refresco) | **Q** o rueda | LB / RB |
| Soltar lo que llevas (la comida se ensucia) | **G** | Y |
| Abrir el local / siguiente día | Enter | Start |
| Ayuda | F1 | Select |
| Liberar el ratón | Esc (clic para recuperarlo) | |

Apuntas con el punto del centro de la pantalla. Al mirar algo se resalta y aparece una ayuda con lo que puedes hacer con lo que llevas en la mano.

## 4. Cómo se prepara cada cosa

| Producto | Pasos |
|---|---|
| **Hamburguesa** | Coge un **papel de hamburguesa** y déjalo en una encimera de montaje. Pon carne cruda en la **plancha**; a mitad hay que **darle la vuelta (F)**; cuando esté hecha retírala antes de que se queme. Apila sobre el papel: pan base → ingredientes → pan tapa. Truco: con el papel en la mano, al usar una caja (queso, pan, salsa) se añade directamente, y al usar la plancha recoges la carne hecha encima. |
| Verduras | Lechuga o tomate a la **tabla de cortar** y mantener **F**. |
| Bacon | A la plancha, igual que la carne (sin vuelta). |
| **Patatas** | Patatas congeladas a la **freidora** (se queman si las dejas). Vuelca la cesta en el **reparto de patatas** (da 5 raciones que se enfrían a los 90 s). Con una **caja de patatas vacía** en la mano, usa el reparto para llenarla. La freidora pide **cambiar el aceite** cada 8 cestas (mantener F). |
| **Batido** | Vaso de batido a la **batidora** → **Q** para elegir sabor → **F** para batir. Se limpia cada 6 usos. |
| Refresco | Vaso al **grifo de refrescos** → **Q** elige sabor → mantener **F** para servir. |
| **Helado** | Cucurucho o tarrina a la **máquina de helados** y mantener **F**. Para la tarrina con toppings, añade sirope o galleta (cada uno es un producto distinto de la carta). La máquina necesita limpieza cada 10 usos y a veces **se avería** (mantener F para repararla). |
| **Servir** | Mete todo en una **bandeja** (hasta 4 cosas) y llévala a **Entrega de pedidos**, o entrega cada cosa suelta. Se reparte solo entre los pedidos abiertos, del más antiguo al más nuevo. |
| Caja | Cuando un cliente llega a la caja dice "¡Quiero pedir!". Usa la **caja registradora** para tomar nota; el pedido aparece arriba a la derecha con su barra de paciencia. |

Reglas: lo quemado, lo crudo y lo que se ha caído al suelo no cuenta para ningún pedido; tíralo en la **basura** (las bandejas y papeles se vacían y se conservan). Cuanto antes sirves, más **propina** y más **estrellas**. Cada día se desbloquean platos nuevos y llegan más clientes; el progreso se guarda.

## 5. Montarlo en tu escenario

La forma más rápida es partir de la cocina de prueba y sustituir piezas. Si prefieres montar desde cero, estos son los pasos:

### 5.1 Sistemas (una vez por escena)

Crea un objeto vacío `Sistemas` con:
- `KitchenGameManager`: asigna **Settings** (`Shift_Normal`) y **Menu** (`Menu_Principal`).
- `OrderManager`.
- `KitchenHUD` (interfaz provisional; puedes quitarla cuando tengas la tuya).

### 5.2 Jugador

```
Player                (CharacterController altura 1.8, centro y=0.9 · PlayerInputReader · FirstPersonController · PlayerInteractor)
└── CameraPivot       (posición local y = 1.65)
    └── Main Camera   (tag MainCamera, near clip 0.03)
        └── HoldPoint (posición local 0.28, -0.32, 0.6: dónde se ve lo que llevas)
```

En `FirstPersonController` asigna `Camera Pivot`; en `PlayerInteractor` asigna la cámara y `HoldPoint` (si no, los busca solo). Si usas capas, quita la capa del jugador de `Interaction Mask`.

### 5.3 Estaciones

Para cada mueble de tu escenario:
1. Asegúrate de que tiene un **Collider** (en él o en un hijo). Es lo que el jugador apunta.
2. Añade el componente que toque (tabla de abajo) y opcionalmente `Highlightable` (resalta al mirar).
3. Para estaciones con huecos (`CounterStation`, `ProcessingStation`), crea **hijos vacíos sin escala** donde quieras que se coloquen los objetos (sobre la plancha, dentro de la freidora...) y arrástralos a **Slot Anchors**. El hueco más cercano al punto donde miras es el que se usa.

| Mueble | Componente | Configuración |
|---|---|---|
| Encimera de montaje / bandejas | `CounterStation` | Huecos. |
| Plancha | `ProcessingStation` | Recetas `GrillPatty`, `BurnPatty`, `GrillBacon`, `BurnBacon` · Automatic · WhenPlaced · 4 huecos. |
| Freidora | `ProcessingStation` + `MachineMaintenance` | `FryFries`, `BurnFries` · Automatic · WhenPlaced · mantenimiento 8 usos "Cambiar el aceite". |
| Tabla de cortar | `ProcessingStation` | `ChopLettuce`, `SliceTomato` · HoldToWork. |
| Batidora | `ProcessingStation` + `MachineMaintenance` | 3 recetas de batido · Automatic · **Manual**. |
| Grifo de refrescos | `ProcessingStation` | `SodaCola`, `SodaOrange` · HoldToWork. |
| Máquina de helados | `ProcessingStation` + `MachineMaintenance` | `FillCone`, `FillCup` · HoldToWork · avería 6 %. |
| Reparto de patatas | `BulkPortionStation` | Bulk Input `FriesBatch` · receta `FriesBoxEmpty → Fries`. |
| Nevera, cajas, pilas de vasos, salsas... | `ItemSource` | El `ItemDefinition` que da. Stock y coste opcionales. |
| Basura | `TrashStation` | |
| Caja registradora | `OrderRegister` | El `CustomerFlow` (lo busca solo). |
| Mostrador de entrega | `PickupCounter` | |

### 5.4 Clientes

Crea un objeto `Clientes` con `CustomerFlow` y `CustomerSpawner`, y estos hijos vacíos:
- **Spawn Point** (puerta de entrada) y **Exit Point** (por donde se van).
- **Queue Points**: la cola, empezando por el sitio justo delante de la caja.
- **Wait Points**: donde esperan su comida tras pedir.
- **Look At Point**: hacia dónde miran mientras esperan (el mostrador).

En la escena verás la cola en cian, la espera en amarillo, la entrada en verde y la salida en rojo.

Modelos de cliente: arrastra tus prefabs a `Customer Prefabs`. Si tienen `NavMeshAgent` y la escena tiene NavMesh, lo usan; si tienen `Animator`, reciben el parámetro float `Speed`. Marca **Auto Take Orders** si quieres que pidan solos (estilo quiosco).

### 5.5 Modelos de los ingredientes

Abre cada `Item_*.asset` en `Assets/BurgerKitchenData/Items` y arrastra tu modelo a **Visual Prefab**. El modelo debe tener **el pivote en la base**, sin colisionador ni Rigidbody (el sistema pone los suyos). Ajusta **Stack Height** a la altura real para que la hamburguesa se apile bien.

### 5.6 Sonido y efectos

Las estaciones tienen eventos en el inspector (`On Process Started`, `On Process Completed`, `On Overcooked`, `On Flip Needed`, `On Working Changed(bool)`, `On Item Taken`, `On Delivered`...). Engancha ahí tu `AudioSource.Play`, partículas de humo o vapor, animaciones de puertas, etc. Para la interfaz y la lógica global usa los eventos estáticos de `KitchenEvents`.

### Lista de comprobación (para montarlo en el PC)

- [ ] Copiar `Assets/BurgerKitchen` al proyecto y esperar a que compile sin errores.
- [ ] `Tools > Burger Kitchen > 1` para generar los datos.
- [ ] Abrir la escena del escenario y ejecutar `Tools > Burger Kitchen > 2` (o montar a mano según 5.1-5.4).
- [ ] Mover cada estación de `BurgerKitchen_Greybox` encima de su mueble del escenario, borrar el hijo "Modelo (cámbialo por el tuyo)" y ajustar su `BoxCollider` (o usar el collider del mueble).
- [ ] Recolocar los huecos (*Hueco 1, 2...*) sobre las superficies reales.
- [ ] Recolocar los puntos de `Clientes` siguiendo el recorrido del local y borrar el suelo provisional.
- [ ] Probar en Play un día completo: sencilla, con queso, patatas, batido, refresco, cucurucho y tarrina.

## 6. Referencia de componentes

| Componente | Para qué sirve |
|---|---|
| `ItemDefinition` (asset) | Un objeto: nombre, etiquetas, modelo, altura de apilado, si se ensucia al caer, si es un estado estropeado, y si es contenedor (qué etiquetas acepta, capacidad, apilado o cuadrícula). |
| `ProcessRecipe` (asset) | Entrada → salida en X segundos con un verbo ("Freír"). `isOvercook` marca el paso a quemado; `flipAt` obliga a darle la vuelta. |
| `ProductDefinition` (asset) | Un producto de la carta y cómo se reconoce: objeto exacto (patatas, batido) o contenedor con ingredientes (hamburguesa, tarrina). Reglas de orden: cualquiera, primero y último fijos (panes) o exacto. |
| `MenuDefinition` (asset) | La carta: productos, peso, día de desbloqueo y probabilidad de principal, acompañamiento, bebida y postre. |
| `ShiftSettings` (asset) | Duración del día, ritmo de clientes, paciencia, propinas y dinero inicial. |
| `Item` | Objeto físico que se coge. Se crea solo con `ItemFactory.Spawn`. |
| `ItemContainer` | Contenido de un contenedor (papel, bandeja, tarrina). Se añade solo. |
| `PlayerInputReader` | Entrada (Input System o clásico). |
| `FirstPersonController` | Movimiento y cámara en primera persona. |
| `PlayerInteractor` | Apuntar, ayuda, acciones y objeto en la mano. |
| `Station` (base) | Huecos y comportamiento de superficie (coger, dejar, combinar). |
| `CounterStation`, `ProcessingStation`, `BulkPortionStation`, `ItemSource`, `TrashStation` | Estaciones de cocina. |
| `MachineMaintenance` | Limpieza periódica y averías de una máquina. |
| `OrderRegister`, `PickupCounter` | Caja y mostrador de entrega. |
| `CustomerFlow`, `CustomerSpawner`, `Customer` | Recorrido, aparición y comportamiento de los clientes. |
| `KitchenGameManager` | Jornada, dinero, valoración y guardado. |
| `OrderManager` | Pedidos activos, entregas y cobro. |
| `KitchenHUD` | Interfaz provisional (IMGUI). |
| `Highlightable` | Resaltado al mirar. |

## 7. Cómo ampliarlo

**Un ingrediente nuevo (p. ej. pepinillos):** `Create > Burger Kitchen > Item Definition`, etiquetas `Topping | BurgerLayer`. Añade un `ItemSource` que lo dé. Si se prepara, crea una `ProcessRecipe` (pepino → pepinillos, "Cortar") y añádela a la tabla de cortar.

**Un plato nuevo:** `Create > Burger Kitchen > Product`, modo `Assembly`, base `Item_Wrap`, lista de ingredientes de abajo arriba, regla `FirstAndLastFixed`. Añádelo a `Menu_Principal` con su día de desbloqueo.

**Un sabor de batido nuevo:** un `ItemDefinition` (batido de plátano), una receta `ShakeCupEmpty → batido de plátano`, añadirla a la batidora y un producto `SingleItem` en la carta. La batidora mostrará la opción nueva con Q.

**Una máquina nueva (horno de tartas, tostadora de pan, cafetera):** un objeto con `ProcessingStation` y sus recetas. Elige *Automatic* si va sola o *HoldToWork* si el jugador la acciona; *Manual* si hay que elegir antes de empezar. Añade `MachineMaintenance` si quieres que se ensucie o se averíe.

**Envases nuevos (vaso de café, caja de nuggets):** un `ItemDefinition` contenedor o un `BulkPortionStation` con su receta de ración.

**Una categoría nueva:** añade un valor al `enum ItemTags` (siguiente potencia de 2).

**Una mecánica de estación totalmente nueva:** hereda de `Station` (si tiene huecos) o implementa `IInteractable` (`GetPrompt`, `Interact`, `InteractAlt`, `Cycle`). Implementa `IWorldStatusSource` para mostrar barras flotantes.

**Tu propia interfaz:** lee `OrderManager.Instance.ActiveOrders`, `KitchenGameManager.Instance` (día, dinero, tiempo, estrellas) y `PlayerInteractor.CurrentPrompt`, o suscríbete a `KitchenEvents`. Después quita `KitchenHUD`.

## 8. Arquitectura del código

```
Assets/BurgerKitchen/
├── Runtime/
│   ├── Data/          ItemDefinition, ProcessRecipe, ProductDefinition, MenuDefinition, ShiftSettings,
│   │                  ItemTags, ItemSnapshot, ProductMatcher (lógica pura de reconocimiento)
│   ├── Items/         Item, ItemContainer, ItemFactory, KitchenUtil
│   ├── Interaction/   IInteractable, IItemHolder, ItemTransfer (reglas de combinar), Highlightable
│   ├── Player/        PlayerInputReader, FirstPersonController, PlayerInteractor
│   ├── Stations/      Station, CounterStation, ProcessingStation, BulkPortionStation, ItemSource,
│   │                  TrashStation, MachineMaintenance
│   ├── Orders/        Order, OrderGenerator (lógica pura), OrderManager, OrderRegister, PickupCounter
│   ├── Customers/     Customer, CustomerFlow, CustomerSpawner
│   ├── Game/          KitchenGameManager, KitchenEvents, DayStats
│   └── UI/            KitchenHUD, WorldStatus (barras flotantes)
├── Editor/            SampleContentGenerator (datos), GreyboxKitchenBuilder (cocina de prueba)
└── Tests/EditMode/    ProductMatcherTests, OrderTests
```

Ideas clave:
- **Un objeto cambia de definición, no de GameObject.** La carne cruda se convierte en hecha con `Item.SetDefinition`; el modelo se cambia solo y el contenido se conserva.
- **Posesión única.** Cada objeto tiene un único dueño (`IItemHolder`: la mano, un hueco o un contenedor). Al moverlo, el dueño anterior se entera solo; así nunca hay dos sitios con el mismo objeto.
- **Combinar es genérico.** `ItemTransfer` mete un objeto en otro si el contenedor acepta sus etiquetas, en el sentido que sea posible. Por eso puedes montar en la encimera, recoger la carne de la plancha con el papel en la mano o meter el papel en la bandeja sin código específico.
- **Reconocer pedidos es lógica pura.** `ProductMatcher` y `OrderGenerator` trabajan con `ItemSnapshot` y `System.Random`, sin escena, y tienen pruebas.

## 9. Pruebas

`Window > General > Test Runner > EditMode > Run All`. Cubren el reconocimiento de hamburguesas (orden de panes, ingredientes de más o de menos, quemados y sucios), los productos sencillos, las entregas parciales y el generador de pedidos (desbloqueos, límites, nunca vacío).

## 10. Limitaciones y siguientes pasos

- La interfaz es provisional (IMGUI). El siguiente paso natural es una UI con uGUI o UI Toolkit usando `KitchenEvents`.
- Los clientes sin NavMesh andan en línea recta: coloca los puntos de la cola y la espera sin obstáculos en medio, o hornea un NavMesh y dales `NavMeshAgent`.
- No hay sonido ni animaciones: los eventos de las estaciones están listos para engancharlos.
- Ideas para crecer: mejoras compradas con el dinero (plancha más rápida con `speedMultiplier`, más huecos), coste de ingredientes (`ItemSource.costPerItem`), autoservicio o *drive-thru* (`autoTakeOrders` y otro `CustomerFlow`), limpieza de mesas y suelo, empleados con IA.
