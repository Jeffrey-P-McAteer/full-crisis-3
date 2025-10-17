#!/usr/bin/env python3
"""
Full Crisis 3 Debug Runner
Builds and runs the debug version of the game for the host platform
"""

import os
import platform
import subprocess
import sys
from pathlib import Path

def detect_platform():
    """Detect the host platform and return runtime ID"""
    system = platform.system().lower()
    
    if system == "linux":
        return "linux-x64", "FullCrisis3.Desktop"
    elif system == "windows":
        return "win-x64", "FullCrisis3.Desktop.exe"
    elif system == "darwin":
        # macOS - use linux runtime as fallback
        print("WARNING: macOS detected, using linux-x64 runtime")
        return "linux-x64", "FullCrisis3.Desktop"
    else:
        print(f"ERROR: Unsupported platform: {system}")
        sys.exit(1)

def run_command(cmd, cwd=None, description="", capture_output=False):
    """Run a shell command"""
    print(f"Running: {description}")
    print(f"Command: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    print()
    
    try:
        if capture_output:
            result = subprocess.run(cmd, cwd=cwd, check=True, capture_output=True, text=True)
            return result
        else:
            # Connect stdout/stderr to terminal for interactive execution
            result = subprocess.run(cmd, cwd=cwd, check=True)
            return result
    except subprocess.CalledProcessError as e:
        print(f"ERROR: Command failed with exit code {e.returncode}")
        if capture_output and e.stdout:
            print(f"Stdout: {e.stdout}")
        if capture_output and e.stderr:
            print(f"Stderr: {e.stderr}")
        sys.exit(e.returncode)

def build_debug(runtime_id):
    """Build the debug version for the host platform"""
    desktop_dir = Path("FullCrisis3.Desktop")
    
    if not desktop_dir.exists():
        print("ERROR: FullCrisis3.Desktop directory not found")
        sys.exit(1)
    
    # Build debug version
    cmd = [
        "dotnet", "build",
        "-c", "Debug",
        "-r", runtime_id
    ]
    
    run_command(cmd, cwd=desktop_dir, description=f"Building debug version for {runtime_id}")

def find_debug_executable(runtime_id, exe_name):
    """Find the debug executable in the build output"""
    desktop_dir = Path("FullCrisis3.Desktop")
    
    # Common debug output paths
    possible_paths = [
        desktop_dir / "bin" / "Debug" / "net9.0" / runtime_id / exe_name,
        desktop_dir / "bin" / "Debug" / "net9.0" / exe_name,
        desktop_dir / "bin" / "Debug" / exe_name
    ]
    
    for path in possible_paths:
        if path.exists():
            return path
    
    # If not found, search recursively in bin/Debug
    debug_dir = desktop_dir / "bin" / "Debug"
    if debug_dir.exists():
        for exe_path in debug_dir.rglob(exe_name):
            return exe_path
    
    print("ERROR: Debug executable not found")
    print("Searched in:")
    for path in possible_paths:
        print(f"  {path}")
    sys.exit(1)

def run_game(exe_path, game_args):
    """Run the game executable with arguments"""
    # Make executable on Unix systems
    if platform.system() != "Windows":
        exe_path.chmod(0o755)
    
    # Prepare command with game arguments
    cmd = [str(exe_path)] + game_args
    
    # Run the game with stdout connected to terminal
    run_command(cmd, description=f"Running debug executable with args: {game_args}")

def main():
    """Main runner function"""
    print("Full Crisis 3 Debug Runner")
    print("=" * 50)
    
    # Check if we're in the right directory
    if not Path("FullCrisis3.sln").exists():
        print("ERROR: FullCrisis3.sln not found. Please run this script from the project root.")
        sys.exit(1)
    
    # Get command line arguments to pass to the game
    game_args = sys.argv[1:]
    if game_args:
        print(f"Game arguments: {' '.join(game_args)}")
    
    # Detect platform
    runtime_id, exe_name = detect_platform()
    print(f"Detected platform: {runtime_id}")
    print(f"Executable name: {exe_name}")
    print()
    
    # Build debug version
    build_debug(runtime_id)
    print()
    
    # Find the executable
    exe_path = find_debug_executable(runtime_id, exe_name)
    print(f"Found executable: {exe_path}")
    print()
    
    # Run the game
    run_game(exe_path, game_args)

if __name__ == "__main__":
    main()
