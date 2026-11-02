using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>Ayudas para los materiales de la vista previa y para medir objetos.</summary>
    public static class BuildVisualUtility
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Crea un material transparente sin iluminación que acepta colores de vértice. Usa
        /// "Sprites/Default", que existe en Built-in, URP y HDRP y se incluye siempre en las builds.
        /// </summary>
        public static Material CreateTransparentMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material m = new Material(shader) { name = name };
            SetColor(m, color);
            m.renderQueue = 3000;
            return m;
        }

        public static void SetColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
        }

        /// <summary>
        /// Caja que envuelve las mallas y colisionadores de un objeto, en el espacio local de su raíz.
        /// Funciona con prefabs y con objetos desactivados.
        /// </summary>
        public static Bounds CalculateLocalBounds(GameObject root)
        {
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            bool has = false;
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);

            foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
            {
                if (box.isTrigger) continue;
                Encapsulate(ref result, ref has, toRoot * box.transform.localToWorldMatrix, new Bounds(box.center, box.size));
            }

            if (!has)
            {
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    Encapsulate(ref result, ref has, toRoot * mf.transform.localToWorldMatrix, mf.sharedMesh.bounds);
                }
                foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    Encapsulate(ref result, ref has, toRoot * smr.transform.localToWorldMatrix, smr.sharedMesh.bounds);
                }
            }

            if (!has)
            {
                foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
                {
                    if (c.isTrigger) continue;
                    if (c is SphereCollider s)
                    {
                        Encapsulate(ref result, ref has, toRoot * s.transform.localToWorldMatrix, new Bounds(s.center, Vector3.one * s.radius * 2f));
                    }
                    else if (c is CapsuleCollider cap)
                    {
                        Vector3 size = Vector3.one * cap.radius * 2f;
                        size[cap.direction] = Mathf.Max(cap.height, cap.radius * 2f);
                        Encapsulate(ref result, ref has, toRoot * cap.transform.localToWorldMatrix, new Bounds(cap.center, size));
                    }
                }
            }

            return has ? result : new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
        }

        private static void Encapsulate(ref Bounds result, ref bool has, Matrix4x4 m, Bounds b)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!has)
                {
                    result = new Bounds(p, Vector3.zero);
                    has = true;
                }
                else
                {
                    result.Encapsulate(p);
                }
            }
        }

        /// <summary>
        /// Quita a una copia todo lo que no sea visual (scripts, colisionadores, físicas, audio, luces…)
        /// para que la vista previa no haga nada en el juego. Debe llamarse con la copia desactivada.
        /// </summary>
        public static void StripToVisuals(GameObject root)
        {
            var pending = new List<Component>();
            for (int pass = 0; pass < 8; pass++)
            {
                pending.Clear();
                foreach (Component c in root.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || IsVisual(c)) continue;
                    pending.Add(c);
                }
                if (pending.Count == 0) return;

                pending.Sort((a, b) => RemovalOrder(a).CompareTo(RemovalOrder(b)));
                bool removedAny = false;
                foreach (Component c in pending)
                {
                    if (c == null || IsRequiredByOthers(c)) continue;
                    Object.DestroyImmediate(c);
                    removedAny = true;
                }
                if (!removedAny) break;
            }
        }

        // Evita el error "Can't remove X because Y depends on it": se quita primero lo que nadie necesita.
        private static bool IsRequiredByOthers(Component target)
        {
            System.Type targetType = target.GetType();
            foreach (Component other in target.GetComponents<Component>())
            {
                if (other == null || other == target || IsVisual(other)) continue;
                object[] attrs = other.GetType().GetCustomAttributes(typeof(RequireComponent), true);
                foreach (object o in attrs)
                {
                    var rc = (RequireComponent)o;
                    if (Matches(rc.m_Type0, targetType) || Matches(rc.m_Type1, targetType) || Matches(rc.m_Type2, targetType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool Matches(System.Type required, System.Type actual)
        {
            return required != null && required.IsAssignableFrom(actual);
        }

        private static int RemovalOrder(Component c)
        {
            if (c is Joint) return 0;
            if (c is MonoBehaviour) return 1;
            if (c is ParticleSystem) return 1;
            if (c is Rigidbody) return 3;
            return 2;
        }

        private static bool IsVisual(Component c)
        {
            return c is Transform
                || c is MeshFilter
                || c is MeshRenderer
                || c is SkinnedMeshRenderer
                || c is LODGroup;
        }
    }
}
