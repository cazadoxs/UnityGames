using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Zona con forma de caja que permite o prohíbe construir. Si en el BuildSystem activas
    /// "Exigir zona permitida", solo se podrá construir dentro de zonas de tipo Permitida.
    /// Las zonas Prohibidas bloquean siempre.
    /// </summary>
    [AddComponentMenu("Mecánicas/Construcción/Zona de construcción")]
    public class BuildZone : MonoBehaviour
    {
        public enum ZoneType
        {
            Allowed = 0,
            Forbidden = 1
        }

        private static readonly List<BuildZone> ActiveZones = new List<BuildZone>();
        public static IReadOnlyList<BuildZone> All => ActiveZones;

        [SerializeField] private ZoneType zoneType = ZoneType.Allowed;
        [Tooltip("Centro de la caja, en coordenadas locales.")]
        [SerializeField] private Vector3 center = new Vector3(0f, 1f, 0f);
        [Tooltip("Tamaño de la caja, en coordenadas locales.")]
        [SerializeField] private Vector3 size = new Vector3(5f, 2f, 5f);

        public ZoneType Type => zoneType;

        private void OnEnable()
        {
            if (!ActiveZones.Contains(this)) ActiveZones.Add(this);
        }

        private void OnDisable()
        {
            ActiveZones.Remove(this);
        }

        public bool Contains(Vector3 worldPoint)
        {
            Vector3 p = transform.InverseTransformPoint(worldPoint) - center;
            Vector3 h = size * 0.5f;
            return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
        }

        /// <summary>Comprueba un punto contra todas las zonas activas.</summary>
        public static bool IsPointAllowed(Vector3 worldPoint, bool requireAllowedZone)
        {
            bool insideAllowed = false;
            bool anyAllowed = false;
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                BuildZone z = ActiveZones[i];
                if (z.zoneType == ZoneType.Forbidden)
                {
                    if (z.Contains(worldPoint)) return false;
                }
                else
                {
                    anyAllowed = true;
                    if (z.Contains(worldPoint)) insideAllowed = true;
                }
            }
            if (!requireAllowedZone) return true;
            return anyAllowed && insideAllowed;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Color c = zoneType == ZoneType.Allowed ? new Color(0.2f, 1f, 0.3f, 1f) : new Color(1f, 0.25f, 0.2f, 1f);
            Gizmos.color = new Color(c.r, c.g, c.b, 0.12f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = c;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = old;
        }
    }
}
