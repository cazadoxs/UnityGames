using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>Personas realistas (MPFB, CC0) exportadas desde Tools/blender_humanos.py: HSent0..5 sentadas, HPie0..2 de pie.</summary>
    public static class Humans
    {
        public const int SeatedCount = 6, StandingCount = 3;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Map(string n)
        {
            if (!n.StartsWith("H_")) return null;
            if (!cache.TryGetValue(n, out var m))
            {
                m = Resources.Load<Material>("Humanos/Mats/" + n);
                cache[n] = m;
            }
            return m;
        }

        public static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = Find(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>Instancia el modelo, corrige escala y orientación (los pies miran a +Z del padre) y lo devuelve.</summary>
        public static GameObject Spawn(string model, Transform root, float headY)
        {
            var go = Build.Model(model, root, Vector3.zero, null, n => Map(n) ?? Build.Mat(new Color(0.5f, 0.5f, 0.5f), 0.3f));
            if (go == null) return null;
            var t = go.transform;
            var head = Find(t, "head");
            var bl = Find(t, "ball_l"); var br = Find(t, "ball_r");
            var fl = Find(t, "foot_l"); var fr = Find(t, "foot_r");
            if (head != null)
            {
                float hy = root.InverseTransformPoint(head.position).y;
                if (hy > 0.01f && Mathf.Abs(hy / headY - 1f) > 0.2f) t.localScale *= headY / hy;
            }
            if (bl != null && br != null && fl != null && fr != null)
            {
                var fwd = root.InverseTransformDirection((bl.position + br.position) - (fl.position + fr.position));
                fwd.y = 0;
                if (fwd.sqrMagnitude > 1e-6f)
                    t.localRotation = Quaternion.Euler(0, Vector3.SignedAngle(fwd, Vector3.forward, Vector3.up), 0) * t.localRotation;
            }
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = false;
                smr.skinnedMotionVectors = false;
            }
            var anim = go.GetComponent<Animator>();
            if (anim != null) Object.Destroy(anim);
            return go;
        }
    }

    /// <summary>Aplica a los huesos el giro local de unos "mandos" vacíos (los que anima Factory), expresado en el espacio de la persona.</summary>
    public class HumanRig : MonoBehaviour
    {
        Transform[] bones, proxies;
        Quaternion[] rest;

        public void Bind(Transform model, string[] boneNames, Transform[] drivers)
        {
            bones = new Transform[boneNames.Length]; rest = new Quaternion[boneNames.Length]; proxies = drivers;
            for (int i = 0; i < boneNames.Length; i++)
            {
                bones[i] = Humans.Find(model, boneNames[i]);
                if (bones[i] != null) rest[i] = bones[i].localRotation;
            }
        }

        void LateUpdate()
        {
            if (bones == null) return;
            var rr = transform.rotation; var inv = Quaternion.Inverse(rr);
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null || proxies[i] == null) continue;
                bones[i].localRotation = rest[i];
                bones[i].rotation = rr * proxies[i].localRotation * inv * bones[i].rotation;
            }
        }
    }
}
