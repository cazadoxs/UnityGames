using System.Collections.Generic;

namespace Imprenta
{
    /// <summary>
    /// Base de datos de frases (doc 4.4). Capa 1: frases curadas. Capa 2: plantillas con huecos.
    /// El orden se calcula con una permutación de Feistel sobre el índice, así que nunca se repite
    /// una frase y no hace falta guardar la lista de «ya usadas»: basta con la semilla y el contador.
    /// Primero salen todas las frases cortas (onboarding del acto 1) y luego el resto mezclado.
    /// </summary>
    public static class Phrases
    {
        static readonly string[] Curated =
        {
            // Cortas (< 40 caracteres)
            "Hoy empieza algo grande.",
            "Una hoja cada vez.",
            "El papel aguanta todo.",
            "Paso a paso se sube la montaña.",
            "Lo pequeño también cuenta.",
            "Respira y escribe.",
            "Cada letra es un ladrillo.",
            "Hoy vas a poder.",
            "Despacio, pero sin parar.",
            "El primer paso ya está dado.",
            "Tu esfuerzo deja huella.",
            "Mañana lo agradecerás.",
            "Haz ruido con tus teclas.",
            "La constancia gana.",
            "Nadie escribe por ti.",
            "Empieza donde estás.",
            "Sigue, que vas bien.",
            "La tinta no se rinde.",
            "Hoy toca brillar.",
            "Las ideas pesan poco.",
            "Un día más, una hoja más.",
            "El talento también se entrena.",
            "Hazlo con calma y con ganas.",
            "Lo difícil se hace fácil.",
            "Tienes más fuerza de la que crees.",
            "Escribe tu propio camino.",
            "Hoy ganas tú.",
            "Cree en la próxima línea.",
            "La montaña no es eterna.",
            "Que suene la campanilla.",
            "Cada error enseña algo.",
            "Ánimo, el ducto te espera.",
            "Grandes cosas, hojas pequeñas.",
            "Un folio limpio es una promesa.",
            "Nunca subestimes una coma.",
            "Lo mejor está por imprimir.",
            "Hasta el papel tiene sueños.",
            "Hoy no se arruga nadie.",
            "Tu ritmo es tu fuerza.",
            "Escribe como si nadie leyera.",
            "Sonríe, la máquina te ve.",
            "Tú puedes con este folio.",
            "El polvo no te detiene.",
            "Un poco cada día basta.",
            "Pon el alma en cada tecla.",
            // Medias (40-90)
            "No hace falta verlo todo para dar el siguiente paso.",
            "Las montañas se mueven hoja a hoja, nunca de golpe.",
            "Si te equivocas, respira hondo y vuelve a la tecla.",
            "La disciplina es recordar lo que quieres de verdad.",
            "Hoy es un buen día para terminar lo que empezaste ayer.",
            "Tu futuro se escribe a máquina, con paciencia y con ritmo.",
            "El éxito es la suma de pequeños esfuerzos repetidos.",
            "No cuentes los días; haz que cada día cuente una página.",
            "Lo que hoy parece una montaña mañana será un recuerdo.",
            "Escribe primero, corrige después y celebra siempre.",
            "La motivación te arranca; la costumbre te mantiene.",
            "Quien teclea con cariño nunca se queda sin papel.",
            "Cada frase que terminas es una pequeña victoria.",
            "Ningún folio se llenó solo, pero todos acabaron llenos.",
            "Ser constante es más importante que ser rápido.",
            "Las mejores ideas llegan cuando ya estás trabajando.",
            "El ruido de las teclas es la música del progreso.",
            "Si el camino es largo, al menos que sea entretenido.",
            "Confía en el proceso aunque la montaña no se mueva.",
            "Ya has llegado más lejos que ayer, y eso es mucho.",
            "Un mal día también se escribe, y luego se pasa la hoja.",
            "La perfección no existe, pero la próxima línea sí.",
            "El cansancio pasa; las páginas escritas se quedan.",
            "Haz hoy lo que tu yo de mañana te va a agradecer.",
            "Algún día alguien leerá esta hoja y sonreirá.",
            "Las grandes obras están hechas de líneas pequeñas.",
            "El talento abre la puerta, pero el trabajo la cruza.",
            "No esperes a tener ganas: las ganas llegan escribiendo.",
            "Tus manos saben más de lo que tu cabeza cree.",
            "Pequeños avances, repetidos mil veces, cambian todo.",
            "Si suena la campanilla, es que vas por buen camino.",
            "La montaña de papel solo asusta desde lejos.",
            "Es increíble lo que cabe en un solo folio bien escrito.",
            "Escribir despacio también es escribir.",
            "Cuando dudes, pulsa la siguiente tecla.",
            "Hoy la tinta está de tu parte, aprovéchala.",
            "Nadie recuerda los borradores, solo lo que terminaste.",
            "Una buena racha empieza con una sola hoja impecable.",
            // Largas (> 90) y algunas irónicas o absurdas
            "Dicen que una montaña de papel no se vacía en un día, pero nadie dijo que no pudieras intentarlo con estilo.",
            "Si un folio pesa cinco gramos, tu montaña pesa toneladas; por suerte tú solo tienes que levantar uno cada vez.",
            "La máquina de escribir no tiene tecla para rendirse, así que tendrás que seguir escribiendo como todos.",
            "Todo gran impresor empezó exactamente así: una silla vieja, una lámpara de flexo y demasiadas hojas.",
            "El ducto no juzga lo que escribes, pero el contador de la pared sí, y ese contador está deseando subir.",
            "Hay quien colecciona sellos y hay quien vacía naves llenas de papel; los dos necesitan mucha paciencia.",
            "No eres lento: estás dejando que cada letra tenga el tiempo que se merece antes de golpear el papel.",
            "Algún día las máquinas escribirán por ti, pero hoy la campanilla suena porque la has hecho sonar tú.",
            "Si te pierdes en la montaña, sigue el olor a tinta fresca y el sonido de las teclas hasta tu escritorio.",
            "Recuerda que incluso la rotativa más grande del mundo empezó con alguien tecleando una frase motivadora.",
            "Una hoja en blanco no es un problema; es solo una frase que todavía no se ha atrevido a salir.",
            "Las palomas de la nave te observan en silencio, y francamente parecen impresionadas con tu progreso.",
            "Hoy el papel cruje un poco más alegre de lo normal, y probablemente sea porque lo estás haciendo genial.",
            "El secreto de la productividad es sencillo: siéntate, coge una hoja, escribe y no mires la montaña.",
            "Si cada persona del mundo escribiera una hoja al día, la montaña desaparecería en un momento; tú empieza.",
        };

