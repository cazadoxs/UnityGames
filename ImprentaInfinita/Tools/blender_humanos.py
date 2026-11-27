# Humanos realistas con MPFB (MakeHuman para Blender, CC0). Uso:
#   blender -b --python blender_humanos.py -- <Assets/Resources>
# Crea personas sentadas (HSent0..5) y de pie (HPie0..2) con piel, ropa, pelo, cejas, ojos y esqueleto
# "game_engine", hornea la pose como pose de reposo y exporta FBX con piel + texturas + materiales.txt.
import bpy, sys, os, math, shutil, re
from mathutils import Vector, Matrix
from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.assetservice import AssetService
import bl_ext.blender_org.mpfb.services.targetservice as tsmod
TS = tsmod.TargetService

MARK = "HUMANOS_V1"
res = sys.argv[sys.argv.index("--") + 1]
models = os.path.join(res, "Models"); texdir = os.path.join(res, "Humanos")
os.makedirs(models, exist_ok=True); os.makedirs(texdir, exist_ok=True)
only = sys.argv[sys.argv.index("--") + 2:] if len(sys.argv) > sys.argv.index("--") + 2 else []

LISTS = {}
def find(kind, label):
    if kind not in LISTS:
        LISTS[kind] = AssetService.get_asset_list(kind) or {}
        print("ASSETS", kind, sorted(LISTS[kind].keys()))
    d = LISTS[kind]
    lo = label.lower()
    for k, v in d.items():
        if k.lower() == lo: return v["full_path"]
    for k, v in d.items():
        if lo in k.lower(): return v["full_path"]
    print("NO ENCONTRADO", kind, label)
    return None

# gender 0=mujer 1=hombre; age 0.5=25 años, 0.875=~50, 1=90
VARIANTS = [
    dict(name="HSent0", seated=True, gender=1.0, age=0.5, race="caucasian", weight=0.5, muscle=0.5, skin="young caucasian male",
         clothes=["male casualsuit01"], hair="short02", brows="eyebrow001", shoes="shoes01"),
    dict(name="HSent1", seated=True, gender=0.0, age=0.5, race="caucasian", weight=0.45, muscle=0.45, skin="young caucasian female",
         clothes=["female casualsuit01"], hair="bob02", brows="eyebrow009", shoes="shoes02"),
    dict(name="HSent2", seated=True, gender=1.0, age=0.8, race="african", weight=0.6, muscle=0.55, skin="middleage african male",
         clothes=["male worksuit01"], hair="short04", brows="eyebrow003", shoes="shoes03"),
    dict(name="HSent3", seated=True, gender=0.0, age=0.75, race="asian", weight=0.5, muscle=0.45, skin="middleage asian female",
         clothes=["female elegantsuit01"], hair="ponytail01", brows="eyebrow010", shoes="shoes04"),
    dict(name="HSent4", seated=True, gender=1.0, age=0.95, race="caucasian", weight=0.65, muscle=0.4, skin="old caucasian male",
         clothes=["male casualsuit04"], hair="short01", brows="eyebrow005", shoes="shoes05"),
    dict(name="HSent5", seated=True, gender=0.0, age=0.55, race="african", weight=0.5, muscle=0.5, skin="young african female",
         clothes=["female casualsuit02"], hair="afro01", brows="eyebrow011", shoes="shoes06"),
    dict(name="HPie0", seated=False, gender=1.0, age=0.7, race="caucasian", weight=0.55, muscle=0.55, skin="middleage caucasian male",
         clothes=["male elegantsuit01"], hair="short03", brows="eyebrow002", shoes="shoes01"),
    dict(name="HPie1", seated=False, gender=0.0, age=0.6, race="caucasian", weight=0.5, muscle=0.5, skin="young caucasian female",
         clothes=["female sportsuit01"], hair="long01", brows="eyebrow009", shoes="shoes02"),
    dict(name="HPie2", seated=False, gender=1.0, age=0.55, race="asian", weight=0.5, muscle=0.6, skin="young asian male",
         clothes=["male casualsuit06"], hair="short02", brows="eyebrow004", shoes="shoes03"),
]

def clear():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials):
        for b in list(coll):
            if b.users == 0: coll.remove(b)
    try: bpy.data.orphans_purge(do_recursive=True)
    except Exception as e: print("purge", e)

def upd(): bpy.context.view_layer.update()

def rot_about(rig, pb, axis, ang_deg, pivot=None):
    h = pivot if pivot is not None else pb.head.copy()
    R = Matrix.Rotation(math.radians(ang_deg), 4, Vector(axis).normalized())
    pb.matrix = Matrix.Translation(h) @ R @ Matrix.Translation(-h) @ pb.matrix
    upd()

def align(rig, pb, direction):
    d = Vector(direction).normalized()
    cur = (pb.tail - pb.head).normalized()
    q = cur.rotation_difference(d)
    h = pb.head.copy()
    pb.matrix = Matrix.Translation(h) @ q.to_matrix().to_4x4() @ Matrix.Translation(-h) @ pb.matrix
    upd()

