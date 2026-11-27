# Modelos de la oficina (actos 2-4): personas, mesa, PC 286, impresoras, atril y mostrador.
# Uso: blender -b --python blender_oficina.py -- <carpeta_salida> [modelo ...]
import bpy, sys, os, math, random
from mathutils import Vector
outdir = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "."
only = sys.argv[sys.argv.index("--") + 2:] if "--" in sys.argv else []
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "blender_lib.py"), encoding="utf-8").read())
TURN = True

# Ejes: Blender X = Unity X, Blender Y = Unity Z (hacia delante), Blender Z = Unity Y (arriba).

# ---------------------------------------------------------------- Persona sentada (sin brazos)
def persona():
    reset()
    # piernas: cadera, rodilla, tobillo (sentado en una silla de 0.45 m)
    for sx in (-1, 1):
        skin([(sx * 0.085, -0.02, 0.53), (sx * 0.1, 0.38, 0.53), (sx * 0.1, 0.41, 0.11)], [(0, 1), (1, 2)], [0.09, 0.064, 0.048], "Pantalon")
    sphere((0, -0.04, 0.54), 0.16, "Pantalon", scale=(1.0, 0.75, 0.5))
    # torso con hombros
    v = [(0, -0.04, 0.55), (0, -0.045, 0.72), (0, -0.04, 0.9), (0, -0.03, 1.0),
         (-0.17, -0.025, 1.0), (0.17, -0.025, 1.0)]
    e = [(0, 1), (1, 2), (2, 3), (3, 4), (3, 5)]
    r = [(0.155, 0.1), (0.14, 0.095), (0.165, 0.105), (0.1, 0.08), 0.06, 0.06]
    skin(v, e, r, "Ropa")
    # cuello de la camisa
    torus((0, -0.03, 1.06), 0.05, 0.012, "Ropa")
    # zapatos
    for sx in (-1, 1):
        sphere((sx * 0.1, 0.47, 0.045), 0.06, "Zapatos", scale=(0.75, 1.6, 0.7))
        box((sx * 0.1, 0.47, 0.008), (0.09, 0.2, 0.016), "Goma", bevel=0.004)
    # cinturón
    torus((0, -0.03, 0.6), 0.135, 0.016, "Cuero")
    head((0, -0.015, 1.225))
    export("Persona")

# ---------------------------------------------------------------- Brazo (izquierdo; el derecho se espeja en Unity)
def persona_brazo():
    reset()
    v = [(0, 0, 0), (0, 0.02, -0.27), (0, 0.29, -0.27)]
    skin(v, [(0, 1), (1, 2)], [0.052, 0.043, 0.034], "Ropa")
    torus((0, 0.285, -0.27), 0.034, 0.008, "Ropa", rot=(90, 0, 0))
    hand((0, 0.35, -0.28))
    export("PersonaBrazo")

# ---------------------------------------------------------------- Persona de pie con carpeta
def persona_pie():
    reset()
    for sx in (-1, 1):
        skin([(sx * 0.085, 0, 0.95), (sx * 0.09, 0.015, 0.5), (sx * 0.09, 0, 0.09)], [(0, 1), (1, 2)], [0.09, 0.062, 0.046], "Pantalon")
    sphere((0, 0, 0.96), 0.16, "Pantalon", scale=(1.0, 0.68, 0.55))
    v = [(0, 0, 0.95), (0, -0.005, 1.15), (0, 0, 1.33), (0, 0, 1.43), (-0.19, 0, 1.42), (0.19, 0, 1.42),
         (-0.22, 0.02, 1.15), (-0.24, 0.05, 0.9), (0.22, 0.0, 1.16), (0.17, 0.2, 1.08)]
    e = [(0, 1), (1, 2), (2, 3), (3, 4), (3, 5), (4, 6), (6, 7), (5, 8), (8, 9)]
    r = [(0.155, 0.1), (0.145, 0.095), (0.17, 0.105), (0.1, 0.08), 0.06, 0.06, 0.045, 0.036, 0.045, 0.036]
    skin(v, e, r, "Ropa")
    torus((0, 0, 1.49), 0.05, 0.012, "Ropa")
    torus((0, 0, 1.0), 0.135, 0.016, "Cuero")
    hand((-0.245, 0.06, 0.84), s=1.0)
    hand((0.16, 0.25, 1.07), s=1.0)
    for sx in (-1, 1):
        sphere((sx * 0.09, 0.06, 0.045), 0.06, "Zapatos", scale=(0.75, 1.6, 0.7))
        box((sx * 0.09, 0.06, 0.008), (0.09, 0.2, 0.016), "Goma", bevel=0.004)
    head((0, 0.01, 1.66))
    export("PersonaDePie")