        /// <summary>Frases doradas (1 de cada 500): pistas de la historia de la montaña.</summary>
        static readonly string[] Golden =
        {
            "Si encuentras esto, la montaña ya te conoce. — A.",
            "Nadie trajo este papel. Creció aquí, noche tras noche.",
            "Las hojas del fondo están escritas. No las leas todavía.",
            "El vigilante sabe por qué se apagan las luces al revés.",
            "Mi imprenta tenía mil máquinas. Ahora solo queda una.",
            "Escribe hasta llegar al fondo. Allí está lo que perdí.",
        };

        // Capa 2: plantillas con huecos. {0}, {1}, {2} indexan los diccionarios de cada plantilla.
        struct Template
        {
            public string format;
            public string[][] slots;
        }

        static readonly string[] Verbos = { "ordenar", "empezar", "mejorar", "terminar", "cuidar", "imaginar", "construir", "reinventar", "celebrar", "escribir", "limpiar", "despertar", "sorprender", "multiplicar", "pulir", "abrazar", "organizar", "acelerar" };
        static readonly string[] Objetos = { "tu futuro", "tus ideas", "tu escritorio", "tus sueños", "esa montaña", "tu rutina", "tu mejor frase", "tus planes", "cada mañana", "tu pequeño taller", "tus metas", "esa idea loca", "tu propia historia", "la siguiente página", "tu forma de trabajar" };
        static readonly string[] Complementos = { "con calma", "sin miedo", "con una sonrisa", "paso a paso", "desde el principio", "a tu manera", "con mucha tinta", "antes del café", "aunque llueva", "sin mirar atrás", "con buena letra", "hoja a hoja" };
        static readonly string[] Sujetos = { "La paciencia", "La constancia", "La curiosidad", "La calma", "La valentía", "La práctica", "La ilusión", "La buena letra", "La tinta fresca", "La costumbre" };
        static readonly string[] Efectos = { "mueve montañas", "abre puertas", "llena folios", "vence al cansancio", "hace milagros", "gana carreras largas", "convierte borradores en obras", "nunca pasa de moda", "se nota en cada línea", "hace sonar la campanilla" };
        static readonly string[] Cuandos = { "Hoy", "Esta mañana", "Ahora mismo", "Antes de comer", "Esta semana", "Mientras suena la máquina", "En esta hoja", "Desde hoy" };
        static readonly string[] Acciones = { "vas a dar un paso enorme", "toca escribir sin prisa", "puedes con todo", "empiezas una racha", "se mueve la montaña", "nace una gran idea", "vas a sorprenderte", "es tu turno de brillar", "el papel está de tu lado" };
        static readonly string[] Cosas = { "Un folio", "Una línea", "Una tecla", "Una idea", "Una hoja limpia", "Un minuto", "Un buen café", "Una frase", "Un pequeño gesto", "Un error corregido" };
        static readonly string[] Resultados = { "puede cambiar el día", "vale más que mil planes", "es el principio de todo", "también es progreso", "acerca el final de la montaña", "merece una celebración", "pesa menos de lo que parece", "es suficiente por ahora" };
        static readonly string[] Adjetivos = { "imparable", "brillante", "tranquilo", "valiente", "constante", "creativo", "paciente", "certero", "incansable" };
        static readonly string[] Cierres = { "y la montaña lo sabe", "y se nota en cada hoja", "aunque todavía no lo creas", "y hoy lo vas a demostrar", "como una buena rotativa", "como la tinta en el papel" };

