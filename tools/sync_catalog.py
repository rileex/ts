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

def get(url: str, binary: bool = False, timeout: int = 45, retries: int = 5):
    last = None
    for attempt in range(retries):
        try:
            response = session.get(url, timeout=timeout)
            response.raise_for_status()
            body = response.content if binary else response.text
            if not binary and len(body) < 1000 and "Checking your browser" in body:
                raise RuntimeError("anti-bot challenge")
            return response
        except Exception as exc:
            last = exc
            if attempt + 1 < retries:
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

def image_urls(soup: BeautifulSoup, product_name: str) -> list[str]:
    # Read only the product gallery. The old site-wide img scan picked up
    # header/branding imagery and copied it into every product directory.
    selectors = (
        ".woocommerce-product-gallery__wrapper",
        ".woocommerce-product-gallery",
        "[data-product-gallery]",
        ".product-gallery",
        ".product__gallery",
        ".product-images",
        ".product-image",
        ".product-thumbnails",
    )
    containers = []
    for selector in selectors:
        containers.extend(soup.select(selector))
    found = []

    def add(value: str | None) -> None:
        if not value:
            return
        url = urljoin(BASE, value.strip())
        if "/Media/" not in url:
            return
        filename = Path(unquote(urlparse(url).path)).name.casefold()
        if any(token in filename for token in ("logo", "sprite", "favicon", "icon")):
            return
        if url not in found:
            found.append(url)

    for container in containers:
        for image in container.select("img"):
            for attr in ("data-large_image", "data-src", "data-original", "data-lazy-src", "src"):
                add(image.get(attr))
            for part in image.get("srcset", "").split(","):
                if part.strip():
                    add(part.strip().split()[0])
        for anchor in container.select("a[href]"):
            add(anchor.get("href"))

    # Product themes sometimes omit a gallery wrapper. In that case only
    # accept images explicitly labelled with the product heading.
    if not found:
        words = {word.casefold() for word in re.findall(r"[\wА-Яа-яІіЇїЄєҐґ]{4,}", product_name)}
        for image in soup.select("img[alt], img[title]"):
            labels = " ".join((image.get("alt", ""), image.get("title", ""))).casefold()
            if words and sum(word in labels for word in words) >= min(2, len(words)):
                for attr in ("data-large_image", "data-src", "data-original", "data-lazy-src", "src"):
                    add(image.get(attr))
                for part in image.get("srcset", "").split(","):
                    if part.strip():
                        add(part.strip().split()[0])
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
            response = get(url, timeout=12, retries=1)
            soup = BeautifulSoup(response.text, "html.parser")
            if is_product_page(soup):
                return response.url, soup
        except Exception:
            continue
    return None

def product_links(html: str) -> list[str]:
    excluded = {
        "shampuni", "konditsioneri", "maski-dlya-volossya", "nezmivniy-doglyad",
        "podarunkovi-nabori", "dopovnennya-doglyad", "na-wkiry", "lineyka-emotsii",
    }
    soup = BeautifulSoup(html, "html.parser")
    links = []
    for anchor in soup.select("a[href]"):
        href = urljoin(BASE, anchor.get("href", "").strip()).split("#", 1)[0]
        path = urlparse(href).path.rstrip("/")
        parts = path.split("/")
        if len(parts) != 3 or parts[1] != "shop":
            continue
        slug = parts[2]
        if slug in {"koshyk", "cart", "basket", "checkout"}:
            continue
        if slug in excluded or slug.startswith("page-"):
            continue
        if href not in links:
            links.append(href + "/")
    return links

CATEGORY_PAGES = [
    f"{BASE}/shop/",
    f"{BASE}/shop/shampuni/",
    f"{BASE}/shop/konditsioneri/",
    f"{BASE}/shop/maski-dlya-volossya/",
    f"{BASE}/shop/nezmivniy-doglyad/",
    f"{BASE}/shop/podarunkovi-nabori/",
    f"{BASE}/shop/dopovnennya-doglyad/",
    f"{BASE}/shop/na-wkiry/",
]

def collect_links_with_browser() -> list[str]:
    from playwright.sync_api import sync_playwright
    links = []
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        page = browser.new_page(
            user_agent=HEADERS["User-Agent"],
            locale="uk-UA",
            viewport={"width": 1440, "height": 1000},
        )
        for base in CATEGORY_PAGES:
            for page_number in range(1, 7):
                url = base if page_number == 1 else base.rstrip("/") + f"/page-{page_number}/"
                print(f"browser catalog {url}", flush=True)
                try:
                    page.goto(url, wait_until="domcontentloaded", timeout=60000)
                    page.wait_for_timeout(2500)
                    page.wait_for_selector("a[href*='/shop/']", timeout=15000)
                    found = product_links(page.content())
                except Exception as exc:
                    print(f"  page skipped: {exc}", flush=True)
                    found = []
                before = len(links)
                for link in found:
                    if link not in links:
                        links.append(link)
                if page_number > 1 and len(links) == before:
                    break
        browser.close()
    return links

