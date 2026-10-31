using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    public enum StatusKind { Normal, Good, Warning, Danger }

    /// <summary>Etiqueta o barra flotante en el mundo (progreso de la plancha, paciencia de un cliente...).</summary>
    public struct WorldStatus
    {
        public Vector3 Position;
        public string Label;
        /// <summary>0-1, o negativo para no dibujar barra.</summary>
        public float Progress;
        public StatusKind Kind;

        public WorldStatus(Vector3 position, string label, float progress = -1f, StatusKind kind = StatusKind.Normal)
        {
            Position = position; Label = label; Progress = progress; Kind = kind;
        }
    }

    public interface IWorldStatusSource
    {
        void CollectStatus(List<WorldStatus> buffer);
    }

    /// <summary>Registro de fuentes de estado. La interfaz lo recorre cada frame.</summary>
    public static class WorldStatusRegistry
    {
        static readonly List<IWorldStatusSource> sources = new List<IWorldStatusSource>();
        public static IReadOnlyList<IWorldStatusSource> Sources => sources;

        public static void Register(IWorldStatusSource s) { if (!sources.Contains(s)) sources.Add(s); }
        public static void Unregister(IWorldStatusSource s) => sources.Remove(s);

        public static void Collect(List<WorldStatus> buffer)
        {
            buffer.Clear();
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                var s = sources[i];
                if (s == null || (s is Object o && o == null)) { sources.RemoveAt(i); continue; }
                s.CollectStatus(buffer);
            }
        }
    }
}
