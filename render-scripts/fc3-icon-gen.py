# /// script
# dependencies = ["pycairo"]
# ///

import sys
import math

import cairo

# --- Lighting / scene parameters ---
SIZE = 256
RADIUS = 36
BOX_MARGIN = 18

LIGHT_DIRECTION_DEG = -45     # angle in degrees: -45 = top-left, +45 = top-right
LIGHT_ELEVATION_DEG = 60      # how high the light is above the plane
SHADOW_SOFTNESS = 12          # larger = softer, blurrier shadow
SHADOW_OPACITY = 0.45
TEXT = "T"
FONT = "Sans Bold"
FONT_SIZE = 128

# --- Derived lighting vectors ---
theta = math.radians(LIGHT_DIRECTION_DEG)
lx = math.cos(theta)
ly = math.sin(theta)
shadow_offset = (
    int((1 - math.cos(math.radians(LIGHT_ELEVATION_DEG))) * SHADOW_SOFTNESS * lx * 3),
    int((1 - math.cos(math.radians(LIGHT_ELEVATION_DEG))) * SHADOW_SOFTNESS * ly * 3)
)

def rounded_rect(ctx, x, y, w, h, r):
    ctx.new_sub_path()
    ctx.arc(x + w - r, y + r, r, -math.pi/2, 0)
    ctx.arc(x + w - r, y + h - r, r, 0, math.pi/2)
    ctx.arc(x + r, y + h - r, r, math.pi/2, math.pi)
    ctx.arc(x + r, y + r, r, math.pi, 3*math.pi/2)
    ctx.close_path()

# --- Create surface ---
surface = cairo.ImageSurface(cairo.FORMAT_ARGB32, SIZE, SIZE)
ctx = cairo.Context(surface)
ctx.set_source_rgba(0, 0, 0, 0)
ctx.paint()

# --- Shadow ---
# A soft shadow using a radial gradient approximating Gaussian blur falloff
x0 = BOX_MARGIN + (SIZE - 2 * BOX_MARGIN) / 2 + shadow_offset[0]
y0 = BOX_MARGIN + (SIZE - 2 * BOX_MARGIN) / 2 + shadow_offset[1]
r_inner = (SIZE - 2 * BOX_MARGIN) / 2
r_outer = r_inner + SHADOW_SOFTNESS

# Physically inspired falloff (approximating Gaussian falloff)
def gaussian(alpha, sigma=0.5):
    # Maps [0,1] -> opacity using Gaussian decay
    return math.exp(-((alpha / sigma) ** 2))

gradient = cairo.RadialGradient(x0, y0, r_inner, x0, y0, r_outer)
for i in range(6):
    t = i / 5.0
    opacity = SHADOW_OPACITY * gaussian(t, sigma=0.6)
    gradient.add_color_stop_rgba(t, 0, 0, 0, opacity)
gradient.add_color_stop_rgba(1.0, 0, 0, 0, 0.0)

ctx.set_source(gradient)
rounded_rect(ctx, BOX_MARGIN + shadow_offset[0],
                  BOX_MARGIN + shadow_offset[1],
                  SIZE - 2 * BOX_MARGIN,
                  SIZE - 2 * BOX_MARGIN,
                  RADIUS)
ctx.fill()

# --- Main box ---
ctx.set_source_rgba(1, 1, 1, 1)
rounded_rect(ctx, BOX_MARGIN, BOX_MARGIN, SIZE - 2*BOX_MARGIN, SIZE - 2*BOX_MARGIN, RADIUS)
ctx.fill()

# --- Lighting overlay (subtle highlight and falloff) ---
# Simulates diffuse reflection: bright on light side, darker on shadow side
highlight = cairo.LinearGradient(0, 0, SIZE, SIZE)
# Direction of light: rotate gradient accordingly
hx = 0.5 - 0.5 * lx
hy = 0.5 - 0.5 * ly
highlight = cairo.LinearGradient(SIZE * hx, SIZE * hy,
                                 SIZE * (1 - hx), SIZE * (1 - hy))
highlight.add_color_stop_rgba(0.0, 1, 1, 1, 0.25)  # lit edge
highlight.add_color_stop_rgba(0.5, 1, 1, 1, 0.0)
highlight.add_color_stop_rgba(1.0, 0, 0, 0, 0.15)  # shadow edge

ctx.set_source(highlight)
rounded_rect(ctx, BOX_MARGIN, BOX_MARGIN, SIZE - 2*BOX_MARGIN, SIZE - 2*BOX_MARGIN, RADIUS)
ctx.fill()

# --- Text ---
ctx.select_font_face(FONT, cairo.FONT_SLANT_NORMAL, cairo.FONT_WEIGHT_NORMAL)
ctx.set_font_size(FONT_SIZE)
x_bearing, y_bearing, w, h, _, _ = ctx.text_extents(TEXT)
text_x = (SIZE - w) / 2 - x_bearing
text_y = (SIZE - h) / 2 - y_bearing
ctx.move_to(text_x, text_y)
ctx.set_source_rgba(0.1, 0.1, 0.1, 1)
ctx.show_text(TEXT)

# --- Save to PNG ---
out_png = sys.argv[1]
surface.write_to_png(out_png)
print(f"[ Saved ] {out_png}")
