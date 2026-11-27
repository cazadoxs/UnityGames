import bpy, sys, os, math
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]; src, dst, names = a[0], a[1], a[2:]
os.makedirs(dst, exist_ok=True)
for n in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(src, n + ".fbx"))
    sc = bpy.context.scene
    for vw, d in (("lado", Vector((1, 0, 0))), ("frente", Vector((0, -1, 0)))):
        cd = bpy.data.cameras.new("C"); cd.type = 'ORTHO'; cd.ortho_scale = 2.0
        cam = bpy.data.objects.new("C", cd); sc.collection.objects.link(cam)
        cam.location = Vector((0, -0.2, 0.9)) + d * 5
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        sc.camera = cam
        sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'
        sc.render.resolution_x = 400; sc.render.resolution_y = 400
        sc.render.filepath = os.path.join(dst, n + "_" + vw + ".png")
        bpy.ops.render.render(write_still=True)
    print("SIDE", n)
