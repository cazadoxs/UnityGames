# Biblia de arte — Edad Media (primer juego)

> Estilo **realista histórico** (Vol. I §14). Esta biblia fija qué se ve en cada nivel. Todo asset nuevo se compara con ella antes de entrar en el juego.

## 1. Principios

1. **Fidelidad por siglo.** Cada nivel tiene su equipo de época; no se mezclan armaduras de 1066 con las de 1415.
2. **Legibilidad a escala.** Cada bando tiene dos colores dominantes y estandartes altos que se distinguen a 300 m.
3. **Desgaste.** Nada está limpio: barro hasta la rodilla, telas descoloridas, metal con óxido y golpes.
4. **Variedad modular.** Cabeza, torso, brazos, piernas, escudo y sobreveste combinables por cultura (Vol. I §14.2).
5. **Presupuesto.** Límites de polígonos y texturas del [Volumen V, 6](../GDD/05_Mapas_y_Mods.md#6-rendimiento-y-límites); LOD0–LOD3 e impostor para todo personaje.

## 2. Culturas visuales de los cinco niveles

| Cultura | Nivel | Armadura y ropa | Armas y escudos | Colores de bando | Fuentes visuales de dominio público |
|---|---|---|---|---|---|
| **Noroccidental temprana** (s. XI) | 1 · Hastings | Cota de malla larga (*hauberk*) con capucha de malla, yelmo cónico con nasal, túnicas de lana; el *fyrd* con ropa civil y gambesón | Escudos de cometa (normandos) y redondos (anglosajones), hacha danesa, lanza, espada de hoja ancha, arco corto | Normandos: rojo y oro · Anglosajones: rojo oscuro y amarillo con el dragón de Wessex | Tapiz de Bayeux |
| **Franco-angevina** (c. 1200) | 2 · Château Gaillard | Malla completa con calzas de malla, yelmo de cimera plana o capacete, sobreveste larga; ballesteros con gambesón | Escudo de cometa recortado o heater, ballesta, maza, espada | Francia: azul y oro (flor de lis) · Angevinos: rojo y oro (leones) | Biblia Maciejowski (c. 1250), sellos y efigies funerarias |
| **Ibérica cristiana** (s. XIII) | 3 · Las Navas | Malla y sobreveste, yelmos de cimera plana; jinetes ligeros con adarga | Lanza, espada, escudos heater con heráldica de los reinos y órdenes militares | Castilla: rojo y oro (castillo) · Aragón: oro y rojo (barras) · Navarra: rojo y oro | Cantigas de Santa María, Beatos, pinturas murales |
| **Andalusí-magrebí** (s. XIII) | 3 · Las Navas | Túnicas y turbantes, algunas cotas de malla, cascos cónicos | Adarga (escudo de cuero en forma de corazón), lanza, arco compuesto, jabalinas | Blanco y negro, con detalles de color | Cantigas de Santa María, piezas de museo andalusíes |
| **Escocesa e inglesa** (finales del s. XIII) | 4 · Stirling | Escoceses: gambesón, cascos sencillos, poca malla · Ingleses: malla, sobreveste, yelmos de cimera plana | Escoceses: lanza larga, escudo pequeño · Ingleses: lanza de caballería, espada, arco | Escocia: azul y blanco (aspa) · Inglaterra: rojo y oro | Salterio de Luttrell (algo posterior), efigies |
| **Anglo-francesa tardía** (s. XV) | 5 · Agincourt | Hombres de armas en arnés de placas completo con bacinete de visera; arqueros con *jack* acolchado y capacete o sin casco | Arco largo, mazos de plomo, espada de mano y media, hacha de armas, estacas | Inglaterra: rojo y azul (cuartelado con lises) · Francia: azul y oro | Crónicas de Froissart, *Très Riches Heures* |

## 3. Paleta y materiales

- **Tierra y barro:** marrones fríos en invierno (Gaillard), ocres en verano (Las Navas), barro casi negro en Agincourt.
- **Metal:** acero gris mate con zonas pulidas por el uso; óxido en juntas.
- **Telas:** tintes de época (rubia para rojos, gualda para amarillos, glasto para azules), nunca saturados como un tinte moderno.
- **Cuero:** marrones con desgaste en bordes.

## 4. Variación por *shader*

Cada instancia lleva: color principal y secundario de la heráldica, nivel de suciedad, nivel de sangre, desgaste. Las máscaras van en el canal de máscaras de la textura de la pieza.

## 5. Convenciones

Ver el [anexo de convenciones del Volumen IV](../GDD/04_Guia_de_Construccion.md#anexo-convenciones-del-proyecto) para nombres de modelos, texturas y materiales.
