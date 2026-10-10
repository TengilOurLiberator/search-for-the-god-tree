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

import bmesh
import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
parser = argparse.ArgumentParser()
parser.add_argument("--skeleton", required=True)
parser.add_argument("--body", required=True)
parser.add_argument("--out", required=True)
parser.add_argument("--name", required=True)
parser.add_argument("--style", default="bimos", choices=["bimos", "m2m"],
                    help="bone names of the skeleton: the player rig's, or the CC0 Mesh2Motion human rig's")
parser.add_argument("--mode", default="uv", choices=["uv", "skinned"],
                    help="uv: mesh alone with its binding in texture coordinates (the player's body); "
                         "skinned: mesh with the skeleton in the file (townsfolk and monsters, CC0 skeleton only)")
parser.add_argument("--parts", action="store_true",
                    help="skinned mode only: cut the bound body into ten pieces at the neck, shoulders, elbows, "
                         "hips and knees, each closed with a cap, so a limb can be severed in the game")
args = parser.parse_args(argv)
os.makedirs(args.out, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
if args.skeleton.lower().endswith(".fbx"):
    bpy.ops.import_scene.fbx(filepath=args.skeleton)
else:
    bpy.ops.import_scene.gltf(filepath=args.skeleton)
armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
# The skeleton file's own figure is the largest mesh in it; anything else (helper shapes) goes.
skeleton_meshes = sorted((o for o in bpy.data.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
mannequin = skeleton_meshes[-1]
for extra in skeleton_meshes[:-1]:
    bpy.data.objects.remove(extra, do_unlink=True)
# An animation file leaves the skeleton mid-movement; the binding needs it at rest.
armature.animation_data_clear()
for pose_bone in armature.pose.bones:
    pose_bone.matrix_basis = Matrix()
bpy.context.view_layer.update()

# What each joint is called in this skeleton.
if args.style == "bimos":
    N = {k: k for k in ("Hips", "Chest", "Head", "Neck", "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm",
                        "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "LeftFoot")}
    NOT_BOUND = ("_end", "Index", "Middle", "Ring", "Little", "Thumb")
else:
    N = {"Hips": "pelvis", "Chest": "spine_02", "Head": "head", "Neck": "neck_01", "LeftUpperArm": "upperarm_l", "RightUpperArm": "upperarm_r",
         "LeftLowerArm": "lowerarm_l", "RightLowerArm": "lowerarm_r", "LeftUpperLeg": "thigh_l", "RightUpperLeg": "thigh_r",
         "LeftLowerLeg": "calf_l", "LeftFoot": "foot_l"}
    NOT_BOUND = ("_leaf", "index_", "middle_", "pinky_", "ring_", "thumb_", "root")
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

# Make the body as tall as the skeleton's own figure, feet on the ground.
figure_top = max((mannequin.matrix_world @ v.co).z for v in mannequin.data.vertices)
body_low = min(v.co.z for v in body.data.vertices)
body_top = max(v.co.z for v in body.data.vertices)
fit = figure_top / (body_top - body_low)
for vertex in body.data.vertices:
    vertex.co = Vector((vertex.co.x * fit, vertex.co.y * fit, (vertex.co.z - body_low) * fit))
body.data.update()

def world_points():
    return [body.matrix_world @ v.co for v in body.data.vertices]


def bone_head(name):
    return armature.matrix_world @ armature.data.bones[name].head_local


# Stand the body on the skeleton front to back: the middle of the chest over the spine.
points = world_points()
chest = [p.y for p in points if 0.60 * figure_top < p.z < 0.78 * figure_top and abs(p.x) < 0.18]
spine_y = (armature.matrix_world @ armature.data.bones[N["Chest"]].head_local).y
shift_y = spine_y - (min(chest) + max(chest)) * 0.5
for vertex in body.data.vertices:
    vertex.co.y += shift_y
body.data.update()

points = world_points()
height = max(p.z for p in points)
shoulder = bone_head(N["LeftUpperArm"])
hip = bone_head(N["LeftUpperLeg"])
ankle = bone_head(N["LeftFoot"])

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
        bone.use_deform = not any(mark in bone.name for mark in NOT_BOUND)


def pose_limbs(arm_obj, sign):
    """Turn the upper arms down and the upper legs out by the measured angles (sign=-1 undoes it)."""
    activate(arm_obj, "POSE")
    turns = {
        N["LeftUpperArm"]: arm_left, N["RightUpperArm"]: -arm_right,
        N["LeftUpperLeg"]: -leg_left, N["RightUpperLeg"]: leg_right,
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

vertex_count = len(body.data.vertices)
triangle_count = sum(len(p.vertices) - 2 for p in body.data.polygons)

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


turn_bone(N["LeftUpperArm"], 65, "Y")
turn_bone(N["RightUpperArm"], -40, "Y")
turn_bone(N["RightLowerArm"], -80, "Z")
turn_bone(N["LeftUpperLeg"], -50, "X")
turn_bone(N["LeftLowerLeg"], 70, "X")
turn_bone(N["Head"], 25, "Z")
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

glb_path = os.path.join(args.out, args.name + ".glb")
part_names = []
if args.mode == "skinned" and args.parts:
    # ---- cut the bound body into pieces that can come off
    # In the T-pose the limbs lie along the axes, so each joint is a flat cut: across the arm at
    # the shoulder and elbow, level at the neck, hip and knee. Every cut is closed with a cap
    # in its own material, so a severed limb and its stump are both solid.
    shoulder_x = bone_head(N["LeftUpperArm"]).x
    elbow_x = bone_head(N["LeftLowerArm"]).x
    neck_z = bone_head(N["Neck"]).z
    hip_z = bone_head(N["LeftUpperLeg"]).z
    knee_z = bone_head(N["LeftLowerLeg"]).z
    points = world_points()
    # The underside of the arm: going down from the shoulder joint through the points at mid
    # upper arm, the arm ends at the first gap (below it is the flank or the leg of a broad
    # creature, which belongs to the body). Never more than a third of a metre below the joint.
    shoulder_z = bone_head(N["LeftUpperArm"]).z
    mid_x = (shoulder_x + elbow_x) * 0.5
    arm_low = shoulder_z
    for z in sorted((p.z for p in points if abs(p.x - mid_x) < 0.04 and p.z < shoulder_z + 0.02), reverse=True):
        if arm_low - z > 0.035:
            break
        arm_low = z
    arm_low = max(arm_low, shoulder_z - 0.32) - 0.015
    head_x = shoulder_x * 0.75
    X, Z = Vector((1, 0, 0)), Vector((0, 0, 1))

    cap = bpy.data.materials.new("cut")
    cap.use_nodes = True
    cap.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.03, 0.05, 1.0)
    cap.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.6
    body.data.materials.append(cap)
    cap_index = len(body.data.materials) - 1

    def keep(*bounds):
        """A box given as (point, direction to throw away) pairs."""
        return list(bounds)

    def below(z): return (Vector((0, 0, z)), Z)       # throw away what is above z
    def above(z): return (Vector((0, 0, z)), -Z)      # throw away what is below z
    def left_of(x): return (Vector((x, 0, 0)), X)     # throw away what has larger x
    def right_of(x): return (Vector((x, 0, 0)), -X)   # throw away what has smaller x

    pieces = {
        "head": [keep(above(neck_z), left_of(head_x), right_of(-head_x))],
        "torso": [keep(above(hip_z), below(neck_z), left_of(shoulder_x), right_of(-shoulder_x)),
                  keep(above(hip_z), below(arm_low), right_of(shoulder_x)),
                  keep(above(hip_z), below(arm_low), left_of(-shoulder_x)),
                  keep(above(neck_z), right_of(head_x), left_of(shoulder_x)),
                  keep(above(neck_z), left_of(-head_x), right_of(-shoulder_x))],
        "arm_upper_l": [keep(above(arm_low), right_of(shoulder_x), left_of(elbow_x))],
        "arm_lower_l": [keep(above(arm_low), right_of(elbow_x))],
        "arm_upper_r": [keep(above(arm_low), left_of(-shoulder_x), right_of(-elbow_x))],
        "arm_lower_r": [keep(above(arm_low), left_of(-elbow_x))],
        "leg_upper_l": [keep(below(hip_z), above(knee_z), right_of(0.0))],
        "leg_lower_l": [keep(below(knee_z), right_of(0.0))],
        "leg_upper_r": [keep(below(hip_z), above(knee_z), left_of(0.0))],
        "leg_lower_r": [keep(below(knee_z), left_of(0.0))],
    }
    to_local = body.matrix_world.inverted()
    made = []
    for part, boxes in pieces.items():
        merged = bmesh.new()
        for box in boxes:
            bm = bmesh.new()
            bm.from_mesh(body.data)
            for point, discard in box:
                cut = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=0.00001,
                                             plane_co=to_local @ point, plane_no=(to_local.to_3x3() @ discard).normalized(),
                                             clear_outer=True, clear_inner=False)
                edges = [e for e in cut["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
                if edges:
                    filled = bmesh.ops.holes_fill(bm, edges=edges, sides=0)
                    for face in filled["faces"]:
                        face.material_index = cap_index
            if bm.faces:
                temp = bpy.data.meshes.new("temp")
                bm.to_mesh(temp)
                merged.from_mesh(temp)
                bpy.data.meshes.remove(temp)
            bm.free()
        if not merged.faces:
            merged.free()
            continue
        bmesh.ops.triangulate(merged, faces=merged.faces[:])
        piece = body.copy()
        piece.data = body.data.copy()
        piece.name = "part_" + part
        piece.data.name = "part_" + part
        merged.to_mesh(piece.data)
        merged.free()
        bpy.context.scene.collection.objects.link(piece)
        made.append(piece)
        part_names.append(part)
    bpy.data.objects.remove(body, do_unlink=True)
    body = None
    bpy.data.objects.remove(mannequin, do_unlink=True)
    armature.name = "Armature"

    # Check pictures of the pieces: pulled apart to show the cuts and caps, then posed together
    # to show every piece still follows the skeleton.
    apart = {"head": (0, 0, 0.18), "arm_upper_l": (0.14, 0, 0), "arm_lower_l": (0.30, 0, 0), "arm_upper_r": (-0.14, 0, 0),
             "arm_lower_r": (-0.30, 0, 0), "leg_upper_l": (0.07, 0, -0.14), "leg_lower_l": (0.07, 0, -0.30),
             "leg_upper_r": (-0.07, 0, -0.14), "leg_lower_r": (-0.07, 0, -0.30)}
    scene.display.shading.color_type = "TEXTURE"
    camera.location = (0, -6, 0.85)
    camera.rotation_euler = (math.radians(90), 0, 0)
    camera_data.ortho_scale = 2.9
    for piece in made:
        piece.location = Vector(apart.get(piece.name[5:], (0, 0, 0)))
    scene.render.filepath = os.path.join(args.out, args.name + "_check_parts.png")
    bpy.ops.render.render(write_still=True)
    for piece in made:
        piece.location = Vector((0, 0, 0))
    activate(armature, "POSE")
    turn_bone(N["LeftUpperArm"], 65, "Y")
    turn_bone(N["RightUpperArm"], -40, "Y")
    turn_bone(N["RightLowerArm"], -80, "Z")
    turn_bone(N["LeftUpperLeg"], -50, "X")
    turn_bone(N["LeftLowerLeg"], 70, "X")
    bpy.ops.object.mode_set(mode="OBJECT")
    camera_data.ortho_scale = 2.3
    camera.location = (4.2, -4.2, 1.2)
    camera.rotation_euler = (math.radians(86), 0, math.radians(45))
    scene.render.filepath = os.path.join(args.out, args.name + "_check_parts_posed.png")
    bpy.ops.render.render(write_still=True)
    activate(armature, "POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.transforms_clear()
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    for piece in made:
        piece.select_set(True)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    for stray in [o for o in bpy.data.objects if o.type == "MESH" and o not in made]:
        bpy.data.objects.remove(stray, do_unlink=True)
    bpy.ops.export_scene.gltf(filepath=glb_path, export_format="GLB", use_selection=True, export_yup=True,
                              export_skins=True, export_animations=False, export_texcoords=True,
                              export_normals=True, export_def_bones=False)
elif args.mode == "skinned":
    # ---- export the body bound to the skeleton, both in the file
    bpy.data.objects.remove(mannequin, do_unlink=True)
    for stray in [o for o in bpy.data.objects if o.type == "MESH" and o is not body]:
        bpy.data.objects.remove(stray, do_unlink=True)
    armature.name = "Armature"
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.gltf(filepath=glb_path, export_format="GLB", use_selection=True, export_yup=True,
                              export_skins=True, export_animations=False, export_texcoords=True,
                              export_normals=True, export_def_bones=False)
else:
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

    hips = armature.matrix_world @ armature.data.bones[N["Hips"]].head_local
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
    bpy.ops.export_scene.gltf(filepath=glb_path, export_format="GLB", use_selection=True,
                              export_yup=True, export_apply=True, export_skins=False,
                              export_animations=False, export_texcoords=True, export_normals=True)

print("GODTREE_RESULT " + json.dumps({
    "name": args.name,
    "height": round(height, 3),
    "arm_angle_deg": [round(math.degrees(arm_left), 1), round(math.degrees(arm_right), 1)],
    "leg_angle_deg": [round(math.degrees(leg_left), 1), round(math.degrees(leg_right), 1)],
    "arm_stretch": [round(stretch_left, 3), round(stretch_right, 3)],
    "vertices": vertex_count,
    "triangles": triangle_count, "parts": part_names,
    "unbound_vertices_first_pass": unbound_first,
    "unbound_vertices": unbound,
    "glb": glb_path, "front_back_shift": round(shift_y, 3),
    "albedo": albedo_path if image else None,
}))
