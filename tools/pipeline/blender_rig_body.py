"""Bind a generated character to the player rig's own skeleton.

Run:  blender -b --python blender_rig_body.py -- --skeleton Human.fbx --body body.glb --out outdir --name chr_x

The generated body stands with its arms part-way down and its feet apart; the rig's skeleton
is a T-pose. Steps:
  1. A copy of the skeleton is posed to match the body (arm and leg angles are measured from
     the mesh), the body is bound to it, and the copy is straightened back to the T-pose,
     carrying the body with it. That result is made permanent.
  2. The arms, now level, are stretched or shortened along their length so the fingertips
     reach where the skeleton's do. In VR the drawn hand must sit where the real hand is.
  3. The body is bound afresh to the original, unchanged skeleton.
Fingers are left out of the binding: the generated hand is one solid shape and bends badly.

Writes <name>.glb: the body alone, in the T-pose, with no skeleton in the file. The binding is
carried in four extra texture-coordinate sets (TEXCOORD_1 to 4), one per influence:
u = (bone number + 0.5) / 64, v = weight. Bone numbers index BONES below, which are the
ordinary humanoid bone names. Positions are in metres relative to the hip joint. The game
rebuilds the skinned mesh from this against whatever skeleton the rig has, so the asset
contains nothing of the rig's own files.

Also writes check pictures: <name>_check_rest.png (T-pose with the rig's mannequin in wire),
<name>_check_pose.png and <name>_check_pose_side.png (arms lowered, an elbow and a knee bent).
Prints one GODTREE_RESULT line of measurements.
"""
import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
parser = argparse.ArgumentParser()
parser.add_argument("--skeleton", required=True)
parser.add_argument("--body", required=True)
parser.add_argument("--out", required=True)
parser.add_argument("--name", required=True)
args = parser.parse_args(argv)
os.makedirs(args.out, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=args.skeleton)
armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mannequin = next(o for o in bpy.data.objects if o.type == "MESH")
known = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=args.body)
body = next(o for o in bpy.data.objects if o not in known and o.type == "MESH")
body.name = args.name


def activate(obj, mode="OBJECT"):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if mode != "OBJECT":
        bpy.ops.object.mode_set(mode=mode)


# Make the body's own transform plain and weld the seams the file format split, or the
# automatic binding cannot spread weights across them.
activate(body)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=0.00002)
bpy.ops.object.mode_set(mode="OBJECT")


def world_points():
    return [body.matrix_world @ v.co for v in body.data.vertices]


def bone_head(name):
    return armature.matrix_world @ armature.data.bones[name].head_local


# Stand the body on the skeleton front to back: the middle of the chest over the spine.
points = world_points()
chest = [p.y for p in points if 1.05 < p.z < 1.40 and abs(p.x) < 0.18]
spine_y = (armature.matrix_world @ armature.data.bones["Chest"].head_local).y
shift_y = spine_y - (min(chest) + max(chest)) * 0.5
for vertex in body.data.vertices:
    vertex.co.y += shift_y
body.data.update()

points = world_points()
height = max(p.z for p in points)
shoulder = bone_head("LeftUpperArm")
hip = bone_head("LeftUpperLeg")
ankle = bone_head("LeftFoot")

# Arm angle below level, from the shoulder joint to the middle of the hand, each side.
reach = max(p.x for p in points)
left_hand = [p for p in points if p.x > reach - 0.09]
left_hand_centre = sum(left_hand, Vector()) / len(left_hand)
reach_r = min(p.x for p in points)
right_hand = [p for p in points if p.x < reach_r + 0.09]
right_hand_centre = sum(right_hand, Vector()) / len(right_hand)
arm_left = math.atan2(shoulder.z - left_hand_centre.z, left_hand_centre.x - shoulder.x)
arm_right = math.atan2(shoulder.z - right_hand_centre.z, -right_hand_centre.x - shoulder.x)

