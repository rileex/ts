from __future__ import annotations

import json
import re
import sys
import time
from html import unescape
from pathlib import Path
from urllib.parse import urljoin, urlparse, unquote

import requests
from bs4 import BeautifulSoup

BASE = "https://brovkohair.com"
CATALOG = f"{BASE}/shop/"
OUT = Path("catalog.json")
ASSETS = Path("assets/products")
PAGES = [CATALOG] + [f"{BASE}/shop/page-{n}/" for n in range(2, 9)]
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36",
    "Accept-Language": "uk-UA,uk;q=0.9,en;q=0.8",
}
session = requests.Session()
session.headers.update(HEADERS)


def get(url: str, binary: bool = False):
    last = None
    for attempt in range(4):
        try:
            response = session.get(url, timeout=45)
            response.raise_for_status()
            if not binary and len(response.content) < 1000 and "Checking your browser" in response.text:
                raise RuntimeError("anti-bot challenge")
            return response
        except Exception as exc:
            last = exc
            time.sleep(1.5 * (attempt + 1))
    raise RuntimeError(f"GET failed for {url}: {last}")


def clean(value: str) -> str:
    return re.sub(r"\s+", " ", unescape(value or "")).strip()


def product_links(html: str) -> list[str]:
    soup = BeautifulSoup(html, "html.parser")
    links = []
    for anchor in soup.select("a[href]"):
        href = urljoin(BASE, anchor.get("href", "").strip())
        parsed = urlparse(href)
        path = parsed.path.rstrip("/") + "/"
        if not path.startswith("/shop/") or path in {"/shop/", "/shop/page-2/", "/shop/page-3/", "/shop/page-4/", "/shop/page-5/", "/shop/page-6/", "/shop/page-7/", "/shop/page-8/"}:
            continue
        if "/page-" in path or not anchor.find("img"):
            continue
        if path not in links:
            links.append(href.split("#", 1)[0])
    return links


def image_urls(soup: BeautifulSoup) -> list[str]:
    found = []
    for image in soup.select("img"):
        values = [
            image.get("src"),
            image.get("data-src"),
            image.get("data-original"),
            image.get("data-lazy-src"),
        ]
        srcset = image.get("srcset", "")
        values.extend(part.strip().split(" ", 1)[0] for part in srcset.split(",") if part.strip())
        for value in values:
            if not value:
                continue
            url = urljoin(BASE, value)
            if "/Media/" not in url:
                continue
            if url not in found:
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


def name_from_page(soup: BeautifulSoup) -> str:
    heading = soup.find("h1")
    if heading:
        return clean(heading.get_text(" ", strip=True))
    for selector in ("meta[property='og:title']", "title"):
        node = soup.select_one(selector)
        if node:
            return clean(node.get("content") or node.get_text(" ", strip=True)).split(" — ")[0]
    return "Товар SKOK HAIR PRO"


def price_from_page(soup: BeautifulSoup) -> str:
    text = clean(soup.get_text(" ", strip=True))
    match = re.search(r"(\d[\d\s\u00a0]*)\s*₴", text)
    if not match:
        return "Ціна уточнюється"
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


def safe_slug(name: str, index: int) -> str:
    value = re.sub(r"[^0-9A-Za-zА-Яа-яІіЇїЄєҐґ]+", "-", name).strip("-").lower()
    return f"{index:03d}-{value[:70] or 'product'}"


def extension_for(url: str, response: requests.Response) -> str:
    ext = Path(unquote(urlparse(url).path)).suffix.lower()
    if ext in {".jpg", ".jpeg", ".png", ".webp", ".avif"}:
        return ext
    content_type = response.headers.get("content-type", "").lower()
    return { "image/png": ".png", "image/webp": ".webp", "image/avif": ".avif" }.get(content_type, ".jpg")


def main() -> None:
    all_links = []
    for page in PAGES:
        print(f"catalog {page}")
        html = get(page).text
        for link in product_links(html):
            if link not in all_links:
                all_links.append(link)
    if len(all_links) < 100:
        raise RuntimeError(f"Only {len(all_links)} product links found; refusing to publish partial catalog")
    ASSETS.mkdir(parents=True, exist_ok=True)
    products = []
    for index, link in enumerate(all_links):
        print(f"[{index + 1}/{len(all_links)}] {link}")
        soup = BeautifulSoup(get(link).text, "html.parser")
        name = name_from_page(soup)
        images = image_urls(soup)
        if not images:
            raise RuntimeError(f"No localizable gallery images found for {link}")
        product_dir = ASSETS / safe_slug(name, index + 1)
        product_dir.mkdir(parents=True, exist_ok=True)
        local_images = []
        for image_index, image_url in enumerate(images[:12], 1):
            response = get(image_url, binary=True)
            extension = extension_for(image_url, response)
            filename = f"{image_index}{extension}"
            target = product_dir / filename
            target.write_bytes(response.content)
            local_images.append(str(target).replace("\\", "/"))
        products.append({
            "name": name,
            "price": price_from_page(soup),
            "category": category_for(name),
            "photo": local_images[0],
            "photos": local_images,
            "description": description_from_page(soup, name),
        })
    if len(products) < 100 or sum(bool(item["photos"]) for item in products) != len(products):
        raise RuntimeError("Catalog validation failed: missing products or images")
    OUT.write_text(json.dumps({
        "brand": "SKOK HAIR PRO",
        "source": "catalog import",
        "count": len(products),
        "products": products,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {OUT} with {len(products)} products and {sum(len(p['photos']) for p in products)} images")


if __name__ == "__main__":
    main()