# ---------------------------------------------------------------- Mesa de oficina con cajonera
def mesa():
    reset()
    box((0, 0, 0.7425), (1.3, 0.7, 0.035), "Madera", bevel=0.004)
    box((0, 0, 0.7175), (1.3, 0.7, 0.003), "Metal", bevel=0)  # canto inferior oscuro
    for sx in (-1, 1):
        for sy in (-1, 1):
            box((sx * 0.6, sy * 0.3, 0.36), (0.04, 0.04, 0.72), "Metal", bevel=0.004)
            cyl((sx * 0.6, sy * 0.3, 0.006), 0.022, 0.012, "Goma")
        box((sx * 0.6, 0, 0.7), (0.035, 0.6, 0.03), "Metal", bevel=0.003)
    box((0, 0.3, 0.7), (1.16, 0.035, 0.03), "Metal", bevel=0.003)
    box((0, 0.3, 0.45), (1.16, 0.015, 0.4), "Metal", bevel=0.003)  # faldón
    # cajonera a la derecha
    box((0.42, 0.0, 0.36), (0.34, 0.6, 0.68), "Madera", bevel=0.004)
    for k, (z, h) in enumerate([(0.6, 0.14), (0.42, 0.18), (0.17, 0.3)]):
        box((0.42, -0.302, z), (0.32, 0.01, h - 0.012), "Madera", bevel=0.003)
        box((0.42, -0.312, z + h / 2 - 0.035), (0.12, 0.012, 0.012), "Cromo", bevel=0.003)
    # lapicero, grapadora y papeles
    cyl((-0.5, 0.22, 0.81), 0.035, 0.1, "PlasticoOscuro", verts=20)
    for k in range(4):
        a = k * 1.6
        cyl((-0.5 + 0.01 * math.cos(a), 0.22 + 0.01 * math.sin(a), 0.85), 0.004, 0.16, ["PinturaRoja", "PinturaAzul", "Tinta", "PinturaAmarilla"][k], verts=8, rot=(8 * math.cos(a), 8 * math.sin(a), 0))
    box((-0.32, 0.26, 0.775), (0.05, 0.14, 0.03), "PlasticoOscuro", bevel=0.006)
    box((0.35, 0.2, 0.762), (0.21, 0.297, 0.004), "Papel", rot=(0, 0, 8), bevel=0)
    box((0.36, 0.21, 0.766), (0.21, 0.297, 0.004), "Papel", rot=(0, 0, -5), bevel=0)
    export("Mesa")

# ---------------------------------------------------------------- PC 286: caja horizontal, monitor CRT y teclado
def pc286():
    reset()
    box((0, 0.12, 0.07), (0.42, 0.38, 0.14), "Plastico", bevel=0.006)
    for k in range(2):
        box((-0.08, -0.071, 0.095 - k * 0.042), (0.16, 0.006, 0.034), "PlasticoGris", bevel=0.002)
        box((-0.08, -0.075, 0.098 - k * 0.042), (0.12, 0.004, 0.006), "Tinta", bevel=0)
        box((-0.015, -0.075, 0.088 - k * 0.042), (0.012, 0.004, 0.01), "PlasticoOscuro", bevel=0.001)
    for k in range(7):
        box((0.1 + k * 0.012, -0.071, 0.07), (0.005, 0.004, 0.08), "PlasticoGris", bevel=0)
    box((0.17, -0.072, 0.03), (0.02, 0.006, 0.02), "PlasticoGris", bevel=0.002)
    box((0.17, -0.073, 0.055), (0.006, 0.004, 0.006), "LedVerde", bevel=0)
    box((0.17, -0.073, 0.065), (0.006, 0.004, 0.006), "Led", bevel=0)
    # pie giratorio
    cyl((0, 0.1, 0.145), 0.11, 0.012, "Plastico", verts=40)
    box((0, 0.1, 0.17), (0.12, 0.12, 0.04), "Plastico", bevel=0.008)
    # monitor: marco frontal, pantalla oscura hundida y culo del tubo
    bez = box((0, 0.003, 0.395), (0.37, 0.06, 0.31), "Plastico", bevel=0.012)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -0.03, 0.398))
    c = bpy.context.active_object; c.scale = (0.3, 0.05, 0.232)
    cut(bez, c)
    scr = box((0, -0.008, 0.398), (0.3, 0.02, 0.232), "PantallaOscura", bevel=0.012, seg=4)
    frustum(0.03, 0.31, 0, 0.395, 0.35, 0.29, 0.41, 0.2, 0.16, "Plastico", bevel=0.01)
    for k in range(8):
        box((0, 0.16 + k * 0.018, 0.538 - k * 0.0075), (0.16, 0.006, 0.004), "PlasticoOscuro", bevel=0)
    for sx in (0.1, 0.14):
        cyl((sx, -0.028, 0.258), 0.008, 0.012, "PlasticoGris", rot=(90, 0, 0), verts=16)
    box((-0.12, -0.028, 0.258), (0.05, 0.004, 0.01), "Etiqueta", bevel=0)
    # teclado inclinado
    profile_x([(-0.29, 0.0), (-0.11, 0.0), (-0.11, 0.036), (-0.29, 0.018)], 0.46, "Plastico", bevel=0.004)
    slope = math.degrees(math.atan2(0.018, 0.18))
    for row in range(5):
        y = -0.272 + row * 0.033
        z = 0.018 + (y + 0.29) / 0.18 * 0.018 + 0.007
        cols = 15 if row > 0 else 12
        for col in range(cols):
            x = -0.2 + col * 0.0285
            if row == 0 and col >= 4 and col < 11:  # barra espaciadora
                if col == 4:
                    box((-0.2 + 7 * 0.0285, y, z), (0.18, 0.024, 0.012), "TeclasClaras", rot=(slope, 0, 0), bevel=0.003)
                continue
            m = "PlasticoGris" if row == 4 or col == 0 or col == cols - 1 else "TeclasClaras"
            box((x, y, z), (0.024, 0.024, 0.012), m, rot=(slope, 0, 0), bevel=0.003)
    # cable en espiral simplificado
    tube([(0.0, -0.11, 0.02), (0.0, -0.08, 0.01), (0.05, -0.06, 0.005), (0.1, -0.06, 0.005), (0.15, -0.065, 0.02)], 0.004, "PlasticoOscuro")
    export("PC286")

