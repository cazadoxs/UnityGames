# Lluvia de ideas: juego incremental 3D para PC

Hay 160 ideas originales, evaluadas por 4 agentes, cada uno con un criterio distinto (escala 1-10):

| Evaluador | Criterio | Peso |
|---|---|---|
| **M**ercado | Originalidad real y hueco en Steam (verificado con búsquedas web) | 30 % |
| **D**iseño | Profundidad del bucle incremental, prestigio temático, horas de juego | 30 % |
| **V**iral | Gancho en 3 segundos, clips, streamers, frase de venta | 25 % |
| **T**écnica | Viabilidad para un indie de 1-3 personas en Unity (arte, offline, rendimiento) | 15 % |

## Top 10 final

| # | Idea | M | D | V | T | Total | Frase de venta |
|---|---|---|---|---|---|---|---|
| **1** | **Origami Infinito** | 9 | 8 | 9 | 6 | **8,25** | *"Dobla un papel 42 veces y llegarás a la Luna. 103, y envolverás el universo."* |
| 2 | Masa Madre | 8 | 8 | 8 | 7 | 7,85 | *"Alimentaste tu masa madre. Ahora ella alimenta su imperio planetario."* |
| 3 | Velocidad Relativista | 8 | 8 | 7 | 7 | 7,60 | *"Acelera hasta que el universo se doble a tu alrededor."* |
| 4 | Ruinas Futuras | 8 | 9 | 6 | 6 | 7,50 | *"Tu civilización caerá. La siguiente vivirá de tus ruinas."* |
| 5 | Luciérnagas | 8 | 7 | 6 | 8 | 7,20 | *"Mil luces desordenadas. Haz que el bosque entero lata a la vez."* |
| 6 | Moho en la Nevera | 7 | 7 | 7 | 8 | 7,15 | *"Una civilización vive en tu nevera. Y alguien va a abrir la puerta."* |
| 7 | Caja de Música Gigante | 8 | 6 | 7 | 8 | 7,15 | *"Tu economía suena. Cada mejora es una nota nueva."* |
| 8 | Bufanda de la Abuela | 7 | 6 | 8 | 8 | 7,10 | *"La abuela no ha dejado de tejer. El planeta ya no tiene frío."* |
| 9 | Barco en Botella | 7 | 7 | 7 | 7 | 7,00 | *"Construye un barco en una botella. Luego mete la botella en otra."* |
| 10 | Ascensor Liminal | 7 | 6 | 8 | 7 | 6,95 | *"Pulsa bajar. No hay último piso."* |

**Criterios de curación sobre el ranking bruto:**
- **Relojero Cósmico** (7,60) queda fuera. Era la mejor en diseño y técnica, pero ya existen juegos muy cercanos (*Idling Gears*, *Googol-gear machine*). Sus engranajes como multiplicadores literales se pueden reciclar como sistema dentro de otra idea.
- **Maqueta de Tren** (7,00) queda fuera a favor de **Ascensor Liminal**, que tiene más potencial con streamers. Además, el nicho de trenes de juguete ya tiene sandboxes (*Attic Transit Authority*, *Tracks*).
- Se descartan todas las de "compañero de escritorio que lee tus teclas". Ese nicho se ha saturado en 2025-2026 (*Keyboard Hero*, *Deskpot*, *Desktop Garden*…) y además da problemas de privacidad y antivirus.
- Ideas con mucho gancho viral pero ya hechas: Gato y Mesa (*Catlateral Damage*), Agujero de Escritorio (*Donut County*), Pelusa (*Katamari*), Canicas (*IncrediMarble*).

## Resumen de las 10

