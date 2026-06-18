#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
from pathlib import Path
from typing import Optional

import requests
from bs4 import BeautifulSoup

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.5",
}

DEFAULT_URL = "https://civilization.fandom.com/wiki/List_of_terrains_in_Civ3"

TERRAIN_ID_MAP = {
    "coast": "coast",
    "desert": "desert",
    "flood plain": "floodplains",
    "flood plains": "floodplains",
    "forest": "forest",
    "grassland": "grassland",
    "hills": "hills",
    "jungle": "jungle",
    "marsh": "marsh",
    "mountain": "mountain",
    "mountains": "mountain",
    "ocean": "ocean",
    "plains": "plains",
    "sea": "sea",
    "tundra": "tundra",
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


from PIL import Image

def download_file(url: str, path: Path) -> bool:
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


def parse_int(text: str, default: int = 0) -> int:
    m = re.search(r"-?\d+", text)
    return int(m.group(0)) if m else default


def normalize_terrain_id(name: str) -> Optional[str]:
    key = re.sub(r"\s+", " ", name.strip().lower())
    return TERRAIN_ID_MAP.get(key)


def extract_stats_table(soup: BeautifulSoup):
    for table in soup.find_all("table", class_="wikitable"):
        headers = [th.get_text(" ", strip=True).lower() for th in table.find_all("th")]
        joined = " | ".join(headers)
        if all(k in joined for k in ["name", "movement", "defense", "food", "shield", "commerce"]):
            return table
    return None


def parse_terrain_rows(table, images_dir: Path):
    items = []

    for row in table.find_all("tr"):
        cells = row.find_all("td")
        if len(cells) < 6:
            continue

        raw_name = cells[0].get_text(" ", strip=True)
        terrain_id = normalize_terrain_id(raw_name)
        if not terrain_id:
            continue

        movement_cost = parse_int(cells[1].get_text(" ", strip=True), 1)
        defense_bonus_percent = parse_int(cells[2].get_text(" ", strip=True), 10)
        food = parse_int(cells[3].get_text(" ", strip=True), 0)
        shields = parse_int(cells[4].get_text(" ", strip=True), 0)
        commerce = parse_int(cells[5].get_text(" ", strip=True), 0)

        image_path = None
        img = cells[0].find("img")
        if img is not None:
            img_url = img.get("data-src") or img.get("src")
            if img_url:
                clean_url = clean_fandom_image_url(img_url)
                ext = ".png"
                lowered = clean_url.lower()
                if ".jpg" in lowered or ".jpeg" in lowered:
                    ext = ".jpg"
                elif ".gif" in lowered:
                    ext = ".gif"

                local_file = images_dir / f"{terrain_id}{ext}"
                if not local_file.exists():
                    ok = download_file(clean_url, local_file)
                    if ok:
                        print(f"[ok] downloaded image: {terrain_id} -> {local_file.name}")
                    else:
                        print(f"[warn] failed image for {terrain_id}: {clean_url}")
                image_path = str(local_file).replace("\\", "/")

        items.append(
            {
                "id": terrain_id,
                "name": raw_name,
                "movement_cost": movement_cost,
                "defense_bonus_percent": defense_bonus_percent,
                "food": food,
                "shields": shields,
                "commerce": commerce,
                "image_path": image_path,
            }
        )

    unique = {}
    for item in items:
        unique[item["id"]] = item
    return list(unique.values())


def main():
    parser = argparse.ArgumentParser(description="Download Civ3 terrain images and stats from Fandom")
    parser.add_argument("--url", default=DEFAULT_URL, help="Source page URL")
    parser.add_argument("--images-dir", default="assets/terrains", help="Output dir for terrain images")
    parser.add_argument("--stats-out", default="assets/terrains/terrain_stats.json", help="Output JSON path")
    args = parser.parse_args()

    print(f"[info] fetching: {args.url}")
    html = fetch_html(args.url)
    soup = BeautifulSoup(html, "html.parser")

    table = extract_stats_table(soup)
    if table is None:
        raise RuntimeError("Could not find terrain stats table on page")

    images_dir = Path(args.images_dir)
    stats_out = Path(args.stats_out)

    terrain_items = parse_terrain_rows(table, images_dir)
    terrain_items.sort(key=lambda x: x["id"])

    stats_out.parent.mkdir(parents=True, exist_ok=True)
    stats_out.write_text(json.dumps(terrain_items, indent=2), encoding="utf-8")

    print(f"[done] terrains parsed: {len(terrain_items)}")
    print(f"[done] stats json: {stats_out}")
    print(f"[done] images dir: {images_dir}")


if __name__ == "__main__":
    main()