# ---------------------------------------------------------------- Impresora matricial
def matricial():
    reset()
    box((0, 0, 0.055), (0.6, 0.36, 0.11), "Plastico", bevel=0.012)
    box((0, 0.11, 0.13), (0.6, 0.14, 0.05), "Plastico", bevel=0.01)
    box((0, -0.13, 0.12), (0.6, 0.1, 0.03), "Plastico", bevel=0.01)
    # hueco del carro con el rodillo
    box((0, 0.01, 0.11), (0.52, 0.12, 0.004), "PlasticoOscuro", bevel=0)
    cyl((0, 0.05, 0.13), 0.025, 0.5, "Goma", rot=(0, 90, 0))
    cyl((0, 0.0, 0.145), 0.006, 0.5, "Cromo", rot=(0, 90, 0), verts=12)
    for sx in (-1, 1):
        cyl((sx * 0.31, 0.05, 0.13), 0.032, 0.03, "PlasticoOscuro", rot=(0, 90, 0))
        box((sx * 0.2, 0.12, 0.165), (0.06, 0.1, 0.03), "PlasticoOscuro", bevel=0.005)
        for k in range(6):
            cyl((sx * 0.2, 0.08 + k * 0.016, 0.182), 0.003, 0.006, "Metal", verts=8)
    box((0, -0.03, 0.19), (0.52, 0.16, 0.005), "Cristal", rot=(-14, 0, 0), bevel=0.002)
    # panel de control
    box((0.22, -0.15, 0.137), (0.12, 0.05, 0.004), "PlasticoGris", bevel=0.001)
    for k, m in enumerate(["LedVerde", "Led", "LedVerde"]):
        box((0.18 + k * 0.025, -0.165, 0.14), (0.006, 0.006, 0.004), m, bevel=0)
        box((0.18 + k * 0.025, -0.14, 0.141), (0.016, 0.012, 0.006), "PlasticoOscuro", bevel=0.002)
    box((-0.2, -0.181, 0.06), (0.12, 0.004, 0.02), "Etiqueta", bevel=0)
    for k in range(10):
        box((-0.27 + k * 0.012, 0.181, 0.06), (0.005, 0.004, 0.06), "PlasticoOscuro", bevel=0)
    export("Matricial")

