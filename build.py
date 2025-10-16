#!/usr/bin/env python3
"""
Full Crisis 3 Build Script
Builds the game for Linux x86_64 and Windows x86_64 platforms
"""

import os
import shutil
import subprocess
import sys
from pathlib import Path

def run_command(cmd, cwd=None, description=""):
    """Run a shell command and handle errors"""
    print(f"Building: {description}")
    print(f"   Command: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    
    try:
        result = subprocess.run(cmd, cwd=cwd, check=True, capture_output=True, text=True)
        if result.stdout:
            print(f"   Output: {result.stdout.strip()}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"ERROR: {e}")
        if e.stdout:
            print(f"   Stdout: {e.stdout}")
        if e.stderr:
            print(f"   Stderr: {e.stderr}")
        return False

def clean_release_dir():
    """Clean the release directory"""
    release_dir = Path("release")
    if release_dir.exists():
        print("Cleaning release directory...")
        shutil.rmtree(release_dir)
    release_dir.mkdir(exist_ok=True)
    return release_dir

def build_desktop(platform, runtime_id, output_name, release_dir):
    """Build desktop version for specified platform"""
    print(f"\nBuilding {platform} version...")
    
    desktop_dir = Path("FullCrisis3.Desktop")
    output_dir = release_dir / f"temp_{platform}"
    
    # Build command
    cmd = [
        "dotnet", "publish", 
        "-c", "Release",
        "-r", runtime_id,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=true",
        "-o", str(output_dir.absolute())
    ]
    
    if not run_command(cmd, cwd=desktop_dir, description=f"Building {platform} executable"):
        return False
    
    # Find the executable and copy to final location
    exe_extension = ".exe" if platform == "Windows" else ""
    source_exe = output_dir / f"FullCrisis3.Desktop{exe_extension}"
    target_exe = release_dir / output_name
    
    if source_exe.exists():
        shutil.copy2(source_exe, target_exe)
        print(f"SUCCESS: {platform} build complete: {target_exe}")
    else:
        print(f"ERROR: Executable not found: {source_exe}")
        return False
    
    # Clean up temp directory
    shutil.rmtree(output_dir)
    return True



def main():
    """Main build function"""
    print("Full Crisis 3 Build Script")
    print("=" * 50)
    
    # Check if we're in the right directory
    if not Path("FullCrisis3.sln").exists():
        print("ERROR: FullCrisis3.sln not found. Please run this script from the project root.")
        sys.exit(1)
    
    # Clean release directory
    release_dir = clean_release_dir()
    
    # Track build results
    results = {}
    
    # Build Linux version
    results['Linux'] = build_desktop(
        "Linux", 
        "linux-x64", 
        "FullCrisis3.linux.x64", 
        release_dir
    )
    
    # Build Windows version
    results['Windows'] = build_desktop(
        "Windows", 
        "win-x64", 
        "FullCrisis3.win.x64.exe", 
        release_dir
    )
    
    
    # Print summary
    print("\n" + "=" * 50)
    print("Build Summary:")
    for platform, success in results.items():
        status = "SUCCESS" if success else "FAILED"
        print(f"   {platform}: {status}")
    
    if all(results.values()):
        print("\nAll builds completed successfully!")
        print(f"Release files are in: {release_dir.absolute()}")
        print("\nFiles created:")
        for item in sorted(release_dir.rglob("*")):
            if item.is_file():
                rel_path = item.relative_to(release_dir)
                print(f"   {rel_path}")
    else:
        print("\nWARNING: Some builds failed. Check the output above for details.")
        failed_platforms = [p for p, success in results.items() if not success]
        print(f"   Failed platforms: {', '.join(failed_platforms)}")
        sys.exit(1)

if __name__ == "__main__":
    main()