"""Collect chosen clips from the CC0 Mesh2Motion human animation files into one .glb.

Run:  blender -b --python blender_export_animations.py -- --inputs a.glb b.glb --out anm_human_basic.glb [--clips Walk Idle_A ...]

The clips keep their bone names and the skeleton keeps its rest pose exactly as
blender_rig_body.py --style m2m --mode skinned exports it, because both go through the same
Blender import. That is what lets one clip file drive every character rigged that way.
The skeleton object is named "Armature" in both. No mesh is exported.
Prints GODTREE_RESULT with the clip names and lengths.
"""
import argparse
import json
import sys

import bpy
from mathutils import Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
parser = argparse.ArgumentParser()
parser.add_argument("--inputs", nargs="+", required=True)
parser.add_argument("--out", required=True)
parser.add_argument("--clips", nargs="*", default=[])
args = parser.parse_args(argv)

bpy.ops.wm.read_factory_settings(use_empty=True)
armature = None
for path in args.inputs:
    known = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    for obj in [o for o in bpy.data.objects if o not in known]:
        if obj.type == "ARMATURE" and armature is None:
            armature = obj
        else:
            bpy.data.objects.remove(obj, do_unlink=True)

wanted = set(args.clips)
kept = []
for action in list(bpy.data.actions):
    if wanted and action.name not in wanted:
        bpy.data.actions.remove(action)
    else:
        action.use_fake_user = True
        kept.append(action)
missing = sorted(wanted - {a.name for a in kept})

armature.name = "Armature"
armature.animation_data_clear()
for pose_bone in armature.pose.bones:
    pose_bone.matrix_basis = Matrix()

# One track per clip, so the exporter writes each as its own animation.
armature.animation_data_create()
for action in kept:
    track = armature.animation_data.nla_tracks.new()
    track.name = action.name
    track.strips.new(action.name, int(action.frame_range[0]), action)

bpy.ops.object.select_all(action="DESELECT")
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.gltf(filepath=args.out, export_format="GLB", use_selection=True, export_yup=True,
                          export_animations=True, export_animation_mode="NLA_TRACKS", export_skins=True,
                          export_def_bones=False, export_force_sampling=True, export_optimize_animation_size=True)

print("GODTREE_RESULT " + json.dumps({
    "out": args.out,
    "clips": {a.name: int(a.frame_range[1] - a.frame_range[0]) for a in sorted(kept, key=lambda a: a.name)},
    "missing": missing,
}))