# ---------------------------------------------------------------- Impresora de inyección
def inyeccion():
    reset()
    box((0, 0.02, 0.07), (0.46, 0.3, 0.14), "PlasticoGris", bevel=0.018, seg=4)
    box((0, 0.05, 0.143), (0.42, 0.2, 0.008), "PlasticoOscuro", bevel=0.004)
    box((0, -0.06, 0.147), (0.36, 0.01, 0.004), "Tinta", bevel=0)
    # bandeja de entrada trasera con papel
    box((0, 0.2, 0.2), (0.26, 0.008, 0.22), "PlasticoOscuro", rot=(-22, 0, 0), bevel=0.003)
    box((0, 0.19, 0.205), (0.21, 0.006, 0.2), "Papel", rot=(-22, 0, 0), bevel=0)
    for sx in (-1, 1):
        box((sx * 0.115, 0.185, 0.2), (0.008, 0.02, 0.12), "PlasticoGris", rot=(-22, 0, 0), bevel=0.002)
    # bandeja de salida delantera
    box((0, -0.19, 0.03), (0.26, 0.12, 0.005), "PlasticoOscuro", bevel=0.002)
    box((0, -0.25, 0.04), (0.26, 0.005, 0.02), "PlasticoOscuro", bevel=0.002)
    box((0, -0.131, 0.05), (0.32, 0.006, 0.018), "Tinta", bevel=0)
    # botones
    cyl((0.19, -0.1, 0.142), 0.01, 0.006, "PlasticoOscuro", verts=16)
    box((0.19, -0.075, 0.142), (0.006, 0.006, 0.004), "LedVerde", bevel=0)
    cyl((0.16, -0.1, 0.142), 0.008, 0.006, "PlasticoOscuro", verts=16)
    box((-0.17, -0.131, 0.11), (0.08, 0.004, 0.015), "Etiqueta", bevel=0)
    export("Inyeccion")

# ---------------------------------------------------------------- Atril del catálogo
def atril():
    reset()
    lathe([(0.0, 0.0), (0.24, 0.0), (0.24, 0.02), (0.2, 0.04), (0.06, 0.07), (0.04, 0.09)], "Metal", steps=40)
    lathe([(0.0, 0.08), (0.04, 0.08), (0.035, 0.2), (0.05, 0.24), (0.03, 0.3), (0.028, 0.85), (0.045, 0.9), (0.032, 0.95), (0.03, 1.06), (0.0, 1.06)], "MaderaOscura", steps=24)
    box((0, 0, 1.1), (0.56, 0.43, 0.035), "MaderaOscura", rot=(25, 0, 0), bevel=0.006)
    box((0, -0.2, 1.03), (0.54, 0.03, 0.035), "MaderaOscura", rot=(25, 0, 0), bevel=0.005)
    box((0, 0.02, 1.06), (0.12, 0.12, 0.05), "MaderaOscura", bevel=0.006)
    export("Atril")

# ---------------------------------------------------------------- Mostrador de la ventanilla
def mostrador():
    reset()
    L = 9.0
    box((0, 0, 0.05), (L, 0.62, 0.1), "Metal", bevel=0.005)
    box((0, 0, 0.59), (L, 0.68, 0.98), "MaderaOscura", bevel=0.01)
    for k in range(int(L / 0.3)):
        box((-L / 2 + 0.15 + k * 0.3, -0.345, 0.59), (0.012, 0.01, 0.9), "Tinta", bevel=0)
    box((0, -0.02, 1.12), (L + 0.1, 0.8, 0.05), "PinturaBlanca", bevel=0.008)
    # mampara de cristal con postes y ventanillas
    for k in range(7):
        x = -L / 2 + k * (L / 6)
        box((x, 0.0, 1.72), (0.05, 0.05, 1.15), "MetalClaro", bevel=0.004)
    box((0, 0, 2.31), (L, 0.06, 0.05), "MetalClaro", bevel=0.004)
    for k in range(6):
        x = -L / 2 + (k + 0.5) * (L / 6)
        box((x, 0, 1.78), (L / 6 - 0.06, 0.012, 1.0), "Cristal", bevel=0)
        cyl((x, 0, 1.6), 0.08, 0.016, "MetalClaro", rot=(90, 0, 0), verts=32)
        box((x, -0.05, 1.16), (0.5, 0.16, 0.02), "MetalClaro", bevel=0.003)  # pasabandejas
    # caja registradora y papeles
    box((2.2, 0.15, 1.24), (0.4, 0.35, 0.18), "PlasticoGris", bevel=0.02)
    box((2.2, 0.08, 1.35), (0.3, 0.15, 0.05), "Teclas", rot=(20, 0, 0), bevel=0.01)
    box((2.2, 0.3, 1.42), (0.22, 0.03, 0.12), "PantallaOscura", bevel=0.006)
    box((-1.5, 0.15, 1.17), (0.21, 0.297, 0.06), "Papel", bevel=0.002)
    box((-0.9, 0.2, 1.2), (0.32, 0.24, 0.12), "Carton", bevel=0.004)
    export("Mostrador")

ALL = {"Persona": persona, "PersonaBrazo": persona_brazo, "PersonaDePie": persona_pie, "Mesa": mesa, "PC286": pc286,
       "Matricial": matricial, "Inyeccion": inyeccion, "Atril": atril, "Mostrador": mostrador}
for k, f in ALL.items():
    if not only or k in only:
        try:
            f()
        except Exception as ex:
            import traceback; traceback.print_exc()
            print("FALLO", k, ex)
print("LISTO")
