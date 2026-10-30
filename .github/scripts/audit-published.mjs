import { chromium } from "playwright";
import { createHash } from "node:crypto";
import { mkdir, writeFile } from "node:fs/promises";

const base = "https://rileex.github.io/ts/";
const browser = await chromium.launch({ headless: true });
const results = { base, desktop: {}, mobile: {}, allImages: 0, uniquePrimary: 0, errors: [] };
await mkdir("artifacts", { recursive: true });

async function inspectViewport(viewport, name) {
  const context = await browser.newContext({ viewport, deviceScaleFactor: 1, isMobile: name === "mobile", hasTouch: name === "mobile" });
  const page = await context.newPage();
  await context.addInitScript(() => localStorage.setItem("skokHairProTheme", "dark"));
  const response = await page.goto(base + "?audit=" + name + "-images", { waitUntil: "domcontentloaded", timeout: 90000 });
  if (!response?.ok()) throw new Error(name + " homepage HTTP " + response?.status());
  await page.waitForFunction(() => document.documentElement.dataset.catalogCount === "142", { timeout: 90000 });
  await page.waitForFunction(() => document.querySelectorAll("#products .product").length === 142, { timeout: 30000 });
  const palette = await page.evaluate(() => ({
    darkMode: document.documentElement.classList.contains("dark-mode"),
    body: getComputedStyle(document.body).backgroundColor,
    text: getComputedStyle(document.body).color,
    catalog: getComputedStyle(document.querySelector("#catalog")).backgroundColor,
    firstCard: getComputedStyle(document.querySelector("#products .product")).backgroundColor
  }));
  if (palette.darkMode || palette.body !== "rgb(255, 255, 255)" || palette.text === "rgb(0, 0, 0)") {
    throw new Error(name + " theme is not visible light mode: " + JSON.stringify(palette));
  }
  const cardInfo = await page.locator("#products .product").evaluateAll(cards => cards.map(card => ({
    name: card.querySelector("h3")?.textContent?.trim(),
    src: card.querySelector("img")?.getAttribute("src"),
    index: Number(card.dataset.productIndex)
  })));
  if (cardInfo.some(x => !x.name || !x.src || !Number.isInteger(x.index))) throw new Error(name + " contains a malformed product card");
  const first = await page.locator("#products .product").first();
  await first.locator(".buy").click();
  await page.waitForFunction(() => document.querySelector("#productModal")?.classList.contains("open"), { timeout: 10000 });
  const detail = await page.evaluate(() => ({
    title: document.querySelector("#detailTitle")?.textContent?.trim(),
    src: document.querySelector("#detailImage")?.getAttribute("src"),
    thumbs: [...document.querySelectorAll("#detailThumbs img")].map(x => x.getAttribute("src"))
  }));
  if (!detail.title || !detail.src || detail.thumbs.length !== 12) throw new Error(name + " product detail/gallery failed to open");
  if (!detail.title.includes(cardInfo[0].name.slice(0, 20))) throw new Error(name + " detail title does not match first card");
  await page.screenshot({ path: "artifacts/" + name + "-product-detail.png", fullPage: false });
  await page.locator("#productClose").dispatchEvent("click");
  await page.locator("#catalog").scrollIntoViewIfNeeded();
  await page.screenshot({ path: "artifacts/" + name + "-catalog.png", fullPage: false });
  results[name] = { status: response.status(), palette, cards: cardInfo.length, firstProduct: cardInfo[0].name, detailGalleryImages: detail.thumbs.length };
  await context.close();
}

try {
  await inspectViewport({ width: 1440, height: 1050 }, "desktop");
  await inspectViewport({ width: 390, height: 844 }, "mobile");

  const response = await fetch(base + "catalog.json?audit=" + Date.now(), { cache: "no-store" });
  if (!response.ok) throw new Error("Published catalog HTTP " + response.status());
  const catalog = await response.json();
  if (catalog.count !== 142 || catalog.products.length !== 142) throw new Error("Published catalog count mismatch: " + catalog.products.length);
  const assets = catalog.products.flatMap(product => product.photos || []);
  if (assets.length !== 1704) throw new Error("Expected 1704 gallery files, found " + assets.length);

  const primaryHashes = new Set();
  const queue = [...new Set(assets)];
  let cursor = 0;
  const workers = Array.from({ length: 18 }, async () => {
    while (true) {
      const index = cursor++;
      if (index >= queue.length) return;
      const path = queue[index];
      let imageResponse;
      for (let attempt = 0; attempt < 3; attempt++) {
        try {
          imageResponse = await fetch(new URL(path, base), { cache: "no-store" });
          if (imageResponse.ok) break;
        } catch {}
        await new Promise(resolve => setTimeout(resolve, 350 * (attempt + 1)));
      }
      if (!imageResponse?.ok) {
        results.errors.push(path + " HTTP " + (imageResponse?.status || "network error"));
        continue;
      }
      const type = imageResponse.headers.get("content-type") || "";
      const bytes = Buffer.from(await imageResponse.arrayBuffer());
      if (!type.startsWith("image/") || bytes.length < 1000) {
        results.errors.push(path + " invalid image payload " + type + "/" + bytes.length);
        continue;
      }
      if (/\/1\.(?:png|jpe?g|webp|avif)$/i.test(path)) primaryHashes.add(createHash("sha256").update(bytes).digest("hex"));
    }
  });
  await Promise.all(workers);
  results.allImages = queue.length;
  results.uniquePrimary = primaryHashes.size;
  if (results.errors.length) throw new Error("Image download failures: " + results.errors.slice(0, 12).join("; "));
  if (primaryHashes.size !== 142) throw new Error("Expected 142 unique main product images; got " + primaryHashes.size);

  await writeFile("artifacts/audit.json", JSON.stringify(results, null, 2));
  console.log(JSON.stringify(results, null, 2));
} finally {
  await browser.close();
}
