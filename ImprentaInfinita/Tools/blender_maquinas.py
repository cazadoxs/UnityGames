# Máquinas de los actos 2-8, furgoneta y vestuario.
# Uso: blender -b --python blender_maquinas.py -- <carpeta_salida> [modelo ...]
import bpy, sys, os, math, random
from mathutils import Vector
outdir = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "."
only = sys.argv[sys.argv.index("--") + 2:] if "--" in sys.argv else []
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "blender_lib.py"), encoding="utf-8").read())
TURN = True

# ---------------------------------------------------------------- Fotocopiadora
def fotocopiadora():
    reset()
    box((0, 0, 0.04), (0.96, 0.66, 0.08), "PlasticoOscuro", bevel=0.01)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl((sx * 0.4, sy * 0.26, 0.03), 0.03, 0.04, "Goma", rot=(0, 90, 0))
    box((0, 0, 0.52), (1.0, 0.7, 0.88), "PinturaBlanca", bevel=0.02)
    # cajones de papel
    for k, z in enumerate([0.2, 0.38, 0.56]):
        box((-0.05, -0.352, z), (0.86, 0.006, 0.004), "Tinta", bevel=0)
        box((-0.05, -0.36, z + 0.085), (0.22, 0.02, 0.03), "PlasticoGris", bevel=0.006)
        box((0.3, -0.354, z + 0.085), (0.04, 0.004, 0.06), "PlasticoGris", bevel=0)
    box((0, -0.352, 0.82), (0.98, 0.006, 0.004), "Tinta", bevel=0)
    # marco del cristal y cristal con fondo oscuro
    box((0, 0, 0.99), (0.8, 0.5, 0.01), "Tinta", bevel=0)
    box((0, 0, 1.0), (0.8, 0.5, 0.008), "Cristal", bevel=0)
    box((0, -0.3, 0.99), (1.0, 0.1, 0.03), "PlasticoGris", bevel=0.006)
    box((0, 0.3, 0.99), (1.0, 0.1, 0.03), "PlasticoGris", bevel=0.006)
    for sx in (-1, 1):
        box((sx * 0.45, 0, 0.99), (0.1, 0.5, 0.03), "PlasticoGris", bevel=0.006)
    # tapa levantada
    box((0, 0.37, 1.3), (0.86, 0.05, 0.6), "PlasticoGris", rot=(-12, 0, 0), bevel=0.01)
    box((0, 0.34, 1.3), (0.78, 0.01, 0.52), "PinturaBlanca", rot=(-12, 0, 0), bevel=0)
    for sx in (-1, 1):
        cyl((sx * 0.3, 0.36, 1.01), 0.02, 0.08, "MetalClaro", rot=(0, 90, 0), verts=16)
    # panel de mando
    box((0.3, -0.39, 0.96), (0.36, 0.14, 0.05), "PlasticoGris", rot=(22, 0, 0), bevel=0.008)
    box((0.24, -0.41, 0.99), (0.14, 0.006, 0.06), "PantallaAzul", rot=(-68, 0, 0), bevel=0)
    for k in range(9):
        box((0.36 + (k % 3) * 0.025, -0.4 - (k // 3) * 0.005, 0.975 - (k // 3) * 0.018), (0.018, 0.018, 0.01), "TeclasClaras", rot=(22, 0, 0), bevel=0.002)
    box((0.44, -0.39, 0.99), (0.03, 0.03, 0.012), "LedVerde", rot=(22, 0, 0), bevel=0.003)
    # bandeja de salida con copias
    box((0.58, 0, 0.56), (0.18, 0.42, 0.01), "PlasticoGris", rot=(0, -10, 0), bevel=0.003)
    box((0.58, 0, 0.58), (0.15, 0.297, 0.03), "Papel", rot=(0, -10, 0), bevel=0.001)
    # bypass izquierdo
    box((-0.56, 0, 0.62), (0.12, 0.3, 0.008), "PlasticoGris", rot=(0, 20, 0), bevel=0.002)
    box((-0.3, -0.352, 0.9), (0.18, 0.004, 0.04), "Etiqueta", bevel=0)
    export("Fotocopiadora")

# ---------------------------------------------------------------- Duplicadora de disquetes y casetes
def duplicadora():
    reset()
    box((0, 0.02, 0.9), (0.6, 0.56, 1.8), "Metal", bevel=0.01)
    box((0, -0.27, 0.1), (0.6, 0.02, 0.2), "PlasticoOscuro", bevel=0.004)
    for k in range(10):
        box((-0.2 + k * 0.045, -0.282, 0.1), (0.02, 0.004, 0.14), "Tinta", bevel=0)
    xs = [-0.2 + i * 0.13 for i in range(4)]
    for row, z in enumerate([0.32, 0.55, 0.8, 1.05, 1.3]):
        floppy = row in (1, 3)
        for x in xs:
            box((x + 0.005, -0.29, z), (0.122, 0.02, 0.2), "PlasticoGris" if floppy else "PlasticoOscuro", bevel=0.004)
            if floppy:
                box((x + 0.005, -0.301, z - 0.03), (0.09, 0.004, 0.008), "Tinta", bevel=0)
                box((x + 0.04, -0.301, z - 0.07), (0.016, 0.004, 0.01), "PlasticoOscuro", bevel=0.001)
            else:
                box((x + 0.005, -0.3, z + 0.01), (0.1, 0.004, 0.065), "Cristal", bevel=0.002)
                box((x + 0.005, -0.296, z + 0.01), (0.09, 0.004, 0.055), "PlasticoOscuro", bevel=0)
                for sx in (-0.022, 0.022):
                    cyl((x + 0.005 + sx, -0.299, z + 0.012), 0.01, 0.004, "PinturaBlanca", rot=(90, 0, 0), verts=12)
                for b in range(4):
                    box((x - 0.03 + b * 0.022, -0.301, z - 0.06), (0.016, 0.006, 0.012), "PlasticoGris", bevel=0.001)
    # panel con pantalla verde
    box((0, -0.29, 1.58), (0.56, 0.02, 0.22), "PlasticoGris", bevel=0.004)
    box((-0.08, -0.302, 1.6), (0.3, 0.004, 0.08), "Pantalla", bevel=0)
    for k in range(4):
        cyl((0.12 + (k % 2) * 0.05, -0.302, 1.62 - (k // 2) * 0.05), 0.014, 0.008, "PinturaRoja" if k == 0 else "PlasticoOscuro", rot=(90, 0, 0), verts=16)
    # disquetes y casetes apilados encima
    random.seed(4)
    for k in range(9):
        box((-0.15 + random.random() * 0.1, -0.1 + random.random() * 0.05, 1.805 + k * 0.004), (0.09, 0.094, 0.0035), "PlasticoOscuro", rot=(0, 0, random.random() * 20 - 10), bevel=0)
    box((-0.13, -0.1, 1.845), (0.05, 0.03, 0.002), "MetalClaro", bevel=0)
    for k in range(5):
        box((0.15, 0.05 + k * 0.0, 1.81 + k * 0.016), (0.11, 0.07, 0.015), "Cristal" if k % 2 else "PlasticoOscuro", rot=(0, 0, k * 6), bevel=0.002)
    export("Duplicadora")

# ---------------------------------------------------------------- Plóter de gran formato
def plotter():
    reset()
    box((0, 0, 1.0), (2.3, 0.38, 0.28), "PlasticoOscuro", bevel=0.03)
    box((0, -0.03, 1.15), (2.1, 0.3, 0.04), "PlasticoGris", bevel=0.015)
    box((0, -0.192, 1.06), (2.1, 0.01, 0.03), "Cromo", bevel=0.003)
    box((0, -0.19, 0.89), (2.1, 0.02, 0.02), "Tinta", bevel=0)
    for sx in (-1, 1):
        box((sx * 1.17, 0, 1.0), (0.14, 0.42, 0.34), "PlasticoGris", bevel=0.02)
        box((sx * 1.0, 0.0, 0.45), (0.06, 0.06, 0.82), "Metal", bevel=0.005)
        box((sx * 1.0, 0.0, 0.03), (0.07, 0.62, 0.05), "Metal", bevel=0.006)
        for sy in (-1, 1):
            cyl((sx * 1.0, sy * 0.28, 0.02), 0.025, 0.03, "Goma", rot=(0, 90, 0), verts=16)
    box((0, 0, 0.22), (2.0, 0.04, 0.05), "Metal", bevel=0.005)
    box((1.17, -0.215, 1.08), (0.1, 0.01, 0.07), "PantallaAzul", bevel=0)
    for k in range(3):
        box((1.14 + k * 0.03, -0.215, 1.02), (0.02, 0.01, 0.02), "PlasticoOscuro", bevel=0.002)
    # rollo de papel detrás y cesta delante
    cyl((0, 0.2, 0.8), 0.08, 1.9, "Papel", rot=(0, 90, 0), verts=32)
    for sx in (-1, 1):
        box((sx * 0.96, 0.2, 0.8), (0.03, 0.05, 0.2), "Metal", bevel=0.003)
    tube([(-0.96, -0.1, 0.75), (-0.96, -0.45, 0.4), (-0.96, -0.2, 0.25)], 0.012, "Metal")
    tube([(0.96, -0.1, 0.75), (0.96, -0.45, 0.4), (0.96, -0.2, 0.25)], 0.012, "Metal")
    tube([(-0.96, -0.45, 0.4), (0.96, -0.45, 0.4)], 0.012, "Metal")
    box((0, -0.3, 0.36), (1.9, 0.28, 0.004), "Tela", rot=(-25, 0, 0), bevel=0)
    box((-0.9, -0.21, 1.05), (0.25, 0.004, 0.05), "Etiqueta", bevel=0)
    export("Plotter")

# ---------------------------------------------------------------- Laboratorio fotográfico (minilab)
def fotolab():
    reset()
    box((0, 0, 0.06), (2.36, 0.86, 0.12), "PlasticoOscuro", bevel=0.01)
    box((0, 0, 0.6), (2.4, 0.9, 0.96), "PinturaBlanca", bevel=0.02)
    box((0, -0.452, 0.72), (2.38, 0.01, 0.06), "PinturaAzul", bevel=0)
    for k in range(4):
        box((-0.9 + k * 0.6, -0.452, 0.38), (0.004, 0.006, 0.5), "Tinta", bevel=0)
        box((-0.95 + k * 0.6, -0.46, 0.55), (0.12, 0.02, 0.02), "PlasticoGris", bevel=0.005)
    box((0, 0.1, 1.1), (2.4, 0.7, 0.04), "PlasticoGris", bevel=0.01)
    box((-0.55, -0.45, 0.99), (1.1, 0.12, 0.04), "PlasticoOscuro", bevel=0.006)
    # tolva de revelado con tapas
    for k in range(3):
        box((-0.8 + k * 0.4, 0.15, 1.14), (0.36, 0.5, 0.04), "PlasticoGris", bevel=0.01)
        cyl((-0.8 + k * 0.4, -0.08, 1.17), 0.012, 0.15, "PlasticoOscuro", rot=(0, 90, 0), verts=12)
    # monitor en brazo y teclado
    box((0.7, 0.15, 1.13), (0.08, 0.08, 0.04), "Metal", bevel=0.005)
    box((0.7, 0.15, 1.28), (0.04, 0.04, 0.3), "Metal", bevel=0.004)
    box((0.7, 0.12, 1.55), (0.48, 0.05, 0.32), "PlasticoOscuro", rot=(-8, 0, 0), bevel=0.01)
    box((0.7, 0.094, 1.552), (0.42, 0.004, 0.26), "PantallaAzul", rot=(-8, 0, 0), bevel=0)
    box((0.7, -0.38, 1.13), (0.46, 0.17, 0.02), "PlasticoGris", bevel=0.004)
    for k in range(30):
        box((0.52 + (k % 10) * 0.04, -0.43 + (k // 10) * 0.04, 1.145), (0.032, 0.032, 0.01), "TeclasClaras", bevel=0.002)
    # bandejas de fotos a la derecha
    for k in range(4):
        z = 0.35 + k * 0.17
        box((1.3, 0, z), (0.2, 0.6, 0.01), "PlasticoGris", rot=(0, -12, 0), bevel=0.002)
        for j in range(3):
            box((1.3, -0.18 + j * 0.18, z + 0.012), (0.1, 0.15, 0.006), ["Cian", "Magenta", "PinturaAmarilla"][(j + k) % 3], rot=(0, -12, 0), bevel=0)
    box((-1.0, -0.455, 0.9), (0.3, 0.004, 0.05), "Etiqueta", bevel=0)
    export("Fotolab")

# ---------------------------------------------------------------- Prensa offset de cuatro cuerpos
def offset():
    reset()
    box((0, 0, 0.075), (1.5, 3.6, 0.15), "Metal", bevel=0.01)
    cols = ["Cian", "Magenta", "PinturaAmarilla", "Tinta"]
    for i in range(4):
        y = -1.2 + i * 0.8
        for sx in (-1, 1):
            box((sx * 0.64, y, 1.05), (0.08, 0.74, 1.8), "PinturaVerde", bevel=0.02)
            cyl((sx * 0.64, y, 1.95), 0.37, 0.08, "PinturaVerde", rot=(0, 90, 0), verts=40)
            for k in range(3):
                cyl((sx * 0.69, y - 0.2 + k * 0.2, 1.5 - k * 0.25), 0.05, 0.03, "MetalClaro", rot=(0, 90, 0), verts=20)
        cyl((0, y + 0.12, 0.95), 0.22, 1.2, "Cromo", rot=(0, 90, 0))
        cyl((0, y - 0.2, 1.15), 0.15, 1.2, "Goma", rot=(0, 90, 0))
        # tintero de cada color
        box((0, y + 0.1, 2.07), (1.2, 0.28, 0.16), "Metal", bevel=0.01)
        box((0, y + 0.1, 2.12), (1.1, 0.22, 0.06), cols[i], bevel=0.008)
        for k in range(3):
            cyl((0, y - 0.1 + k * 0.1, 1.85), 0.045, 1.18, "Goma" if k % 2 else "Cromo", rot=(0, 90, 0), verts=20)
        box((0, y, 0.45), (1.2, 0.74, 0.6), "PinturaVerde", bevel=0.01)
        box((0.0, y - 0.37, 0.45), (1.2, 0.02, 0.4), "Metal", bevel=0)
        box((0.63, y + 0.25, 0.9), (0.06, 0.12, 0.08), "PlasticoOscuro", bevel=0.01)
        box((0.66, y + 0.25, 0.92), (0.01, 0.03, 0.03), "LedVerde", bevel=0)
    # alimentador
    box((0, -1.95, 0.35), (1.2, 0.7, 0.7), "PinturaVerde", bevel=0.02)
    box((0, -1.95, 0.85), (0.9, 0.62, 0.3), "Papel", bevel=0.003)
    box((0, -1.95, 1.4), (1.3, 0.8, 0.2), "PinturaVerde", bevel=0.02)
    for sx in (-1, 1):
        box((sx * 0.6, -1.95, 1.0), (0.08, 0.08, 0.8), "PinturaVerde", bevel=0.01)
    # salida
    box((0, 1.95, 0.3), (1.2, 0.7, 0.6), "PinturaVerde", bevel=0.02)
    box((0, 1.95, 0.75), (0.9, 0.62, 0.3), "Papel", bevel=0.003)
    box((0, 1.85, 1.5), (1.3, 0.95, 0.25), "PinturaVerde", bevel=0.02)
    for sx in (-1, 1):
        box((sx * 0.6, 2.25, 1.05), (0.08, 0.08, 0.9), "PinturaVerde", bevel=0.01)
    # pasarela lateral con barandilla
    box((-1.05, 0, 0.6), (0.5, 3.4, 0.04), "MetalClaro", bevel=0.004)
    for k in range(6):
        box((-1.05, -1.6 + k * 0.64, 0.3), (0.05, 0.05, 0.58), "Metal", bevel=0.004)
        cyl((-1.28, -1.6 + k * 0.64, 1.1), 0.02, 1.0, "PinturaAmarilla", verts=12)
    tube([(-1.28, -1.6, 1.6), (-1.28, 1.6, 1.6)], 0.025, "PinturaAmarilla")
    box((-0.3, -0.75, 1.15), (0.003, 0.25, 0.08), "Etiqueta", bevel=0)
    # consola
    box((1.2, 1.4, 0.55), (0.6, 0.5, 1.1), "PlasticoGris", bevel=0.02)
    box((1.2, 1.3, 1.15), (0.55, 0.4, 0.04), "PlasticoOscuro", rot=(25, 0, 0), bevel=0.01)
    box((1.2, 1.38, 1.3), (0.4, 0.05, 0.25), "PantallaAzul", rot=(-10, 0, 0), bevel=0.006)
    export("Offset")

# ---------------------------------------------------------------- Rotativa de periódico
def rotativa():
    reset()
    box((0, 0, 0.1), (2.6, 11, 0.2), "Metal", bevel=0.01)
    for i in range(3):
        y = -3 + i * 3
        for sx in (-1, 1):
            box((sx * 1.12, y, 2.5), (0.16, 2.4, 4.6), "PinturaAzul", bevel=0.03)
            for k in range(3):
                cyl((sx * 1.21, y + (k - 1) * 0.7, 1.4 + k * 0.9), 0.14, 0.04, "MetalClaro", rot=(0, 90, 0), verts=24)
        box((0, y, 4.85), (2.4, 2.4, 0.2), "PinturaAzul", bevel=0.02)
        box((0, y, 0.55), (2.1, 2.2, 0.7), "PinturaAzul", bevel=0.02)
        cyl((0, y + 0.8, 2.2), 0.3, 2.0, "Goma", rot=(0, 90, 0))
        cyl((0, y - 0.8, 2.2), 0.3, 2.0, "Cromo", rot=(0, 90, 0))
        box((0.0, y, 4.98), (1.6, 1.4, 0.06), "Metal", bevel=0.004)
        # pasarela a media altura
        for sx in (-1, 1):
            box((sx * 1.55, y, 2.3), (0.7, 2.4, 0.05), "MetalClaro", bevel=0.004)
    for sx in (-1, 1):
        tube([(sx * 1.88, -4.2, 3.3), (sx * 1.88, 4.2, 3.3)], 0.03, "PinturaAmarilla")
        tube([(sx * 1.88, -4.2, 2.8), (sx * 1.88, 4.2, 2.8)], 0.02, "PinturaAmarilla")
        for k in range(8):
            cyl((sx * 1.88, -4.2 + k * 1.2, 2.8), 0.025, 1.0, "PinturaAmarilla", verts=12)
            box((sx * 1.55, -4.2 + k * 1.2, 1.15), (0.08, 0.08, 2.3), "Metal", bevel=0.005)
    # escalera
    for k in range(10):
        box((1.6, -4.45 - (9 - k) * 0.25, 0.23 * (k + 1)), (0.6, 0.24, 0.04), "MetalClaro", bevel=0.003)
    # banda de papel entre cuerpos
    pts = [(-0.0, -5.0, 1.5), (0, -3.6, 2.2), (0, -2.4, 3.1), (0, -0.6, 2.2), (0, 0.6, 3.1), (0, 2.4, 2.2), (0, 3.6, 3.1), (0, 4.3, 2.0)]
    for a, b in zip(pts, pts[1:]):
        a, b = Vector(a), Vector(b)
        d = b - a
        ang = math.degrees(math.atan2(d.z, d.y))
        box(tuple((a + b) / 2), (1.7, d.length, 0.004), "Papel", rot=(ang, 0, 0), bevel=0)
    # soporte de bobina
    for sx in (-1, 1):
        box((sx * 1.05, -5.0, 0.6), (0.12, 0.5, 1.2), "Metal", bevel=0.01)
    cyl((0, -5.0, 0.75), 0.08, 2.3, "Cromo", rot=(0, 90, 0))
    # plegadora y cinta de salida
    box((0, 4.6, 1.2), (1.9, 1.2, 2.2), "PinturaAzul", bevel=0.03)
    box((0, 4.55, 2.35), (1.6, 1.0, 0.1), "Metal", bevel=0.01)
    box((0, 5.3, 0.85), (0.8, 0.7, 0.06), "Goma", bevel=0.01)
    for k in range(6):
        box((0, 5.05 + k * 0.1, 0.9 + k * 0.012), (0.42, 0.3, 0.02), "Papel", rot=(0, 0, k * 3), bevel=0.002)
    box((0.96, 4.6, 1.6), (0.003, 0.6, 0.15), "Etiqueta", bevel=0)
    export("Rotativa")

# ---------------------------------------------------------------- IA redactora
def ia():
    reset()
    box((0, 0, 0.55), (1.2, 0.6, 1.1), "PlasticoOscuro", bevel=0.05, seg=5)
    box((0, -0.302, 0.08), (1.1, 0.01, 0.02), "LedAzul", bevel=0)
    box((0, -0.15, 1.12), (1.1, 0.3, 0.04), "Cristal", rot=(12, 0, 0), bevel=0.01)
    for k in range(18):
        box((-0.45 + (k % 9) * 0.11, -0.2 + (k // 9) * 0.1, 1.13), (0.08, 0.06, 0.003), "LedAzul", rot=(12, 0, 0), bevel=0) if k % 4 == 0 else None
    box((0, 0.05, 1.42), (1.08, 0.08, 0.66), "MetalClaro", bevel=0.02)
    box((0, 0.008, 1.42), (1.0, 0.01, 0.58), "PantallaAzul", bevel=0)
    cyl((0, 0.08, 1.9), 0.03, 0.4, "Cromo", verts=20)
    torus((0, 0.08, 2.1), 0.26, 0.025, "Cromo")
    for k in range(14):
        box((-0.55, -0.31, 0.25 + k * 0.05), (0.06, 0.005, 0.02), "Metal", bevel=0)
        box((0.55, -0.31, 0.25 + k * 0.05), (0.06, 0.005, 0.02), "Metal", bevel=0)
    export("IA")

# ---------------------------------------------------------------- Rack de servidores
def rack():
    reset()
    box((0, 0, 1.0), (0.62, 1.0, 2.0), "PlasticoOscuro", bevel=0.01)
    for sx in (-1, 1):
        box((sx * 0.29, -0.49, 1.0), (0.04, 0.03, 1.96), "Metal", bevel=0.003)
    box((0, 0, 2.01), (0.6, 0.98, 0.02), "Metal", bevel=0.003)
    for k in range(10):
        box((-0.1 + k * 0.04, 0.1, 2.025), (0.02, 0.6, 0.006), "Tinta", bevel=0)
    # servidores 2U
    for k in range(18):
        z = 0.12 + k * 0.1
        box((0, -0.475, z), (0.54, 0.05, 0.088), "MetalClaro" if k % 3 else "PlasticoGris", bevel=0.003)
        for h in range(10):
            box((0.12 + (h % 5) * 0.025, -0.501, z + (h // 5) * 0.03 - 0.015), (0.015, 0.004, 0.015), "Tinta", bevel=0)
        for sx in (-1, 1):
            box((sx * 0.25, -0.505, z), (0.012, 0.012, 0.06), "Cromo", bevel=0.002)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl((sx * 0.25, sy * 0.42, 0.0), 0.03, 0.04, "Goma", verts=16)
    # cables por detrás
    for k in range(6):
        tube([(-0.2 + k * 0.08, 0.5, 1.9), (-0.2 + k * 0.08, 0.56, 1.0), (-0.2 + k * 0.08, 0.52, 0.1)], 0.008, ["PinturaAzul", "PinturaAmarilla", "PinturaRoja"][k % 3])
    export("Rack")

# ---------------------------------------------------------------- Prensa cuántica
def cuantica():
    reset()
    lathe([(0, 0), (2.0, 0), (2.0, 0.5), (1.8, 0.6), (1.7, 0.8), (0, 0.8)], "MetalClaro", steps=64)
    torus((0, 0, 0.52), 1.95, 0.04, "LedAzul")
    torus((0, 0, 0.82), 1.4, 0.03, "Nucleo")
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        x, y = 1.55 * math.cos(a), 1.55 * math.sin(a)
        cyl((x, y, 3.0), 0.09, 4.4, "Cromo", verts=24)
    lathe([(1.4, 5.2), (1.75, 5.2), (1.75, 5.5), (1.4, 5.5)], "MetalClaro", steps=64)
    # candelabro dorado
    for k, (z, r) in enumerate([(5.0, 1.0), (4.65, 0.8), (4.3, 0.6), (3.95, 0.4)]):
        cyl((0, 0, z), r, 0.05, "Oro", verts=48)
        for j in range(6):
            a = j * math.pi / 3 + k * 0.3
            cyl((0.6 * r * math.cos(a), 0.6 * r * math.sin(a), z - 0.17), 0.018, 0.33, "Oro", verts=10)
    for j in range(8):
        a = j * math.pi / 4
        tube([(1.0 * math.cos(a), 1.0 * math.sin(a), 5.0), (0.75 * math.cos(a), 0.75 * math.sin(a), 4.3), (0.25 * math.cos(a), 0.25 * math.sin(a), 3.9)], 0.012, "Oro")
    for j in range(3):
        a = j * 2.1
        tube([(1.7 * math.cos(a), 1.7 * math.sin(a), 0.8), (1.6 * math.cos(a), 1.6 * math.sin(a), 2.0), (1.65 * math.cos(a + 0.3), 1.65 * math.sin(a + 0.3), 5.2)], 0.05, "PlasticoOscuro")
    export("Cuantica")

# ---------------------------------------------------------------- Brazo robótico de recogida
def brazo():
    reset()
    cyl((0, 0, 0.04), 0.34, 0.08, "Metal", verts=48)
    for k in range(8):
        a = k * math.pi / 4
        cyl((0.29 * math.cos(a), 0.29 * math.sin(a), 0.09), 0.018, 0.02, "Cromo", verts=12)
    lathe([(0, 0.08), (0.27, 0.08), (0.25, 0.18), (0.2, 0.26), (0.18, 0.34), (0, 0.34)], "PinturaAmarilla", steps=48)
    box((0, 0, 0.4), (0.3, 0.26, 0.12), "PinturaAmarilla", bevel=0.03)
    cyl((0, 0, 0.45), 0.14, 0.32, "Metal", rot=(0, 90, 0))
    cyl((-0.2, -0.05, 0.3), 0.07, 0.16, "Metal", rot=(0, 90, 0), verts=24)
    box((0.16, -0.12, 0.2), (0.06, 0.004, 0.04), "Etiqueta", bevel=0)
    export("BrazoBase")
    reset()
    box((0, 0, 0.6), (0.2, 0.18, 1.0), "PinturaAmarilla", bevel=0.04)
    for z in (0.0, 1.2):
        cyl((0, 0, z), 0.12, 0.26, "PinturaAmarilla", rot=(0, 90, 0))
        cyl((0, 0, z), 0.07, 0.3, "Metal", rot=(0, 90, 0), verts=24)
    cyl((0.15, 0, 1.05), 0.06, 0.14, "Metal", rot=(0, 90, 0), verts=24)
    tube([(0.12, 0.08, 0.1), (0.14, 0.12, 0.6), (0.12, 0.08, 1.1)], 0.015, "Goma")
    export("BrazoSegmento")
    reset()
    box((0, 0.6, 0), (0.15, 1.0, 0.15), "PinturaAmarilla", bevel=0.035)
    cyl((0, 0, 0), 0.1, 0.22, "PinturaAmarilla", rot=(0, 90, 0))
    cyl((0, 1.15, 0), 0.07, 0.14, "Metal", verts=24)
    cyl((0, 1.2, -0.03), 0.05, 0.06, "Cromo", verts=24)
    tube([(0.08, 0.05, 0.06), (0.1, 0.6, 0.09), (0.06, 1.1, 0.05)], 0.012, "Goma")
    export("BrazoSegmento2")

# ---------------------------------------------------------------- Garra de pulpo
def garra():
    reset()
    cyl((0, 0, -0.1), 0.06, 0.2, "Cromo", verts=24)
    cyl((0, 0, -0.25), 0.26, 0.18, "PinturaAmarilla", verts=40)
    cyl((0, 0, -0.36), 0.2, 0.06, "Metal", verts=40)
    for k in range(5):
        a = k * 2 * math.pi / 5
        c, s = math.cos(a), math.sin(a)
        pts = [(0.18 * c, 0.18 * s, -0.36), (0.38 * c, 0.38 * s, -0.5), (0.42 * c, 0.42 * s, -0.75), (0.28 * c, 0.28 * s, -0.98), (0.12 * c, 0.12 * s, -1.02)]
        tube(pts, 0.035, "Metal", verts=10)
        tube([(0.2 * c, 0.2 * s, -0.25), (0.33 * c, 0.33 * s, -0.48)], 0.022, "Cromo", verts=10)
    tube([(0.1, 0, -0.1), (0.25, 0.05, -0.2), (0.22, 0.1, -0.34)], 0.015, "Goma")
    export("Garra")

# ---------------------------------------------------------------- Excavadora compacta
def excavadora():
    reset()
    for sx in (-1, 1):
        box((sx * 0.85, 0, 0.32), (0.45, 2.6, 0.5), "Goma", bevel=0.18, seg=5)
        for k in range(5):
            cyl((sx * 0.85, -1.0 + k * 0.5, 0.2), 0.13, 0.47, "Metal", rot=(0, 90, 0), verts=24)
        cyl((sx * 0.85, 1.12, 0.34), 0.2, 0.47, "MetalClaro", rot=(0, 90, 0), verts=24)
        for k in range(26):
            box((sx * 0.85, -1.2 + k * 0.096, 0.07), (0.46, 0.04, 0.03), "Metal", bevel=0.004)
    box((0, 0, 0.45), (1.2, 1.6, 0.3), "Metal", bevel=0.02)
    cyl((0, 0, 0.65), 0.6, 0.12, "Metal", verts=40)
    # casa giratoria
    box((0, -0.3, 1.0), (1.9, 1.9, 0.6), "PinturaAmarilla", bevel=0.06)
    box((0, -1.25, 1.05), (1.9, 0.3, 0.7), "Metal", bevel=0.08)
    box((0.45, -0.5, 1.38), (0.9, 1.3, 0.18), "PinturaAmarilla", bevel=0.05)
    for k in range(6):
        box((0.45, -0.9 + k * 0.13, 1.48), (0.7, 0.05, 0.02), "Tinta", bevel=0)
    cyl((0.8, -0.9, 1.75), 0.05, 0.6, "Cromo", verts=16)
    # cabina
    box((-0.45, 0.1, 1.85), (0.95, 1.1, 1.1), "PinturaAmarilla", bevel=0.05)
    box((-0.45, 0.66, 1.95), (0.82, 0.02, 0.8), "Cristal", rot=(-6, 0, 0), bevel=0.01)
    box((-0.93, 0.15, 1.95), (0.02, 0.9, 0.75), "Cristal", bevel=0.01)
    box((0.03, 0.15, 1.95), (0.02, 0.9, 0.75), "Cristal", bevel=0.01)
    box((-0.45, 0.1, 2.42), (1.0, 1.15, 0.06), "PinturaAmarilla", bevel=0.02)
    box((-0.45, 0.0, 1.55), (0.5, 0.4, 0.15), "PlasticoOscuro", bevel=0.03)
    box((-0.45, -0.15, 1.75), (0.45, 0.1, 0.4), "PlasticoOscuro", bevel=0.03)
    box((-0.2, 0.67, 2.35), (0.12, 0.04, 0.06), "Luz", bevel=0.01)
    box((0.95, -0.3, 1.0), (0.004, 0.5, 0.12), "Etiqueta", bevel=0)
    export("Excavadora")

# ---------------------------------------------------------------- Furgoneta de reparto
def furgoneta():
    reset()
    prof = [(-2.1, 0.38), (-2.12, 2.15), (-2.0, 2.25), (1.25, 2.25), (1.45, 2.15), (2.05, 1.38), (2.5, 1.18), (2.62, 0.95), (2.62, 0.45), (2.5, 0.38)]
    body = profile_x(prof, 1.9, "Pintura", bevel=0.06)
    for y in (-1.35, 1.85):
        bpy.ops.mesh.primitive_cylinder_add(vertices=40, radius=0.44, depth=2.2, location=(0, y, 0.38), rotation=(0, math.radians(90), 0))
        cut(body, bpy.context.active_object)
    for sx in (-1, 1):
        for y in (-1.35, 1.85):
            cyl((sx * 0.82, y, 0.37), 0.36, 0.24, "Goma", rot=(0, 90, 0), verts=40, bevel=0.04)
            cyl((sx * 0.94, y, 0.37), 0.2, 0.02, "MetalClaro", rot=(0, 90, 0), verts=32)
            cyl((sx * 0.95, y, 0.37), 0.05, 0.02, "Cromo", rot=(0, 90, 0), verts=16)
        # ventanillas de la cabina y espejos
        box((sx * 0.951, 1.35, 1.75), (0.01, 0.7, 0.55), "Cristal", bevel=0.02)
        box((sx * 1.05, 1.62, 1.6), (0.08, 0.04, 0.2), "PlasticoOscuro", bevel=0.02)
        box((sx * 0.951, 0.95, 1.1), (0.006, 0.006, 1.6), "Tinta", bevel=0)
        box((sx * 0.951, 1.75, 1.0), (0.006, 0.006, 1.2), "Tinta", bevel=0)
        box((sx * 0.96, 1.25, 1.2), (0.03, 0.15, 0.03), "PlasticoOscuro", bevel=0.01)
        # faros y pilotos
        box((sx * 0.72, 2.6, 0.98), (0.28, 0.04, 0.16), "Luz", bevel=0.02)
        box((sx * 0.85, -2.13, 1.2), (0.12, 0.03, 0.3), "Led", bevel=0.01)
    # parabrisas sobre la pendiente del capó
    ang = math.degrees(math.atan2(2.15 - 1.38, 2.05 - 1.45))
    box((0, 1.75, 1.77), (1.7, 0.86, 0.02), "Cristal", rot=(ang - 90, 0, 0), bevel=0.02)
    box((0, 2.64, 0.6), (1.9, 0.1, 0.3), "PlasticoOscuro", bevel=0.04)
    box((0, 2.63, 0.98), (0.9, 0.03, 0.18), "PlasticoOscuro", bevel=0.01)
    box((0, -2.15, 0.5), (1.9, 0.1, 0.22), "PlasticoOscuro", bevel=0.04)
    box((0, -2.122, 1.3), (0.006, 0.006, 1.7), "Tinta", bevel=0)
    for sx in (-1, 1):
        box((sx * 0.1, -2.14, 1.3), (0.03, 0.03, 0.2), "Cromo", bevel=0.01)
    box((0, 0.1, 0.32), (1.6, 3.8, 0.12), "Metal", bevel=0.02)
    box((0, 2.68, 0.62), (0.4, 0.01, 0.1), "PinturaBlanca", bevel=0)
    export("Furgoneta")

# ---------------------------------------------------------------- Reloj de pared grande (mira a -Y)
def reloj_pared():
    reset()
    cyl((0, 0.03, 0), 1.0, 0.12, "PinturaBlanca", rot=(90, 0, 0), verts=64)
    torus((0, -0.03, 0), 1.03, 0.07, "Metal", rot=(90, 0, 0))
    cyl((0, -0.035, 0), 0.97, 0.01, "Esfera", rot=(90, 0, 0), verts=64)
    for i in range(60):
        a = i * math.pi / 30
        big = i % 5 == 0
        r = 0.85 if big else 0.88
        box((r * math.sin(a), -0.043, r * math.cos(a)), (0.045 if big else 0.012, 0.006, 0.16 if big else 0.05), "Negro", rot=(0, math.degrees(a), 0), bevel=0)
    cyl((0, -0.12, 0), 0.04, 0.02, "Negro", rot=(90, 0, 0), verts=24)
    cyl((0, -0.13, 0), 1.0, 0.01, "Cristal", rot=(90, 0, 0), verts=64)
    export("RelojPared")

# ---------------------------------------------------------------- Taquillas del vestuario (miran a -Y)
def taquillas():
    reset()
    for i in range(6):
        x = -1.5 + i * 0.6
        box((x, 0, 0.98), (0.58, 0.48, 1.8), "PinturaTaquilla", bevel=0.006)
        box((x, -0.245, 0.98), (0.54, 0.012, 1.74), "PinturaTaquilla", bevel=0.004)
        for k in range(5):
            box((x, -0.252, 1.6 + k * 0.03), (0.3, 0.006, 0.012), "Tinta", bevel=0)
            box((x, -0.252, 0.3 + k * 0.03), (0.3, 0.006, 0.012), "Tinta", bevel=0)
        box((x + 0.2, -0.26, 1.0), (0.03, 0.02, 0.14), "Cromo", bevel=0.005)
        box((x, -0.254, 1.82), (0.1, 0.004, 0.05), "PinturaBlanca", bevel=0)
        if i in (1, 4):
            torus((x + 0.2, -0.29, 0.9), 0.02, 0.005, "Cromo", rot=(90, 0, 0))
            box((x + 0.2, -0.29, 0.86), (0.04, 0.02, 0.045), "Oro", bevel=0.005)
    box((0, 0, 0.04), (3.6, 0.48, 0.08), "Tinta", bevel=0.004)
    # banco
    box((0, -0.65, 0.44), (2.6, 0.32, 0.04), "Madera", bevel=0.005)
    for sx in (-1, 1):
        box((sx * 1.1, -0.65, 0.21), (0.05, 0.28, 0.42), "Metal", bevel=0.004)
    box((-0.9, -0.62, 0.52), (0.4, 0.3, 0.12), "Tela", bevel=0.02)
    box((1.2, 0.0, 1.95), (0.5, 0.4, 0.3), "Carton", bevel=0.006)
    sphere((-1.2, 0.0, 1.95), 0.15, "PinturaAmarilla", scale=(1, 1.2, 0.75))
    export("Taquillas")

# ---------------------------------------------------------------- Reloj de fichar (mira a -Y)
def reloj_fichar():
    reset()
    box((0, 0.02, 1.45), (0.4, 0.04, 0.6), "Metal", bevel=0.006)
    box((0, -0.02, 1.45), (0.36, 0.2, 0.48), "PlasticoGris", bevel=0.03)
    cyl((0, -0.115, 1.55), 0.11, 0.02, "Esfera", rot=(90, 0, 0), verts=48)
    torus((0, -0.12, 1.55), 0.115, 0.012, "Cromo", rot=(90, 0, 0))
    for i in range(12):
        a = i * math.pi / 6
        box((0.09 * math.sin(a), -0.127, 1.55 + 0.09 * math.cos(a)), (0.008, 0.004, 0.02), "Negro", rot=(0, math.degrees(a), 0), bevel=0)
    box((0, -0.125, 1.57), (0.006, 0.004, 0.07), "Negro", rot=(0, 30, 0), bevel=0)
    box((0, -0.126, 1.55), (0.004, 0.004, 0.09), "Negro", rot=(0, -60, 0), bevel=0)
    cyl((0, -0.135, 1.55), 0.11, 0.008, "Cristal", rot=(90, 0, 0), verts=48)
    box((0, -0.12, 1.3), (0.16, 0.02, 0.05), "PlasticoOscuro", bevel=0.006)
    box((0, -0.13, 1.3), (0.12, 0.004, 0.012), "Tinta", bevel=0)
    box((0, -0.121, 1.4), (0.12, 0.004, 0.025), "Oro", bevel=0)
    cyl((0.14, -0.12, 1.3), 0.02, 0.03, "PinturaRoja", rot=(90, 0, 0), verts=16)
    export("RelojFichar")

# ---------------------------------------------------------------- Biblioteca de Babel (torre de estanterías)
def babel():
    reset()
    random.seed(7)
    lathe([(0, 0), (5.0, 0), (5.0, 0.4), (4.6, 0.6), (0, 0.6)], "Piedra", steps=64)
    books = {1: [], 2: [], 3: [], 4: []}
    shelves, cols = [], []
    for lvl in range(7):
        z0 = 0.6 + lvl * 2.6
        R = 4.3 - lvl * 0.28
        lathe([(R - 0.6, z0), (R - 0.05, z0), (R - 0.05, z0 + 2.6), (R - 0.6, z0 + 2.6)], "MaderaOscura", steps=48)
        lathe([(R - 0.1, z0 + 2.45), (R + 0.25, z0 + 2.5), (R + 0.25, z0 + 2.6), (R - 0.1, z0 + 2.6)], "Piedra", steps=48)
        n = 20
        w = 2 * math.pi * R / n
        for k in range(n):
            a = 2 * math.pi * k / n
            c, s_ = math.cos(a), math.sin(a)
            for sh in range(4):
                zz = z0 + 0.35 + sh * 0.55
                # libros sueltos de alturas distintas
                t = -w / 2 + 0.12
                while t < w / 2 - 0.12:
                    bw = random.uniform(0.04, 0.09); bh = random.uniform(0.3, 0.45)
                    px, py = -s_ * (t + bw / 2), c * (t + bw / 2)
                    books[random.randint(1, 4)].append((((R) * c + px, R * s_ + py, zz - 0.22 + bh / 2), (0.16, bw * 0.92, bh), math.degrees(a)))
                    t += bw
                shelves.append((((R + 0.02) * c, (R + 0.02) * s_, zz - 0.24), (0.2, w - 0.1, 0.04), math.degrees(a)))
            a2 = 2 * math.pi * (k + 0.5) / n
            cols.append((((R + 0.04) * math.cos(a2), (R + 0.04) * math.sin(a2), z0 + 1.3), (0.16, 0.16, 2.6), math.degrees(a2)))
        pts = []
        for k in range(25):
            a = 2 * math.pi * k / 24 + lvl * 0.7
            pts.append(((R + 0.5) * math.cos(a), (R + 0.5) * math.sin(a), z0 + 2.6 * k / 24))
        tube(pts, 0.05, "Oro", verts=8)
    for i in range(1, 5):
        boxes(books[i], "Libros%d" % i)
    boxes(shelves, "Madera")
    boxes(cols, "Madera")
    top = 0.6 + 7 * 2.6
    lathe([(0, top), (2.6, top), (2.0, top + 1.2), (0.3, top + 2.4), (0, top + 2.5)], "Oro", steps=32)
    sphere((0, 0, top + 0.9), 0.6, "Luz")
    export("Babel")


# ---------------------------------------------------------------- Camión de caja (cabina hacia +Y, caja abierta por detrás)
def camion():
    reset()
    L0, L1 = -3.9, 0.55          # caja de carga
    W = 2.44
    # chasis y largueros
    box((0, -1.0, 0.72), (0.9, 7.4, 0.22), "Metal", bevel=0.02)
    for sx in (-1, 1):
        box((sx * 0.45, -1.0, 0.72), (0.12, 7.4, 0.3), "Negro", bevel=0.01)
    # caja: suelo, techo, laterales y frente (detrás queda abierta para las puertas)
    box((0, (L0 + L1) / 2, 1.0), (W, L1 - L0, 0.1), "Madera", bevel=0.01)
    box((0, (L0 + L1) / 2, 3.55), (W, L1 - L0, 0.08), "PinturaBlanca", bevel=0.02)
    for sx in (-1, 1):
        box((sx * (W / 2 - 0.03), (L0 + L1) / 2, 2.28), (0.06, L1 - L0, 2.5), "PinturaBlanca", bevel=0.01)
        # perfiles de aluminio y nervios
        for k in range(9):
            y = L0 + 0.25 + k * (L1 - L0 - 0.5) / 8
            box((sx * (W / 2 + 0.005), y, 2.28), (0.02, 0.05, 2.46), "MetalClaro", bevel=0.004)
        box((sx * (W / 2 + 0.01), (L0 + L1) / 2, 1.07), (0.04, L1 - L0, 0.08), "MetalClaro", bevel=0.005)
        box((sx * (W / 2 + 0.01), (L0 + L1) / 2, 3.52), (0.04, L1 - L0, 0.08), "MetalClaro", bevel=0.005)
        # franja de color
        box((sx * (W / 2 + 0.012), (L0 + L1) / 2, 1.5), (0.006, L1 - L0 - 0.2, 0.16), "Pintura", bevel=0)
        # interior oscuro
        box((sx * (W / 2 - 0.07), (L0 + L1) / 2, 2.28), (0.01, L1 - L0 - 0.05, 2.4), "MaderaOscura", bevel=0)
    box((0, L1 - 0.03, 2.28), (W, 0.06, 2.5), "PinturaBlanca", bevel=0.01)
    box((0, L1 - 0.07, 2.28), (W - 0.15, 0.01, 2.4), "MaderaOscura", bevel=0)
    # marco trasero de acero
    for sx in (-1, 1):
        box((sx * (W / 2 - 0.04), L0 - 0.02, 2.28), (0.1, 0.08, 2.56), "Metal", bevel=0.01)
    box((0, L0 - 0.02, 3.56), (W, 0.08, 0.12), "Metal", bevel=0.01)
    box((0, L0 - 0.02, 1.0), (W, 0.1, 0.12), "Metal", bevel=0.01)
    # parachoques trasero, pilotos y matrícula
    box((0, L0 - 0.15, 0.62), (2.2, 0.12, 0.14), "Negro", bevel=0.02)
    for sx in (-1, 1):
        box((sx * 0.9, L0 - 0.1, 0.82), (0.3, 0.06, 0.14), "Led", bevel=0.01)
        box((sx * 0.55, L0 - 0.1, 0.82), (0.16, 0.06, 0.14), "Luz", bevel=0.01)
    box((0, L0 - 0.13, 0.85), (0.52, 0.02, 0.12), "PinturaBlanca", bevel=0.004)
    # guardabarros y ruedas (gemelas detrás)
    for sx in (-1, 1):
        for y, twin in ((2.1, False), (-2.6, True), (-1.5, True)):
            for t in ((0, 0.12) if twin else (0,)):
                cyl((sx * (0.98 - t), y, 0.52), 0.52, 0.3, "Goma", rot=(0, 90, 0), verts=48, bevel=0.05)
            cyl((sx * 1.12, y, 0.52), 0.3, 0.02, "MetalClaro", rot=(0, 90, 0), verts=32)
            cyl((sx * 1.13, y, 0.52), 0.08, 0.03, "Cromo", rot=(0, 90, 0), verts=16)
            for k in range(8):
                a = k * math.pi / 4
                cyl((sx * 1.13, y + 0.2 * math.cos(a), 0.52 + 0.2 * math.sin(a)), 0.025, 0.03, "Cromo", rot=(0, 90, 0), verts=8)
        box((sx * 1.05, -2.05, 1.12), (0.3, 1.9, 0.05), "Negro", bevel=0.02)
        # depósito y caja de herramientas
        cyl((sx * 0.85, -0.2, 0.75), 0.3, 1.0, "MetalClaro", rot=(90, 0, 0), verts=32)
    # cabina
    prof = [(0.75, 0.75), (0.75, 3.1), (0.85, 3.2), (2.6, 3.2), (2.85, 3.0), (3.05, 2.0), (3.15, 1.85), (3.15, 0.75)]
    profile_x(prof, 2.38, "Pintura", bevel=0.08)
    for sx in (-1, 1):
        box((sx * 1.191, 2.2, 2.45), (0.01, 1.0, 0.8), "Cristal", bevel=0.03)
        box((sx * 1.191, 1.55, 1.6), (0.01, 0.008, 1.5), "Tinta", bevel=0)
        box((sx * 1.191, 2.75, 1.6), (0.01, 0.008, 1.5), "Tinta", bevel=0)
        box((sx * 1.2, 2.3, 1.75), (0.03, 0.2, 0.04), "Cromo", bevel=0.01)
        # retrovisores grandes
        tube([(sx * 1.19, 2.85, 2.6), (sx * 1.45, 2.95, 2.65), (sx * 1.48, 2.95, 2.4)], 0.02, "Negro", verts=8)
        box((sx * 1.5, 2.95, 2.25), (0.06, 0.18, 0.4), "Negro", bevel=0.02)
        box((sx * 0.85, 3.16, 1.2), (0.38, 0.04, 0.2), "Luz", bevel=0.02)
        box((sx * 1.2, 2.2, 0.95), (0.04, 0.5, 0.15), "MetalClaro", bevel=0.01)
    ang = math.degrees(math.atan2(3.0 - 2.0, 3.05 - 2.85))
    box((0, 2.97, 2.5), (2.1, 0.02, 0.95), "Cristal", rot=(-(90 - ang) * 0.3, 0, 0), bevel=0.02)
    for k in range(7):
        box((0, 3.165, 1.35 + k * 0.08), (1.1, 0.02, 0.03), "Cromo", bevel=0.005)
    box((0, 3.2, 0.85), (2.38, 0.14, 0.26), "Negro", bevel=0.03)
    box((0, 3.22, 0.85), (0.52, 0.02, 0.12), "PinturaBlanca", bevel=0.004)
    box((0, 1.6, 3.45), (2.0, 1.5, 0.5), "PlasticoOscuro", bevel=0.1)
    export("Camion")

def camion_puerta():
    reset()
    # bisagra en x=0, la hoja crece hacia +x; altura desde z=0
    box((0.6, 0, 1.25), (1.18, 0.05, 2.46), "PinturaBlanca", bevel=0.01)
    for z in (0.35, 1.25, 2.15):
        box((0.6, -0.03, z), (1.18, 0.012, 0.05), "MetalClaro", bevel=0.004)
    for z in (0.3, 2.2):
        cyl((0.02, -0.03, z), 0.035, 0.18, "Metal", verts=16)
    # barras de cierre verticales con manillas
    tube([(1.0, -0.06, 0.1), (1.0, -0.06, 2.4)], 0.018, "Cromo", verts=8)
    box((1.0, -0.09, 1.2), (0.05, 0.05, 0.25), "Negro", bevel=0.01)
    box((0.6, -0.03, 1.9), (0.3, 0.008, 0.15), "Pintura", bevel=0)
    box((0.6, 0.03, 1.25), (1.1, 0.01, 2.36), "MaderaOscura", bevel=0)
    export("CamionPuerta")

ALL = {"Camion": camion, "CamionPuerta": camion_puerta, "Babel": babel, "Fotocopiadora": fotocopiadora, "Duplicadora": duplicadora, "Plotter": plotter, "Fotolab": fotolab, "Offset": offset,
       "Rotativa": rotativa, "IA": ia, "Rack": rack, "Cuantica": cuantica, "Brazo": brazo, "Garra": garra,
       "Excavadora": excavadora, "Furgoneta": furgoneta, "RelojPared": reloj_pared, "Taquillas": taquillas, "RelojFichar": reloj_fichar}
for k, f in ALL.items():
    if not only or k in only:
        try:
            f()
        except Exception as ex:
            import traceback; traceback.print_exc()
            print("FALLO", k, ex)
print("LISTO")