        static readonly Template[] Templates =
        {
            new Template { format = "Hoy es un buen día para {0} {1} {2}.", slots = new[] { Verbos, Objetos, Complementos } },
            new Template { format = "{0} {1}.", slots = new[] { Sujetos, Efectos } },
            new Template { format = "{0} {1}.", slots = new[] { Cuandos, Acciones } },
            new Template { format = "{0} {1}.", slots = new[] { Cosas, Resultados } },
            new Template { format = "Eres {0} {1}.", slots = new[] { Adjetivos, Cierres } },
            new Template { format = "Atrévete a {0} {1} {2}.", slots = new[] { Verbos, Objetos, Complementos } },
            new Template { format = "{0} {1}, {2}.", slots = new[] { Cuandos, Acciones, Complementos } },
            new Template { format = "No hay prisa: {0} {1} {2}.", slots = new[] { Verbos, Objetos, Complementos } },
            new Template { format = "{0} {1}, y tú eres {2}.", slots = new[] { Sujetos, Efectos, Adjetivos } },
        };

        static List<string> shortList, restCurated;
        static long templateTotal;
        static long[] templateOffsets;

        static void Init()
        {
            if (shortList != null) return;
            shortList = new List<string>();
            restCurated = new List<string>();
            foreach (var p in Curated) (p.Length < 40 ? shortList : restCurated).Add(p);
            templateOffsets = new long[Templates.Length + 1];
            long acc = 0;
            for (int i = 0; i < Templates.Length; i++)
            {
                templateOffsets[i] = acc;
                long c = 1;
                foreach (var s in Templates[i].slots) c *= s.Length;
                acc += c;
            }
            templateOffsets[Templates.Length] = acc;
            templateTotal = acc;
        }

        public static long TotalPhrases { get { Init(); return shortList.Count + restCurated.Count + templateTotal; } }

        /// <summary>Frase número <paramref name="seq"/> de la partida. Nunca se repite hasta agotar TotalPhrases.</summary>
        public static string Get(long seq, uint seed, int goldenCount, out bool golden)
        {
            Init();
            golden = seq > 30 && Hash((uint)seq ^ seed ^ 0xA5A5u) % 500 == 0;
            if (golden) return Golden[goldenCount % Golden.Length];

            long total = TotalPhrases;
            seq %= total;
            if (seq < shortList.Count)
                return shortList[(int)Permute(seq, shortList.Count, seed)];
            long rest = restCurated.Count + templateTotal;
            long i = Permute(seq - shortList.Count, rest, seed ^ 0x51ED27u);
            if (i < restCurated.Count) return restCurated[(int)i];
            return FromTemplate(i - restCurated.Count);
        }

        static string FromTemplate(long idx)
        {
            int t = 0;
            while (idx >= templateOffsets[t + 1]) t++;
            long local = idx - templateOffsets[t];
            var tpl = Templates[t];
            var args = new object[tpl.slots.Length];
            for (int s = 0; s < tpl.slots.Length; s++)
            {
                int n = tpl.slots[s].Length;
                args[s] = tpl.slots[s][local % n];
                local /= n;
            }
            return string.Format(tpl.format, args);
        }

        // ---- Permutación de Feistel con «cycle walking» para dominios que no son potencia de 2 ----
        public static long Permute(long i, long n, uint seed)
        {
            if (n <= 1) return 0;
            int bits = 2;
            while ((1L << bits) < n) bits++;
            if ((bits & 1) == 1) bits++;
            int half = bits / 2;
            long mask = (1L << half) - 1;
            long x = i;
            do { x = Feistel(x, half, mask, seed); } while (x >= n);
            return x;
        }

        static long Feistel(long x, int half, long mask, uint seed)
        {
            long l = x >> half, r = x & mask;
            for (int k = 0; k < 4; k++)
            {
                long nl = r;
                long nr = l ^ ((long)Hash((uint)r ^ seed ^ (uint)(k * 0x27D4EB2D)) & mask);
                l = nl; r = nr;
            }
            return (l << half) | r;
        }

        public static uint Hash(uint h)
        {
            h ^= h >> 16; h *= 0x7FEB352Du;
            h ^= h >> 15; h *= 0x846CA68Bu;
            h ^= h >> 16;
            return h;
        }
    }
}
