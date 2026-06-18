#!/usr/bin/env python3
import argparse
import os
import re
import subprocess
from pathlib import Path
from typing import Optional
import requests
from bs4 import BeautifulSoup
from PIL import Image

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.5",
}

DEFAULT_URL = "https://civilization.fandom.com/wiki/Improvement_(Civ3)"

# Keywords to match improvements and map them to our IDs
IMPROVEMENT_ID_MAP = {
    "irrigation": "farm",
    "mine": "mine",
    "road": "road",
    "railroad": "railroad",
    "fortress": "fortress",
    "barricade": "barricade",
    "outpost": "outpost",
    "airfield": "airfield",
    "radar tower": "radar_tower",
    "colony": "colony",
    "plantation": "plantation"
}

def clean_fandom_image_url(url: str) -> str:
    return re.sub(r"/revision/latest.*$", "", url)

def fetch_html(url: str) -> str:
    try:
        result = subprocess.run(
            ["curl", "-s", "-L", "-A", HEADERS["User-Agent"], url],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="ignore",
            timeout=20,
        )
        if result.returncode == 0 and result.stdout.strip():
            return result.stdout
    except Exception:
        pass

    resp = requests.get(url, headers=HEADERS, timeout=20)
    resp.raise_for_status()
    return resp.text

def download_and_convert_to_png(url: str, path: Path) -> bool:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = path.with_suffix(".tmp")
    
    success = False
    try:
        result = subprocess.run(
            ["curl", "-s", "-L", "-A", HEADERS["User-Agent"], "-o", str(temp_path), url],
            timeout=20,
        )
        if result.returncode == 0 and temp_path.exists() and temp_path.stat().st_size > 0:
            success = True
    except Exception:
        pass

    if not success:
        try:
            resp = requests.get(url, headers=HEADERS, timeout=20)
            if resp.status_code == 200:
                temp_path.write_bytes(resp.content)
                success = True
        except Exception:
            pass

    if success and temp_path.exists():
        try:
            with Image.open(temp_path) as img:
                img.save(path, "PNG")
            temp_path.unlink()
            return True
        except Exception as e:
            print(f"[error] PIL conversion to PNG failed for {url}: {e}")
            if temp_path.exists():
                temp_path.unlink()
            
    return False

def main():
    parser = argparse.ArgumentParser(description="Download Civ3 improvement images from Fandom")
    parser.add_argument("--url", default=DEFAULT_URL, help="Source page URL")
    parser.add_argument("--out-dir", default="assets/improvements", help="Output dir for images")
    args = parser.parse_args()

    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    print(f"[info] Fetching: {args.url}")
    html = fetch_html(args.url)
    soup = BeautifulSoup(html, "html.parser")

    # Let's search all images on the page
    images_downloaded = {}
    
    for img_tag in soup.find_all("img"):
        src = img_tag.get("data-src") or img_tag.get("src")
        if not src:
            continue
            
        alt = (img_tag.get("alt") or "").strip().lower()
        title = (img_tag.get("title") or "").strip().lower()
        
        # Ignore site logos and other non-content images
        if "logo" in alt or "logo" in title or "button" in alt or "site-logo" in src:
            continue
            
        # Check text surrounding the image, or in the parent tags
        parent_text = ""
        parent = img_tag.parent
        for _ in range(3):
            if parent:
                parent_text += " " + parent.get_text(" ", strip=True).lower()
                parent = parent.parent
                
        # Also check file name in the URL
        clean_url = clean_fandom_image_url(src)
        filename = clean_url.split("/")[-1].lower()

        if "logo" in filename or "button" in filename:
            continue

        # Find which improvement this image represents
        matched_id = None
        for keyword, imp_id in IMPROVEMENT_ID_MAP.items():
            if (keyword in alt or 
                keyword in title or 
                keyword in filename or 
                f" {keyword} " in parent_text or 
                parent_text.startswith(keyword)):
                matched_id = imp_id
                break
                
        if matched_id and matched_id not in images_downloaded:
            dest_file = out_dir / f"{matched_id}.png"
            print(f"[match] Found potential image for '{matched_id}' -> Alt: '{alt}', Title: '{title}', Filename: '{filename}'")
            ok = download_and_convert_to_png(clean_url, dest_file)
            if ok:
                print(f"[ok] Downloaded and saved: {dest_file.name}")
                images_downloaded[matched_id] = str(dest_file)
            else:
                print(f"[warn] Failed to download image for '{matched_id}'")

    print(f"[done] Downloaded {len(images_downloaded)} improvement images into: {out_dir}")
    print(f"Downloaded improvements: {', '.join(images_downloaded.keys())}")

if __name__ == "__main__":
    main()
