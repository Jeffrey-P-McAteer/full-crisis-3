#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.11"
# dependencies = [
#    "GitPython",
#    "matplotlib>=3.7.0",
#    "pandas>=2.0.0",
#    "seaborn>=0.12.0",
#    "numpy>=1.24.0"
# ]
# ///
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
import webbrowser
import datetime
import base64

# third-party dependencies
import git

BUILD_TIMESTAMP = datetime.datetime.now().strftime('%Y-%m-%d %H:%M')

r = git.Repo('.')
h = r.head.commit.hexsha[:7]
dirty = r.is_dirty()
if dirty:
    added,deleted = 0,0
    for diff in r.index.diff(None):
        for (a,b) in [(diff.a_blob, diff.b_blob)]:
            pass
    # simplified: call git directly for numstat
    out = subprocess.getoutput('git diff --numstat')
    added = sum(int(l.split()[0]) for l in out.splitlines() if l.split()[0].isdigit())
    deleted = sum(int(l.split()[1]) for l in out.splitlines() if l.split()[1].isdigit())
    GIT_HASH_AND_DELTAS = f"{h}-dirty+{added}-{deleted}"
else:
    GIT_HASH_AND_DELTAS = h

# HTML template for the release download page
INDEX_HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Full Crisis 3 - Download</title>
    <style>
        @font-face {
          font-family: "Letter-Gothic";
          src: url("Letter-Gothic.ttf") format("truetype");
          font-weight: normal;
          font-style: normal;
        }
        @font-face {
          font-family: "Rockwell";
          src: url("Rockwell.ttf") format("truetype");
          font-weight: normal;
          font-style: normal;
        }
        body {
            font-family: 'Letter-Gothic', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            margin: 0;
            padding: 80pt 0pt 0pt 0pt;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        h1, h2, h3 {
            font-family: 'Rockwell', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
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
        .download-button-holder {
            display: flex;
            flex-direction: row;
        }
        .download-button {
            /*display: inline-block;*/
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
            display: flex;
            align-items: center;   /* Vertically centers items */
            justify-content: center; /* Horizontally centers items (optional) */
            text-decoration: none; /* Removes underline */
            gap: 8px; /* Space between image and text */
            flex-direction: row;
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
        .project-status {
            background: #f8f9fa;
            border-radius: 8px;
            padding: 20px;
            margin-top: 30px;
            text-align: center;
        }
        .project-status h3 {
            margin-top: 0;
            color: #333;
        }
        .chart-container {
            margin: 20px 0;
            max-width: 100%;
            overflow: hidden;
        }
        .chart-container img {
            max-width: 100%;
            height: auto;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
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
        <p class="subtitle">
             Join historic disasters as emergency response personnel and hone your crisis-solving skills while saving the world!
        </p>
        
        <div class="download-section">
            <h2>Download Latest Release</h2>
            <div class="download-button-holder">
                <a href="FullCrisis3.linux.x64" class="download-button" style="width:35%;">
                    <img class="platform-icon" src="linux-icon.png" width="64" height="64" />
                    <span>Linux x64</span>
                </a>

                <a href="FullCrisis3.win.x64.exe" class="download-button" style="width:35%;">
                    <img class="platform-icon" src="windows-icon.png" width="64" height="64" />
                    <span>Windows x64</span>
                </a>
            </div>
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
        
        <div class="project-status">
            <h3>Project Status</h3>
            <div class="chart-container">
                <img src="data:image/png;base64,{CHART_DATA}" alt="Lines of Code Over Time" />
            </div>
        </div>
        
        <div class="footer">
            <p>Built at """+BUILD_TIMESTAMP+""" from """+GIT_HASH_AND_DELTAS+"""</p>
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

def generate_project_stats_chart():
    """Generate project statistics chart and return base64 encoded image"""
    print("Generating project statistics chart...")
    
    with tempfile.TemporaryDirectory() as stats_temp_dir:
        # Run project-stats.py script to generate charts
        if not run_command([
            "uv", "run", "project-stats.py", stats_temp_dir
        ], description="Generating project statistics"):
            print("WARNING: Failed to generate project statistics chart")
            return None
        
        # Read the generated chart file
        chart_file = Path(stats_temp_dir) / "lines_of_code_over_time.png"
        if not chart_file.exists():
            print("WARNING: Chart file not found after generation")
            return None
        
        # Convert to base64
        with open(chart_file, 'rb') as f:
            chart_data = base64.b64encode(f.read()).decode('utf-8')
        
        print("SUCCESS: Project statistics chart generated and encoded")
        return chart_data

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
    repo_dir = Path(os.path.join(os.path.dirname(__file__)))
    release_dir = repo_dir / "release"
    
    linux_exe = release_dir / "FullCrisis3.linux.x64"
    windows_exe = release_dir / "FullCrisis3.win.x64.exe"
    
    shutil.copy2(linux_exe, temp_path / "FullCrisis3.linux.x64")
    shutil.copy2(windows_exe, temp_path / "FullCrisis3.win.x64.exe")

    linux_icon_png = repo_dir / "graphics" / "linux-icon.png"
    windows_icon_png = repo_dir / "graphics" / "windows-icon.png"

    shutil.copy2(linux_icon_png, temp_path / "linux-icon.png")
    shutil.copy2(windows_icon_png, temp_path / "windows-icon.png")

    letter_gothic_ttf = repo_dir / "thirdparty-assets" / "fonts" / "Letter-Gothic.ttf"
    rockwell_ttf = repo_dir / "thirdparty-assets" / "fonts" / "Rockwell.ttf"

    shutil.copy2(letter_gothic_ttf, temp_path / "Letter-Gothic.ttf")
    shutil.copy2(rockwell_ttf, temp_path / "Rockwell.ttf")

    # Create the CNAME file, used by github itself for custom domains
    with open(temp_path / "CNAME", 'w') as fd:
        fd.write('full-crisis-3.jmcateer.com\n')
    
    # Generate project statistics chart
    chart_data = generate_project_stats_chart()
    
    # Create index.html with embedded chart
    index_file = temp_path / "index.html"
    with open(index_file, 'w', encoding='utf-8') as f:
        html_content = INDEX_HTML_TEMPLATE
        if chart_data:
            html_content = html_content.replace("{CHART_DATA}", chart_data)
        else:
            # If chart generation failed, remove the chart section
            html_content = html_content.replace(
                '''        <div class="project-status">
            <h3>Project Status</h3>
            <div class="chart-container">
                <img src="data:image/png;base64,{CHART_DATA}" alt="Lines of Code Over Time" />
            </div>
        </div>
        ''', '')
        f.write(html_content)
    
    print("SUCCESS: Files copied to temporary directory")

    if 'preview' in sys.argv:
        webbrowser.open(f'file:///{str(index_file)}')
        input(f'Pausing to allow user to inspect page at {index_file}')
        input('Press enter to continue...')
    else:
        print('Pushing directly to remote because "preview" not passed as an argument')
    
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