def ik_arm(rig, side, target, pole):
    P = rig.pose.bones
    up, lo = P["upperarm_" + side], P["lowerarm_" + side]
    S = up.head.copy(); L1 = (up.tail - up.head).length; L2 = (lo.tail - lo.head).length
    T = Vector(target); dv = T - S; d = min(dv.length, (L1 + L2) * 0.995); u = dv.normalized()
    a = (L1 * L1 - L2 * L2 + d * d) / (2 * d); h = math.sqrt(max(L1 * L1 - a * a, 0))
    p = Vector(pole); p = (p - u * p.dot(u)).normalized()
    E = S + u * a + p * h
    align(rig, up, E - S)
    align(rig, lo, (S + u * d) - lo.head)

def pose_seated(rig):
    P = rig.pose.bones
    hip = (P["thigh_l"].head + P["thigh_r"].head) / 2
    pel = P["pelvis"]
    delta = Vector((0, 0.03, 0.55)) - hip
    pel.matrix = Matrix.Translation(delta) @ pel.matrix; upd()
    for s, sx in (("l", 1), ("r", -1)):
        align(rig, P["thigh_" + s], (0.07 * sx, -1, -0.04))
        calf = P["calf_" + s]; L = (calf.tail - calf.head).length
        kz = calf.head.z; dz = min(0.999, max(0.2, (kz - 0.085) / L)); dy = math.sqrt(1 - dz * dz)
        align(rig, calf, (0.02 * sx, -dy, -dz))
        align(rig, P["foot_" + s], (0.05 * sx, -0.55, -1.0))
        if "ball_" + s in P: align(rig, P["ball_" + s], (0.03 * sx, -1, 0))
    # ligera inclinación hacia la mesa
    rot_about(rig, P["spine_01"], (1, 0, 0), 4)
    rot_about(rig, P["spine_02"], (1, 0, 0), 3)
    for s, sx in (("l", 1), ("r", -1)):
        ik_arm(rig, s, (0.14 * sx, -0.40, 0.84), (0.6 * sx, 0.3, -1))
        hand = P["hand_" + s]
        align(rig, hand, (-0.12 * sx, -1, -0.32))
        for f in ("index", "middle", "ring", "pinky"):
            for i, ang in ((1, 18), (2, 22), (3, 14)):
                b = P.get("%s_0%d_%s" % (f, i, s))
                if b: rot_about(rig, b, (1, 0, 0), ang)
    rot_about(rig, P["head"], (1, 0, 0), 9)

def pose_standing(rig):
    P = rig.pose.bones
    for s, sx in (("l", 1), ("r", -1)):
        align(rig, P["upperarm_" + s], (0.16 * sx, 0.03, -1))
        if s == "r":
            align(rig, P["lowerarm_" + s], (-0.12, -1, 0.15))   # sujeta la carpeta delante
            align(rig, P["hand_" + s], (-0.2, -1, 0.1))
        else:
            align(rig, P["lowerarm_" + s], (0.06, -0.22, -1))
            align(rig, P["hand_" + s], (0.04, -0.15, -1))
        for f in ("index", "middle", "ring", "pinky"):
            for i, ang in ((1, 10), (2, 14), (3, 8)):
                b = P.get("%s_0%d_%s" % (f, i, s))
                if b: rot_about(rig, b, (0, sx, 0), ang)

def tex_of(mat):
    out = {"d": None, "n": None, "c": (1, 1, 1)}
    if not mat or not mat.use_nodes: return out
    imgs = [n for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image]
    for n in imgs:
        key = (n.name + " " + n.label + " " + n.image.name).lower()
        if "normal" in key and not out["n"]: out["n"] = n.image
        elif any(k in key for k in ("bump", "rough", "spec", "sss", "displ", "transmission", "metal")): continue
        elif not out["d"]: out["d"] = n.image
    for n in mat.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED' and not n.inputs["Base Color"].is_linked:
            c = n.inputs["Base Color"].default_value; out["c"] = (c[0], c[1], c[2])
    return out

def copy_img(img, suffix=""):
    p = bpy.path.abspath(img.filepath_raw or img.filepath)
    if not p or not os.path.isfile(p):
        print("SIN ARCHIVO", img.name, p); return None
    base = re.sub(r"[^A-Za-z0-9_]", "_", os.path.splitext(os.path.basename(p))[0])
    if suffix and not base.lower().endswith(suffix): base += suffix
    ext = os.path.splitext(p)[1].lower()
    dst = os.path.join(texdir, base + ext)
    if not os.path.exists(dst): shutil.copyfile(p, dst)
    return "Humanos/" + base

MANIFEST = []

