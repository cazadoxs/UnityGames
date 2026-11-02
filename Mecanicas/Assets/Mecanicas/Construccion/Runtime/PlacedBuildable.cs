using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Se añade automáticamente a todo lo que construye el <see cref="BuildSystem"/>. Guarda qué ficha
    /// lo creó y, si está en una cuadrícula, qué celdas ocupa. Libera las celdas al destruirse.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecánicas/Construcción/Objeto construido")]
    public class PlacedBuildable : MonoBehaviour
    {
        private static readonly List<PlacedBuildable> ActiveObjects = new List<PlacedBuildable>();

        /// <summary>Todos los objetos construidos que hay activos en la escena.</summary>
        public static IReadOnlyList<PlacedBuildable> All => ActiveObjects;

        /// <summary>Cuántos objetos construidos de una ficha hay ahora mismo (null = todos).</summary>
        public static int Count(BuildableDefinition definition)
        {
            if (definition == null) return ActiveObjects.Count;
            int n = 0;
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i].definition == definition) n++;
            }
            return n;
        }

        [SerializeField] private BuildableDefinition definition;
        [Tooltip("Si está desactivado, el modo demoler no puede quitar este objeto.")]
        [SerializeField] private bool canBeDemolished = true;

        public BuildableDefinition Definition => definition;
        public bool CanBeDemolished { get => canBeDemolished; set => canBeDemolished = value; }
        public PlacementMode Mode { get; private set; }
        public BuildGrid Grid { get; private set; }
        public CellArea Area { get; private set; }
        public int GridId { get; private set; }
        public bool IsOnGrid => Grid != null;

        public void Initialize(BuildableDefinition def, PlacementMode mode)
        {
            definition = def;
            Mode = mode;
        }

        internal void AttachToGrid(BuildGrid grid, CellArea area, int id)
        {
            Grid = grid;
            Area = area;
            GridId = id;
        }

        internal void DetachFromGrid()
        {
            Grid = null;
            GridId = 0;
        }

        private void OnEnable()
        {
            if (!ActiveObjects.Contains(this)) ActiveObjects.Add(this);
        }

        private void OnDisable()
        {
            ActiveObjects.Remove(this);
        }

        private void OnDestroy()
        {
            if (Grid != null) Grid.Unregister(this);
        }
    }
}
