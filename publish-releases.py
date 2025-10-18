#!/usr/bin/env python3
"""
Full Crisis 3 Release Publisher
Publishes release files to the 'pages' branch for GitHub Pages distribution
"""

import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

# HTML template for the release download page
INDEX_HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Full Crisis 3 - Download</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            margin: 0;
            padding: 0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .container {
            background: white;
            border-radius: 12px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            padding: 40px;
            max-width: 600px;
            text-align: center;
        }
        h1 {
            color: #333;
            margin-bottom: 10px;
            font-size: 2.5em;
        }
        .subtitle {
            color: #666;
            margin-bottom: 30px;
            font-size: 1.1em;
        }
        .download-section {
            margin: 30px 0;
        }
        .download-button {
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            padding: 15px 30px;
            border-radius: 8px;
            margin: 10px;
            font-weight: bold;
            font-size: 1.1em;
            transition: transform 0.2s, box-shadow 0.2s;
            min-width: 200px;
        }
        .download-button:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0,0,0,0.2);
        }
        .platform-icon {
            margin-right: 8px;
            font-size: 1.2em;
        }
        .file-info {
            font-size: 0.9em;
            color: #888;
            margin-top: 5px;
        }
        .controls {
            background: #f8f9fa;
            border-radius: 8px;
            padding: 20px;
            margin-top: 30px;
            text-align: left;
        }
        .controls h3 {
            margin-top: 0;
            color: #333;
        }
        .controls ul {
            margin: 0;
            padding-left: 20px;
        }
        .controls li {
            margin: 5px 0;
            color: #555;
        }
        .footer {
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            color: #888;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Full Crisis 3</h1>
        <p class="subtitle">Cross-platform 2D game built with .NET 9 and MonoGame</p>
        
        <div class="download-section">
            <h2>Download Latest Release</h2>
            
            <a href="FullCrisis3.linux.x64" class="download-button" download>
                <span class="platform-icon">🐧</span>
                Linux x64
                <div class="file-info">Self-contained executable</div>
            </a>
            
            <a href="FullCrisis3.win.x64.exe" class="download-button" download>
                <span class="platform-icon">🪟</span>
                Windows x64
                <div class="file-info">Self-contained executable</div>
            </a>
        </div>
        
        <div class="controls">
            <h3>Controls</h3>
            <ul>
                <li><strong>Navigation:</strong> Arrow Keys, WASD, Mouse, or Gamepad D-Pad/Left Stick</li>
                <li><strong>Select:</strong> Enter, Space, Mouse Click, or Gamepad A button</li>
                <li><strong>Back/Cancel:</strong> Escape or Gamepad B button</li>
                <li><strong>Quit:</strong> Escape (from main menu)</li>
            </ul>
        </div>
        
        <div class="footer">
            <p>Built with .NET 9 and MonoGame Framework</p>
        </div>
    </div>
</body>
</html>"""

def run_command(cmd, cwd=None, description="", capture_output=True):
    """Run a shell command and handle errors"""
    print(f"Running: {description}")
    print(f"Command: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    
    try:
        result = subprocess.run(cmd, cwd=cwd, check=True, capture_output=capture_output, text=True)
        if capture_output and result.stdout:
            print(f"Output: {result.stdout.strip()}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"ERROR: {e}")
        if capture_output:
            if e.stdout:
                print(f"Stdout: {e.stdout}")
            if e.stderr:
                print(f"Stderr: {e.stderr}")
        return False

def check_release_files():
    """Check if required release files exist"""
    release_dir = Path("release")
    if not release_dir.exists():
        print("ERROR: release directory not found. Run build.py first.")
        return False
    
    required_files = ["FullCrisis3.linux.x64", "FullCrisis3.win.x64.exe"]
    missing_files = []
    
    for file_name in required_files:
        file_path = release_dir / file_name
        if not file_path.exists():
            missing_files.append(file_name)
    
    if missing_files:
        print(f"ERROR: Missing release files: {', '.join(missing_files)}")
        print("Run build.py to create release files first.")
        return False
    
    print("SUCCESS: All required release files found")
    return True

def create_pages_branch(temp_dir):
    """Create orphan pages branch and copy files"""
    temp_path = Path(temp_dir)
    
    # Initialize git repo in temp directory
    if not run_command(["git", "init"], cwd=temp_path, description="Initializing temporary git repository"):
        return False
    
    # # Configure git (use existing config or defaults)
    # run_command(["git", "config", "user.name", "Release Publisher"], cwd=temp_path, description="Setting git user name")
    # run_command(["git", "config", "user.email", "noreply@github.com"], cwd=temp_path, description="Setting git user email")
    
    # Copy release files
    release_dir = Path("release")
    
    linux_exe = release_dir / "FullCrisis3.linux.x64"
    windows_exe = release_dir / "FullCrisis3.win.x64.exe"
    
    shutil.copy2(linux_exe, temp_path / "FullCrisis3.linux.x64")
    shutil.copy2(windows_exe, temp_path / "FullCrisis3.win.x64.exe")
    
    # Create index.html
    index_file = temp_path / "index.html"
    with open(index_file, 'w', encoding='utf-8') as f:
        f.write(INDEX_HTML_TEMPLATE)
    
    print("SUCCESS: Files copied to temporary directory")
    
    # Add and commit files
    if not run_command(["git", "add", "."], cwd=temp_path, description="Adding files to git"):
        return False
    
    if not run_command(["git", "commit", "-m", "Release files"], cwd=temp_path, description="Creating initial commit"):
        return False
    
    print("SUCCESS: Initial commit created")
    return True

def push_to_pages_branch(temp_dir):
    """Push the pages branch to origin"""
    temp_path = Path(temp_dir)
    
    # Get the current repository's remote URL
    result = subprocess.run(["git", "remote", "get-url", "origin"], capture_output=True, text=True)
    if result.returncode != 0:
        print("ERROR: Could not get origin remote URL")
        return False
    
    remote_url = result.stdout.strip()
    print(f"Remote URL: {remote_url}")
    
    # Add remote to temp repo
    if not run_command(["git", "remote", "add", "origin", remote_url], cwd=temp_path, description="Adding remote origin"):
        return False
    
    # Force push to pages branch (this will overwrite any existing pages branch)
    if not run_command(["git", "push", "-f", "origin", "HEAD:pages"], cwd=temp_path, description="Force pushing to pages branch"):
        return False
    
    print("SUCCESS: Pages branch published")
    return True

def main():
    """Main publish function"""
    print("Full Crisis 3 Release Publisher")
    print("=" * 50)
    
    # Check if we're in the right directory
    if not Path("FullCrisis3.sln").exists():
        print("ERROR: FullCrisis3.sln not found. Please run this script from the project root.")
        sys.exit(1)

    # Perform a build if requested
    if 'build' in sys.argv or 'rebuild' in sys.argv:
        subprocess.run(['uv', 'run', 'build.py', 'download-assets'], check=True)
    else:
        print('Skipping build because "build" not passed as argument')

    # Check if release files exist
    if not check_release_files():
        sys.exit(1)
    
    # Create temporary directory
    with tempfile.TemporaryDirectory() as temp_dir:
        print(f"Using temporary directory: {temp_dir}")
        
        # Create pages branch content
        if not create_pages_branch(temp_dir):
            print("ERROR: Failed to create pages branch content")
            sys.exit(1)
        
        # Push to remote pages branch
        if not push_to_pages_branch(temp_dir):
            print("ERROR: Failed to push pages branch")
            sys.exit(1)
    
    print("\n" + "=" * 50)
    print("SUCCESS: Release published to pages branch")
    print("GitHub Pages will be available at your repository's pages URL")
    print("Note: It may take a few minutes for GitHub Pages to update")

if __name__ == "__main__":
    main()
