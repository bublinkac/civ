#!/usr/bin/env python3
import argparse
import os
import re
import subprocess
from pathlib import Path
import requests
from bs4 import BeautifulSoup
from PIL import Image

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.5",
}

DEFAULT_URL = "https://civilization.fandom.com/wiki/Civilizations_(Civ3)"

# Map exact alt text of the 64x64 civilization icons to our civilization IDs
CIV_ICON_MAP = {
    "american": "america",
    "aztec": "aztec",
    "babylonian": "babylon",
    "byzantine": "byzantines",
    "carthaginian": "carthage",
    "celtic": "celts",
    "chinese": "china",
    "dutch": "dutch",
    "egyptian": "egypt",
    "english": "england",
    "french": "france",
    "german": "germany",
    "greek": "greece",
    "hittite": "hittites",
    "inca": "inca",
    "incan": "inca",
    "incas": "inca",
    "indian": "india",
    "iroquois": "iroquois",
    "japanese": "japan",
    "korean": "korea",
    "mayan": "maya",
    "mongol": "mongols",
    "ottoman": "ottomans",
    "persian": "persia",
    "portuguese": "portugal",
    "roman": "rome",
    "russian": "russia",
    "spanish": "spain",
    "sumerian": "sumeria",
    "viking": "vikings",
    "zulu": "zulu",
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
            print(f"[error] PIL conversion failed for {url}: {e}")
            if temp_path.exists():
                temp_path.unlink()
            
    return False

def main():
    parser = argparse.ArgumentParser(description="Download Civ3 civilization icons from Fandom")
    parser.add_argument("--url", default=DEFAULT_URL, help="Source page URL")
    parser.add_argument("--out-dir", default="assets/leaders", help="Output dir for images")
    args = parser.parse_args()

    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    # Let's delete any old america.png to prevent logo collision
    logo_file = out_dir / "america.png"
    if logo_file.exists():
        try:
            logo_file.unlink()
        except Exception:
            pass

    print(f"[info] Fetching: {args.url}")
    html = fetch_html(args.url)
    soup = BeautifulSoup(html, "html.parser")

    downloaded = {}
    
    for img_tag in soup.find_all("img"):
        src = img_tag.get("data-src") or img_tag.get("src")
        if not src:
            continue
            
        alt = (img_tag.get("alt") or "").strip().lower()
        title = (img_tag.get("title") or "").strip().lower()
        
        # Match exact Alt texts of civilization icons
        matched_civ = CIV_ICON_MAP.get(alt)
        if not matched_civ:
            continue

        clean_url = clean_fandom_image_url(src)
        filename = clean_url.split("/")[-1].lower()

        # Skip site logo or formatting junk
        if "logo" in filename or "site" in filename:
            continue

        if matched_civ not in downloaded:
            dest_file = out_dir / f"{matched_civ}.png"
            print(f"[match] Found civilization icon for '{matched_civ}' (Alt: '{alt}') -> URL: {clean_url[:80]}...")
            ok = download_and_convert_to_png(clean_url, dest_file)
            if ok:
                print(f"[ok] Downloaded and saved: {dest_file.name}")
                downloaded[matched_civ] = str(dest_file)
            else:
                print(f"[warn] Failed to download image for '{matched_civ}'")

    print(f"\n[done] Downloaded {len(downloaded)} civilization icons into: {out_dir}")
    print(f"Downloaded: {', '.join(downloaded.keys())}")

if __name__ == "__main__":
    main()
