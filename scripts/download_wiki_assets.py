#!/usr/bin/env python3
import os
import re
import sys
import argparse
import requests
from bs4 import BeautifulSoup
from urllib.parse import urlparse

# Spoof popular browser headers to bypass Fandom Wiki restrictions
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.5",
    "Accept-Encoding": "gzip, deflate, br, zstd",
    "Connection": "keep-alive",
    "Upgrade-Insecure-Requests": "1",
    "Sec-Fetch-Dest": "document",
    "Sec-Fetch-Mode": "navigate",
    "Sec-Fetch-Site": "none",
    "Sec-Fetch-User": "?1",
    "Priority": "u=1"
}

import subprocess

def clean_fandom_image_url(url):
    """
    Cleans up Fandom image URL to get the highest resolution original image.
    E.g. cuts off '/revision/latest/scale-to-width-down/...' parameters.
    """
    if not url:
        return None
    # Remove any query parameters or revision/scale-down modifiers
    # Typically Fandom formats URLs like ".../image.png/revision/latest/scale-to-width-down/30"
    cleaned = re.sub(re.compile(r"\/revision\/latest.*$"), "", url)
    return cleaned

def fetch_url_content(url):
    """
    Fetches URL content using curl as a primary choice to bypass Cloudflare TLS blocking,
    falling back to requests if curl fails.
    """
    # 1. Try with curl
    try:
        result = subprocess.run(
            ["curl", "-s", "-L", "-A", HEADERS["User-Agent"], url],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="ignore",
            timeout=15
        )
        if result.returncode == 0 and result.stdout.strip():
            # Check for cloudflare challenge in response
            if "cf-challenge" not in result.stdout and "attention required!" not in result.stdout.lower():
                return result.stdout
    except Exception as e:
        # Silently fall back to requests
        pass

    # 2. Fall back to requests
    response = requests.get(url, headers=HEADERS, timeout=15)
    if response.status_code == 200:
        return response.text
    else:
        raise Exception(f"HTTP Status {response.status_code}")

def download_image_file(url, file_path):
    """
    Downloads an image file using curl to bypass Cloudflare, falling back to requests.
    """
    # 1. Try with curl
    try:
        result = subprocess.run(
            ["curl", "-s", "-L", "-A", HEADERS["User-Agent"], "-o", file_path, url],
            timeout=15
        )
        if result.returncode == 0 and os.path.exists(file_path) and os.path.getsize(file_path) > 0:
            return True
    except Exception:
        pass

    # 2. Fall back to requests
    response = requests.get(url, headers=HEADERS, timeout=15)
    if response.status_code == 200:
        with open(file_path, "wb") as f:
            f.write(response.content)
        return True
    return False

def get_safe_filename(name):
    """
    Turns an entity name into a clean, lowercased, safe filename.
    """
    if not name:
        return "unnamed"
    # Remove parenthesis/brackets content e.g. "Knight (Civ3)" -> "Knight"
    name = re.sub(r"[\(\[].*?[\)\]]", "", name)
    # Filter non-alphanumeric chars (keep spaces, dashes, underscores)
    safe = "".join(c for c in name if c.isalnum() or c in (" ", "_", "-")).strip()
    safe = safe.replace(" ", "_").lower()
    return safe

def find_entity_name_in_row(row, cells, asset_type):
    """
    Attempts to extract a high-quality name from a table row.
    """
    if not cells:
        return None

    # Primary Strategy (Perfect for Units, Buildings, Wonders):
    # The first cell almost always contains the entity's image AND its name link.
    first_cell = cells[0]
    for link in first_cell.find_all("a"):
        name = link.text.strip()
        if name and not any(bad in name.lower() for bad in ["file:", "category:", "template:", "portal:"]):
            return name

    # Fallback 1: Bold text in first cell
    bold = first_cell.find(["b", "strong"])
    if bold and bold.text.strip():
        return bold.text.strip()

    # Fallback 2: Any text in first cell if it doesn't contain just whitespace
    first_cell_text = first_cell.text.strip()
    if first_cell_text and len(first_cell_text) < 40 and not any(bad in first_cell_text.lower() for bad in ["file:", "category:"]):
        return first_cell_text

    # Fallback 3: Look for first link in subsequent cells that doesn't contain an image
    for cell in cells[1:]:
        link = cell.find("a")
        if link and link.text.strip() and not cell.find("img"):
            name = link.text.strip()
            if name and not any(bad in name.lower() for bad in ["file:", "category:", "template:", "portal:"]):
                return name

    return None