1. **Origami Infinito.** Ver la ficha más abajo.
2. **Masa Madre.** La levadura crece de forma exponencial de verdad. Conquista la panadería, luego el barrio, la ciudad y el planeta. Tiene escalada absurda y humor, como *Universal Paperclips* pero en 3D y cozy. Prestigio: "secar la masa", y la cepa conserva mutaciones.
3. **Velocidad Relativista.** Un vehículo que nunca deja de acelerar. Cerca de *c* el mundo se deforma (Doppler, aberración, contracción) y el factor gamma justifica la escala. La dilatación temporal es una mecánica: el progreso offline es relativo.
4. **Ruinas Futuras.** Construyes una civilización sobre un mismo sitio. El prestigio la entierra y la siguiente excava tus artefactos como bonus. Los estratos se ven en 3D. Tiene el mejor prestigio temático de las 160 ideas.
5. **Luciérnagas.** Sincronizar el parpadeo (modelo de Kuramoto) es el multiplicador. Es matemática barata con un resultado visual hipnótico: miles de luces instanciadas.
6. **Moho en la Nevera.** Cada estante es un continente. Abrir la puerta es un cataclismo, un evento periódico que puedes prever y aprovechar.
7. **Caja de Música Gigante.** Cada púa del cilindro es un generador, así que la melodía que suena ES el estado de tu economía. Gancho para el público ASMR y cozy.
8. **Bufanda de la Abuela.** Una bufanda infinita que acaba envolviendo el planeta. Mezcla ternura y escala absurda, y la cápsula se reconoce al instante.
9. **Barco en Botella.** La escala es recursiva: la botella acaba dentro de otra botella, y cada capa de prestigio es una botella más grande.
10. **Ascensor Liminal.** Un ascensor baja a pisos infinitos generados (backrooms). Es incremental de exploración con tensión, ideal para Twitch.

## Top 1: Origami Infinito

**Gancho.** Un folio de 0,1 mm. Cada pliegue duplica su grosor (2^n literal):

| Pliegues | Grosor |
|---|---|
| 7 | 1,3 cm |
| 17 | 13 m (una casa) |
| 27 | 13 km (más que el Everest) |
| 42 | 440.000 km (más allá de la Luna) |
| 51 | El Sol |
| 103 | El tamaño del universo observable |

Es un dato real que la gente comparte. Fue la única idea que sacó 8 o más en mercado, diseño y viralidad a la vez.

**Por qué es la mejor apuesta:**
- **Nadie lo ha hecho en Steam.** Solo hay puzles de origami y un idle pequeño en itch.io (*Fold Quest*).
- **La matemática ES la temática.** El crecimiento exponencial no es abstracto: lo ves, porque la columna de papel atraviesa la habitación, las nubes y la órbita.
- **El 3D es imprescindible.** La cámara hace un zoom out continuo: de la mesa al edificio, a la ciudad y al espacio. Es un "Powers of Ten" jugable, y cada hito se convierte en un clip.

**Bucle base:**
1. Pliegas, lo que cuesta "fuerza de pliegue". Al principio es un clic y después se automatiza: manos, prensas, máquinas, gravedad, agujeros negros.
2. Los **patrones de pliegue** (grulla, rana, estrella…) son generadores. Cada figura plegada produce recursos, y su forma 3D se coloca en el mundo como un edificio.
3. **Tipos de papel** como mejoras: seda, cartón, metal, grafeno, espacio-tiempo. Cambian el coste y el grosor base.

**Capas de prestigio (temáticas):**
1. **Desplegar.** El papel vuelve a ser un folio, pero las marcas de los pliegues quedan y abaratan los siguientes (memoria del papel).
2. **Grulla de mil pliegues.** La leyenda de las 1000 grullas: cada 1000 grullas se concede un "deseo", una mejora permanente que tú eliges.
3. **Plegar el espacio.** Al llegar al tamaño del universo, pliegas el propio espacio-tiempo y el universo entero pasa a ser la hoja del siguiente ciclo (escala recursiva).

**Riesgos (técnica 6/10) y soluciones:**
- **Precisión numérica en escalas extremas.** Hay que usar una cámara con origen flotante y escalas logarítmicas por tramos.
- **Plegado visual convincente.** No hace falta una simulación de tela: basta con animaciones procedurales por bisagras y un shader de papel. La columna de pliegues se representa por instancias apiladas.
- **Profundidad a largo plazo.** La sostienen los patrones (generadores) y los deseos (meta-progresión), no solo el número de pliegues.

**MVP en 8-10 semanas:** habitación, pliegue manual y automático, 3 patrones y el zoom out continuo hasta la Luna (42 pliegues). Basta para una demo de Next Fest y un tráiler de 30 segundos.

