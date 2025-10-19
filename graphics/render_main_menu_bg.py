#!/usr/bin/env python3
"""
3D animated cityscape background for main menu
Continuous looping animation with prism buildings and particle effects
"""

# /// script
# dependencies = [
#   "manim",
#   "numpy",
#   "pillow",
# ]
# ///

from manim import *
import numpy as np

class MainMenuBackground(ThreeDScene):
    def construct(self):
        # Set up 3D camera for cityscape view
        self.set_camera_orientation(phi=70 * DEGREES, theta=-30 * DEGREES, distance=15)
        
        # Create 2D grid floor
        grid_size = 20
        grid_spacing = 1
        grid_lines = VGroup()
        
        # Create grid lines
        for i in range(-grid_size//2, grid_size//2 + 1):
            # X-direction lines
            line_x = Line3D(
                start=[i * grid_spacing, -grid_size//2 * grid_spacing, 0],
                end=[i * grid_spacing, grid_size//2 * grid_spacing, 0],
                color=BLUE_E,
                stroke_width=1
            )
            grid_lines.add(line_x)
            
            # Y-direction lines
            line_y = Line3D(
                start=[-grid_size//2 * grid_spacing, i * grid_spacing, 0],
                end=[grid_size//2 * grid_spacing, i * grid_spacing, 0],
                color=BLUE_E,
                stroke_width=1
            )
            grid_lines.add(line_y)
        
        grid_lines.set_opacity(0.3)
        
        # Create cityscape using prisms (buildings)
        buildings = VGroup()
        building_positions = [
            ([-3, -2, 0], 2.5, BLUE),
            ([1, -3, 0], 3.2, GREEN),
            ([-1, 1, 0], 1.8, RED),
            ([3, 2, 0], 2.9, ORANGE),
            ([-4, 3, 0], 2.1, PURPLE),
            ([2, -1, 0], 3.5, YELLOW),
            ([-2, -4, 0], 1.5, PINK),
            ([4, -2, 0], 2.7, TEAL),
            ([0, 3, 0], 2.3, MAROON),
            ([-3, 0, 0], 1.9, GREY),
        ]
        
        for pos, height, color in building_positions:
            building = Prism(
                dimensions=[0.8, 0.8, height],
                fill_color=color,
                fill_opacity=0.7,
                stroke_color=WHITE,
                stroke_width=1
            )
            building.move_to([pos[0], pos[1], height/2])
            buildings.add(building)
        
        # Create particle effects hovering up from the grid
        particles = VGroup()
        for i in range(15):  # Reduced from 50 to 15
            particle = Dot3D(
                point=[
                    np.random.uniform(-6, 6),
                    np.random.uniform(-6, 6),
                    np.random.uniform(0.1, 1.5)
                ],
                radius=0.05,
                color=interpolate_color(BLUE, WHITE, np.random.random())
            )
            particles.add(particle)
        
        # Add all objects to scene
        self.add(grid_lines, buildings, particles)
        
        # Create continuous looping animation
        # First half: Camera orbits while buildings and particles animate
        building_animations = []
        building_animations.extend([
            Rotate(building, angle=PI/4, axis=UP, about_point=building.get_center()) 
            for building in buildings[::2]
        ])
        building_animations.extend([
            Rotate(building, angle=-PI/4, axis=UP, about_point=building.get_center()) 
            for building in buildings[1::2]
        ])
        
        particle_animations = [
            particle.animate.shift([
                np.random.uniform(-0.5, 0.5),
                np.random.uniform(-0.5, 0.5),
                np.random.uniform(1, 3)
            ]) for particle in particles
        ]
        
        # Start camera movement and animations simultaneously
        self.begin_ambient_camera_rotation(rate=0.15)
        
        self.play(
            *building_animations,
            *particle_animations,
            run_time=4  # Reduced from 6 to 4
        )
        
        # Second half: Reverse animations while camera continues
        reverse_building_animations = []
        reverse_building_animations.extend([
            Rotate(building, angle=-PI/4, axis=UP, about_point=building.get_center()) 
            for building in buildings[::2]
        ])
        reverse_building_animations.extend([
            Rotate(building, angle=PI/4, axis=UP, about_point=building.get_center()) 
            for building in buildings[1::2]
        ])
        
        reset_particle_animations = [
            particle.animate.move_to([
                np.random.uniform(-6, 6),
                np.random.uniform(-6, 6),
                np.random.uniform(0.1, 1.5)
            ]) for particle in particles
        ]
        
        self.play(
            *reverse_building_animations,
            *reset_particle_animations,
            run_time=4  # Reduced from 6 to 4
        )
        
        self.stop_ambient_camera_rotation()

def render_background():
    """Render the main menu background animation to MP4"""
    import os
    
    # Get the directory containing this script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    build_dir = os.path.join(script_dir, "build")
    
    # Ensure output directory exists
    os.makedirs(build_dir, exist_ok=True)
    
    # Configure manim to output MP4 at lower quality for faster rendering
    config.media_dir = build_dir
    config.output_file = "main-menu-bg"
    config.format = "mp4"
    config.frame_rate = 15  # Reduced from 30 to 15
    config.quality = "low_quality"  # Use low quality for faster rendering
    
    # Create and render the scene
    scene = MainMenuBackground()
    scene.render()
    
    output_path = os.path.join(build_dir, "main-menu-bg.mp4")
    print(f"Background animation rendered to: {output_path}")

if __name__ == "__main__":
    render_background()