def download_assets(url, asset_type, output_dir):
    """
    Downloads assets of a specified type from a given Wiki URL.
    """
    print(f"\n==================================================")
    print(f"📡 Requesting: {url}")
    print(f"📦 Target Type: {asset_type.upper()}")
    print(f"📂 Output Dir : {output_dir}")
    print(f"==================================================")

    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        print(f"📁 Created folder: {output_dir}")

    try:
        html_content = fetch_url_content(url)
    except Exception as e:
        print(f"❌ Connection error or load failed: {e}")
        return

    soup = BeautifulSoup(html_content, "html.parser")
    tables = soup.find_all("table", class_="wikitable")

    if not tables:
        # Fallback to general page images if no tables with 'wikitable' exist
        print("⚠️ No standard 'wikitable' elements found on this page.")
        print("🔍 Searching for general content tables or images instead...")
        tables = soup.find_all("table")

    print(f"💡 Found {len(tables)} tables to analyze...")

    downloaded_count = 0
    skipped_count = 0

    # Avoid processing duplicate items on the same page
    processed_urls = set()

    for t_idx, table in enumerate(tables, 1):
        rows = table.find_all("tr")
        print(f"📊 Table {t_idx}/{len(tables)}: Analyzing {len(rows)} rows...")

        for row in rows:
            cells = row.find_all(["td", "th"])
            if not cells:
                continue

            # Look for img tag
            img_tag = row.find("img")
            if not img_tag:
                continue

            # Get image URL (data-src for lazy loading, src as fallback)
            img_url = img_tag.get("data-src") or img_tag.get("src") or img_tag.get("data-image-key")
            if not img_url:
                continue

            # Skip tracking files or empty shells
            if "sprite" in img_url.lower() or "placeholder" in img_url.lower():
                continue

            cleaned_url = clean_fandom_image_url(img_url)
            if not cleaned_url or cleaned_url in processed_urls:
                continue

            # Determine name
            raw_name = find_entity_name_in_row(row, cells, asset_type)
            if not raw_name:
                continue

            # Skip some common header or noisy text
            if len(raw_name) > 40 or any(noise in raw_name.lower() for noise in ["modifier", "attack", "defense", "shield", "cost", "pre-requisite", "requirements"]):
                continue

            safe_name = get_safe_filename(raw_name)
            if not safe_name or len(safe_name) < 2:
                continue

            # Determine extension
            ext = ".png"
            if ".jpg" in cleaned_url.lower() or ".jpeg" in cleaned_url.lower():
                ext = ".jpg"
            elif ".gif" in cleaned_url.lower():
                ext = ".gif"

            file_path = os.path.join(output_dir, f"{safe_name}{ext}")

            # Check if file already exists
            if os.path.exists(file_path):
                skipped_count += 1
                processed_urls.add(cleaned_url)
                continue

            try:
                # Download image
                if download_image_file(cleaned_url, file_path):
                    print(f"✅ Downloaded: {raw_name} -> {file_path}")
                    downloaded_count += 1
                    processed_urls.add(cleaned_url)
                else:
                    print(f"❌ Failed to download {raw_name} from {cleaned_url}")
            except Exception as e:
                print(f"❌ Error downloading {raw_name} from {cleaned_url}: {e}")

    print(f"\n✨ COMPLETED!")
    print(f"📥 Successfully Downloaded: {downloaded_count}")
    print(f"⏭️ Skipped (already exists): {skipped_count}")
    print(f"📂 Destination: {os.path.abspath(output_dir)}")


def get_interactive_inputs():
    """
    Prompts the user interactively if no command-line arguments are provided.
    """
    print("==================================================")
    print("🏛️  CIVILIZATION III ASSET DOWNLOADER  🏛️")
    print("==================================================")
    print("Which Wiki page would you like to scrape?")
    print("1. Units (List of units in Civ3)")
    print("2. Buildings & Wonders (List of buildings in Civ3 / Wonders)")
    print("3. Technologies (Technology tree / Techs in Civ3)")
    print("4. Custom URL")
    
    choice = input("Select option (1-4): ").strip()
    
    url = ""
    asset_type = "generic"
    
    if choice == "1":
        url = "https://civilization.fandom.com/wiki/List_of_units_in_Civ3"
        asset_type = "units"
    elif choice == "2":
        url = "https://civilization.fandom.com/wiki/List_of_buildings_in_Civ3"
        asset_type = "buildings"
    elif choice == "3":
        url = "https://civilization.fandom.com/wiki/List_of_advances_in_Civ3"
        asset_type = "technologies"
    else:
        url = input("Enter custom Civilization Fandom Wiki URL: ").strip()
        asset_type = input("Enter asset type (e.g. units, buildings, tech, terrain, generic): ").strip().lower()

    output_dir = input(f"Enter output folder name [default: civ3_{asset_type}]: ").strip()
    if not output_dir:
        output_dir = f"civ3_{asset_type}"

    return url, asset_type, output_dir


def main():
    parser = argparse.ArgumentParser(description="Universal Civ3 asset downloader from Fandom Wiki.")
    parser.add_argument("-u", "--url", help="Target Civilization Fandom Wiki URL")
    parser.add_argument("-t", "--type", help="Asset type (e.g. units, buildings, wonders, tech, terrain, generic)")
    parser.add_argument("-o", "--output", help="Custom output directory")

    args = parser.parse_args()

    # If no arguments are provided, switch to interactive mode
    if not args.url:
        url, asset_type, output_dir = get_interactive_inputs()
    else:
        url = args.url
        asset_type = args.type or "generic"
        output_dir = args.output or f"civ3_{asset_type}"

    if not url:
        print("❌ Error: A valid target URL is required.")
        sys.exit(1)

    download_assets(url, asset_type, output_dir)


if __name__ == "__main__":
    main()
