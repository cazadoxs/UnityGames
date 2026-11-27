# Blender (5.x) en modo batch: atrezo del acto 1 para Imprenta Infinita.
# Uso: blender -b --python blender_props.py -- <carpeta_salida>
# Genera Silla.fbx, Flexo.fbx, FlexoPantalla.fbx, Margarita.fbx, Pistola.fbx y Papelera.fbx.
# Ejes: X = derecha, Y = fondo (en Unity, +Z), Z = arriba. 1 unidad = 1 m.
# Cada pieza se une por material; Unity asigna materiales por el nombre (Plastico, Tela, Metal, Cromo, Goma...).
import bpy, bmesh, sys, os, math
from mathutils import Vector, Matrix

outdir = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "."
os.makedirs(outdir, exist_ok=True)

MATS = {}

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATS.clear()

def mat(name):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        MATS[name] = m
    return MATS[name]

def finish(ob, mname, bevel=0.0, seg=3, angle=40, subsurf=0):
    ob.data.materials.append(mat(mname))
    if bevel > 0:
        b = ob.modifiers.new("Bisel", 'BEVEL')
        b.width = bevel; b.segments = seg; b.limit_method = 'ANGLE'; b.angle_limit = math.radians(35)
        b.harden_normals = True
    if subsurf:
        s = ob.modifiers.new("Sub", 'SUBSURF'); s.levels = subsurf; s.render_levels = subsurf
    wn = ob.modifiers.new("Normales", 'WEIGHTED_NORMAL'); wn.keep_sharp = True
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle))
    ob.select_set(False)
    return ob

def box(loc, size, m, rot=(0, 0, 0), bevel=0.003, seg=3):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=[math.radians(a) for a in rot])
    ob = bpy.context.active_object
    ob.scale = size
    bpy.ops.object.transform_apply(scale=True)
    return finish(ob, m, bevel, seg)

def cyl(loc, r, depth, m, rot=(0, 0, 0), verts=32, bevel=0.002, r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=[math.radians(a) for a in rot])
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=depth, location=loc, rotation=[math.radians(a) for a in rot])
    return finish(bpy.context.active_object, m, bevel)

def sphere(loc, r, m, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=r, location=loc)
    ob = bpy.context.active_object
    ob.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    return finish(ob, m, 0, angle=80)

def torus(loc, R, r, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=40, minor_segments=10, location=loc,
                                     rotation=[math.radians(a) for a in rot])
    return finish(bpy.context.active_object, m, 0, angle=80)

def tube(points, r, m, verts=12):
    """Tubo que sigue una polilínea (curva con bisel convertida a malla)."""
    cu = bpy.data.curves.new("Tubo", 'CURVE'); cu.dimensions = '3D'
    sp = cu.splines.new('POLY'); sp.points.add(len(points) - 1)
    for i, p in enumerate(points):
        sp.points[i].co = (*p, 1)
    cu.bevel_depth = r; cu.bevel_resolution = 3; cu.use_fill_caps = True
    ob = bpy.data.objects.new("Tubo", cu); bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.convert(target='MESH'); ob.select_set(False)
    return finish(ob, m, 0, angle=70)

def lathe(profile, m, steps=40, axis='Z', loc=(0, 0, 0), thick=0.0):
    """Gira un perfil (radio, altura) alrededor de Z. axis='Y' lo tumba para que la altura vaya hacia +Y."""
    me = bpy.data.meshes.new("Torno"); bm = bmesh.new()
    rings = []
    for k in range(steps):
        a = 2 * math.pi * k / steps
        rings.append([bm.verts.new((r * math.cos(a), r * math.sin(a), h)) for (r, h) in profile])
    for k in range(steps):
        a, b = rings[k], rings[(k + 1) % steps]
        for i in range(len(profile) - 1):
            bm.faces.new([a[i], b[i], b[i + 1], a[i + 1]])
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("Torno", me); bpy.context.scene.collection.objects.link(ob)
    if axis == 'Y':
        ob.rotation_euler = (math.radians(-90), 0, 0)
    ob.location = loc
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.select_set(False)
    if thick > 0:
        s = ob.modifiers.new("Grosor", 'SOLIDIFY'); s.thickness = thick
    return finish(ob, m, 0, angle=60)