# Leg angle away from straight down, from the hip joint to the middle of the foot.
left_foot = [p for p in points if p.z < 0.12 and p.x > 0]
right_foot = [p for p in points if p.z < 0.12 and p.x < 0]
left_foot_x = sum(p.x for p in left_foot) / len(left_foot)
right_foot_x = sum(p.x for p in right_foot) / len(right_foot)
leg_left = math.atan2(left_foot_x - hip.x, hip.z - ankle.z)
leg_right = math.atan2(-right_foot_x - hip.x, hip.z - ankle.z)

FINGERS = ("Index", "Middle", "Ring", "Little", "Thumb")

# The bones the body is bound to, in the order their numbers are written to the file.
# The game holds the same list (PlayerBody.cs).
BONES = [
    "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
    "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
    "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
    "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
    "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
]


def set_deform(arm_obj):
    for bone in arm_obj.data.bones:
        bone.use_deform = not (bone.name.endswith("_end") or any(f in bone.name for f in FINGERS))


def pose_limbs(arm_obj, sign):
    """Turn the upper arms down and the upper legs out by the measured angles (sign=-1 undoes it)."""
    activate(arm_obj, "POSE")
    turns = {
        "LeftUpperArm": arm_left, "RightUpperArm": -arm_right,
        "LeftUpperLeg": -leg_left, "RightUpperLeg": leg_right,
    }
    for name, angle in turns.items():
        pose_bone = arm_obj.pose.bones[name]
        pivot = arm_obj.data.bones[name].head_local
        turn = Matrix.Translation(pivot) @ Matrix.Rotation(sign * angle, 4, "Y") @ Matrix.Translation(-pivot)
        pose_bone.matrix = turn @ arm_obj.data.bones[name].matrix_local
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")


def bind(mesh_obj, arm_obj):
    for group in list(mesh_obj.vertex_groups):
        mesh_obj.vertex_groups.remove(group)
    for modifier in list(mesh_obj.modifiers):
        mesh_obj.modifiers.remove(modifier)
    set_deform(arm_obj)
    bpy.ops.object.select_all(action="DESELECT")
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    return sum(1 for v in mesh_obj.data.vertices if not v.groups)


# ---- 1. bind to a copy posed like the body, then straighten the copy and keep the result
activate(armature)
bpy.ops.object.duplicate()
fitted = bpy.context.object
fitted.name = "FittedSkeleton"
pose_limbs(fitted, 1)
activate(fitted, "POSE")
bpy.ops.pose.armature_apply(selected=False)
bpy.ops.object.mode_set(mode="OBJECT")
unbound_first = bind(body, fitted)
pose_limbs(fitted, -1)
activate(body)
for modifier in list(body.modifiers):
    if modifier.type == "ARMATURE":
        bpy.ops.object.modifier_apply(modifier=modifier.name)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.data.objects.remove(fitted, do_unlink=True)

# ---- 2. make the level arms as long as the skeleton's
points = world_points()
skeleton_reach = max((mannequin.matrix_world @ v.co).x for v in mannequin.data.vertices)
stretch_left = (skeleton_reach - shoulder.x) / (max(p.x for p in points) - shoulder.x)
stretch_right = (skeleton_reach - shoulder.x) / (-min(p.x for p in points) - shoulder.x)
inverse = body.matrix_world.inverted()
for vertex in body.data.vertices:
    p = body.matrix_world @ vertex.co
    if p.x > shoulder.x:
        p.x = shoulder.x + (p.x - shoulder.x) * stretch_left
    elif p.x < -shoulder.x:
        p.x = -shoulder.x + (p.x + shoulder.x) * stretch_right
    vertex.co = inverse @ p
body.data.update()

# ---- 3. bind to the original skeleton
unbound = bind(body, armature)

# ---- texture
image = None
for material in body.data.materials:
    if material and material.use_nodes:
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image:
                image = node.image
albedo_path = os.path.join(args.out, args.name + "_albedo.png")
if image:
    image.filepath_raw = albedo_path
    image.file_format = "PNG"
    image.save()

