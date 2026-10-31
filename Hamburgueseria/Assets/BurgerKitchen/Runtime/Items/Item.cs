using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Objeto que se puede coger. Su comportamiento lo define su <see cref="ItemDefinition"/>.
    /// Normalmente se crea con <see cref="ItemFactory.Spawn"/>, pero también se puede colocar a mano en la escena.
    /// El GameObject raíz nunca se escala: el modelo vive en el hijo "Visual".
    /// </summary>
    [DisallowMultipleComponent]
    public class Item : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemDefinition definition;

        public ItemDefinition Definition => definition;
        public bool IsDirty { get; private set; }
        public IItemHolder Holder { get; private set; }
        public ItemContainer Container { get; private set; }
        public bool IsLoose => Holder == null;

        /// <summary>Se dispara cuando el objeto se transforma (se cocina, se quema...).</summary>
        public event Action<Item> DefinitionChanged;

        Rigidbody body;
        Collider[] colliders = new Collider[0];
        Transform visualRoot;
        bool initialized;

        void Awake()
        {
            if (definition != null) Initialize(definition);
        }

        public void Initialize(ItemDefinition def)
        {
            definition = def;
            if (!initialized)
            {
                initialized = true;
                body = GetComponent<Rigidbody>();
                if (body == null)
                {
                    body = gameObject.AddComponent<Rigidbody>();
                    body.mass = 0.3f;
                }
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            RebuildVisual();
            RefreshContainer();
            gameObject.name = "Item_" + definition.DisplayName;
        }

        /// <summary>Cambia lo que es el objeto, conservando su contenido.</summary>
        public void SetDefinition(ItemDefinition def)
        {
            if (def == null || def == definition) return;
            definition = def;
            RebuildVisual();
            RefreshContainer();
            gameObject.name = "Item_" + definition.DisplayName;
            DefinitionChanged?.Invoke(this);
        }

        void RefreshContainer()
        {
            if (Container == null) Container = GetComponent<ItemContainer>();
            if (definition.IsContainer && Container == null)
                Container = gameObject.AddComponent<ItemContainer>();
            if (Container != null) Container.Bind(this);
        }

        void RebuildVisual()
        {
            if (visualRoot == null)
            {
                var existing = transform.Find("Visual");
                visualRoot = existing != null ? existing : new GameObject("Visual").transform;
                visualRoot.SetParent(transform, false);
            }
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
                KitchenUtil.SafeDestroy(visualRoot.GetChild(i).gameObject);

            if (definition.visualPrefab != null)
            {
                var v = Instantiate(definition.visualPrefab, visualRoot, false);
                foreach (var c in v.GetComponentsInChildren<Collider>()) KitchenUtil.SafeDestroy(c);
            }
            else
            {
                KitchenUtil.CreatePlaceholder(definition.placeholderShape, definition.placeholderSize,
                    definition.placeholderColor, visualRoot);
            }
            FitCollider();
        }

        void FitCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                // Respeta colisionadores puestos a mano en el prefab.
                if (GetComponent<Collider>() != null) { CacheColliders(); return; }
                box = gameObject.AddComponent<BoxCollider>();
            }

            Vector3 size = definition.placeholderSize;
            Vector3 center = new Vector3(0f, size.y * 0.5f, 0f);
            if (definition.visualPrefab != null)
            {
                var renderers = visualRoot.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                    center = transform.InverseTransformPoint(b.center);
                    size = b.size;
                }
            }
            box.center = center;
            box.size = new Vector3(Mathf.Max(size.x, 0.05f), Mathf.Max(size.y, 0.05f), Mathf.Max(size.z, 0.05f));
            CacheColliders();
        }

        void CacheColliders() => colliders = GetComponents<Collider>();

        // ---------- Posesión ----------

        /// <summary>Engancha el objeto a un nuevo dueño (mano, hueco, contenedor). Avisa al dueño anterior.</summary>
        public void AttachTo(Transform parent, IItemHolder newHolder, Vector3 localPosition, Quaternion localRotation)
        {
            var old = Holder;
            Holder = newHolder;
            if (old != null && !ReferenceEquals(old, newHolder)) old.OnItemRemoved(this);

            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            SetPhysics(false);
        }

        /// <summary>Suelta el objeto al mundo con física. La comida se ensucia si así lo indica su definición.</summary>
        public void Drop(Vector3 velocity)
        {
            var old = Holder;
            Holder = null;
            if (old != null) old.OnItemRemoved(this);

            transform.SetParent(null, true);
            SetPhysics(true);
            body.linearVelocity = velocity;
            if (definition.getsDirtyWhenDropped) MarkDirty();
        }

        public void MarkDirty()
        {
            IsDirty = true;
            if (Container != null)
                foreach (var c in Container.Contents) c.MarkDirty();
        }

        /// <summary>Destruye el objeto (y su contenido) avisando a su dueño.</summary>
        public void Consume()
        {
            var old = Holder;
            Holder = null;
            if (old != null) old.OnItemRemoved(this);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            var old = Holder;
            Holder = null;
            if (old != null) old.OnItemRemoved(this);
        }

        void SetPhysics(bool free)
        {
            if (body != null)
            {
                if (!free) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = !free;
                body.detectCollisions = free;
            }
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = free;
        }

        // ---------- Consultas ----------

        public ItemSnapshot ToSnapshot()
        {
            if (Container == null || Container.Contents.Count == 0)
                return new ItemSnapshot(definition, null, IsDirty);

            var defs = new List<ItemDefinition>(Container.Contents.Count);
            bool dirty = IsDirty;
            foreach (var c in Container.Contents)
            {
                defs.Add(c.Definition);
                dirty |= c.IsDirty;
            }
            return new ItemSnapshot(definition, defs, dirty);
        }

        /// <summary>Nombre con su contenido, p. ej. "Papel de hamburguesa (Pan, Carne, Pan)".</summary>
        public string Describe()
        {
            string n = definition.DisplayName;
            if (IsDirty) n += " (sucio)";
            if (Container != null && Container.Contents.Count > 0)
            {
                var parts = new List<string>();
                foreach (var c in Container.Contents) parts.Add(c.Definition.DisplayName);
                n += " [" + string.Join(", ", parts) + "]";
            }
            return n;
        }

        // ---------- Interacción cuando está suelto en el mundo ----------

        public bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            if (!IsLoose) return false;
            prompt.Title = Describe();
            var held = ctx.Held;
            if (held == null) prompt.Primary = "Coger";
            else if (ItemTransfer.CanCombine(held, this)) prompt.Primary = ItemTransfer.DescribeCombine(held, this);
            else return false;
            return true;
        }

        public void Interact(InteractionContext ctx)
        {
            if (!IsLoose) return;
            var held = ctx.Held;
            if (held == null) ctx.Interactor.TryPickUp(this);
            else ItemTransfer.TryCombine(held, this);
        }

        public void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime) { }
        public void Cycle(InteractionContext ctx, int direction) { }
    }
}