def profile_x(pts, width, m, cx=0.0, bevel=0.006):
    """Extruye un perfil lateral (y, z) a lo ancho en X, centrado en cx."""
    me = bpy.data.meshes.new("Perfil"); bm = bmesh.new()
    hw = width / 2
    L = [bm.verts.new((cx - hw, y, z)) for (y, z) in pts]
    R = [bm.verts.new((cx + hw, y, z)) for (y, z) in pts]
    bm.faces.new(list(reversed(L))); bm.faces.new(R)
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        bm.faces.new([L[i], L[j], R[j], R[i]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("Perfil", me); bpy.context.scene.collection.objects.link(ob)
    return finish(ob, m, bevel)

def cut(target, cutter_ob):
    mod = target.modifiers.new("Corte", 'BOOLEAN'); mod.object = cutter_ob; mod.operation = 'DIFFERENCE'
    target.modifiers.move(target.modifiers.find("Corte"), 0)
    cutter_ob.hide_set(True); cutter_ob.hide_render = True
    return cutter_ob

def export(name):
    scene = bpy.context.scene
    cutters = [o for o in scene.objects if o.hide_get()]
    for ob in list(scene.objects):
        if ob.type != 'MESH' or ob in cutters:
            continue
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        for mod in list(ob.modifiers):
            try:
                bpy.ops.object.modifier_apply(modifier=mod.name)
            except Exception as e:
                print("modificador", mod.name, e)
        ob.select_set(False)
    for c in cutters:
        bpy.data.objects.remove(c, do_unlink=True)
    groups = {}
    for ob in scene.objects:
        if ob.type == 'MESH':
            groups.setdefault(ob.data.materials[0].name, []).append(ob)
    for mname, obs in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for ob in obs:
            ob.select_set(True)
        bpy.context.view_layer.objects.active = obs[0]
        if len(obs) > 1:
            bpy.ops.object.join()
        bpy.context.active_object.name = name + "_" + mname
    bpy.ops.object.select_all(action='SELECT')
    path = os.path.join(outdir, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='Z', axis_up='Y', bake_space_transform=True, mesh_smooth_type='FACE',
                             use_mesh_modifiers=True, add_leaf_bones=False, path_mode='STRIP')
    faces = sum(len(o.data.polygons) for o in scene.objects if o.type == 'MESH')
    print("EXPORTADO", name, "objetos:", len(scene.objects), "caras:", faces)

# ---------------------------------------------------------------- Silla de oficina de los 70
def silla():
    reset()
    # Base de cinco radios con ruedas
    for i in range(5):
        a = math.radians(i * 72 + 90)
        d = Vector((math.cos(a), math.sin(a), 0))
        tube([(0, 0, 0.09), (d.x * 0.15, d.y * 0.15, 0.085), (d.x * 0.29, d.y * 0.29, 0.07)], 0.018, "Metal")
        cyl((d.x * 0.29, d.y * 0.29, 0.05), 0.012, 0.04, "Metal")
        # rueda doble
        rot = (0, 90, math.degrees(a) + 90)
        cyl((d.x * 0.3, d.y * 0.3, 0.028), 0.028, 0.03, "Goma", rot=(90, 0, i * 72), verts=20, bevel=0.006)
    cyl((0, 0, 0.1), 0.045, 0.05, "Metal", bevel=0.008)
    cyl((0, 0, 0.24), 0.028, 0.26, "Cromo")                 # pistón de gas
    cyl((0, 0, 0.15), 0.036, 0.1, "Goma", verts=24, bevel=0.003)  # fuelle
    box((0, 0, 0.385), (0.22, 0.2, 0.03), "Metal", bevel=0.004)   # mecanismo
    box((0.13, 0.02, 0.37), (0.12, 0.012, 0.012), "Metal", rot=(0, -10, 0), bevel=0.003)  # palanca
    sphere((0.2, 0.02, 0.36), 0.016, "Plastico")
    # Asiento acolchado
    seat = box((0, 0, 0.45), (0.48, 0.46, 0.085), "Tela", bevel=0.03, seg=5)
    box((0, 0, 0.405), (0.46, 0.44, 0.012), "Plastico", bevel=0.004)  # concha
    # Brazo de respaldo y respaldo curvado
    tube([(0, -0.1, 0.39), (0, -0.27, 0.4), (0, -0.3, 0.55), (0, -0.3, 0.68)], 0.018, "Metal")
    me = bpy.data.meshes.new("Respaldo"); bm = bmesh.new()
    cols, rows = 12, 8
    grid = []
    for r in range(rows + 1):
        v = r / rows
        row = []
        for c in range(cols + 1):
            u = c / cols - 0.5
            x = u * 0.44
            y = -0.27 - 0.0 * v + 0.06 * (u * 2) ** 2  # curvatura horizontal
            z = 0.6 + 0.4 * v
            y -= 0.04 * math.sin(v * math.pi)            # leve curva lumbar
            row.append(bm.verts.new((x, y, z)))
        grid.append(row)
    for r in range(rows):
        for c in range(cols):
            bm.faces.new([grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]])
    bm.to_mesh(me); bm.free()
    back = bpy.data.objects.new("Respaldo", me); bpy.context.scene.collection.objects.link(back)
    s = back.modifiers.new("Grosor", 'SOLIDIFY'); s.thickness = 0.07; s.offset = 0
    b = back.modifiers.new("Bisel", 'BEVEL'); b.width = 0.022; b.segments = 4
    finish(back, "Tela", 0, angle=60)
    export("Silla")

# ---------------------------------------------------------------- Flexo de oficina
def flexo():
    reset()
    lathe([(0.0, 0.0), (0.14, 0.0), (0.155, 0.006), (0.15, 0.02), (0.12, 0.032), (0.04, 0.038), (0.03, 0.05), (0.0, 0.05)], "Pintura", steps=48)
    box((0.06, -0.06, 0.04), (0.03, 0.02, 0.01), "Goma", bevel=0.003)  # interruptor
    cyl((0, 0, 0.06), 0.022, 0.03, "Cromo", rot=(0, 90, 0))
    # Brazos dobles con muelles (el codo queda en (0,-0.1,0.4), la cabeza en (-0.18,-0.25,0.38))
    elbow = Vector((0.0, -0.1, 0.40))
    head = Vector((-0.18, -0.25, 0.38))
    for off in (-0.012, 0.012):
        tube([(off, 0, 0.06), (off, elbow.y, elbow.z)], 0.006, "Cromo")
        tube([(off + elbow.x, elbow.y, elbow.z), (head.x + off, head.y, head.z + 0.02)], 0.006, "Cromo")
    cyl(elbow, 0.018, 0.04, "Pintura", rot=(0, 90, 0), bevel=0.003)
    # Muelles: espirales finas al lado de cada brazo
    for (a, b) in (((0.03, -0.01, 0.1), (0.03, -0.085, 0.36)), ((0.03 + elbow.x, -0.12, 0.40), (head.x + 0.03, head.y + 0.03, head.z + 0.02))):
        A, B = Vector(a), Vector(b)
        pts = []
        n = 70
        axis = (B - A).normalized()
        side = axis.cross(Vector((1, 0, 0)))
        if side.length < 0.1:
            side = axis.cross(Vector((0, 1, 0)))
        side.normalize(); up = axis.cross(side)
        for k in range(n + 1):
            t = k / n
            ang = t * 2 * math.pi * 22
            p = A.lerp(B, t) + (side * math.cos(ang) + up * math.sin(ang)) * 0.006
            pts.append(tuple(p))
        tube(pts, 0.0012, "Cromo", verts=6)
    export("Flexo")

    # Pantalla aparte: la boca mira a +Y (en Unity, +Z), donde va el foco
    reset()
    lathe([(0.012, -0.07), (0.03, -0.068), (0.04, -0.05), (0.075, 0.0), (0.105, 0.05), (0.11, 0.06)], "Pintura", axis='Y', thick=0.0025)
    cyl((0, -0.075, 0), 0.016, 0.03, "Cromo", rot=(90, 0, 0))
    sphere((0, 0.005, 0), 0.04, "Bombilla", scale=(1, 1.3, 1))
    lathe([(0.0, -0.025), (0.025, -0.025), (0.024, -0.01)], "Cromo", axis='Y')
    export("FlexoPantalla")

# ---------------------------------------------------------------- Máquina electrónica de margarita (años 80)
def margarita():
    reset()
    cx = 0.12  # centro de la carcasa en X (la bandeja de hojas queda a la izquierda)
    W = 0.5
    base = 0.72
    pts = [(-0.23, base), (-0.23, base + 0.025), (-0.03, base + 0.09), (-0.01, base + 0.14), (0.2, base + 0.14), (0.21, base + 0.12), (0.21, base)]
    body = profile_x(pts, W, "Plastico", cx=cx, bevel=0.01)
    # Pozo del mecanismo de impresión, donde gira la rueda margarita
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.07, 0.06, base + 0.17))
    c = bpy.context.active_object; c.scale = (0.26, 0.15, 0.13); bpy.ops.object.transform_apply(scale=True)
    cut(body, c)
    # Tapa ahumada abatible detrás del pozo
    box((0.07, 0.115, base + 0.185), (0.27, 0.01, 0.09), "Cristal", rot=(25, 0, 0), bevel=0.004)
    # Rodillo, mandos y guía de papel
    cyl((cx, 0.14, 0.9), 0.03, W - 0.06, "Goma", rot=(0, 90, 0), verts=40, bevel=0.003)
    for sx in (-1, 1):
        cyl((cx + sx * (W / 2 + 0.005), 0.14, 0.9), 0.035, 0.03, "Teclas", rot=(0, 90, 0), verts=32, bevel=0.006)
        cyl((cx + sx * (W / 2 + 0.024), 0.14, 0.9), 0.012, 0.012, "Cromo", rot=(0, 90, 0), verts=16)
    box((cx, 0.185, 0.96), (W - 0.12, 0.006, 0.12), "Plastico", rot=(-12, 0, 0), bevel=0.002)  # soporte de papel
    box((cx, 0.11, 0.875), (W - 0.04, 0.02, 0.012), "Cromo", bevel=0.002)                   # escala
    # Teclado: teclas cuadradas de perfil cilíndrico, 5 filas
    rows = [13, 12, 12, 11, 1]
    for r, n in enumerate(rows):
        y = -0.205 + r * 0.034
        z = base + 0.03 + r * 0.0115 + 0.012
        for k in range(n):
            if r == 4:
                x, w = cx, 0.2
            else:
                x = cx - 0.2 + k * 0.031 + r * 0.008
                w = 0.026
            m = "Teclas" if (r == 0 and k in (0, n - 1)) or (r == 3 and k > 9) else "TeclasClaras"
            box((x, y, z), (w, 0.026, 0.016), m, rot=(-17, 0, 0), bevel=0.004)
    # Rejilla de ventilación trasera y pies
    for k in range(10):
        box((cx - 0.12 + k * 0.027, 0.212, base + 0.06), (0.01, 0.004, 0.07), "Goma", bevel=0.001)
    for sx in (-1, 1):
        for y in (-0.2, 0.18):
            cyl((cx + sx * 0.21, y, base + 0.003), 0.014, 0.008, "Goma")
    # Placa de modelo
    box((cx - 0.17, -0.03, base + 0.142), (0.08, 0.02, 0.002), "Cromo", bevel=0.0008)
    export("Margarita")

