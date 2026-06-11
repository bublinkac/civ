# CivGame - Development Asset Downloader Scripts

This directory contains useful Python scripts to automate downloading, processing, and caching of original Civilization III image assets from the **Civilization Fandom Wiki** for development, placeholder, and testing purposes.

---

## 📦 Requirements

To run these scripts, you need Python 3 installed. You can install the required packages with:

```bash
pip install -r requirements.txt
```

---

## 🛠️ Universal Asset Downloader (`download_wiki_assets.py`)

`download_wiki_assets.py` is a highly robust, customizable, and universal script designed to extract high-quality, uncompressed original images directly from the tables of any Civilization Fandom Wiki page. 

It handles:
- **User-Agent spoofing** to bypass fandom image scraping rate-limits.
- **Smart name-matching** based on row analysis (bold titles, non-image text links, table headers fallback).
- **Url cleanup** to remove Fandom resizing/transform modifiers (`/revision/latest/...`), fetching the highest-resolution original PNG/JPG files.
- **Automatic name cleaning** to convert labels into safe, lowercased, snake_case filenames.
- **Duplicate filtering** to avoid redundant downloads on the same page.

### 🎮 Interactive Mode (easiest)

Simply run the script with no arguments. It will present you with an interactive prompt to choose your target assets:

```bash
python download_wiki_assets.py
```

### 💻 CLI Arguments Mode

You can also specify the parameters directly via command-line flags.

```bash
python download_wiki_assets.py -u <URL> -t <TYPE> -o <OUTPUT_DIR>
```

#### CLI Parameters:
- `-u`, `--url`: The target Fandom Wiki URL. *(Required)*
- `-t`, `--type`: Asset category (e.g. `units`, `buildings`, `wonders`, `tech`, `terrain`, `generic`). *(Default: `generic`)*
- `-o`, `--output`: Custom destination folder. *(Default: `civ3_<type>`)*

---

## 📖 Useful Target URLs (Civilization III)

Here are the most popular Fandom Wiki lists with original Civ3 graphics that you can feed into the script:

### 1. Units
- **URL**: `https://civilization.fandom.com/wiki/List_of_units_in_Civ3`
- **Asset Type**: `units`
- **Usage**:
  ```bash
  python download_wiki_assets.py -u "https://civilization.fandom.com/wiki/List_of_units_in_Civ3" -t units -o "civ3_units"
  ```

### 2. City Improvements (Buildings)
- **URL**: `https://civilization.fandom.com/wiki/List_of_buildings_in_Civ3`
- **Asset Type**: `buildings`
- **Usage**:
  ```bash
  python download_wiki_assets.py -u "https://civilization.fandom.com/wiki/List_of_buildings_in_Civ3" -t buildings -o "civ3_buildings"
  ```

### 3. Wonders & Great Wonders
- **URL**: `https://civilization.fandom.com/wiki/List_of_wonders_in_Civ3`
- **Asset Type**: `wonders`
- **Usage**:
  ```bash
  python download_wiki_assets.py -u "https://civilization.fandom.com/wiki/List_of_wonders_in_Civ3" -t wonders -o "civ3_wonders"
  ```

### 4. Technologies (Research Tree)
- **URL**: `https://civilization.fandom.com/wiki/List_of_advances_in_Civ3`
- **Asset Type**: `technologies`
- **Usage**:
  ```bash
  python download_wiki_assets.py -u "https://civilization.fandom.com/wiki/List_of_advances_in_Civ3" -t technologies -o "civ3_techs"
  ```

---

## 🛡️ Best Practices & Terms of Use
These assets are fetched from a public wiki page and are strictly intended for **personal evaluation, testing, and development placeholder purposes**. Ensure you respect the source terms of service and replace copyrighted imagery with your own custom creations before releasing your game!
