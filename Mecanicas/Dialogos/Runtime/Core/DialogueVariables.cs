using System;
using System.Collections.Generic;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Variables de sí/no ("flags") compartidas por todos los diálogos: "ya_saludo", "tiene_llave"...
    /// Las respuestas y líneas pueden activarlas o depender de ellas. Otros sistemas (inventario,
    /// puertas, eventos) pueden leerlas y escribirlas también.
    /// Se pierden al cerrar el juego: para guardarlas usa <see cref="Export"/> e <see cref="Import"/>.
    /// </summary>
    public static class DialogueVariables
    {
        static readonly HashSet<string> flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Se lanza al cambiar una variable (nombre, valor nuevo).</summary>
        public static event Action<string, bool> Changed;

        public static bool Get(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag.Trim());

        public static void Set(string flag, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;
            flag = flag.Trim();
            bool changed = value ? flags.Add(flag) : flags.Remove(flag);
            if (changed) Changed?.Invoke(flag, value);
        }

        public static void Clear(string flag) => Set(flag, false);

        /// <summary>Borra todas las variables (por ejemplo, al empezar partida nueva).</summary>
        public static void ResetAll() => flags.Clear();

        /// <summary>Lista de variables activas, para guardarla en tu sistema de guardado.</summary>
        public static string[] Export()
        {
            var arr = new string[flags.Count];
            flags.CopyTo(arr);
            return arr;
        }

        /// <summary>Restaura las variables guardadas con <see cref="Export"/>.</summary>
        public static void Import(IEnumerable<string> saved)
        {
            flags.Clear();
            if (saved == null) return;
            foreach (var f in saved) if (!string.IsNullOrWhiteSpace(f)) flags.Add(f.Trim());
        }

        /// <summary>Comprueba una pareja requerida / prohibida.</summary>
        public static bool Check(string requiredFlag, string forbiddenFlag) =>
            (string.IsNullOrWhiteSpace(requiredFlag) || Get(requiredFlag)) &&
            (string.IsNullOrWhiteSpace(forbiddenFlag) || !Get(forbiddenFlag));
    }
}
