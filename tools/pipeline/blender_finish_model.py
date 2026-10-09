"""Turn a raw generated mesh with vertex colours into a game-ready .glb, headless.

Usage:
  blender -b --factory-startup -P tools/pipeline/blender_finish_model.py -- \
      IN.glb OUT.glb PREVIEW.png [--tris 5000] [--tex 1024] [--size 0.95] [--up-axis Y]

Steps: import, join, clean, decimate to the triangle budget, unwrap, bake the vertex
colours to a texture, scale so the longest side is --size metres, put the origin at
the centre of the base, export .glb with the texture embedded, and render a preview.

Runs in Blender 4.2 or newer. Blender is GPL; this script uses its Python API and is
published under a GPL-compatible licence (MIT).
"""
import math
import sys

import bpy
from mathutils import Vector


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:]
    opts = {"tris": 5000, "tex": 1024, "size": 1.0, "rotx": -90.0}
    positional = []
    i = 0
    while i < len(argv):
        if argv[i].startswith("--"):
            opts[argv[i][2:]] = float(argv[i + 1])
            i += 2
        else:
            positional.append(argv[i])
            i += 1
    return positional[0], positional[1], positional[2], opts


def main():
    src, dst, preview, opts = parse_args()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise SystemExit("no mesh in " + src)
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # TripoSR meshes arrive with the image's up direction along Blender's Y axis; stand them up.
    obj.rotation_mode = "XYZ"  # the glTF importer leaves objects in quaternion mode
    obj.rotation_euler = (math.radians(opts["rotx"]), 0.0, 0.0)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Clean: weld, drop loose bits, consistent outward normals.
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=1e-5)
    bpy.ops.mesh.delete_loose()
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")

    mesh = obj.data
    src_tris = sum(len(p.vertices) - 2 for p in mesh.polygons)

    # Keep an untouched copy to bake colours from.
    bpy.ops.object.duplicate()
    high = bpy.context.view_layer.objects.active
    high.name = "bake_source"
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    target = int(opts["tris"])
    if src_tris > target:
        mod = obj.modifiers.new("decimate", "DECIMATE")
        mod.ratio = target / src_tris
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.shade_smooth()

    # Unwrap the low mesh.
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")

    # Source material: vertex colour as emission, so the bake is lighting-free.
    def colour_material(target_obj):
        mat = bpy.data.materials.new("vertex_colour")
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        nodes.clear()
        out = nodes.new("ShaderNodeOutputMaterial")
        emit = nodes.new("ShaderNodeEmission")
        attr = nodes.new("ShaderNodeVertexColor")
        cols = target_obj.data.color_attributes
        if len(cols) > 0:
            attr.layer_name = cols[0].name
        mat.node_tree.links.new(attr.outputs["Color"], emit.inputs["Color"])
        mat.node_tree.links.new(emit.outputs["Emission"], out.inputs["Surface"])
        target_obj.data.materials.clear()
        target_obj.data.materials.append(mat)

    colour_material(high)

    # Destination material with the bake target image.
    size = int(opts["tex"])
    image = bpy.data.images.new("albedo", size, size, alpha=False)
    mat = bpy.data.materials.new(obj.name + "_mat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = image
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    nodes.active = tex

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 1
    scene.cycles.device = "CPU"
    scene.render.bake.use_selected_to_active = True
    scene.render.bake.cage_extrusion = 0.02
    scene.render.bake.max_ray_distance = 0.05
    scene.render.bake.margin = 8
    bpy.ops.object.select_all(action="DESELECT")
    high.select_set(True)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type="EMIT")
    image.pack()

    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0
    bsdf.inputs["Metallic"].default_value = 0.0

    bpy.data.objects.remove(high, do_unlink=True)

    # Scale so the longest side is --size metres; origin at the centre of the base.
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    dims = obj.dimensions
    longest = max(dims.x, dims.y, dims.z)
    if longest > 0:
        s = opts["size"] / longest
        obj.scale = (s, s, s)
        bpy.ops.object.transform_apply(scale=True)
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    mx = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    base = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    obj.data.transform(__import__("mathutils").Matrix.Translation(-base))
    obj.location = (0, 0, 0)

    final_tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    bpy.ops.export_scene.gltf(filepath=dst, export_format="GLB", use_selection=True, export_yup=True, export_apply=True)

    # Preview: flat-lit three-quarter view on white.
    dims = obj.dimensions
    centre = Vector((0, 0, dims.z / 2))
    radius = max(dims.x, dims.y, dims.z)
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = centre + Vector((1.6, -1.9, 0.9)).normalized() * radius * 1.45
    direction = centre - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT"
    scene.display.shading.color_type = "TEXTURE"
    scene.display.shading.show_object_outline = True
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("w")
    world.color = (1, 1, 1)
    scene.world = world
    scene.view_settings.view_transform = "Standard"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = preview
    bpy.ops.render.render(write_still=True)

    print(f"GODTREE_RESULT source_tris={src_tris} final_tris={final_tris} "
          f"dims={dims.x:.3f}x{dims.y:.3f}x{dims.z:.3f} tex={size}")


main()
