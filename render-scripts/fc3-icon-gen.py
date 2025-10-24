# /// script
# requires-python = "==3.11" # For prebuilt bpy - will need to update as releases come down
# dependencies = [
#   "matplotlib",
#   "bpy",
# ]
# ///

import os
import sys
import math
import pathlib

REPO_ROOT = os.path.dirname(os.path.dirname(__file__))
out_png = sys.argv[1]

from matplotlib import font_manager

def find_ttf_fonts(*font_names):
    thirdparty_asset_ttf_files = []
    for ttf_file in pathlib.Path(os.path.join(REPO_ROOT, 'thirdparty-assets')).rglob('*.ttf'):
        if os.path.isfile(ttf_file):
            thirdparty_asset_ttf_files.append(ttf_file)

    font_names = list(font_names)
    for font_name in font_names:
        font_name = font_name.lower()

        for font in thirdparty_asset_ttf_files:
            if font_name in os.path.basename(font).lower():
                return font

        for font in font_manager.findSystemFonts(fontpaths=None, fontext='ttf'):
            if font_name in os.path.basename(font).lower():
                return font

    raise FileNotFoundError(f"Cannot find any of the following fonts: {font_names}")

text_font_path = find_ttf_fonts('Rockwell', 'Arial', 'NotoSans-Regular')
print("Using Font TTF:", text_font_path)

import bpy

# --- Clean the scene ---
bpy.ops.wm.read_factory_settings(use_empty=True)

# --- Create a light source ---
bpy.ops.object.light_add(type='AREA', location=(3, -3, 5))
light = bpy.context.object
light.data.energy = 500
light.data.size = 4

# --- Add the camera ---
bpy.ops.object.camera_add(location=(2.5, -2.5, 2.5))
camera = bpy.context.object
bpy.context.scene.camera = camera

# Function to make camera point at a target
def point_at(obj, target):
    direction = target.location - obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    obj.rotation_euler = rot_quat.to_euler()

# --- Add the cube base ---
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, 0))
cube = bpy.context.object
cube.scale = (1, 1, 0.2)
cube.name = "AppBase"

# Material for cube
mat_base = bpy.data.materials.new(name="BaseMaterial")
mat_base.use_nodes = True
nodes = mat_base.node_tree.nodes
nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.1, 0.5, 1.0, 1)
nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.3
cube.data.materials.append(mat_base)

# --- Add the 3D Text ---
bpy.ops.object.text_add(location=(0, 0, 0.15))
text_obj = bpy.context.object
text_obj.data.body = "T"
text_obj.data.extrude = 0.05
text_obj.data.align_x = 'CENTER'
text_obj.data.align_y = 'CENTER'

# Center the text geometry
bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
text_obj.location = (0, 0, 0.15)

# Text material
mat_text = bpy.data.materials.new(name="TextMaterial")
mat_text.use_nodes = True
nodes_t = mat_text.node_tree.nodes
nodes_t["Principled BSDF"].inputs["Base Color"].default_value = (1, 1, 1, 1)
nodes_t["Principled BSDF"].inputs["Roughness"].default_value = 0.4
text_obj.data.materials.append(mat_text)

# --- Point the camera at the cube ---
point_at(camera, cube)

# --- Scene and render settings ---
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 64
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.film_transparent = False
scene.render.filepath = out_png

# --- Add a smooth ground plane for shadows ---
bpy.ops.mesh.primitive_plane_add(size=6, location=(0, 0, -0.21))
plane = bpy.context.object
plane_mat = bpy.data.materials.new(name="GroundMaterial")
plane_mat.use_nodes = True
plane_nodes = plane_mat.node_tree.nodes
plane_nodes["Principled BSDF"].inputs["Base Color"].default_value = (1, 1, 1, 1)
plane_nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
plane.data.materials.append(plane_mat)

# --- Render to file ---
bpy.ops.render.render(write_still=True)

