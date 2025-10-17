#!/usr/bin/env python3
"""
Asset Download Script for Full Crisis 3
Downloads third-party assets from URLs to specified file paths.
Supports both direct file downloads and ZIP archive extraction.
"""

import os
import urllib.request
import zipfile
import io
from pathlib import Path

class Asset:
    """Represents an asset to download."""
    def __init__(self, url: str, zip_file: str = None):
        self.url = url
        self.zip_file = zip_file  # If set, extract this file from the ZIP

    def is_zip(self) -> bool:
        return self.zip_file is not None

# Global asset configuration - file paths relative to repository root
THIRDPARTY_ASSETS = {
    "thirdparty/fonts/Rockwell.ttf":
        Asset("https://dn.freefontsfamily.com/download/rockwell-font", "ROCK.TTF"),
    "thirdparty/fonts/Letter-Gothic.ttf":
        Asset("https://media.fontsgeek.com/download/zip/l/e/letter-gothic_WIGQV.zip", "Letter Gothic/Letter Gothic Regular/Letter Gothic Regular.ttf"),
    # Example ZIP archive extraction:
    # "thirdparty/examples/font.ttf": Asset("https://example.com/fonts.zip", "font.ttf"),
    # "thirdparty/images/texture.png": Asset("https://example.com/assets.zip", "textures/texture.png"),
}

def download_asset(asset: Asset, filepath: Path) -> bool:
    """Download an asset (direct file or ZIP extraction) to filepath."""
    try:
        if asset.is_zip():
            print(f"> Downloading ZIP {asset.url} -> extracting {asset.zip_file} -> {filepath}")
            return download_from_zip(asset.url, asset.zip_file, filepath)
        else:
            print(f"> Downloading {asset.url} -> {filepath}")
            return download_direct_file(asset.url, filepath)
        
    except Exception as e:
        print(f"[ Failed ] {asset.url}: {e}")
        return False

def download_direct_file(url: str, filepath: Path) -> bool:
    """Download a file directly from URL to filepath."""
    req = urllib.request.Request(
        url,
        headers={'User-Agent': 'Mozilla/5.0'}
    )

    with urllib.request.urlopen(req) as response:
        with open(filepath, 'wb') as f:
            f.write(response.read())

    return True

def download_from_zip(url: str, zip_filename: str, output_filepath: Path) -> bool:
    """Download a ZIP archive and extract a specific file from it."""
    req = urllib.request.Request(
        url,
        headers={'User-Agent': 'Mozilla/5.0'}
    )

    with urllib.request.urlopen(req) as response:
        zip_data = response.read()

    # Extract the specified file from the ZIP archive in memory
    with zipfile.ZipFile(io.BytesIO(zip_data)) as zip_file:
        if zip_filename not in zip_file.namelist():
            raise Exception(f"File '{zip_filename}' not found in ZIP archive")

        with zip_file.open(zip_filename) as source_file:
            with open(output_filepath, 'wb') as output_file:
                output_file.write(source_file.read())

    return True

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
    
    for file_path, asset in THIRDPARTY_ASSETS.items():
        filepath = Path(file_path)
        
        if should_download(filepath):
            # Create parent directories if they don't exist
            filepath.parent.mkdir(parents=True, exist_ok=True)
            
            if download_asset(asset, filepath):
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