def make(v):
    clear()
    md = TS.get_default_macro_info_dict()
    md["gender"] = v["gender"]; md["age"] = v["age"]; md["weight"] = v["weight"]; md["muscle"] = v["muscle"]
    md["race"] = {"asian": 0.05, "caucasian": 0.05, "african": 0.05}; md["race"][v["race"]] = 0.9
    bm = HumanService.create_human(macro_detail_dict=md)
    rig = HumanService.add_builtin_rig(bm, "game_engine")
    sk = find("skins", v["skin"])
    if not sk:
        fn = v["skin"].replace(" ", "_")
        sd = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(find("eyes", "low-poly")))), "skins", fn)
        if os.path.isdir(sd):
            mm = [f for f in os.listdir(sd) if f.endswith(".mhmat")]
            if mm: sk = os.path.join(sd, mm[0])
        print("SKIN", sk)
    if sk: HumanService.set_character_skin(sk, bm, skin_type="GAMEENGINE", material_instances=False)
    parts = [("eyes", "low-poly", "Eyes"), ("eyebrows", v["brows"], "Eyebrows"), ("eyelashes", "eyelashes01", "Eyelashes"),
             ("hair", v["hair"], "Hair"), ("clothes", v["shoes"], "Clothes")] + [("clothes", c, "Clothes") for c in v["clothes"]]
    kinds = {}
    for kind, label, atype in parts:
        p = find(kind, label)
        if not p: continue
        try:
            ob = HumanService.add_mhclo_asset(p, bm, asset_type=atype, subdiv_levels=0, material_type='MAKESKIN')
            if ob is not None: kinds[ob.name] = atype
        except Exception as e:
            print("FALLO ASSET", label, e)
    upd()
    (pose_seated if v["seated"] else pose_standing)(rig)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in meshes:
        if o.hide_get() or o.hide_viewport:
            bpy.data.objects.remove(o, do_unlink=True)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in meshes:
        bpy.ops.object.mode_set(mode='OBJECT') if bpy.context.object and bpy.context.object.mode != 'OBJECT' else None
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = o; o.select_set(True)
        if o.data.shape_keys:
            bpy.ops.object.shape_key_remove(all=True, apply_mix=True)
        for m in list(o.modifiers):
            try: bpy.ops.object.modifier_apply(modifier=m.name)
            except Exception as e:
                print("mod", o.name, m.name, e); o.modifiers.remove(m)
        if o == bm:
            dm = o.modifiers.new("Dec", 'DECIMATE'); dm.ratio = 0.6
            bpy.ops.object.modifier_apply(modifier="Dec")
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = rig; rig.select_set(True)
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    for o in meshes:
        if len(o.vertex_groups):
            am = o.modifiers.new("Armature", 'ARMATURE'); am.object = rig
        if o.parent != rig:
            mw = o.matrix_world.copy(); o.parent = rig; o.matrix_world = mw
    # materiales: un nombre único sin puntos + entrada en el manifiesto
    k = 0
    for o in meshes:
        atype = kinds.get(o.name, "Body" if o == bm else "Clothes")
        lname = o.name.lower()
        alpha = atype in ("Hair", "Eyebrows", "Eyelashes") or "hair" in lname or "brow" in lname or "lash" in lname
        for slot in o.material_slots:
            m = slot.material
            if m is None: continue
            t = tex_of(m)
            nm = "H_%s_%d" % (v["name"], k); k += 1
            m.name = nm
            d = copy_img(t["d"]) if t["d"] else ""
            n = copy_img(t["n"], "_normal") if t["n"] else ""
            kind = "piel" if o == bm else ("ojos" if atype == "Eyes" else ("pelo" if alpha else "ropa"))
            MANIFEST.append("%s|%s|%s|%d|%.3f,%.3f,%.3f|%s" % (nm, d or "", n or "", 1 if alpha else 0, t["c"][0], t["c"][1], t["c"][2], kind))
    P = rig.pose.bones
    W = lambda b: tuple(round(c, 2) for c in (rig.matrix_world @ P[b].head))
    print("BONES", v["name"], "rigscale", tuple(round(c, 3) for c in rig.matrix_world.to_scale()),
          {b: W(b) for b in ("pelvis", "thigh_l", "calf_l", "foot_l", "upperarm_l", "lowerarm_l", "hand_l", "middle_01_l", "head")})
    print("HUMANO", v["name"], "cabeza z=%.3f" % (rig.matrix_world @ P["head"].head).z,
          "ball_l", tuple(round(c, 3) for c in (rig.matrix_world @ P["ball_l"].head)),
          "caras", sum(len(o.data.polygons) for o in meshes), "mallas", [o.name for o in meshes])
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = rig
    path = os.path.join(models, v["name"] + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             path_mode='STRIP', apply_scale_options='FBX_SCALE_ALL', bake_anim=False, mesh_smooth_type='FACE',
                             use_armature_deform_only=True, primary_bone_axis='Y', secondary_bone_axis='X')
    print("EXPORTADO", path)

print(MARK)
for v in VARIANTS:
    if only and v["name"] not in only: continue
    try: make(v)
    except Exception as e:
        import traceback; traceback.print_exc(); print("FALLO VARIANTE", v["name"], e)
mf = os.path.join(texdir, "materiales.txt")
old = []
if only and os.path.exists(mf):
    keep = tuple("H_%s_" % n for n in only)
    old = [l.rstrip("\n") for l in open(mf, encoding="utf-8") if l.strip() and not l.startswith(keep)]
with open(mf, "w", encoding="utf-8") as f:
    f.write("\n".join(old + MANIFEST) + "\n")
print("MANIFIESTO", len(MANIFEST))
