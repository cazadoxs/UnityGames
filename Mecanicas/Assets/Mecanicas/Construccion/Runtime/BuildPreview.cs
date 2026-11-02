using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// "Fantasma" del objeto que se va a construir. Es una copia solo visual del prefab con el
    /// material verde (se puede) o rojo (no se puede), más una marca plana en el suelo bajo el objeto.
    /// La crea y la mueve el <see cref="BuildSystem"/>.
    /// </summary>
    public sealed class BuildPreview
    {
        private readonly GameObject _root;
        private readonly GameObject _model;
        private readonly GameObject _footprint;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _validArrays = new List<Material[]>();
        private readonly List<Material[]> _invalidArrays = new List<Material[]>();
        private readonly Material _footprintMaterial;
        private readonly Color _validFootprint;
        private readonly Color _invalidFootprint;
        private bool? _shownValid;

        public BuildableDefinition Definition { get; }
        /// <summary>Caja del objeto en su espacio local (para comprobar superposiciones).</summary>
        public Bounds LocalBounds { get; }
        public GameObject Root => _root;

        public BuildPreview(BuildableDefinition definition, Material validMaterial, Material invalidMaterial,
            bool showFootprint, Color validFootprint, Color invalidFootprint, int layer)
        {
            Definition = definition;
            _validFootprint = validFootprint;
            _invalidFootprint = invalidFootprint;

            _root = new GameObject("[Vista previa] " + definition.DisplayNameOrAssetName);
            _root.SetActive(false);

            _model = Object.Instantiate(definition.prefab, _root.transform);
            _model.name = definition.prefab.name;
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;

            LocalBounds = definition.overrideBounds
                ? definition.customBounds
                : BuildVisualUtility.CalculateLocalBounds(_model, _root.transform);

            BuildVisualUtility.StripToVisuals(_model);

            foreach (Renderer r in _model.GetComponentsInChildren<Renderer>(true))
            {
                int count = Mathf.Max(1, r.sharedMaterials.Length);
                var valid = new Material[count];
                var invalid = new Material[count];
                for (int i = 0; i < count; i++)
                {
                    valid[i] = validMaterial;
                    invalid[i] = invalidMaterial;
                }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                _renderers.Add(r);
                _validArrays.Add(valid);
                _invalidArrays.Add(invalid);
            }

            if (showFootprint)
            {
                _footprint = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _footprint.name = "Huella";
                Object.DestroyImmediate(_footprint.GetComponent<Collider>());
                _footprint.transform.SetParent(_root.transform, false);
                _footprintMaterial = BuildVisualUtility.CreateTransparentMaterial(validFootprint, "Huella vista previa");
                Renderer fr = _footprint.GetComponent<Renderer>();
                fr.sharedMaterial = _footprintMaterial;
                fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fr.receiveShadows = false;
                SetFootprintSize(new Vector2(LocalBounds.size.x, LocalBounds.size.z), new Vector3(LocalBounds.center.x, 0f, LocalBounds.center.z));
            }

            SetLayerRecursive(_root, layer);
        }

        public bool Visible
        {
            get => _root != null && _root.activeSelf;
            set
            {
                if (_root != null && _root.activeSelf != value) _root.SetActive(value);
            }
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            _root.transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Tamaño y centro locales de la marca del suelo.</summary>
        public void SetFootprintSize(Vector2 size, Vector3 localCenter)
        {
            if (_footprint == null) return;
            _footprint.transform.localPosition = localCenter + Vector3.up * 0.02f;
            _footprint.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _footprint.transform.localScale = new Vector3(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y), 1f);
        }

        public void SetFootprintVisible(bool visible)
        {
            if (_footprint != null && _footprint.activeSelf != visible) _footprint.SetActive(visible);
        }

        public void SetValid(bool valid)
        {
            if (_shownValid == valid) return;
            _shownValid = valid;
            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] != null) _renderers[i].sharedMaterials = valid ? _validArrays[i] : _invalidArrays[i];
            }
            if (_footprintMaterial != null) BuildVisualUtility.SetColor(_footprintMaterial, valid ? _validFootprint : _invalidFootprint);
        }

        public void Destroy()
        {
            if (_footprintMaterial != null) Object.Destroy(_footprintMaterial);
            if (_root != null) Object.Destroy(_root);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }
    }
}
