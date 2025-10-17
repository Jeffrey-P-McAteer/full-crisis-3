#!/usr/bin/env python3
"""
Asset Download Script for Full Crisis 3
Downloads third-party assets from URLs to specified file paths.
"""

import os
import urllib.request
from pathlib import Path

# Global asset configuration - file paths relative to repository root
THIRDPARTY_ASSETS = {
    "thirdparty/fonts/PlayfairDisplay-Bold.ttf": "https://github.com/google/fonts/raw/main/ofl/playfairdisplay/PlayfairDisplay%5Bwght%5D.ttf",
    "thirdparty/fonts/RobotoMono-Regular.ttf": "https://github.com/google/fonts/raw/main/ofl/robotomono/RobotoMono%5Bwght%5D.ttf",
    # Add more assets here as needed
    # "thirdparty/images/texture.png": "https://example.com/texture.png",
}

def download_file(url: str, filepath: Path) -> bool:
    """Download a file from URL to filepath."""
    try:
        print(f"> Downloading {url} -> {filepath}")
        
        # Create a request with user agent to avoid blocking
        req = urllib.request.Request(
            url, 
            headers={'User-Agent': 'Mozilla/5.0'}
        )
        
        with urllib.request.urlopen(req) as response:
            with open(filepath, 'wb') as f:
                f.write(response.read())
        
        return True
        
    except Exception as e:
        print(f"[ Failed ] {url}: {e}")
        return False

def should_download(filepath: Path) -> bool:
    """Check if file should be downloaded (doesn't exist or has 0 bytes)."""
    if not filepath.exists():
        return True
    
    if filepath.stat().st_size == 0:
        return True
    
    return False

def main():

    downloaded_count = 0
    skipped_count = 0
    failed_count = 0
    
    for file_path, url in THIRDPARTY_ASSETS.items():
        filepath = Path(file_path)
        
        if should_download(filepath):
            # Create parent directories if they don't exist
            filepath.parent.mkdir(parents=True, exist_ok=True)
            
            if download_file(url, filepath):
                downloaded_count += 1
            else:
                failed_count += 1
        else:
            print(f"> {filepath} already exists and has content, skipping")
            skipped_count += 1
    
    print()
    print(f"Summary: {downloaded_count} downloaded, {skipped_count} skipped, {failed_count} failed")
    
    if failed_count > 0:
        exit(1)

if __name__ == "__main__":
    main()
