# Sistema de construcción y colocación (Unity 3D, primera persona)

Asset independiente para construir y colocar objetos en una escena 3D:

- **Modo cuadrícula**: el objeto encaja en las casillas de una cuadrícula 3D (una o varias por escena), ocupando 1x1, 2x3 o las casillas que quieras. Cada casilla solo admite un objeto.
- **Modo libre**: el objeto se coloca donde apuntas, con el giro que quieras, siempre que **no se superponga** con nada.
- **Vista previa verde y roja**: un "fantasma" del objeto en verde si se puede construir y en rojo si no. En cuadrícula también se colorean las casillas que ocuparía.
- **Leyenda en pantalla** mientras construyes, **totalmente personalizable**: textos, colores, posición, letra, qué líneas se ven en cada momento, iconos, o tu propio diseño (también con TextMeshPro).
- Extras: girar, cambiar de objeto, modo demoler (con devolución de parte del coste), zonas permitidas o prohibidas, inclinación máxima del suelo, coste conectable a tu economía, reglas propias y eventos.

Todo el código está en el espacio de nombres `Mecanicas.Construccion` y no depende de ningún otro sistema del proyecto.

---

## Índice

1. [Instalación](#1-instalación)
2. [Prueba rápida con la escena de demostración](#2-prueba-rápida-con-la-escena-de-demostración)
3. [Cómo funciona](#3-cómo-funciona)
4. [Cómo implementarlo en tu proyecto paso a paso](#4-cómo-implementarlo-en-tu-proyecto-paso-a-paso)
5. [Referencia de componentes](#5-referencia-de-componentes)
6. [Personalizar la leyenda](#6-personalizar-la-leyenda)
7. [Cómo modificarlo y ampliarlo](#7-cómo-modificarlo-y-ampliarlo)
8. [Problemas frecuentes](#8-problemas-frecuentes)
9. [Archivos](#9-archivos)

---

## 1. Instalación

**Requisitos**

- Unity 6 (también debería funcionar en 2022.3 o posterior).
- Paquete **uGUI** (`com.unity.ugui`, viene instalado por defecto) para la leyenda.
- Cualquier pipeline: Built-in, URP o HDRP.
- Entrada: funciona con el **Input System** nuevo, con el **Input Manager** antiguo o con los dos ("Active Input Handling" en *Project Settings > Player*). No hace falta configurar nada.

**Pasos**

1. Copia la carpeta `Construccion` dentro de `Assets/` de tu proyecto. Ruta recomendada: `Assets/Mecanicas/Construccion`.
2. Espera a que Unity compile. Aparecen tres ensamblados: `Mecanicas.Construccion` (lo que usa el juego), `Mecanicas.Construccion.Demo` (solo la demo) y `Mecanicas.Construccion.Editor` (el generador de la demo).
3. Si tu código está en un ensamblado propio (`.asmdef`), añade `Mecanicas.Construccion` a sus referencias. Si tu código no usa asmdef, no tienes que hacer nada.

Cuando ya no necesites la demo, puedes borrar `Demo/` y `Editor/` sin que deje de funcionar nada.

---

## 2. Prueba rápida con la escena de demostración

1. Menú **Herramientas > Mecánicas > Construcción > Crear escena de demostración**.
2. Se crea `Demo/Generado/` con la escena `Construccion_Demo.unity`, los prefabs, los materiales, las fichas de los objetos y la configuración de la leyenda. La escena se abre sola.
3. Pulsa **Play**.

**Controles de la demo**

| Tecla | Acción |
|---|---|
| WASD, ratón, Mayús, Espacio | Moverse, mirar, correr, saltar |
| **B** | Entrar o salir del modo construcción |
| **Clic izquierdo** | Colocar (o demoler en modo demoler) |
| **Clic derecho** o **Esc** | Salir del modo construcción |
| **Q / E** o rueda del ratón | Girar (90° en cuadrícula; en libre, el paso de cada objeto) |
| **G** | Cambiar entre cuadrícula y libre |
| **1-4** o **Tab** | Elegir objeto |
| **X** | Modo demoler |
| **H** | Ocultar o mostrar la leyenda |
| **F1** | Soltar o capturar el ratón |

**Qué hay en la escena para probar cada cosa**

- Una cuadrícula de 9x5 casillas de 1,5 m con **dos casillas bloqueadas** (siempre rojas) y **una roca encima** (rojo por colisión física).
- **Caja** (1x1, cuadrícula y libre, se puede apilar en libre), **Torre** (solo cuadrícula, máximo 4 por una regla de ejemplo), **Casita** (2x3: gírala y verás cómo cambia la huella) y **Farola** (solo libre, gira de 45 en 45°).
- Rocas en la zona libre, una **rampa demasiado inclinada** y una **zona prohibida** (cuadro rojo del suelo).
- Un monedero de ejemplo: empiezas con 300 monedas, cada objeto cuesta algo y ganas 5 por segundo. Al demoler recuperas la mitad.

---

## 3. Cómo funciona

### Piezas

| Pieza | Qué es |
|---|---|
| `BuildableDefinition` (asset) | La **ficha** de un objeto: prefab, casillas que ocupa, modos permitidos, coste, paso de giro, inclinación máxima. |
| `BuildSystem` (componente) | El **cerebro**: lee las teclas, apunta, mueve la vista previa, decide verde o rojo, coloca y demuele. Va en el jugador. |
| `BuildGrid` (componente) | Una **cuadrícula**. Guarda qué casillas están libres, ocupadas o bloqueadas. |
| `BuildGridVisual` (componente) | Dibuja las casillas de la cuadrícula en el juego y la huella verde o roja. |
| `BuildLegend` + `BuildLegendConfig` | La **leyenda** en pantalla y su configuración (asset). |
| `PlacedBuildable` (componente) | Se añade solo a lo que construyes. Recuerda su ficha y sus casillas, y las libera al destruirse. |
| `BuildZone` (componente) | Caja que **permite** o **prohíbe** construir. |
| `BuildCostProvider` (clase base) | Puente con tu **dinero o recursos**. |
| `BuildRule` (clase base) | **Reglas propias** de colocación. |

### Lo que pasa en cada fotograma mientras construyes

1. El `BuildSystem` lanza un rayo desde el **centro de la pantalla** (primera persona) o desde el **ratón**.
2. **Cuadrícula**: corta el rayo con el plano de cada `BuildGrid` activo y se queda con la cuadrícula que tiene el punto encima. Calcula la huella girada (una casa de 2x3 girada 90° ocupa 3x2) y la centra en el punto apuntado. La posición final es el centro de esas casillas.
   **Libre**: lanza un rayo físico contra las capas de suelo (`groundMask`). El objeto se apoya en el punto tocado, con el giro elegido y, si la ficha lo pide, alineado con la inclinación del suelo.
3. Comprueba las reglas **en este orden** y se queda con el primer fallo (es el `{motivo}` de la leyenda):
   1. El objeto admite el modo actual.
   2. Cuadrícula: la huella cabe dentro de la cuadrícula.
   3. Cuadrícula: todas las casillas están libres (ni ocupadas ni bloqueadas).
   4. Libre: el suelo no está más inclinado que `maxSlope`.
   5. No hay **superposición física**: una caja con el tamaño del objeto (`Physics.OverlapBox`) no toca ningún colisionador de `obstacleMask`, salvo el suelo apuntado y los terrenos. En cuadrícula se hace solo si la cuadrícula tiene activado "Comprobar colisiones físicas" (así detecta rocas, árboles o lo que hayas puesto en modo libre encima).
   6. Zonas: no está dentro de una zona prohibida y, si se exige, sí dentro de una permitida.
   7. Reglas propias (`BuildRule`).
   8. Hay dinero suficiente (`BuildCostProvider`).
4. Pinta la vista previa en verde o rojo y, en cuadrícula, colorea las casillas.
5. Si pulsas colocar y todo es válido: instancia el prefab, le añade `PlacedBuildable`, ocupa las casillas, cobra y lanza los eventos.

### La vista previa

Es una copia del prefab a la que se le quitan **todos los componentes que no son visuales** (scripts, colisionadores, físicas, luces, sonidos...) antes de activarla, así que tus scripts no se ejecutan en el fantasma. Sus materiales se sustituyen por uno transparente verde o rojo. La caja para comprobar choques se calcula con los colisionadores del prefab (o con las mallas si no tiene) y se puede fijar a mano en la ficha.

### Coordenadas de la cuadrícula

La cuadrícula empieza en la posición de su objeto (esquina) y crece hacia **+X** (ancho) y **+Z** (fondo) locales. La casilla `(0,0)` es la de la esquina. Puedes moverla, girarla en Y y tener varias. En la vista de escena se dibujan sus líneas y, en rojo, las casillas bloqueadas.

---

## 4. Cómo implementarlo en tu proyecto paso a paso

### 4.1. Prepara los prefabs

- El **pivote en la base** del objeto (los pies tocando el suelo). Si no, corrige la altura con `positionOffset` en la ficha.
- Con **colisionadores**. Son los que se usan para detectar superposiciones y para poder demolerlo.
- En cuadrícula, que el tamaño encaje con las casillas (por ejemplo, 1,4 m para casillas de 1,5 m, para que dos vecinos no se toquen).

### 4.2. Crea una ficha por objeto

*Assets > Create > Mecánicas > Construcción > Objeto construible*. Rellena al menos **Prefab**, **Grid Size** (casillas) y **Allowed Modes**.

### 4.3. Pon el `BuildSystem`

1. Añade **Sistema de construcción** a tu jugador (*Add Component > Mecánicas > Construcción*).
2. Arrastra las fichas a **Catalog**.
3. Asigna **Aim Camera** (si lo dejas vacío usa `Camera.main`).
4. **Aim Mode**: `ScreenCenter` para primera persona; `MousePosition` para cámara cenital o cursor libre.
5. Ajusta **Ground Mask** (lo que cuenta como suelo en libre) y **Obstacle Mask** (lo que impide construir). Lo más limpio es tener una capa `Suelo` y quitarla de `Obstacle Mask`.

### 4.4. Modo cuadrícula

1. Crea un objeto vacío donde quieras la esquina de la cuadrícula.
2. Añade **Cuadrícula de construcción** (`BuildGrid`) y ajusta ancho, fondo y tamaño de casilla.
3. Añade **Visual de la cuadrícula** (`BuildGridVisual`) para ver las casillas y la huella verde o roja en el juego.
4. Opcional: marca casillas bloqueadas (caminos, agua...) en **Blocked Cells**.

### 4.5. Modo libre

No necesita nada más: basta con un suelo con colisionador. Para limitar dónde se puede construir, usa zonas (abajo).

### 4.6. Zonas (opcional)

Añade **Zona de construcción** a un objeto vacío, ajusta la caja y elige `Allowed` o `Forbidden`. Si activas **Require Allowed Zone** en el `BuildSystem`, solo se podrá construir dentro de zonas `Allowed`. Las `Forbidden` bloquean siempre.

### 4.7. Leyenda

Añade **Leyenda de construcción** a cualquier objeto (crea su propio Canvas). Para personalizarla crea un asset en *Create > Mecánicas > Construcción > Configuración de leyenda* y asígnalo (ver sección 6).

### 4.8. Dinero (opcional)

Crea un script que herede de `BuildCostProvider` (sección 7.2), ponlo en la escena y asígnalo a **Cost Provider**. Sin él, construir es gratis.

### 4.9. Integrarlo con tu controlador

- El sistema **no bloquea el movimiento ni la cámara**: en primera persona apuntas con la vista. Si quieres bloquear algo mientras construyes, usa los eventos `onBuildStarted` y `onBuildEnded`.
- Si ya usas B, clic, Q/E, etc. para otra cosa, cambia las teclas en **Input** (sección 5.1) o desactiva **Read Input** y maneja el sistema desde tu código (sección 7.4).

---

## 5. Referencia de componentes

### 5.1. `BuildSystem`

| Campo | Para qué sirve |
|---|---|
| Catalog / Start Index | Objetos disponibles y el que sale al principio. |
| Aim Camera / Aim Mode | Cámara y forma de apuntar. |
| Max Distance | Distancia máxima para construir y demoler. |
| Ground Mask | Capas de suelo en modo libre. Si incluye los objetos construidos, se puede apilar encima. |
| Obstacle Mask | Capas que impiden construir. |
| Overlap Tolerance | Margen para que dos objetos que solo se tocan no cuenten como superpuestos. |
| Free Snap Step | En libre, redondea la posición a pasos (0 = sin redondeo). |
| Start Mode | Cuadrícula o libre al empezar. |
| Keep Building After Place | Seguir construyendo tras colocar. |
| Require Allowed Zone | Solo dentro de zonas permitidas. |
| Allow Demolish | Activa el modo demoler. |
| Placed Parent | Padre de los objetos construidos. |
| Cost Provider | Tu economía. |
| Valid Color / Invalid Color | **Verde y rojo** de la vista previa (también se usan en la leyenda). |
| Valid Material / Invalid Material | Materiales propios para la vista previa (por ejemplo, un shader holográfico). Si están vacíos se crean con los colores. |
| Show Free Footprint + colores | Marca en el suelo bajo el objeto en modo libre. |
| Preview Layer | Capa del fantasma (2 = Ignore Raycast). |
| Read Input | Desactívalo para manejarlo solo por código. |
| Input | Teclas de cada acción, rueda para girar, teclas 1-9. |
| Eventos | `onBuildStarted`, `onBuildEnded`, `onPlaced`, `onDemolished`, `onSelectionChanged`, `onModeChanged`, `onPlaceFailed`. |

### 5.2. `BuildableDefinition`

| Campo | Para qué sirve |
|---|---|
| Display Name / Description / Icon | Lo que muestra la leyenda (`{objeto}`, `{descripcion}`). |
| Prefab | Lo que se construye. |
| Allowed Modes | `Grid`, `Free` o `Both`. |
| Grid Size | Casillas que ocupa sin girar (X = ancho, Y = fondo). |
| Position Offset | Ajuste de posición local. |
| Free Rotation Step | Grados por pulsación en libre. |
| Align To Surface Normal | En libre, se inclina con el suelo. |
| Max Slope | Inclinación máxima del suelo en libre. |
| Override Bounds / Custom Bounds | Caja de colisión a mano, si la automática no te sirve. |
| Cost / Refund On Demolish | Coste y proporción que se devuelve al demoler. |

### 5.3. `BuildGrid`

| Campo | Para qué sirve |
|---|---|
| Width / Depth / Cell Size | Tamaño de la cuadrícula. |
| Blocked Cells | Casillas donde nunca se construye. |
| Check Physics Overlap | Comprobar también colisiones físicas (rocas, objetos sueltos). |
| Draw Gizmos | Dibujar en la vista de escena. |

Métodos útiles: `WorldToCell`, `CellCenterWorld`, `GetCellState`, `GetOccupant`, `SetCellBlocked` (por ejemplo, para desbloquear casillas al comprar terreno) y el evento `OccupancyChanged`.

### 5.4. `BuildGridVisual`

Cuándo se ve (solo en cuadrícula, siempre que se construye, siempre o nunca), colores de casilla libre, ocupada, bloqueada, válida y no válida, hueco entre casillas y un material opcional (debe aceptar colores de vértice).

### 5.5. `BuildZone`

Tipo (`Allowed` o `Forbidden`), centro y tamaño de la caja. Se dibuja en verde o rojo en la vista de escena.

---

## 6. Personalizar la leyenda

La leyenda solo se ve **mientras se está construyendo**. Todo se cambia en el asset `BuildLegendConfig`, sin tocar código.

### 6.1. Aspecto

Esquina de la pantalla, margen, ancho, fuente, tamaños de letra, color del título, del texto y del fondo, imagen de fondo (admite 9-slice), relleno, espacio entre líneas, tamaño del cuadradito de color y orden de dibujo.

### 6.2. Líneas

Cada línea tiene:

- **Text**: el texto. Admite *rich text* (`<b>`, `<i>`, `<color=#ff0>`) y etiquetas.
- **Show When**: cuándo se ve: `Always`, `Placing` (colocando), `GridMode`, `FreeMode`, `Demolishing`, `WhenValid` (en verde), `WhenInvalid` (en rojo).
- **Swatch**: cuadradito de color a la izquierda: ninguno, un color fijo, el estado actual (verde o rojo), siempre el verde o siempre el rojo.
- **Icon**: icono opcional.

Puedes añadir, quitar y reordenar líneas. Si en el componente del asset eliges **Reset**, vuelve a las líneas de ejemplo.

### 6.3. Etiquetas

| Etiqueta | Se sustituye por |
|---|---|
| `{objeto}` | Nombre del objeto elegido |
| `{descripcion}` | Su descripción |
| `{coste}` | Su coste con el formato `Cost Format` (o `Free Cost Text`) |
| `{modo}` | Cuadrícula, Libre o Demoler (textos configurables) |
| `{estado}` | "Se puede construir" o "No se puede construir" (configurables) |
| `{motivo}` | Por qué no se puede (un texto por motivo, todos configurables) |
| `{rotacion}` | Giro actual en grados |
| `{catalogo}` | Lista de objetos con el elegido resaltado |
| `{objetivo}` | Lo que vas a demoler |
| `{tecla:accion}` | La tecla de una acción: `construir`, `colocar`, `cancelar`, `rotarIzq`, `rotarDer`, `cambiarModo`, `siguiente`, `anterior`, `demoler`, `leyenda` |

Las teclas salen de la configuración del `BuildSystem`, así que si cambias una tecla la leyenda se actualiza sola.

**Etiquetas propias**: desde cualquier script,

```csharp
legend.ResolveToken += token => token == "madera" ? inventario.Madera.ToString() : null;
```

y ya puedes escribir `{madera}` en cualquier línea. La demo lo hace con `{dinero}` en `DemoWallet`.

### 6.4. Diseño totalmente propio (o TextMeshPro)

1. Diseña tu panel en tu Canvas con un `VerticalLayoutGroup` y asígnalo en **Custom Panel**.
2. Crea un prefab de línea con el componente **Línea de leyenda** (`BuildLegendEntryView`): asígnale una `Image` para el color, otra para el icono y un `Text`. Con **TextMeshPro**, deja *Text* vacío y en el evento **On Text** conecta `TextMeshProUGUI > text`.
3. Asígnalo en **Entry Template**. La leyenda creará una copia por cada línea de la configuración.

**Hidden Hint** es el aviso pequeño que queda cuando el jugador oculta la leyenda con H (vacío para no mostrar nada).

---

## 7. Cómo modificarlo y ampliarlo

### 7.1. Reglas propias de colocación

Hereda de `BuildRule` y ponlo en el mismo objeto que el `BuildSystem` (o en un hijo). Ejemplo: solo cerca de un camino.

```csharp
using Mecanicas.Construccion;
using UnityEngine;

public class SoloCercaDelCamino : BuildRule
{
    [SerializeField] private Transform camino;
    [SerializeField] private float distanciaMaxima = 5f;

    public override bool IsAllowed(in PlacementCandidate c)
    {
        return Vector3.Distance(c.Position, camino.position) <= distanciaMaxima;
    }
}
```

El texto que sale en la leyenda cuando falla es **Fail Message**, en el inspector. `MaxCountBuildRule` es otro ejemplo incluido (límite de objetos de un tipo). `PlacementCandidate` te da la ficha, el modo, la posición, el giro y, según el modo, la cuadrícula y las casillas o el colisionador del suelo.

### 7.2. Conectar con tu economía o inventario

```csharp
using Mecanicas.Construccion;

public class CosteConMadera : BuildCostProvider
{
    public int madera = 50;
    public override bool CanAfford(BuildableDefinition d, int coste) => madera >= coste;
    public override void Spend(BuildableDefinition d, int coste) => madera -= coste;
    public override void Refund(BuildableDefinition d, int cantidad) => madera += cantidad;
}
```

Si cada objeto necesita varios recursos, crea tu propia ficha heredando de `BuildableDefinition` (por ejemplo, `FichaConRecursos` con una lista de recursos) y compruébalos en `CanAfford` convirtiendo `d` a tu tipo.

### 7.3. Eventos

En el inspector (UnityEvents) o por código:

```csharp
build.Placed += objeto => Debug.Log("Construido: " + objeto.Definition.displayName);
build.Demolished += objeto => audio.PlayOneShot(sonidoDerribo);
build.StateChanged += estado => miControlador.PuedeDisparar = estado == BuildState.Inactive;
```

Útil para sonidos, partículas, logros, misiones o guardar la partida.

### 7.4. Manejarlo desde código (menús, botones, móvil, mando)

Desactiva **Read Input** y llama tú a la API:

| Método | Qué hace |
|---|---|
| `Begin()` / `Begin(ficha)` / `End()` / `Toggle()` | Entrar o salir. `Begin(ficha)` elige ese objeto (y lo añade al catálogo si no está): perfecto para un botón de "comprar y colocar". |
| `Select(i)` / `SelectNext(±1)` / `SetCatalog(lista)` | Elegir objeto o cambiar el catálogo (desbloqueos). |
| `SetMode(modo)` / `ToggleMode()` | Cuadrícula o libre. |
| `Rotate(±1)` | Girar. |
| `TryPlace()` | Colocar si es válido (devuelve el objeto o `null`). |
| `ToggleDemolish()` / `Demolish(objeto)` | Demoler. |
| `PlaceDirect(ficha, cuadrícula, casillas, giro)` | Colocar sin vista previa ni coste (para **cargar partidas** o generar niveles). |

Y para leer el estado: `State`, `IsActive`, `Mode`, `Selected`, `LastResult`, `CanPlaceNow`, `RotationDegrees`, `DemolishTarget`.

**Guardar y cargar**: recorre `PlacedBuildable.All` y guarda de cada uno su ficha, `Mode`, posición y giro; si `IsOnGrid`, guarda también `Area` (casillas). Para cargar usa `PlaceDirect` en los de cuadrícula e `Instantiate` + `AddComponent<PlacedBuildable>().Initialize(ficha, PlacementMode.Free)` en los libres.

### 7.5. Cambiar las teclas o el sistema de entrada

- Teclas: en el `BuildSystem`, apartado **Input**.
- Otra forma de entrada (Input Actions, mando, táctil): desactiva **Read Input** y llama a la API del punto 7.4 desde tus acciones.
- Toda la lectura de teclado y ratón está en `Runtime/Input/InputCompat.cs`, por si quieres cambiarla en un solo sitio.

### 7.6. Cambiar el aspecto de la vista previa

- Colores: **Valid Color / Invalid Color** en el `BuildSystem`.
- Materiales propios: **Valid Material / Invalid Material** (por ejemplo, un shader con borde brillante o efecto holograma).
- Casillas: colores y hueco en `BuildGridVisual`.
- Por código: `BuildPreview.cs` (fantasma) y `BuildVisualUtility.cs` (materiales y cálculo de la caja).

### 7.7. Dónde tocar para cambios grandes

| Quieres... | Archivo |
|---|---|
| Cambiar el orden o añadir comprobaciones fijas | `BuildSystem.cs`, métodos `EvaluateGrid`, `EvaluateFree` y `CommonChecks` |
| Cambiar cómo se centra la huella o se gira | `Runtime/Core/GridOccupancy.cs` (`Footprint`) |
| Casillas de varios pisos o alturas | `BuildGrid.cs` (una cuadrícula por piso, a distinta altura, ya funciona sin cambios) |
| Añadir motivos de rechazo | `BuildTypes.cs` (`PlacementResult`) y `BuildLegendConfig.GetReasonText` |
| Añadir etiquetas fijas a la leyenda | `BuildLegend.Resolve` |

Las pruebas de la lógica de casillas están en `Tests/EditMode` (*Window > General > Test Runner*). Si cambias `GridOccupancy.cs`, pásalas.

---

## 8. Problemas frecuentes

| Síntoma | Solución |
|---|---|
| Todo sale en rojo con "Choca con otro objeto" | El suelo está en `Obstacle Mask` y el objeto se hunde en él: sube el pivote, aumenta **Overlap Tolerance** o quita la capa del suelo de `Obstacle Mask`. |
| En cuadrícula, dos objetos vecinos se ponen rojos | El objeto es más grande que sus casillas: hazlo un poco más pequeño o sube **Overlap Tolerance**. |
| No aparece la vista previa | En cuadrícula hay que mirar a una `BuildGrid`; en libre, el suelo necesita colisionador y estar en `Ground Mask`. Comprueba también **Max Distance**. |
| Me salgo del modo construcción al hacer clic derecho | Es la tecla de cancelar por defecto; cámbiala en **Input**. |
| El objeto aparece hundido o flotando | El pivote no está en la base: ajusta **Position Offset** en la ficha. |
| La caja de colisión no se ajusta | Activa **Override Bounds** en la ficha y escribe la caja a mano. |
| Las casillas parpadean con el suelo | Sube **Height Offset** en `BuildGridVisual`. |
| La leyenda no se ve | Solo aparece en modo construcción. Comprueba que hay un `BuildSystem` activo y que no la has ocultado con H. |

---

## 9. Archivos

```
Construccion/
├── README.md
├── Runtime/                         (lo que necesita tu juego)
│   ├── BuildSystem.cs               controlador principal
│   ├── BuildableDefinition.cs       ficha de objeto (ScriptableObject)
│   ├── BuildGrid.cs                 cuadrícula
│   ├── BuildGridVisual.cs           casillas en el juego
│   ├── BuildPreview.cs              fantasma verde/rojo
│   ├── BuildVisualUtility.cs        materiales y cálculo de cajas
│   ├── BuildZone.cs                 zonas permitidas/prohibidas
│   ├── BuildCostProvider.cs         base para el coste
│   ├── BuildRule.cs                 base para reglas propias
│   ├── BuildTypes.cs                enumeraciones
│   ├── PlacedBuildable.cs           marca de lo construido
│   ├── Core/GridOccupancy.cs        lógica de casillas (C# puro, con pruebas)
│   ├── Input/                       teclas y compatibilidad de entrada
│   ├── Rules/MaxCountBuildRule.cs   regla de ejemplo
│   └── UI/                          leyenda y su configuración
├── Demo/Scripts/                    controlador en primera persona, monedero y ayudas de la demo
├── Editor/ConstruccionDemoCreator.cs  genera la escena de demostración
└── Tests/EditMode/                  pruebas de la cuadrícula
```

---

## Estado

El código compila contra las bibliotecas de Unity (con y sin el Input System) y las pruebas de la cuadrícula pasan, pero todavía **no se ha abierto en el editor de Unity**. Lo primero al copiarlo a un proyecto: dejar que compile, crear la escena de demostración y probarla.