# ---------------------------------------------------------------- Pistola de succión (doc 4.6)
def pistola():
    reset()
    # Cuerpo: carcasa amarilla redondeada
    sphere((0, -0.02, 0), 0.06, "Plastico", scale=(0.85, 2.1, 1.0))
    box((0, 0.05, 0.0), (0.07, 0.1, 0.085), "Plastico", bevel=0.02, seg=4)
    # Depósito transparente encima
    cyl((0, -0.02, 0.07), 0.035, 0.12, "Cristal", rot=(90, 0, 0), verts=32, bevel=0.006)
    for y in (-0.08, 0.04):
        cyl((0, y, 0.07), 0.038, 0.01, "Goma", rot=(90, 0, 0), verts=32, bevel=0.002)
    # Boquilla de aluminio con boca abocinada
    lathe([(0.032, 0.08), (0.03, 0.12), (0.028, 0.2), (0.033, 0.25), (0.045, 0.27), (0.046, 0.28)], "Cromo", axis='Y', thick=0.003)
    torus((0, 0.282, 0), 0.044, 0.004, "Goma", rot=(90, 0, 0))
    # Empuñadura de goma y gatillo
    box((0, -0.06, -0.1), (0.04, 0.05, 0.13), "Goma", rot=(-15, 0, 0), bevel=0.012, seg=4)
    box((0, -0.01, -0.065), (0.012, 0.025, 0.045), "Teclas", rot=(-10, 0, 0), bevel=0.004)
    tube([(0, -0.03, -0.04), (0, 0.025, -0.045), (0, 0.03, -0.02)], 0.004, "Plastico")  # guardamonte
    # Manguera corrugada saliendo por detrás
    pts = []
    for k in range(40):
        t = k / 39
        pts.append((0.02 * math.sin(t * 3), -0.13 - 0.18 * t, -0.03 - 0.25 * t * t))
    tube(pts, 0.022, "Goma", verts=16)
    for k in range(0, 40, 2):
        t = k / 39
        p = (0.02 * math.sin(t * 3), -0.13 - 0.18 * t, -0.03 - 0.25 * t * t)
        torus(p, 0.022, 0.003, "Goma", rot=(90 - 60 * t, 0, 0))
    # Etiqueta de advertencia
    box((0.051, 0.05, 0.0), (0.002, 0.05, 0.03), "Etiqueta", bevel=0.0005)
    export("Pistola")

# ---------------------------------------------------------------- Papelera metálica perforada
def papelera():
    reset()
    lathe([(0.0, 0.002), (0.125, 0.002), (0.13, 0.008), (0.15, 0.36)], "Pintura", steps=48, thick=0.002)
    torus((0, 0, 0.36), 0.15, 0.006, "Pintura")
    torus((0, 0, 0.01), 0.13, 0.005, "Pintura")
    # Bolas de papel arrugado dentro
    import random
    random.seed(3)
    for k in range(7):
        a = random.random() * 6.28; d = random.random() * 0.07
        sp = sphere((math.cos(a) * d, math.sin(a) * d, 0.27 + random.random() * 0.06), 0.045 + random.random() * 0.02, "Papel")
        dm = sp.modifiers.new("Arruga", 'DISPLACE')
        tex = bpy.data.textures.new("Ruido%d" % k, 'VORONOI'); tex.noise_scale = 0.02
        dm.texture = tex; dm.strength = 0.03
    export("Papelera")

silla()
flexo()
margarita()
pistola()
papelera()
print("LISTO")
