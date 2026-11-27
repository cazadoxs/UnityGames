# Blender (4.x) en modo batch: carcasa de la máquina de escribir de varillas para Imprenta Infinita.
# Uso: blender -b --python blender_maquina.py -- <ruta_salida.fbx>
# Ejes: X = derecha, Y = fondo (lejos del mecanógrafo), Z = arriba. 1 unidad = 1 m.
# Las piezas móviles (teclas, varillas, carro, rodillo) las anima Unity por código; aquí va lo fijo.
import bpy, bmesh, sys, math
from mathutils import Vector

out = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "MaquinaCuerpo.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

def material(name, color, metal=0.0, rough=0.4):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Metallic"].default_value = metal
    b.inputs["Roughness"].default_value = rough
    return m

M_ESMALTE = material("Esmalte", (0.02, 0.02, 0.022), 0.0, 0.25)
M_CROMO = material("Cromo", (0.8, 0.8, 0.82), 1.0, 0.15)
M_GOMA = material("Goma", (0.01, 0.01, 0.01), 0.0, 0.8)
M_ORO = material("Oro", (0.75, 0.6, 0.25), 1.0, 0.3)

def finish(obj, mat, bevel=0.0, seg=3, angle=0.6):
    obj.data.materials.append(mat)
    if bevel > 0:
        mod = obj.modifiers.new("Bisel", 'BEVEL')
        mod.width = bevel; mod.segments = seg; mod.limit_method = 'ANGLE'; mod.angle_limit = math.radians(35)
        mod.harden_normals = True
    wn = obj.modifiers.new("Normales", 'WEIGHTED_NORMAL'); wn.keep_sharp = True
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth_by_angle(angle=angle)
    obj.select_set(False)
    return obj