def main() -> None:
    all_links = collect_links_with_browser()
    if len(all_links) < 100:
        raise RuntimeError(f"Only {len(all_links)} product links found; refusing to publish partial catalog")
    ASSETS.mkdir(parents=True, exist_ok=True)
    products = []
    primary_hashes: dict[str, list[str]] = {}
    from hashlib import sha256
    from playwright.sync_api import sync_playwright

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        context = browser.new_context(
            user_agent=HEADERS["User-Agent"],
            locale="uk-UA",
            viewport={"width": 1440, "height": 1000},
        )
        page = context.new_page()
        for index, link in enumerate(all_links):
            print(f"[{index + 1}/{len(all_links)}] browser product {link}", flush=True)
            response = page.goto(link, wait_until="domcontentloaded", timeout=60000)
            page.wait_for_timeout(1200)
            if response is None or response.status >= 400:
                raise RuntimeError(f"Product page failed ({getattr(response, 'status', 'no response')}): {link}")
            soup = BeautifulSoup(page.content(), "html.parser")
            if not is_product_page(soup):
                raise RuntimeError(f"Not a product page in browser session: {link}")
            name = name_from_page(soup, link.rstrip("/").rsplit("/", 1)[-1].replace("-", " "))
            if name.casefold() == "кошик" or "keune" in name.casefold():
                continue
            images = image_urls(soup, name)
            if not images:
                raise RuntimeError(f"No product-gallery images found in rendered browser page: {link}")

            product_dir = ASSETS / safe_slug(name, index + 1)
            product_dir.mkdir(parents=True, exist_ok=True)
            local_images = []
            for image_index, image_url in enumerate(images[:12], 1):
                image_response = page.request.get(image_url, timeout=45000)
                if not image_response.ok:
                    raise RuntimeError(f"Image download failed ({image_response.status}): {image_url}")
                content_type = image_response.headers.get("content-type", "").lower()
                payload = image_response.body()
                if not content_type.startswith("image/") or len(payload) < 1000:
                    raise RuntimeError(f"Not a real image ({content_type}, {len(payload)} bytes): {image_url}")
                target = product_dir / f"{image_index}{extension_for(image_url, requests.Response())}"
                # Preserve the server's actual image format if the URL is extensionless.
                if Path(target).suffix == ".jpg" and "png" in content_type:
                    target = target.with_suffix(".png")
                elif Path(target).suffix == ".jpg" and "webp" in content_type:
                    target = target.with_suffix(".webp")
                target.write_bytes(payload)
                local_images.append(str(target).replace("\\", "/"))
            if not local_images:
                raise RuntimeError(f"Product has no downloaded images: {link}")
            digest = sha256((product_dir / Path(local_images[0]).name).read_bytes()).hexdigest()
            primary_hashes.setdefault(digest, []).append(name)
            products.append({
                "name": name,
                "price": price_from_page(soup, "Ціна уточнюється"),
                "category": category_for(name),
                "photo": local_images[0],
                "photos": local_images,
                "description": description_from_page(soup, name),
            })
            time.sleep(0.4)
        browser.close()

    if len(products) < 100 or sum(bool(item["photos"]) for item in products) != len(products):
        raise RuntimeError("Catalog validation failed: missing products or images")
    repeated = [(digest, names) for digest, names in primary_hashes.items() if len(names) > 3]
    if len(primary_hashes) < max(50, int(len(products) * 0.65)) or repeated:
        detail = "; ".join(f"{len(names)} products share one image: {names[:4]}" for _, names in repeated[:5])
        raise RuntimeError(
            f"Image identity validation failed: only {len(primary_hashes)} unique primary images "
            f"for {len(products)} products. {detail}"
        )
    OUT.write_text(json.dumps({
        "brand": "SKOK HAIR PRO",
        "source": "catalog import",
        "count": len(products),
        "products": products,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        f"Wrote {OUT} with {len(products)} products, "
        f"{sum(len(p['photos']) for p in products)} images and {len(primary_hashes)} unique primary photos",
        flush=True,
    )

if __name__ == "__main__":
    main()
