from __future__ import annotations

import json
import re
import time
import unicodedata
from html import unescape
from pathlib import Path
from urllib.parse import urljoin, urlparse, unquote

import requests
from bs4 import BeautifulSoup

BASE = "https://brovkohair.com"
CATALOG = f"{BASE}/shop/"
OUT = Path("catalog.json")
ASSETS = Path("assets/products")
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36",
    "Accept-Language": "uk-UA,uk;q=0.9,en;q=0.8",
}
session = requests.Session()
session.headers.update(HEADERS)

TRANSLIT = str.maketrans({
    "а": "a", "б": "b", "в": "v", "г": "h", "ґ": "g", "д": "d", "е": "e", "є": "ye",
    "ж": "zh", "з": "z", "и": "y", "і": "i", "ї": "yi", "й": "y", "к": "k", "л": "l",
    "м": "m", "н": "n", "о": "o", "п": "p", "р": "r", "с": "s", "т": "t", "у": "u",
    "ф": "f", "х": "kh", "ц": "ts", "ч": "ch", "ш": "sh", "щ": "shch", "ь": "", "ю": "yu",
    "я": "ya", "ы": "y", "э": "e", "ъ": "", "ё": "yo",
})

def get(url: str, binary: bool = False):
    last = None
    for attempt in range(5):
        try:
            response = session.get(url, timeout=45)
            response.raise_for_status()
            body = response.content if binary else response.text
            if not binary and len(body) < 1000 and "Checking your browser" in body:
                raise RuntimeError("anti-bot challenge")
            return response
        except Exception as exc:
            last = exc
            time.sleep(1.5 * (attempt + 1))
    raise RuntimeError(f"GET failed for {url}: {last}")

def clean(value: str) -> str:
    return re.sub(r"\s+", " ", unescape(value or "")).strip()

def script_products() -> list[tuple[str, str]]:
    source = Path("script.js").read_text(encoding="utf-8")
    pairs = re.findall(r"\['((?:\\\\.|[^'])*)','([^']+)'\]", source)
    result = []
    for name, price in pairs:
        name = name.replace("\\\\'", "'")
        if name not in {item[0] for item in result}:
            result.append((name, price))
    return result

def strip_brand(name: str) -> str:
    return re.sub(r"\s+Na\s+(?:Wkir\[y\]|Gólov\[y\])\s*$", "", name, flags=re.I)

def slug_for(name: str) -> str:
    value = strip_brand(name).casefold().translate(TRANSLIT)
    value = unicodedata.normalize("NFKD", value).encode("ascii", "ignore").decode("ascii")
    value = value.replace("&", " i ").replace("’", "'")
    value = re.sub(r"['\"|/]+", " ", value)
    value = re.sub(r"[^a-z0-9]+", "-", value).strip("-")
    return value

def is_product_page(soup: BeautifulSoup) -> bool:
    heading = soup.find("h1")
    if not heading:
        return False
    text = clean(soup.get_text(" ", strip=True))
    return "Артикул" in text and ("Купити" in text or "Купить" in text)

def name_from_page(soup: BeautifulSoup, fallback: str) -> str:
    heading = soup.find("h1")
    return clean(heading.get_text(" ", strip=True)) if heading else fallback

def price_from_page(soup: BeautifulSoup, fallback: str) -> str:
    text = clean(soup.get_text(" ", strip=True))
    match = re.search(r"(\d[\d\s\u00a0]*)\s*₴", text)
    if not match:
        return fallback
    number = re.sub(r"\s+", " ", match.group(1)).strip()
    return f"{number} грн"

def description_from_page(soup: BeautifulSoup, name: str) -> str:
    text = clean(soup.get_text(" ", strip=True))
    marker = text.find(name)
    if marker >= 0:
        text = text[marker + len(name):]
    text = re.sub(r"Артикул.*?В наявності:?\s*\d*", "", text, count=1)
    text = re.sub(r"Купити.*?Додати в обране", "", text, count=1)
    return clean(text)[:900]

