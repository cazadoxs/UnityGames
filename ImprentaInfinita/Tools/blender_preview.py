# Renderiza una vista previa de cada FBX de una carpeta (Workbench, rápido).
# Uso: blender -b --python blender_preview.py -- <carpeta_fbx> <carpeta_png> Nombre1 Nombre2 ...
import bpy, sys, os, math
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, dst, names = args[0], args[1], args[2:]
os.makedirs(dst, exist_ok=True)
COLORS = {"Plastico": (0.85, 0.78, 0.6), "Tela": (0.24, 0.3, 0.24), "Metal": (0.25, 0.25, 0.27), "Cromo": (0.8, 0.8, 0.82),
          "Goma": (0.05, 0.05, 0.05), "Pintura": (0.18, 0.36, 0.31), "Teclas": (0.25, 0.2, 0.17), "TeclasClaras": (0.8, 0.75, 0.65),
          "Cristal": (0.3, 0.3, 0.35), "Bombilla": (1, 1, 0.9), "Etiqueta": (0.9, 0.2, 0.1), "Papel": (0.95, 0.95, 0.92),
          "PlasticoGris": (0.55, 0.56, 0.58), "PlasticoOscuro": (0.12, 0.12, 0.13), "Piel": (0.79, 0.6, 0.48), "Ropa": (0.35, 0.42, 0.55),
          "Pantalon": (0.18, 0.2, 0.25), "Pelo": (0.17, 0.12, 0.09), "Zapatos": (0.08, 0.07, 0.06), "Ojos": (0.05, 0.04, 0.03),
          "Madera": (0.6, 0.42, 0.25), "MaderaOscura": (0.36, 0.24, 0.14), "PantallaOscura": (0.03, 0.06, 0.04), "PinturaBlanca": (0.9, 0.89, 0.86),
          "PinturaAmarilla": (0.91, 0.72, 0.14), "PinturaAzul": (0.18, 0.36, 0.54), "PinturaVerde": (0.31, 0.48, 0.29), "PinturaRoja": (0.64, 0.19, 0.16),
          "MetalClaro": (0.6, 0.63, 0.65), "Oro": (0.72, 0.59, 0.24), "Led": (1, 0.2, 0.2), "LedVerde": (0.2, 1, 0.3), "LedAzul": (0.3, 0.6, 1),
          "Cuero": (0.25, 0.15, 0.08), "Labios": (0.7, 0.4, 0.38), "Carton": (0.65, 0.51, 0.31), "Tinta": (0.06, 0.06, 0.06), "Esfera": (0.94, 0.91, 0.85), "Negro": (0.1, 0.1, 0.1)}
for n in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(src, n + ".fbx"))
    obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in obs:
        for m in o.data.materials:
            best = max((k for k in COLORS if m.name.startswith(k)), key=len, default=None)
            if best: m.diffuse_color = (*COLORS[best], 1)
    pts = [o.matrix_world @ Vector(c) for o in obs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    ctr = (lo + hi) / 2; size = (hi - lo).length
    cam_data = bpy.data.cameras.new("Cam"); cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    # Unity: +Z de Blender importado es arriba; vista desde delante-izquierda-arriba
    d = Vector((-0.8, 1.2, 0.5) if n.startswith('Persona') else (-0.8, -1.2, 0.7)).normalized()
    cam.location = ctr + d * size * 1.6
    cam.rotation_euler = (ctr - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.camera = cam
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
    sc.display.shading.show_shadows = True; sc.display.shading.show_cavity = True
    sc.render.resolution_x = 640; sc.render.resolution_y = 480
    sc.render.filepath = os.path.join(dst, n + ".png")
    bpy.ops.render.render(write_still=True)
    print("PREVIEW", n, tuple(round(v, 3) for v in lo), tuple(round(v, 3) for v in hi))
