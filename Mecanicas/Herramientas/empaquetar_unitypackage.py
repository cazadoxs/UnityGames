#!/usr/bin/env python3
"""Empaqueta una mecánica de esta carpeta como .unitypackage (sin abrir Unity).

Uso:  python3 Mecanicas/Herramientas/empaquetar_unitypackage.py CintaTransportadora

Lee Mecanicas/<Nombre>/ (con sus .meta) y crea Mecanicas/Paquetes/<Nombre>.unitypackage,
que se instala en Assets/Mecanicas/<Nombre> con Assets > Import Package > Custom Package.
"""
import gzip, io, os, re, sys, tarfile

def guid_de(meta):
    m = re.search(r"^guid:\s*([0-9a-f]{32})", open(meta, encoding="utf-8").read(), re.M)
    if not m:
        sys.exit(f"Sin guid: {meta}")
    return m.group(1)

def anadir(tar, nombre, datos):
    info = tarfile.TarInfo(nombre)
    info.size = len(datos)
    info.mtime = 0
    tar.addfile(info, io.BytesIO(datos))

def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    base = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    nombre = sys.argv[1]
    origen = os.path.join(base, nombre)
    salida = os.path.join(base, "Paquetes", nombre + ".unitypackage")
    os.makedirs(os.path.dirname(salida), exist_ok=True)

    rutas = [origen]
    for d, ds, fs in os.walk(origen):
        ds.sort()
        rutas += [os.path.join(d, x) for x in ds]
        rutas += [os.path.join(d, f) for f in sorted(fs) if not f.endswith(".meta")]

    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w") as tar:
        for ruta in rutas:
            meta = ruta + ".meta"
            if not os.path.exists(meta):
                sys.exit(f"Falta el .meta de {ruta}")
            g = guid_de(meta)
            rel = os.path.relpath(ruta, base).replace(os.sep, "/")
            anadir(tar, f"{g}/pathname", f"Assets/Mecanicas/{rel}".encode("utf-8"))
            anadir(tar, f"{g}/asset.meta", open(meta, "rb").read())
            if os.path.isfile(ruta):
                anadir(tar, f"{g}/asset", open(ruta, "rb").read())
    with open(salida, "wb") as f:
        f.write(gzip.compress(buf.getvalue(), mtime=0))
    print(f"{salida}: {len(rutas)} elementos")

if __name__ == "__main__":
    main()