## Ranking completo de las 160 ideas

El texto completo de cada idea está en [Banco_160_ideas.md](Banco_160_ideas.md).

| Pos | # | Idea | M | D | V | T | Total |
|---|---|---|---|---|---|---|---|
| 1 | 53 | Origami Infinito | 9 | 8 | 9 | 6 | 8,25 |
| 2 | 45 | Masa Madre | 8 | 8 | 8 | 7 | 7,85 |
| 3 | 81 | Velocidad Relativista | 8 | 8 | 7 | 7 | 7,60 |
| 4 | 17 | Relojero Cósmico | 6 | 9 | 7 | 9 | 7,60 |
| 5 | 61 | Ruinas Futuras | 8 | 9 | 6 | 6 | 7,50 |
| 6 | 105 | Luciérnagas | 8 | 7 | 6 | 8 | 7,20 |
| 7 | 50 | Moho en la Nevera | 7 | 7 | 7 | 8 | 7,15 |
| 8 | 31 | Caja de Música Gigante | 8 | 6 | 7 | 8 | 7,15 |
| 9 | 155 | Bufanda de la Abuela | 7 | 6 | 8 | 8 | 7,10 |
| 10 | 149 | Maqueta de Tren | 7 | 7 | 7 | 7 | 7,00 |
| 11 | 147 | Barco en Botella | 7 | 7 | 7 | 7 | 7,00 |
| 12 | 77 | Ascensor Liminal | 7 | 6 | 8 | 7 | 6,95 |
| 13 | 18 | Prisma | 6 | 8 | 6 | 8 | 6,90 |
| 14 | 103 | Murmuración | 7 | 7 | 7 | 5 | 6,70 |
| 15 | 124 | Volcán Isla | 6 | 8 | 6 | 6 | 6,60 |
| 16 | 97 | Geoda | 7 | 6 | 6 | 8 | 6,60 |
| 17 | 23 | Dominó Infinito | 5 | 7 | 9 | 5 | 6,60 |
| 18 | 157 | Montaña Rusa Incremental | 6 | 7 | 7 | 6 | 6,55 |
| 19 | 114 | Castillo de Arena vs. Marea | 7 | 7 | 6 | 5 | 6,45 |
| 20 | 52 | Escaleras de Escher | 7 | 7 | 6 | 5 | 6,45 |
| 21 | 33 | Críptido | 7 | 6 | 7 | 5 | 6,40 |
| 22 | 26 | Ascensor Espacial | 6 | 5 | 7 | 9 | 6,40 |
| 23 | 2 | Micelio | 5 | 8 | 5 | 8 | 6,35 |
| 24 | 38 | Biblioteca de Babel | 6 | 6 | 6 | 8 | 6,30 |
| 25 | 7 | Bonsái Eterno | 6 | 6 | 6 | 8 | 6,30 |
| 26 | 1 | Arrecife Vivo | 6 | 7 | 6 | 6 | 6,30 |
| 27 | 88 | Mundo-Tortuga | 6 | 6 | 7 | 6 | 6,25 |
| 28 | 9 | Isla-Ballena | 6 | 6 | 7 | 6 | 6,25 |
| 29 | 75 | Pozo sin Fondo | 7 | 5 | 7 | 6 | 6,25 |
| 30 | 55 | Matrioska Infinita | 5 | 6 | 7 | 8 | 6,25 |
| 31 | 113 | Castillo de Naipes | 6 | 7 | 7 | 4 | 6,25 |
| 32 | 141 | Gato y Mesa | 4 | 6 | 10 | 4 | 6,10 |
| 33 | 14 | Gravedad de Bolsillo | 6 | 7 | 7 | 3 | 6,10 |
| 34 | 89 | Doble Eje | 5 | 8 | 5 | 6 | 6,05 |
| 35 | 22 | Circuito de Canicas | 5 | 6 | 8 | 5 | 6,05 |
| 36 | 12 | Raíces | 6 | 6 | 5 | 8 | 6,05 |
| 37 | 21 | Rube Goldberg Infinito | 6 | 6 | 8 | 3 | 6,05 |
| 38 | 87 | Árbol de Ciudades | 6 | 6 | 6 | 6 | 6,00 |
| 39 | 16 | Agujero de Escritorio | 3 | 7 | 9 | 5 | 6,00 |
| 40 | 13 | Acreción | 3 | 7 | 7 | 8 | 5,95 |
| 41 | 80 | Pez Abisal | 5 | 6 | 7 | 6 | 5,95 |
| 42 | 135 | Sísifo | 4 | 6 | 7 | 8 | 5,95 |
| 43 | 160 | Cometa | 6 | 6 | 5 | 7 | 5,90 |
| 44 | 93 | Vidrieras | 6 | 6 | 5 | 7 | 5,90 |
| 45 | 66 | Diorama de Escritorio | 6 | 5 | 8 | 4 | 5,90 |
| 46 | 46 | Pelusa | 4 | 6 | 8 | 6 | 5,90 |
| 47 | 32 | Rumor | 5 | 7 | 6 | 5 | 5,85 |
| 48 | 123 | Tormenta | 5 | 6 | 7 | 5 | 5,80 |
| 49 | 100 | Copo de Nieve | 6 | 6 | 4 | 8 | 5,80 |
| 50 | 60 | Estratos | 6 | 6 | 4 | 8 | 5,80 |
| 51 | 30 | Órgano de Tubos | 6 | 5 | 5 | 8 | 5,75 |
| 52 | 148 | Bola de Nieve | 5 | 5 | 6 | 8 | 5,70 |
| 53 | 8 | Terrario Sellado | 5 | 5 | 6 | 8 | 5,70 |
| 54 | 91 | Teatro de Sombras | 6 | 5 | 6 | 6 | 5,70 |
| 55 | 144 | La Ola | 6 | 4 | 6 | 8 | 5,70 |
| 56 | 138 | Moiras | 6 | 6 | 4 | 7 | 5,65 |
| 57 | 20 | Marea Lunar | 6 | 6 | 5 | 5 | 5,60 |
| 58 | 111 | Bolos Infinitos | 5 | 5 | 8 | 4 | 5,60 |
| 59 | 3 | Colmena Fractal | 4 | 6 | 5 | 9 | 5,60 |
| 60 | 96 | Fusión Estelar | 3 | 8 | 4 | 8 | 5,50 |
| 61 | 106 | Plaga Gris | 2 | 7 | 7 | 7 | 5,50 |
| 62 | 140 | Prometeo | 5 | 6 | 5 | 6 | 5,45 |
| 63 | 5 | Liquen | 7 | 5 | 3 | 7 | 5,40 |
| 64 | 152 | Cartógrafo | 5 | 6 | 6 | 4 | 5,40 |
| 65 | 27 | Tren que Nunca Para | 3 | 6 | 6 | 8 | 5,40 |
| 66 | 54 | Zoom Fractal | 4 | 6 | 7 | 4 | 5,35 |
| 67 | 40 | Fantasma de la Casa | 4 | 5 | 7 | 6 | 5,35 |
| 68 | 158 | Péndulos Acoplados | 5 | 5 | 5 | 7 | 5,30 |
| 69 | 137 | Yggdrasil | 4 | 7 | 5 | 5 | 5,30 |
| 70 | 116 | Pinball 3D Incremental | 5 | 5 | 6 | 5 | 5,25 |
| 71 | 131 | Burocracia Infinita | 4 | 5 | 6 | 7 | 5,25 |
| 72 | 90 | Iceberg | 4 | 5 | 6 | 7 | 5,25 |
| 73 | 79 | Contención de Anomalías | 4 | 6 | 6 | 5 | 5,25 |
| 74 | 130 | Ondas en el Estanque | 5 | 6 | 3 | 8 | 5,25 |
| 75 | 4 | Hormiguero de Cristal | 3 | 6 | 6 | 7 | 5,25 |
| 76 | 101 | Hormigas en Casa | 3 | 6 | 7 | 5 | 5,20 |
| 77 | 19 | Entropía Inversa | 5 | 5 | 5 | 6 | 5,15 |
| 78 | 94 | Bioluminiscencia | 4 | 5 | 5 | 8 | 5,15 |
| 79 | 72 | Torre de Babel Mecanografiada | 5 | 4 | 8 | 3 | 5,15 |
| 80 | 76 | Faro contra la Oscuridad | 3 | 6 | 5 | 8 | 5,15 |
| 81 | 122 | Estaciones | 4 | 5 | 4 | 9 | 5,05 |
| 82 | 109 | Correo Neumático | 5 | 5 | 4 | 7 | 5,05 |
| 83 | 73 | Volcán de Escritorio | 5 | 4 | 7 | 4 | 5,05 |
| 84 | 64 | Catedral de Velas | 5 | 5 | 4 | 7 | 5,05 |
| 85 | 25 | Acueducto | 5 | 5 | 4 | 7 | 5,05 |
| 86 | 65 | Paleontología Inversa | 5 | 5 | 5 | 5 | 5,00 |
| 87 | 154 | Puente | 4 | 6 | 5 | 5 | 5,00 |
| 88 | 84 | Rebote de Piedra | 5 | 4 | 5 | 7 | 5,00 |
| 89 | 51 | Rubik Incremental | 3 | 5 | 5 | 9 | 5,00 |
| 90 | 136 | Atlas | 4 | 4 | 6 | 7 | 4,95 |
| 91 | 10 | Polinizador | 3 | 5 | 6 | 7 | 4,95 |
| 92 | 145 | Bostezo Contagioso | 6 | 3 | 6 | 5 | 4,95 |
| 93 | 92 | Eclipse | 4 | 5 | 4 | 8 | 4,90 |
| 94 | 74 | Isla Flotante | 4 | 4 | 7 | 5 | 4,90 |
| 95 | 41 | Recuerdos | 5 | 5 | 4 | 6 | 4,90 |
| 96 | 58 | Esfera Dyson | 2 | 6 | 5 | 8 | 4,85 |
| 97 | 11 | Glaciar | 6 | 5 | 3 | 5 | 4,80 |
| 98 | 71 | Pecera de Escritorio | 4 | 4 | 7 | 4 | 4,75 |
| 99 | 57 | Laberinto Vivo | 4 | 5 | 4 | 7 | 4,75 |
| 100 | 83 | Cohete por Etapas | 2 | 7 | 4 | 7 | 4,75 |
| 101 | 82 | Caída Libre | 4 | 4 | 5 | 7 | 4,70 |
| 102 | 63 | Reloj de Arena | 3 | 5 | 5 | 7 | 4,70 |
| 103 | 86 | Torre de Pisos-Pueblo | 2 | 6 | 5 | 7 | 4,70 |
| 104 | 153 | Niebla | 4 | 5 | 3 | 8 | 4,65 |
| 105 | 127 | Eco del Cañón | 5 | 5 | 3 | 6 | 4,65 |
| 106 | 119 | Tapiz Infinito | 5 | 4 | 3 | 8 | 4,65 |
| 107 | 68 | NPC Consciente | 3 | 5 | 6 | 5 | 4,65 |
| 108 | 102 | Slimes Fusionables | 2 | 5 | 6 | 7 | 4,65 |
| 109 | 85 | Cañón al Horizonte | 2 | 5 | 6 | 7 | 4,65 |
| 110 | 6 | Gota Primordial | 2 | 6 | 6 | 5 | 4,65 |
| 111 | 56 | Teseracto | 4 | 5 | 4 | 6 | 4,60 |
| 112 | 48 | Taza de Café | 5 | 4 | 4 | 6 | 4,60 |
| 113 | 35 | Farero | 4 | 4 | 4 | 8 | 4,60 |
| 114 | 15 | Nebulosa Forja | 3 | 6 | 4 | 6 | 4,60 |
| 115 | 125 | Deriva Continental | 4 | 5 | 5 | 4 | 4,55 |
| 116 | 121 | Grafiti | 4 | 4 | 5 | 6 | 4,55 |
| 117 | 118 | Escultor de Mármol | 5 | 4 | 5 | 4 | 4,55 |
| 118 | 112 | Peonzas | 5 | 4 | 5 | 4 | 4,55 |
| 119 | 42 | Fábrica de Sueños | 4 | 4 | 5 | 6 | 4,55 |
| 120 | 70 | Patch Notes | 3 | 5 | 5 | 6 | 4,55 |
| 121 | 143 | Pelota Rompebloques | 2 | 5 | 5 | 8 | 4,55 |
| 122 | 142 | Imán | 3 | 4 | 6 | 6 | 4,50 |
| 123 | 108 | Banco de Semillas | 4 | 5 | 3 | 7 | 4,50 |
| 124 | 107 | Ruta de la Seda | 3 | 6 | 3 | 7 | 4,50 |
| 125 | 36 | Cartero del Mundo | 4 | 4 | 4 | 7 | 4,45 |
| 126 | 69 | Datacenter | 3 | 5 | 3 | 8 | 4,35 |
| 127 | 59 | Teselación | 4 | 4 | 3 | 8 | 4,35 |
| 128 | 29 | Molino del Clima | 5 | 4 | 3 | 6 | 4,35 |
| 129 | 67 | Pantalla de Carga | 2 | 4 | 6 | 7 | 4,35 |
| 130 | 104 | Cardumen | 3 | 5 | 4 | 6 | 4,30 |
| 131 | 99 | Lámpara de Lava | 4 | 3 | 4 | 8 | 4,30 |
| 132 | 139 | Caronte | 2 | 4 | 5 | 8 | 4,25 |
| 133 | 34 | Dios Menor | 2 | 5 | 5 | 6 | 4,25 |
| 134 | 49 | Óxido | 4 | 4 | 3 | 7 | 4,20 |
| 135 | 132 | Ascensor del Rascacielos | 3 | 4 | 4 | 7 | 4,15 |
| 136 | 47 | Burbujas | 3 | 4 | 4 | 7 | 4,15 |
| 137 | 95 | Alquimia | 2 | 5 | 4 | 7 | 4,15 |
| 138 | 128 | Orquesta | 3 | 4 | 5 | 5 | 4,10 |
| 139 | 78 | Culto | 2 | 5 | 5 | 5 | 4,10 |
| 140 | 159 | Faro de Niebla | 3 | 4 | 3 | 8 | 4,05 |
| 141 | 146 | Dominó Urbano | 3 | 4 | 6 | 3 | 4,05 |
| 142 | 62 | Cápsula del Tiempo | 4 | 4 | 3 | 6 | 4,05 |
| 143 | 24 | Jenga Inverso | 3 | 4 | 6 | 3 | 4,05 |
| 144 | 110 | Puerto de Contenedores | 2 | 5 | 3 | 8 | 4,05 |
| 145 | 120 | Vidrio Soplado | 4 | 3 | 4 | 6 | 4,00 |
| 146 | 37 | Sastrería Divina | 5 | 3 | 4 | 4 | 4,00 |
| 147 | 43 | Hotel para Monstruos | 3 | 4 | 5 | 4 | 3,95 |
| 148 | 129 | Campanario | 4 | 3 | 3 | 7 | 3,90 |
| 149 | 133 | Cinta de Equipajes | 2 | 3 | 5 | 7 | 3,80 |
| 150 | 126 | Ciclo del Agua | 3 | 4 | 3 | 6 | 3,75 |
| 151 | 98 | Pátina | 4 | 3 | 2 | 7 | 3,65 |
| 152 | 28 | Teleférico | 3 | 3 | 3 | 7 | 3,60 |
| 153 | 39 | Museo de lo Cotidiano | 2 | 4 | 4 | 5 | 3,55 |
| 154 | 151 | Gusano de Seda | 3 | 4 | 2 | 6 | 3,50 |
| 155 | 44 | Cementerio Idle | 2 | 3 | 3 | 7 | 3,30 |
| 156 | 156 | Paraguas | 3 | 3 | 2 | 6 | 3,20 |
| 157 | 117 | Pincelada | 3 | 3 | 2 | 6 | 3,20 |
| 158 | 150 | Laberinto del Minotauro | 2 | 3 | 3 | 6 | 3,15 |
| 159 | 134 | Lavandería | 2 | 3 | 3 | 6 | 3,15 |
| 160 | 115 | Globos de Agua | 2 | 2 | 3 | 4 | 2,55 |
