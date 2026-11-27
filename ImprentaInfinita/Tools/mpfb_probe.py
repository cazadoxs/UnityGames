import bpy, sys, os, inspect
from bl_ext.blender_org.mpfb.services.assetservice import AssetService
from bl_ext.blender_org.mpfb.services.humanservice import HumanService
import bl_ext.blender_org.mpfb.services.targetservice as ts
TS = ts.TargetService
print("TSFUNCS", [f for f in dir(TS) if not f.startswith('_')])
try: print("MACRODEF", TS.get_default_macro_info_dict())
except Exception as ex: print("MACRODEF FAIL", ex)
print("SKINSIG", inspect.getsource(HumanService.set_character_skin)[:1500])
bm = HumanService.create_human()
print("BM", bm.name, len(bm.data.vertices), [m.type for m in bm.modifiers], bm.data.shape_keys and [k.name for k in bm.data.shape_keys.key_blocks][:10])
rig = HumanService.add_builtin_rig(bm, "game_engine")
print("RIG", rig and rig.name, rig and [b.name for b in rig.data.bones])
p = AssetService.find_asset_absolute_path("clothes/male_casualsuit01/male_casualsuit01.mhclo")
print("ASSETPATH", p)
print("CLOTHES", AssetService.get_asset_list("clothes").get("Male casualsuit01"))
print("SKIN", AssetService.get_asset_list("skins").get("Young caucasian male"))
