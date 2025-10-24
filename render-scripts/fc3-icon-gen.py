# /// script
# dependencies = [
#   "vapory", # requires yay -S povray
#   "matplotlib",
# ]
# ///

import os
import sys
import math
import pathlib

REPO_ROOT = os.path.dirname(os.path.dirname(__file__))
out_png = sys.argv[1]


from vapory import *
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

def povray_quote(obj):
    if not isinstance(obj, str):
        obj = str(obj)
    return '\"'+obj.replace('"', '\\"')+'\"'

text_font_path = find_ttf_fonts('Rockwell', 'Arial', 'NotoSans-Regular')
print("Using Font TTF:", text_font_path)

# Define the camera
camera = Camera('location', [0, 2, -3], 'look_at', [0, 0, 0])

# Define the light source
light = LightSource([2, 4, -3], 'color', [1, 1, 1])

# Define the box with rounded corners
box = Box([-1, -1, -1], [1, 1, 1], Texture(Pigment('color', [1, 1, 1])))

# Define the text 'T'
text = Text('ttf', povray_quote(text_font_path), povray_quote("T"), 1, 0, Pigment('color', [0, 0, 1]))
print(f'text = {str(text)}')

# Define the scene
scene = Scene(camera, [light, text])

# Render the scene
scene.render(out_png, width=256, height=256)
print(f"[ Saved ] {out_png}")