# ---- check pictures
scene = bpy.context.scene
camera_data = bpy.data.cameras.new("check")
camera_data.type = "ORTHO"
camera_data.ortho_scale = 2.3
camera = bpy.data.objects.new("check", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, -6, 0.95)
camera.rotation_euler = (math.radians(90), 0, 0)
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.color_type = "TEXTURE"
scene.display.shading.light = "FLAT"
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.film_transparent = False
mannequin.display_type = "WIRE"
scene.render.filepath = os.path.join(args.out, args.name + "_check_rest.png")
bpy.ops.render.render(write_still=True)

mannequin.hide_render = True
activate(armature, "POSE")


def turn_bone(name, degrees, axis):
    pose_bone = armature.pose.bones[name]
    pivot = pose_bone.matrix.to_translation()
    turn = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(degrees), 4, axis) @ Matrix.Translation(-pivot)
    pose_bone.matrix = turn @ pose_bone.matrix
    bpy.context.view_layer.update()


turn_bone("LeftUpperArm", 65, "Y")
turn_bone("RightUpperArm", -40, "Y")
turn_bone("RightLowerArm", -80, "Z")
turn_bone("LeftUpperLeg", -50, "X")
turn_bone("LeftLowerLeg", 70, "X")
turn_bone("Head", 25, "Z")
bpy.ops.object.mode_set(mode="OBJECT")
scene.render.filepath = os.path.join(args.out, args.name + "_check_pose.png")
bpy.ops.render.render(write_still=True)
camera.location = (4.2, -4.2, 1.2)
camera.rotation_euler = (math.radians(86), 0, math.radians(45))
scene.render.filepath = os.path.join(args.out, args.name + "_check_pose_side.png")
bpy.ops.render.render(write_still=True)

activate(armature, "POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.transforms_clear()
bpy.ops.object.mode_set(mode="OBJECT")

# ---- write the binding into extra texture-coordinate sets and export the body alone
group_bone = {group.index: group.name for group in body.vertex_groups}
influences = []
for vertex in body.data.vertices:
    pairs = sorted(((g.weight, BONES.index(group_bone[g.group])) for g in vertex.groups
                    if g.weight > 0.0005 and group_bone[g.group] in BONES), reverse=True)[:4]
    total = sum(w for w, _ in pairs) or 1.0
    pairs = [(w / total, i) for w, i in pairs]
    while len(pairs) < 4:
        pairs.append((0.0, pairs[0][1] if pairs else 0))
    influences.append(pairs)
for slot in range(4):
    layer = body.data.uv_layers.new(name="bind%d" % slot, do_init=False)
    for loop in body.data.loops:
        weight, bone = influences[loop.vertex_index][slot]
        layer.data[loop.index].uv = ((bone + 0.5) / 64.0, weight)

hips = armature.matrix_world @ armature.data.bones["Hips"].head_local
activate(body)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
for modifier in list(body.modifiers):
    body.modifiers.remove(modifier)
for group in list(body.vertex_groups):
    body.vertex_groups.remove(group)
bpy.data.objects.remove(mannequin, do_unlink=True)
bpy.data.objects.remove(armature, do_unlink=True)
for vertex in body.data.vertices:
    vertex.co -= hips
body.data.update()
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.context.view_layer.objects.active = body
glb_path = os.path.join(args.out, args.name + ".glb")
bpy.ops.export_scene.gltf(filepath=glb_path, export_format="GLB", use_selection=True,
                          export_yup=True, export_apply=True, export_skins=False,
                          export_animations=False, export_texcoords=True, export_normals=True)
print("GODTREE_RESULT " + json.dumps({
    "name": args.name,
    "height": round(height, 3),
    "arm_angle_deg": [round(math.degrees(arm_left), 1), round(math.degrees(arm_right), 1)],
    "leg_angle_deg": [round(math.degrees(leg_left), 1), round(math.degrees(leg_right), 1)],
    "arm_stretch": [round(stretch_left, 3), round(stretch_right, 3)],
    "vertices": len(body.data.vertices),
    "triangles": sum(len(p.vertices) - 2 for p in body.data.polygons),
    "unbound_vertices_first_pass": unbound_first,
    "unbound_vertices": unbound,
    "glb": glb_path, "front_back_shift": round(shift_y, 3),
    "albedo": albedo_path if image else None,
}))
