using System;
using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>Productor del doc 4.9/4.10 y 6.2: coste base × 1,15^n, hitos ×2 a 10/25/50/100/200 unidades.</summary>
    public class ProducerDef
    {
        public string key, name, desc;
        public int act;               // acto en que aparece
        public double baseCost;
        public double income;         // dinero por segundo de una unidad (páginas/s × valor por página)
        public double pages;          // páginas por segundo de una unidad
        public double sheets;         // hojas que saca de la montaña por segundo (recogedores)
        public string branch = "Escritura";
        public int visualCap = 12;    // cuántas se ven en la nave; el resto lo cuenta el cartel
        public double Cost(int n) => baseCost * Math.Pow(1.15, n);
        public bool IsPicker => sheets > 0;
    }

    /// <summary>Nodo del árbol global (doc 4.13): ramas, niveles y requisitos cruzados.</summary>
    public class NodeDef
    {
        public string key, name, desc, branch;
        public int act = 2, maxLevel = 1;
        public double baseCost, growth = 1.6;
        public string[] requires = new string[0];   // otras claves de nodo (nivel ≥ 1) o "p:clave" de productor (≥ 1 unidad)
        public string exclusiveWith;                // elige uno de dos
        public double Cost(int lvl) => baseCost * Math.Pow(growth, lvl);
    }

    public class AchievementDef
    {
        public string key, name, desc;
        public bool hidden;
        public Func<SaveData, bool> done;
    }

    public class InkNode
    {
        public string key, name, desc;
        public int cost, maxLevel = 1;
        public int layer = 1; // 1 = tinta dorada, 2 = sellos editoriales
    }

    /// <summary>Todo el equilibrio de los actos 2 a 8, el prestigio y el metajuego.</summary>
    public static class Progress
    {
        // ------------------------------------------------------------ actos y terreno
        public static readonly string[] ActNames = { "", "La máquina", "El taller", "La oficina", "El mostrador", "El estudio", "La fábrica", "La red", "Más allá" };
        public static readonly double[] ActArea = { 0, 0, 400, 1200, 5000, 14000, 22000, 0, 0 };
        public const double MountainWidth = 116, MountainDepth = 206, ArcRadius = 58;
        public static double SemiArea => Math.PI * ArcRadius * ArcRadius / 2;
        public static double FullArea => Econ.StartArea + SemiArea + MountainWidth * MountainDepth;

        /// <summary>
        /// Terreno libre: 0,1 m² por hoja hasta 400 m² (acto 1, a mano) y después crece con la raíz cúbica de las hojas,
        /// porque las máquinas mueven millones. Con 4·10^8 hojas se llega al fondo de la montaña.
        /// </summary>
        public static double AreaFor(double sheets)
        {
            if (sheets <= 3000) return Econ.StartArea + sheets * Econ.AreaPerSheet;
            return Math.Min(FullArea, Econ.GoalArea + 40 * (Math.Pow(sheets, 1.0 / 3) - Math.Pow(3000, 1.0 / 3)));
        }

        public static double SheetsForArea(double area)
        {
            if (area <= Econ.GoalArea) return (area - Econ.StartArea) / Econ.AreaPerSheet;
            return Math.Pow((area - Econ.GoalArea) / 40 + Math.Pow(3000, 1.0 / 3), 3);
        }

        /// <summary>Forma del hueco: semicírculo hasta 58 m de radio y después un frente recto que avanza.</summary>
        public static void Shape(double area, out float radius, out float band)
        {
            double won = Math.Max(0, area - Econ.StartArea);
            if (won <= SemiArea) { radius = (float)Math.Sqrt(2 * won / Math.PI); band = 0; }
            else { radius = (float)ArcRadius; band = (float)((won - SemiArea) / MountainWidth); }
        }

        // ------------------------------------------------------------ productores
        public static readonly List<ProducerDef> Producers = new List<ProducerDef>
        {
            // Acto 2: el taller
            new ProducerDef { key = "brazo", name = "Brazo de recogida", desc = "Brazo neumático al pie de la montaña: saca hojas solo y gana terreno.", act = 2, baseCost = 300, sheets = 1, branch = "Logística", visualCap = 16 },
            new ProducerDef { key = "mecanografo", name = "Mecanógrafo", desc = "Se sienta en un puesto y teclea solo. Para a por café de vez en cuando.", act = 2, baseCost = 150, income = 0.5, pages = 0.25, visualCap = 16 },
            new ProducerDef { key = "equipo", name = "Equipo de redacción", desc = "Cuatro mesas con su supervisor, que reduce los errores.", act = 2, baseCost = 2000, income = 4.5, pages = 1.6, visualCap = 6 },
            // Acto 3: la oficina
            new ProducerDef { key = "grua", name = "Grúa pulpo", desc = "Una garra colgada del puente grúa que coge resmas enteras.", act = 3, baseCost = 20000, sheets = 8, branch = "Logística", visualCap = 6 },
            new ProducerDef { key = "pc", name = "Puesto PC 286", desc = "Un mecanógrafo con ordenador y Escriba: no se atasca y corrige solo.", act = 3, baseCost = 25000, income = 35, pages = 6, visualCap = 12 },
            new ProducerDef { key = "matricial", name = "Impresora matricial", desc = "Papel continuo, agujas y un chirrido inolvidable.", act = 3, baseCost = 300000, income = 250, pages = 40, branch = "Impresión", visualCap = 10 },
            new ProducerDef { key = "inyeccion", name = "Impresora de inyección", desc = "El cabezal va y viene; las gotas se ven al trasluz.", act = 3, baseCost = 3.5e6, income = 1800, pages = 200, branch = "Impresión", visualCap = 10 },
            // Acto 4: el mostrador
            new ProducerDef { key = "excavadora", name = "Excavadora de papel", desc = "Una pala pequeña que muerde la montaña sin parar.", act = 4, baseCost = 2e6, sheets = 60, branch = "Logística", visualCap = 4 },
            new ProducerDef { key = "fotocopiadora", name = "Fotocopiadora", desc = "La luz pasa bajo el cristal y salen copias a decenas.", act = 4, baseCost = 4.5e7, income = 13000, pages = 1200, branch = "Impresión", visualCap = 10 },
            new ProducerDef { key = "dependiente", name = "Dependiente de ventanilla", desc = "Atiende a los clientes de la ventanilla y sube la reputación.", act = 4, baseCost = 1.5e8, income = 30000, pages = 0, branch = "Negocio", visualCap = 4 },
            new ProducerDef { key = "duplicadora", name = "Duplicadora de disquetes y casetes", desc = "Copia programas y cintas por docenas.", act = 4, baseCost = 5.5e8, income = 95000, pages = 6000, visualCap = 8 },
            // Acto 5: el estudio
            new ProducerDef { key = "aspirador", name = "Succión industrial", desc = "Un tubo de un metro que traga papel a toneladas.", act = 5, baseCost = 3e8, sheets = 400, branch = "Logística", visualCap = 3 },
            new ProducerDef { key = "plotter", name = "Plóter de gran formato", desc = "Pósters de dos metros, línea a línea.", act = 5, baseCost = 7e9, income = 7e5, pages = 2e4, branch = "Impresión", visualCap = 8 },
            new ProducerDef { key = "fotolab", name = "Laboratorio de fotos", desc = "Revela fotos brillantes que valen mucho más que un folio.", act = 5, baseCost = 9e10, income = 5e6, pages = 9e4, branch = "Impresión", visualCap = 6 },
            new ProducerDef { key = "macrobot", name = "Bot de macros", desc = "Un programa que teclea plantillas en los PC. No ocupa sitio.", act = 5, baseCost = 1.1e12, income = 3.6e7, pages = 8e5, visualCap = 0 },
            // Acto 6: la fábrica
            new ProducerDef { key = "tuneladora", name = "Tuneladora", desc = "Una cabeza giratoria que avanza dentro de la montaña.", act = 6, baseCost = 5e10, sheets = 3000, branch = "Logística", visualCap = 2 },
            new ProducerDef { key = "offset", name = "Prensa offset", desc = "Planchas, rodillos y tinta en cuatro colores.", act = 6, baseCost = 1.4e13, income = 2.6e8, pages = 6e6, branch = "Impresión", visualCap = 6 },
            new ProducerDef { key = "rotativa", name = "Rotativa", desc = "Bobinas de papel de una tonelada; imprime un periódico por segundo.", act = 6, baseCost = 1.8e14, income = 1.9e9, pages = 5e7, branch = "Impresión", visualCap = 3 },
            new ProducerDef { key = "ia", name = "IA redactora", desc = "Escribe frases a medida para cada cliente.", act = 6, baseCost = 2.3e15, income = 1.4e10, pages = 3e8, visualCap = 6 },
            // Acto 7: la red
            new ProducerDef { key = "redia", name = "Rack de la red de IA", desc = "Cientos de instancias; sus luces parpadean al escribir.", act = 7, baseCost = 3e16, income = 1e11, pages = 2e9, visualCap = 10 },
            new ProducerDef { key = "contratos", name = "Contratos de gobierno", desc = "Boletines, censos y formularios por triplicado.", act = 7, baseCost = 4e17, income = 7.5e11, pages = 1.5e10, branch = "Negocio", visualCap = 0 },
            // Acto 8: más allá
            new ProducerDef { key = "cuantica", name = "Prensa cuántica", desc = "Imprime todas las páginas posibles y se queda con la buena.", act = 8, baseCost = 5e18, income = 5.5e12, pages = 1e11, branch = "Impresión", visualCap = 3 },
            new ProducerDef { key = "fractal", name = "Imprenta fractal", desc = "Cada página contiene una imprenta más pequeña.", act = 8, baseCost = 7e19, income = 4e13, pages = 8e11, branch = "Impresión", visualCap = 2 },
            new ProducerDef { key = "babel", name = "Biblioteca de Babel", desc = "Todas las frases de la base, por fin, encuadernadas.", act = 8, baseCost = 1e21, income = 3e14, pages = 6e12, visualCap = 1 },
        };

        public static ProducerDef P(string key) => Producers.Find(p => p.key == key);

        public static readonly int[] Milestones = { 10, 25, 50, 100, 200 };
        public static int MilestoneMult(int n) { int m = 1; foreach (var t in Milestones) if (n >= t) m *= 2; return m; }

        // ------------------------------------------------------------ árbol global (acto 2 en adelante)
        public static readonly List<NodeDef> Nodes = new List<NodeDef>
        {
            // Papel
            new NodeDef { key = "papelGramaje", name = "Papel de 90 g", desc = "+20 % de valor por página.", branch = "Papel", act = 2, baseCost = 800, maxLevel = 10 },
            new NodeDef { key = "papelColor", name = "Papel de colores", desc = "+50 % de valor en las páginas de los mecanógrafos.", branch = "Papel", act = 2, baseCost = 5000, requires = new[] { "papelGramaje" } },
            new NodeDef { key = "papelCartulina", name = "Cartulina", desc = "×2 en impresoras de inyección y plóter.", branch = "Papel", act = 3, baseCost = 2e6, requires = new[] { "papelGramaje", "p:inyeccion" } },
            new NodeDef { key = "papelFoto", name = "Papel fotográfico", desc = "×3 en el laboratorio de fotos.", branch = "Papel", act = 5, baseCost = 2e11, requires = new[] { "papelCartulina", "p:fotolab" } },
            new NodeDef { key = "papelPergamino", name = "Pergamino", desc = "×2 a todo lo escrito a mano y por mecanógrafos.", branch = "Papel", act = 6, baseCost = 1e14, requires = new[] { "papelColor" } },
            // Escritura
            new NodeDef { key = "cafe", name = "Máquina de café", desc = "Sube la moral: mecanógrafos y equipos +25 % por nivel.", branch = "Escritura", act = 2, baseCost = 600, maxLevel = 10 },
            new NodeDef { key = "turnos", name = "Turnos dobles", desc = "×2 a mecanógrafos y equipos.", branch = "Escritura", act = 2, baseCost = 25000, requires = new[] { "cafe", "p:equipo" } },
            new NodeDef { key = "escriba2", name = "Escriba 2.0", desc = "Los PC escriben un 50 % más por nivel.", branch = "Escritura", act = 3, baseCost = 1.5e5, maxLevel = 10, requires = new[] { "p:pc" } },
            new NodeDef { key = "diccionario", name = "Bot con diccionario", desc = "Frases más originales: ×3 a los bots de macros.", branch = "Escritura", act = 5, baseCost = 5e12, requires = new[] { "p:macrobot", "escriba2" } },
            new NodeDef { key = "iaTemas", name = "Temas e idiomas", desc = "×2 por nivel a la IA redactora y a la red.", branch = "Escritura", act = 6, baseCost = 1e16, maxLevel = 5, requires = new[] { "p:ia" } },
            // Impresión
            new NodeDef { key = "tinta", name = "Tinta de calidad", desc = "+30 % a todas las impresoras por nivel.", branch = "Impresión", act = 3, baseCost = 4e5, maxLevel = 10, requires = new[] { "p:matricial" } },
            new NodeDef { key = "laserVel", name = "Láser: velocidad", desc = "Impresoras ×2 de páginas (exclusiva con «calidad»).", branch = "Impresión", act = 4, baseCost = 5e7, exclusiveWith = "laserCal", requires = new[] { "tinta" } },
            new NodeDef { key = "laserCal", name = "Láser: calidad", desc = "Impresoras ×2 de valor y +1 de reputación al servir (exclusiva).", branch = "Impresión", act = 4, baseCost = 5e7, exclusiveWith = "laserVel", requires = new[] { "tinta" } },
            new NodeDef { key = "red", name = "Impresión en red", desc = "×3 a todas las impresoras. Pide Escriba 2.0 nivel 4 y Tinta nivel 3.", branch = "Impresión", act = 5, baseCost = 5e10, requires = new[] { "escriba2:4", "tinta:3", "p:plotter" } },
            new NodeDef { key = "cuatricromia", name = "Cuatricromía", desc = "×2 a offset y rotativa por nivel.", branch = "Impresión", act = 6, baseCost = 5e14, maxLevel = 5, requires = new[] { "p:offset" } },
            // Logística
            new NodeDef { key = "cintaVel", name = "Motores de cinta", desc = "Cintas más rápidas: +25 % a toda la producción por nivel.", branch = "Logística", act = 2, baseCost = 1500, maxLevel = 8 },
            new NodeDef { key = "ventosas", name = "Ventosas de silicona", desc = "Los brazos y grúas sacan el doble de hojas.", branch = "Logística", act = 2, baseCost = 4000, maxLevel = 5, requires = new[] { "p:brazo" } },
            new NodeDef { key = "ductoDoble", name = "Ducto doble", desc = "×2 al valor de lo que echas tú al ducto.", branch = "Logística", act = 2, baseCost = 3000 },
            new NodeDef { key = "dientes", name = "Dientes de tungsteno", desc = "×3 a excavadoras, succión y tuneladoras.", branch = "Logística", act = 4, baseCost = 1e8, maxLevel = 3, requires = new[] { "p:excavadora" } },
            // Negocio
            new NodeDef { key = "horasExtra", name = "Horas extra", desc = "La jornada dura una hora más por nivel.", branch = "Negocio", act = 2, baseCost = 2000, maxLevel = 4 },
            new NodeDef { key = "furgoneta2", name = "Segunda furgoneta", desc = "El bonus del pedido del día es ×1,5.", branch = "Negocio", act = 2, baseCost = 8000 },
            new NodeDef { key = "urgentes", name = "Pedidos urgentes", desc = "A media jornada llega un pedido urgente con su propio bonus.", branch = "Negocio", act = 3, baseCost = 1e5, requires = new[] { "furgoneta2" } },
            new NodeDef { key = "tren", name = "Tren de mercancías", desc = "Al final del día, el tren paga ×3 lo que sobre del pedido.", branch = "Negocio", act = 5, baseCost = 5e10, requires = new[] { "urgentes" } },
            new NodeDef { key = "precios", name = "Lista de precios", desc = "+15 % de valor en todo por nivel.", branch = "Negocio", act = 4, baseCost = 2e7, maxLevel = 10 },
            new NodeDef { key = "rotulo", name = "Rótulo luminoso", desc = "Llegan clientes el doble de rápido a la ventanilla.", branch = "Negocio", act = 4, baseCost = 3e7, requires = new[] { "p:dependiente" } },
            // Investigación
            new NodeDef { key = "lupa", name = "Lupa de anticuario", desc = "Encuentras pergaminos secretos el doble de a menudo.", branch = "Investigación", act = 3, baseCost = 2e5 },
            new NodeDef { key = "archivo", name = "Archivo de la montaña", desc = "Cada pergamino da un +2 % más.", branch = "Investigación", act = 5, baseCost = 1e11, requires = new[] { "lupa" } },
            new NodeDef { key = "linterna", name = "Linterna de petaca", desc = "Si te quedas a oscuras, ves más lejos. Y quizá oyes algo.", branch = "Investigación", act = 2, baseCost = 1200 },
        };

        public static NodeDef N(string key) => Nodes.Find(n => n.key == key);

        public static string Missing(SaveData s, NodeDef n)
        {
            if (s.act < n.act) return "Acto " + n.act;
            if (n.exclusiveWith != null && s.Node(n.exclusiveWith) > 0) return "Elegiste " + N(n.exclusiveWith).name;
            foreach (var r in n.requires)
            {
                if (r.StartsWith("p:")) { var pd = P(r.Substring(2)); if (s.Count(pd.key) < 1) return "Necesita " + pd.name; continue; }
                int need = 1; string k = r;
                int c = r.IndexOf(':'); if (c > 0) { k = r.Substring(0, c); need = int.Parse(r.Substring(c + 1)); }
                if (s.Node(k) < need) return "Necesita " + N(k).name + (need > 1 ? " nivel " + need : "");
            }
            return null;
        }

        // ------------------------------------------------------------ multiplicadores
        public static double ProducerMult(SaveData s, ProducerDef p)
        {
            double m = MilestoneMult(s.Count(p.key));
            m *= 1 + 0.25 * s.Node("cintaVel");
            switch (p.key)
            {
                case "mecanografo":
                case "equipo":
                    m *= 1 + 0.25 * s.Node("cafe");
                    if (s.Node("turnos") > 0) m *= 2;
                    if (s.Node("papelColor") > 0) m *= 1.5;
                    if (s.Node("papelPergamino") > 0) m *= 2;
                    break;
                case "pc": m *= 1 + 0.5 * s.Node("escriba2"); break;
                case "macrobot": if (s.Node("diccionario") > 0) m *= 3; break;
                case "ia": case "redia": m *= Math.Pow(2, s.Node("iaTemas")); break;
                case "offset": case "rotativa": m *= Math.Pow(2, s.Node("cuatricromia")); break;
                case "fotolab": if (s.Node("papelFoto") > 0) m *= 3; break;
            }
            if (p.branch == "Impresión")
            {
                m *= 1 + 0.3 * s.Node("tinta");
                if (s.Node("laserVel") > 0 || s.Node("laserCal") > 0) m *= 2;
                if (s.Node("red") > 0) m *= 3;
                if ((p.key == "inyeccion" || p.key == "plotter") && s.Node("papelCartulina") > 0) m *= 2;
            }
            if (p.IsPicker)
            {
                m = MilestoneMult(s.Count(p.key)) * Math.Pow(2, s.Node("ventosas"));
                if (p.key != "brazo" && p.key != "grua") m *= Math.Pow(3, s.Node("dientes"));
                m *= Halls.PickerMult(s.hall);
            }
            else m *= Halls.ProducerMult(s.hall, p);
            m *= s.ObjLevelMult(p.key);
            return m;
        }

        /// <summary>Multiplicador común a todo el dinero: papel, precios, reputación, logros, pergaminos y prestigio.</summary>
        public static double GlobalMult(SaveData s)
        {
            double m = 1 + 0.2 * s.Node("papelGramaje");
            m *= 1 + 0.15 * s.Node("precios");
            m *= s.reputation;
            m *= 1 + 0.01 * s.achievements.Count;
            m *= 1 + (s.Node("archivo") > 0 ? 0.05 : 0.03) * s.scrolls.Count;
            m *= 1 + 0.02 * s.ink;                    // tinta dorada sin gastar
            m *= 1 + 0.1 * s.InkLevel("tintero");
            m *= Math.Pow(1.5, s.InkLevel("selloColeccion"));
            m *= 1 + 0.25 * s.seals;
            m *= Math.Pow(3, s.hallsDone.Count);
            if (s.boostLeft > 0) m *= 7;
            return m;
        }

        public static double IncomePerSec(SaveData s, out double pagesPerSec, out double sheetsPerSec)
        {
            double inc = 0; pagesPerSec = 0; sheetsPerSec = 0;
            foreach (var p in Producers)
            {
                int n = s.Count(p.key);
                if (n == 0) continue;
                double m = ProducerMult(s, p);
                if (p.IsPicker) sheetsPerSec += n * p.sheets * m;
                else { inc += n * p.income * m; pagesPerSec += n * p.pages * m; }
            }
            inc *= GlobalMult(s);
            return inc;
        }

        // ------------------------------------------------------------ jornada (doc 4.17)
        public const float DaySeconds = 420f;     // 7 minutos reales = 8:00 a 20:00
        public static float DayLength(SaveData s) => DaySeconds * (12f + s.Node("horasExtra")) / 12f;
        public static float EndHour(SaveData s) => 20f + s.Node("horasExtra");
        public static double OrderFor(SaveData s, double pagesPerSec)
        {
            double expect = Math.Max(30, pagesPerSec * DayLength(s) * 0.85);
            return Math.Round(expect * (1 + 0.03 * Math.Min(s.day, 30)) + 10 * s.day);
        }

        // ------------------------------------------------------------ ventanilla y reputación (doc 4.12)
        public static double ReputationFor(double served) => 1 + 4 * (1 - Math.Exp(-served / 80.0));

        // ------------------------------------------------------------ prestigio (doc 6.3)
        public static long InkFor(double pagesThisEdition) => (long)Math.Floor(Math.Pow(Math.Max(0, pagesThisEdition) / 1000.0, 1.0 / 3));
        public static long SealsFor(double inkEver) => (long)Math.Floor(Math.Sqrt(Math.Max(0, inkEver) / 50.0));

        public static readonly List<InkNode> InkTree = new List<InkNode>
        {
            new InkNode { key = "tintero", name = "Tintero sin fondo", desc = "+10 % de producción por nivel.", cost = 5, maxLevel = 20 },
            new InkNode { key = "arranque", name = "Arranque rápido", desc = "Empiezas cada edición con la carpeta, el dedil y la cinta corta.", cost = 3 },
            new InkNode { key = "pistolaIni", name = "Pistola de serie", desc = "Empiezas con la pistola de succión.", cost = 12 },
            new InkNode { key = "herencia", name = "Herencia", desc = "Empiezas con 5 brazos y 10 mecanógrafos.", cost = 25 },
            new InkNode { key = "pergaminosFijos", name = "Pergaminos que no se pierden", desc = "Los pergaminos secretos se quedan entre ediciones.", cost = 40 },
            new InkNode { key = "memoria", name = "Memoria muscular", desc = "La máquina de escribir no se atasca nunca.", cost = 8 },
            new InkNode { key = "capital", name = "Capital inicial", desc = "Empiezas con 1.000 $ por nivel, ×10 cada nivel.", cost = 6, maxLevel = 8 },
            new InkNode { key = "jornadaLarga", name = "Convenio", desc = "La puntualidad paga el doble.", cost = 10 },
            // Capa 2: sellos editoriales
            new InkNode { key = "selloColeccion", name = "Colección", desc = "×1,5 a todo por nivel (líneas de producto con bonus).", cost = 1, maxLevel = 10, layer = 2 },
            new InkNode { key = "selloVentanilla", name = "Ventanilla automática", desc = "La ventanilla atiende sola desde el primer día.", cost = 2, layer = 2 },
            new InkNode { key = "selloTinta", name = "Tinta heredada", desc = "Conservas un 10 % de la tinta dorada al refundar la editorial.", cost = 3, layer = 2 },
            new InkNode { key = "selloActos", name = "Mapa de la montaña", desc = "Cada edición empieza con 1.200 m² ganados.", cost = 4, layer = 2 },
        };

        public static InkNode Ink(string key) => InkTree.Find(n => n.key == key);

        // ------------------------------------------------------------ pergaminos secretos (doc 4.14)
        public static readonly string[] ScrollNames =
        {
            "Carta de un cajista de 1887", "Recibo de la primera resma", "Plano de la nave, a lápiz", "Diario del vigilante, tomo I",
            "Prueba de imprenta con erratas", "Tipo móvil de plomo envuelto", "Factura de tinta de calamar", "Mapa del fondo de la montaña",
            "Poema sin firmar", "Orden de cierre de 1931", "Diario del vigilante, tomo II", "Plantilla de una letra perdida",
            "Fotografía de la plantilla de 1952", "Hoja escrita por nadie", "Contrato con la papelera", "Lista de frases prohibidas",
            "Muestra de papel de trapo", "Carta de la primera clienta", "Diario del vigilante, tomo III", "Manual de la rotativa",
            "Página 1 de un libro sin fin", "Página 2 de un libro sin fin", "Recorte del periódico local", "Sello de la editorial antigua",
            "Diseño de una prensa imposible", "Partitura impresa al revés", "Cuaderno de cuentas del fundador", "Diario del vigilante, tomo IV",
            "La frase número uno", "Una hoja que sigue caliente", "Fotocopia de una fotocopia de una fotocopia", "Billete de tren sin fecha",
            "Plano de una nave en un barco", "Plano de una catedral", "Plano de una estación lunar", "Plano de un sótano inundado",
            "Diario del vigilante, último tomo", "Carta dirigida a ti", "Índice de la Biblioteca", "La última página",
        };
        public static double SheetsForScroll(int k) => 400.0 * Math.Pow(k + 1, 4.2);

        // ------------------------------------------------------------ logros (doc 6.4)
        static List<AchievementDef> achievements;
        public static List<AchievementDef> Achievements
        {
            get
            {
                if (achievements != null) return achievements;
                var l = new List<AchievementDef>();
                double[] money = { 100, 1e4, 1e6, 1e8, 1e10, 1e12, 1e15, 1e18, 1e21, 1e24, 1e27, 1e30 };
                foreach (var m in money)
                {
                    double mm = m;
                    l.Add(new AchievementDef { key = "dinero" + m, name = "Caja de " + Fmt.Money(m), desc = "Gana " + Fmt.Money(m) + " en total.", done = s => s.lifetimeEarned >= mm });
                }
                double[] pages = { 10, 1000, 1e5, 1e7, 1e9, 1e12, 1e15, 1e18, 1e21 };
                foreach (var p in pages)
                {
                    double pp = p;
                    l.Add(new AchievementDef { key = "paginas" + p, name = Fmt.Num(p) + " páginas", desc = "Imprime " + Fmt.Num(p) + " páginas en total.", done = s => s.lifetimePages >= pp });
                }
                foreach (var pd in Producers)
                {
                    var p0 = pd;
                    foreach (var t in new[] { 1, 10, 25, 50, 100, 200 })
                    {
                        int tt = t;
                        l.Add(new AchievementDef { key = "p_" + pd.key + t, name = pd.name + " ×" + t, desc = "Ten " + t + " de «" + pd.name + "».", done = s => s.Count(p0.key) >= tt });
                    }
                }
                for (int a = 2; a <= 8; a++)
                {
                    int aa = a;
                    l.Add(new AchievementDef { key = "acto" + a, name = "Acto " + a + ": " + ActNames[a], desc = "Llega al acto " + a + ".", done = s => s.maxAct >= aa });
                }
                l.Add(new AchievementDef { key = "racha10", name = "Inspirado", desc = "Haz una racha de 10 páginas impecables.", done = s => s.bestStreak >= 10 });
                l.Add(new AchievementDef { key = "racha50", name = "Dedos de seda", desc = "Racha de 50 impecables.", done = s => s.bestStreak >= 50 });
                l.Add(new AchievementDef { key = "dorada", name = "Hoja dorada", desc = "Escribe una frase dorada.", done = s => s.goldenCount >= 1 });
                l.Add(new AchievementDef { key = "dorada10", name = "Orfebre", desc = "Escribe 10 frases doradas.", done = s => s.goldenCount >= 10 });
                l.Add(new AchievementDef { key = "fichar1", name = "Puntual", desc = "Ficha a tiempo.", done = s => s.punctualDays >= 1 });
                l.Add(new AchievementDef { key = "fichar30", name = "Empleado del mes", desc = "Ficha a tiempo 30 días.", done = s => s.punctualDays >= 30 });
                l.Add(new AchievementDef { key = "pedido", name = "Entregado", desc = "Cumple un pedido del día.", done = s => s.ordersMet >= 1 });
                l.Add(new AchievementDef { key = "pedido50", name = "Distribuidora", desc = "Cumple 50 pedidos.", done = s => s.ordersMet >= 50 });
                l.Add(new AchievementDef { key = "cliente", name = "Primer cliente", desc = "Atiende a un cliente en la ventanilla.", done = s => s.customersServed >= 1 });
                l.Add(new AchievementDef { key = "reputacion", name = "Cinco estrellas", desc = "Llega a 4,5 de reputación.", done = s => s.reputation >= 4.5 });
                l.Add(new AchievementDef { key = "pergamino", name = "Arqueólogo", desc = "Encuentra un pergamino secreto.", done = s => s.scrolls.Count >= 1 });
                l.Add(new AchievementDef { key = "pergamino20", name = "Archivero", desc = "Encuentra 20 pergaminos.", done = s => s.scrolls.Count >= 20 });
                l.Add(new AchievementDef { key = "pergaminoTodos", name = "La última página", desc = "Encuentra los 40 pergaminos.", done = s => s.scrolls.Count >= 40 });
                l.Add(new AchievementDef { key = "edicion", name = "Nueva edición", desc = "Haz tu primer prestigio.", done = s => s.editions >= 1 });
                l.Add(new AchievementDef { key = "edicion10", name = "Décima edición", desc = "Haz 10 ediciones.", done = s => s.editions >= 10 });
                l.Add(new AchievementDef { key = "editorial", name = "Editorial", desc = "Funda tu editorial.", done = s => s.publishers >= 1 });
                l.Add(new AchievementDef { key = "nave1", name = "Otra nave", desc = "Completa una nave nueva.", done = s => s.hallsDone.Count >= 1 });
                l.Add(new AchievementDef { key = "naves4", name = "Biblioteca infinita", desc = "Completa las cuatro naves.", done = s => s.hallsDone.Count >= 4 });
                l.Add(new AchievementDef { key = "paloma", name = "Ornitólogo", desc = "Espanta a la paloma de la nave.", hidden = true, done = s => s.pigeons >= 1 });
                l.Add(new AchievementDef { key = "oscuras", name = "¿Hay alguien?", desc = "Quédate a oscuras junto a la montaña.", hidden = true, done = s => s.heardMountain });
                l.Add(new AchievementDef { key = "millon", name = "La frase un millón", desc = "Llega a la frase número 1.000.000.", hidden = true, done = s => s.phraseSeq >= 1000000 });
                l.Add(new AchievementDef { key = "voladora", name = "Atrapada", desc = "Atrapa la hoja dorada que vuela por la nave.", hidden = true, done = s => s.flyingCaught >= 1 });
                l.Add(new AchievementDef { key = "apagon", name = "Se hizo la luz", desc = "Vuelve a subir los plomos tras un apagón.", hidden = true, done = s => s.blackoutsFixed >= 1 });
                l.Add(new AchievementDef { key = "hacienda", name = "Al día con Hacienda", desc = "Pasa una inspección.", hidden = true, done = s => s.inspections >= 1 });
                l.Add(new AchievementDef { key = "escriba", name = "Hola, Escriba", desc = "Escribe una página en el PC.", done = s => s.pagesPC >= 1 });
                l.Add(new AchievementDef { key = "fondo", name = "El fondo", desc = "Vacía la montaña entera.", done = s => s.maxAct >= 7 });
                achievements = l;
                return l;
            }
        }
    }

    /// <summary>Naves de la tercera capa de prestigio (doc 6.3): cada una con sus reglas.</summary>
    public static partial class Halls
    {
        public static readonly string[] Names = { "La nave original", "La nave del barco", "La catedral", "La estación lunar", "El sótano inundado" };
        public static readonly string[] Rules =
        {
            "Sin reglas especiales.",
            "Las hojas se deslizan con el oleaje: los recogedores sacan el doble, las impresoras rinden un 30 % menos.",
            "Solo pergamino y caligrafía: lo escrito a mano y por mecanógrafos vale ×10, las máquinas grandes rinden la mitad.",
            "Baja gravedad: el papel flota. Recogedores ×3, saltas muy alto, las cintas rinden un 25 % menos.",
            "Agua hasta los tobillos: las impresoras rinden la mitad, la ventanilla ×4 (la gente pide que les sequen papeles).",
        };

        public static double PickerMult(int hall) => hall == 1 ? 2 : hall == 3 ? 3 : 1;

        public static double ProducerMult(int hall, ProducerDef p)
        {
            switch (hall)
            {
                case 1: return p.branch == "Impresión" ? 0.7 : 1;
                case 2: return p.key == "mecanografo" || p.key == "equipo" ? 10 : p.act >= 5 ? 0.5 : 1;
                case 3: return 0.75;
                case 4: return p.branch == "Impresión" ? 0.5 : p.branch == "Negocio" ? 4 : 1;
            }
            return 1;
        }
    }
}
