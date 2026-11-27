# Utilidades comunes para los scripts de Blender de Imprenta Infinita (se cargan con exec).
import bpy, bmesh, sys, os, math
from mathutils import Vector, Matrix

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
    if globals().get("TURN"):
        # Unity recibe +Y de Blender como -Z: se gira 180° para que el frente (+Y) mire a +Z en Unity
        rz = Matrix.Rotation(math.pi, 4, 'Z')
        for ob in scene.objects:
            if ob.parent is None:
                ob.matrix_world = rz @ ob.matrix_world
        bpy.context.view_layer.update()
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
    for ob in scene.objects:
        if ob.type != 'MESH' or len(ob.data.polygons) == 0:
            continue
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        try:
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.uv.cube_project(cube_size=1.0)
            bpy.ops.object.mode_set(mode='OBJECT')
        except Exception as e:
            print("uv", ob.name, e)
            bpy.ops.object.mode_set(mode='OBJECT')
        ob.select_set(False)
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


# ---------------------------------------------------------------- metabolas (personas)
def meta(name, parts, mname, res=0.022):
    """parts: lista de (tipo, (x,y,z), tamaño, rot_euler_grados) con tipo BALL, CAPSULE o ELLIPSOID."""
    mb = bpy.data.metaballs.new(name)
    mb.resolution = res; mb.render_resolution = res; mb.threshold = 0.6
    ob = bpy.data.objects.new(name, mb)
    bpy.context.scene.collection.objects.link(ob)
    for p in parts:
        kind, co, size = p[0], p[1], p[2]
        rot = p[3] if len(p) > 3 else (0, 0, 0)
        e = mb.elements.new()
        e.type = kind
        e.co = co
        e.radius = size[0] if isinstance(size, tuple) else size
        if kind == 'CAPSULE':
            e.size_x = size[1]
        if kind == 'ELLIPSOID':
            e.size_x, e.size_y, e.size_z = size[1], size[2], size[3]
        e.rotation = Vector(rot).to_track_quat('X', 'Z') if False else __import__('mathutils').Euler([math.radians(a) for a in rot]).to_quaternion()
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.convert(target='MESH')
    m = bpy.context.active_object
    m.name = name
    for o in list(bpy.context.scene.objects):
        if o.type == 'META':
            bpy.data.objects.remove(o, do_unlink=True)
    return finish(m, mname, 0, angle=80)

def capsule_between(a, b, r):
    """Cápsula de metabola de a a b (sobre X local)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    q = d.normalized().to_track_quat('X', 'Z')
    eul = q.to_euler()
    return ('CAPSULE', tuple((a + b) / 2), (r, d.length / 2), tuple(math.degrees(x) for x in eul))

# ---------------------------------------------------------------- esqueleto con modificador Skin (cuerpos)
def skin(verts, edges, radii, m, sub=2):
    me = bpy.data.meshes.new("Skin")
    me.from_pydata(verts, edges, [])
    me.update()
    ob = bpy.data.objects.new("Skin", me)
    bpy.context.scene.collection.objects.link(ob)
    sk = ob.modifiers.new("Skin", 'SKIN')
    sk.use_smooth_shade = True
    for i, r in enumerate(radii):
        me.skin_vertices[0].data[i].radius = r if isinstance(r, tuple) else (r, r)
    me.skin_vertices[0].data[0].use_root = True
    ss = ob.modifiers.new("Sub", 'SUBSURF'); ss.levels = sub; ss.render_levels = sub
    ob.data.materials.append(mat(m))
    return ob

def frustum(y0, y1, cx, cz0, w0, h0, cz1, w1, h1, m, bevel=0.004):
    """Tronco de pirámide a lo largo de Y (de la cara y0 de w0×h0 a la cara y1 de w1×h1)."""
    me = bpy.data.meshes.new("Tronco"); bm = bmesh.new()
    a = [bm.verts.new((cx + sx * w0 / 2, y0, cz0 + sz * h0 / 2)) for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    b = [bm.verts.new((cx + sx * w1 / 2, y1, cz1 + sz * h1 / 2)) for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    bm.faces.new(a); bm.faces.new(list(reversed(b)))
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new([a[i], b[i], b[j], a[j]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("Tronco", me); bpy.context.scene.collection.objects.link(ob)
    return finish(ob, m, bevel)

def head(c, hair=True, scale=1.0):
    """Cabeza humana sencilla centrada en c (cráneo), mirando a +Y."""
    x, y, z = c
    s = scale
    sphere((x, y, z), 0.1 * s, "Piel", scale=(0.84, 0.95, 1.08))
    sphere((x, y + 0.025 * s, z - 0.055 * s), 0.07 * s, "Piel", scale=(0.92, 1.0, 0.85))
    sphere((x, y + 0.093 * s, z - 0.012 * s), 0.02 * s, "Piel", scale=(0.8, 1.0, 1.5))
    for sx in (-1, 1):
        sphere((x + sx * 0.084 * s, y - 0.005 * s, z - 0.008 * s), 0.026 * s, "Piel", scale=(0.4, 0.9, 1.3))
        sphere((x + sx * 0.034 * s, y + 0.074 * s, z + 0.014 * s), 0.0125 * s, "Ojos")
        box((x + sx * 0.035 * s, y + 0.083 * s, z + 0.04 * s), (0.034 * s, 0.008 * s, 0.008 * s), "Pelo", rot=(0, sx * -8, 0), bevel=0.002)
    box((x, y + 0.075 * s, z - 0.062 * s), (0.04 * s, 0.01 * s, 0.006 * s), "Labios", bevel=0.002)
    if hair:
        h = sphere((x, y - 0.014 * s, z + 0.028 * s), 0.105 * s, "Pelo", scale=(0.9, 1.0, 0.98))
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y + 0.13 * s, z - 0.07 * s), rotation=(math.radians(28), 0, 0))
        cutter = bpy.context.active_object; cutter.scale = (0.4 * s, 0.2 * s, 0.22 * s)
        cut(h, cutter)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y + 0.02 * s, z - 0.13 * s))
        c2 = bpy.context.active_object; c2.scale = (0.4 * s, 0.3 * s, 0.12 * s)
        cut(h, c2)
    cyl((x, y - 0.01 * s, z - 0.13 * s), 0.043 * s, 0.1 * s, "Piel", verts=20)

def hand(c, rot=(0, 0, 0), s=1.0):
    """Mano relajada aproximada (palma + dedos juntos + pulgar), apuntando a +Y."""
    x, y, z = c
    sphere((x, y, z), 0.045 * s, "Piel", scale=(0.95, 1.0, 0.45))
    sphere((x, y + 0.05 * s, z - 0.006 * s), 0.035 * s, "Piel", scale=(1.15, 1.0, 0.4))
    sphere((x - 0.04 * s, y + 0.012 * s, z + 0.004 * s), 0.018 * s, "Piel", scale=(1.0, 1.7, 0.8))

def boxes(items, m):
    """Muchas cajas en una sola malla (rápido). items: lista de ((x,y,z), (sx,sy,sz), rotz_grados)."""
    me = bpy.data.meshes.new("Cajas"); bm = bmesh.new()
    for loc, size, rz in items:
        mtx = Matrix.Translation(loc) @ Matrix.Rotation(math.radians(rz), 4, 'Z') @ Matrix.Diagonal((*size, 1))
        bmesh.ops.create_cube(bm, size=1.0, matrix=mtx)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("Cajas", me); bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(mat(m))
    return ob