def profile_body(name, pts, width, mat, bevel):
    """Extruye un perfil lateral (y, z) a lo ancho en X."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    hw = width / 2
    left = [bm.verts.new((-hw, y, z)) for (y, z) in pts]
    right = [bm.verts.new((hw, y, z)) for (y, z) in pts]
    bm.faces.new(list(reversed(left)))
    bm.faces.new(right)
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new([left[i], left[j], right[j], right[i]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    return finish(ob, mat, bevel)

def cyl(name, loc, r, depth, mat, rot=(0, 0, 0), verts=48, bevel=0.002):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.name = name
    return finish(ob, mat, bevel)

def box(name, loc, size, mat, rot=(0, 0, 0), bevel=0.002):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.name = name
    ob.scale = size
    bpy.ops.object.transform_apply(scale=True)
    return finish(ob, mat, bevel)

# --- Carcasa: perfil lateral clásico (frente bajo, teclado en pendiente, lomo alto detrás) ---
body_pts = [(-0.205, 0.0), (-0.205, 0.028), (-0.10, 0.062), (-0.072, 0.088), (0.02, 0.118),
            (0.085, 0.148), (0.14, 0.152), (0.165, 0.11), (0.165, 0.0)]
body = profile_body("Carcasa", body_pts, 0.40, M_ESMALTE, 0.008)

# Cesta de varillas: hueco semicircular en la tapa, donde se ven las varillas en abanico
bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.095, depth=0.2, location=(0, 0.03, 0.2))
cutter = bpy.context.active_object
cutter.scale = (1.0, 0.75, 1.0)
bm_mod = body.modifiers.new("Cesta", 'BOOLEAN'); bm_mod.object = cutter; bm_mod.operation = 'DIFFERENCE'
body.modifiers.move(body.modifiers.find("Cesta"), 0)
cutter.hide_set(True); cutter.hide_render = True

# Costados con un ligero abombado: placas laterales encima de la carcasa
for sx in (-1, 1):
    side = profile_body("Lateral_" + ("I" if sx < 0 else "D"),
                        [(-0.19, 0.004), (-0.19, 0.03), (-0.09, 0.066), (0.03, 0.12), (0.1, 0.15), (0.15, 0.15), (0.155, 0.004)],
                        0.012, M_ESMALTE, 0.004)
    side.location.x = sx * 0.203

# Tapas de las bobinas de cinta, con aro cromado
for sx in (-1, 1):
    cyl("Bobina_" + str(sx), (sx * 0.13, 0.07, 0.142), 0.046, 0.014, M_ESMALTE, bevel=0.004)
    cyl("Bobina_Aro_" + str(sx), (sx * 0.13, 0.07, 0.150), 0.040, 0.004, M_CROMO, bevel=0.0015)
    cyl("Bobina_Eje_" + str(sx), (sx * 0.13, 0.07, 0.154), 0.008, 0.008, M_CROMO, bevel=0.001)

# Escala de margen cromada delante del rodillo y guía del papel
cyl("Escala", (0, 0.075, 0.172), 0.006, 0.34, M_CROMO, rot=(0, math.pi / 2, 0), verts=24, bevel=0.001)
box("Escala_Regla", (0, 0.068, 0.165), (0.30, 0.012, 0.003), M_CROMO, rot=(math.radians(-20), 0, 0), bevel=0.0008)

# Placa del fabricante (ficticio) en el frente
box("Placa", (0, -0.15, 0.048), (0.15, 0.004, 0.022), M_ORO, rot=(math.radians(-72), 0, 0), bevel=0.001)

# Bandeja trasera del papel (curvada, apoyada en dos brazos)
me = bpy.data.meshes.new("BandejaPapel")
bm = bmesh.new()
rows = 10
vs = []
for i in range(rows + 1):
    t = i / rows
    y = 0.15 + 0.11 * t
    z = 0.19 + 0.16 * t + 0.03 * math.sin(t * math.pi)
    vs.append((bm.verts.new((-0.15, y, z)), bm.verts.new((0.15, y, z))))
for i in range(rows):
    bm.faces.new([vs[i][0], vs[i][1], vs[i + 1][1], vs[i + 1][0]])
bm.to_mesh(me); bm.free()
tray = bpy.data.objects.new("BandejaPapel", me); scene.collection.objects.link(tray)
sol = tray.modifiers.new("Grosor", 'SOLIDIFY'); sol.thickness = 0.003
finish(tray, M_ESMALTE, 0.0)
for sx in (-1, 1):
    box("Brazo_Bandeja_" + str(sx), (sx * 0.12, 0.17, 0.17), (0.006, 0.05, 0.01), M_CROMO, rot=(math.radians(40), 0, 0), bevel=0.001)

# Patas de goma
for sx in (-1, 1):
    for y in (-0.18, 0.14):
        cyl("Pata", (sx * 0.17, y, 0.004), 0.016, 0.01, M_GOMA, bevel=0.002)

# Palanca de tabulador y de cinta (detalles cromados a los lados)
for sx in (-1, 1):
    box("Palanca_Lateral", (sx * 0.212, -0.06, 0.07), (0.004, 0.05, 0.008), M_CROMO, rot=(math.radians(25), 0, 0), bevel=0.001)

# Aplicar modificadores y unir por material para que Unity tenga pocos objetos
bpy.ops.object.select_all(action='DESELECT')
for ob in list(scene.objects):
    if ob.type != 'MESH' or ob == cutter:
        continue
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    for mod in list(ob.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except Exception as e:
            print("modificador", mod.name, e)
    ob.select_set(False)
bpy.data.objects.remove(cutter, do_unlink=True)

groups = {}
for ob in scene.objects:
    if ob.type == 'MESH':
        groups.setdefault(ob.data.materials[0].name, []).append(ob)
for mat, obs in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    if len(obs) > 1:
        bpy.ops.object.join()
    bpy.context.active_object.name = "Maquina_" + mat

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         axis_forward='Z', axis_up='Y', bake_space_transform=True, mesh_smooth_type='FACE',
                         use_mesh_modifiers=True, add_leaf_bones=False, path_mode='STRIP')
tris = sum(len(o.data.polygons) for o in scene.objects if o.type == 'MESH')
print("EXPORTADO", out, "objetos:", len(scene.objects), "caras:", tris)
