#!/usr/bin/env python3
"""
3D animated background generator for main menu
Generates a 12-second MP4 with simple 3D design
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
        # Set up 3D camera with dynamic movement
        self.set_camera_orientation(phi=75 * DEGREES, theta=-45 * DEGREES)
        
        # Create 3D axes with subtle grid
        axes = ThreeDAxes(
            x_range=[-6, 6, 2],
            y_range=[-4, 4, 2], 
            z_range=[-3, 3, 1],
            x_length=8,
            y_length=6,
            z_length=4,
            axis_config={"color": BLUE_E, "stroke_width": 1},
        )
        axes.set_opacity(0.3)
        
        # Create floating geometric shapes
        cube = Cube(side_length=1.5, fill_color=BLUE, fill_opacity=0.7, stroke_color=WHITE)
        cube.move_to([2, 1, 0])
        
        sphere = Sphere(radius=0.8, fill_color=RED, fill_opacity=0.6)
        sphere.move_to([-2, -1, 1])
        
        torus = Torus(major_radius=1.2, minor_radius=0.4, fill_color=GREEN, fill_opacity=0.8)
        torus.move_to([0, 2, -1])
        
        # Create particle system effect
        particles = VGroup()
        for i in range(20):
            particle = Dot3D(
                point=[
                    np.random.uniform(-4, 4),
                    np.random.uniform(-3, 3), 
                    np.random.uniform(-2, 2)
                ],
                radius=0.05,
                color=random_color()
            )
            particles.add(particle)
        
        # Add all objects to scene
        self.add(axes, cube, sphere, torus, particles)
        
        # Animate camera movement (first 4 seconds)
        self.move_camera(phi=60 * DEGREES, theta=-60 * DEGREES, run_time=4)
        
        # Animate shapes rotation and movement (next 4 seconds)
        self.play(
            Rotate(cube, angle=PI, axis=UP, about_point=cube.get_center()),
            Rotate(sphere, angle=2*PI, axis=RIGHT, about_point=sphere.get_center()),
            Rotate(torus, angle=PI, axis=OUT, about_point=torus.get_center()),
            run_time=4
        )
        
        # Move camera during shape animation
        self.move_camera(phi=45 * DEGREES, theta=-90 * DEGREES, run_time=0.1)
        
        # Final camera sweep and particle animation (last 4 seconds)
        self.play(
            *[particle.animate.shift([
                np.random.uniform(-1, 1),
                np.random.uniform(-1, 1),
                np.random.uniform(-1, 1)
            ]) for particle in particles],
            run_time=4
        )
        
        # Final camera position
        self.move_camera(phi=90 * DEGREES, theta=-30 * DEGREES, run_time=0.1)

def render_background():
    """Render the main menu background animation to MP4"""
    import os
    
    # Get the directory containing this script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    build_dir = os.path.join(script_dir, "build")
    
    # Ensure output directory exists
    os.makedirs(build_dir, exist_ok=True)
    
    # Configure manim to output MP4 at 30fps
    config.media_dir = build_dir
    config.output_file = "main-menu-bg"
    config.format = "mp4"
    config.frame_rate = 30
    
    # Create and render the scene
    scene = MainMenuBackground()
    scene.render()
    
    output_path = os.path.join(build_dir, "main-menu-bg.mp4")
    print(f"Background animation rendered to: {output_path}")

if __name__ == "__main__":
    render_background()