def image_urls(soup: BeautifulSoup) -> list[str]:
    found = []
    for image in soup.select("img"):
        values = [image.get("src"), image.get("data-src"), image.get("data-original"), image.get("data-lazy-src")]
        values.extend(part.strip().split(" ", 1)[0] for part in image.get("srcset", "").split(",") if part.strip())
        for value in values:
            if not value:
                continue
            url = urljoin(BASE, value)
            if "/Media/" in url and url not in found:
                found.append(url)
    return found

def category_for(name: str) -> str:
    text = name.casefold()
    if any(word in text for word in ("гребінець", "щітка", "термошапка")):
        return "accessories"
    if "подарунков" in text or "набір" in text:
        return "sets"
    if "шампун" in text or "пілінг" in text:
        return "shampoo"
    if "кондиціонер" in text:
        return "conditioner"
    if "маск" in text:
        return "mask"
    if any(word in text for word in ("спрей", "флюїд", "сироват", "тонік", "крем для волос", "velvet cream", "bb-крем")):
        return "leavein"
    if any(word in text for word in ("душ", "тіл", "рук", "інтим", "скраб", "обгорт")):
        return "body"
    return "hair"

def safe_slug(name: str, index: int) -> str:
    return f"{index:03d}-{slug_for(name)[:70] or 'product'}"

def extension_for(url: str, response: requests.Response) -> str:
    ext = Path(unquote(urlparse(url).path)).suffix.lower()
    if ext in {".jpg", ".jpeg", ".png", ".webp", ".avif"}:
        return ext
    content_type = response.headers.get("content-type", "").lower()
    return {"image/png": ".png", "image/webp": ".webp", "image/avif": ".avif"}.get(content_type, ".jpg")

def candidate_page(name: str) -> tuple[str, BeautifulSoup] | None:
    candidates = [
        f"{BASE}/shop/{slug_for(name)}/",
        f"{BASE}/shop/{slug_for(name)}",
    ]
    for url in candidates:
        try:
            response = get(url)
            soup = BeautifulSoup(response.text, "html.parser")
            if is_product_page(soup):
                return response.url, soup
        except Exception:
            continue
    return None

def main() -> None:
    source_products = script_products()
    if len(source_products) < 100:
        raise RuntimeError(f"Expected at least 100 source products in script.js, found {len(source_products)}")
    ASSETS.mkdir(parents=True, exist_ok=True)
    products = []
    missing = []
    for index, (source_name, source_price) in enumerate(source_products):
        print(f"[{index + 1}/{len(source_products)}] {source_name}", flush=True)
        found = candidate_page(source_name)
        if not found:
            missing.append(source_name)
            continue
        link, soup = found
        name = name_from_page(soup, source_name)
        if "keune" in name.casefold():
            continue
        images = image_urls(soup)
        if not images:
            missing.append(f"{source_name} (no gallery)")
            continue
        product_dir = ASSETS / safe_slug(name, index + 1)
        product_dir.mkdir(parents=True, exist_ok=True)
        local_images = []
        for image_index, image_url in enumerate(images[:12], 1):
            try:
                response = get(image_url, binary=True)
                target = product_dir / f"{image_index}{extension_for(image_url, response)}"
                target.write_bytes(response.content)
                local_images.append(str(target).replace("\\\\", "/"))
            except Exception as exc:
                print(f"  image failed: {image_url}: {exc}", flush=True)
        if not local_images:
            missing.append(f"{source_name} (download failed)")
            continue
        products.append({
            "name": name,
            "price": price_from_page(soup, source_price),
            "category": category_for(name),
            "photo": local_images[0],
            "photos": local_images,
            "description": description_from_page(soup, name),
            "source": link,
        })
    if missing:
        Path("catalog-missing.txt").write_text("\n".join(missing) + "\n", encoding="utf-8")
    if len(products) < len(source_products) - 2:
        raise RuntimeError(f"Only {len(products)} products downloaded; missing {len(missing)}. See catalog-missing.txt")
    OUT.write_text(json.dumps({
        "brand": "SKOK HAIR PRO",
        "source": "catalog import",
        "count": len(products),
        "products": products,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {OUT} with {len(products)} products and {sum(len(p['photos']) for p in products)} images", flush=True)

if __name__ == "__main__":
    main()
